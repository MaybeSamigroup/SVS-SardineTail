using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using System.Collections.Generic;
using Character;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;
using CoastalSmell;
using BepInEx;
using Dependency = System.Tuple<System.Collections.Generic.HashSet<string>, Character.ListInfoBase>;
using Dependencies = System.Tuple<System.Collections.Generic.HashSet<string>, System.Collections.Generic.IEnumerable<Character.ListInfoBase>>;

namespace SardineTail
{
    internal static partial class CategoryExtensions
    {
        internal static void Initialize() =>
            Util<Manager.Game>.Hook(() => Plugin.HardmodConversion.Value.Maybe(() => Convert(new(), new())), () => { });
        internal static readonly JsonSerializerOptions JsonOption = new JsonSerializerOptions()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
        static void Convert(HashSet<string> originalAB, List<Dependency> dependencies)
        {
            All.ForEach(category =>
            {
                foreach (var (id, info) in Human.lstCtrl._table[category.Index])
                {
                    if (0 <= id && id < 100)
                    {
                        category.Entries.Where(entry => entry.Value is Vtype.Store)
                            .Select(entry => entry.Index).Select(info.GetString)
                                .Where(path => !"0".Equals(path)).ForEach(path => originalAB.Add(path));
                    }
                    else if (ModInfo.Translate[category.Index].FromId(id) == null)
                    {
                        dependencies.Add(new(category.Entries
                            .Where(entry => entry.Value is Vtype.Store)
                            .Select(entry => entry.Index).Select(info.GetString)
                            .Where(path => path != null && File.Exists(Path.Combine(Paths.GameRootPath, "abdata", path)))
                            .ToHashSet(), info));
                    }
                }
            });
            dependencies.Select(tuple => new Dependency(tuple.Item1.Except(originalAB).ToHashSet(), tuple.Item2))
                .Aggregate<Dependency, IEnumerable<Dependencies>>([], Accumulate)
                .GroupBy(GeneratePackageName, (name, deps) => name.Merge(deps))
                .ForEach(tuple => ConvertPackage(tuple.Item1, tuple.Item2));
        }
        static Tuple<string, Dependencies> Merge(this string name, IEnumerable<Dependencies> deps) =>
            new(name, deps.Aggregate((fst, snd) => new(fst.Item1.Union(snd.Item1).ToHashSet(), fst.Item2.Concat(snd.Item2))));
        static IEnumerable<Dependencies> Merge(IEnumerable<Dependencies> deps, Dependency dep) =>
            [deps.Aggregate(new Dependencies(dep.Item1, [dep.Item2]), (fst, snd) => new(fst.Item1.Union(snd.Item1).ToHashSet(), fst.Item2.Concat(snd.Item2)))];
        static IEnumerable<Dependencies> Accumulate(IEnumerable<Dependencies> deps, Dependency dep) =>
            dep.Item1.Count() == 0
                ? Merge(deps.Where(item => item.Item1.Count() == 0), dep)
                    .Concat(deps.Where(item => item.Item1.Count() != 0))
                : Merge(deps.Where(item => item.Item1.Any(dep.Item1.Contains)), dep)
                    .Concat(deps.Where(item => !item.Item1.Any(dep.Item1.Contains)));
        static string GeneratePackageName(Dependencies deps) =>
            deps.Item1.Count() == 0 ? "listonly" : deps.Item1
                .Select(Path.GetFileNameWithoutExtension).Where(name => !name.Contains("thumb")).MinBy(name => name.Length);
        static void ConvertPackage(string name, Dependencies deps) =>
            Directory.CreateDirectory(Path.Combine(Plugin.ConversionsPath, $"{name}-0.0.0"))
                .With(() => Plugin.Instance.Log.LogMessage($"Conversion start for package: {name}"))
                .With(deps.Item1.CopyAssetBundles)
                .With(Plugin.StructureConversion.Value ? deps.Item2.ConvertToStructure : deps.Item2.ConvertToCsv);
        static void CopyAssetBundles(this IEnumerable<string> paths, DirectoryInfo dest) =>
            paths.ToDictionary(path => Path.Combine(Paths.GameRootPath, "abdata", path), path => Path.Combine(dest.FullName, path))
                .Where(entry => File.Exists(entry.Key))
                .ForEach(entry => Directory.CreateDirectory(Path.GetDirectoryName(entry.Value)).With(() => File.Copy(entry.Key, entry.Value)));
        static void GenerateHardMigration(this IEnumerable<ListInfoBase> infos, DirectoryInfo path,
            Func<Category, IEnumerable<ListInfoBase>, DirectoryInfo, Dictionary<int, HardMigrationInfo>> process) =>
            infos.GroupBy(info => (CatNo)info.Category, (category, subinfos) => new Tuple<CatNo, Dictionary<int, HardMigrationInfo>>(
                    category, process(All.Where(item => item.Index == category).First(), subinfos, path)))
                .Where(item => item.Item2.Count() > 0).ToDictionary(item => item.Item1, item => item.Item2)
                .With(hardmig => File.WriteAllText(Path.Combine(path.FullName, "hardmig.json"),
                    JsonSerializer.Serialize(hardmig, JsonOption)));
        static void ConvertToCsv(this IEnumerable<ListInfoBase> infos, DirectoryInfo path) =>
            infos.GenerateHardMigration(path, ConvertToCsv);
        static Tuple<int, HardMigrationInfo> NotifyIdConflict(int id, IEnumerable<string> mods) =>
            new(id, new HardMigrationInfo()
            {
                ModId = mods.Count() > 1 ? mods
                .With(() => Plugin.Instance.Log.LogMessage($"Id:{id} conflict between:"))
                .With(() => mods.ForEach(mod => Plugin.Instance.Log.LogMessage(mod))).First() : mods.First(),
                Version = new Version(0, 0, 0)
            });
        static Dictionary<int, HardMigrationInfo> ConvertToCsv(Category category, IEnumerable<ListInfoBase> infos, DirectoryInfo path) =>
            infos.With(category.Entries.Select(entry => entry.Index).ConvertToCsv(Path.Combine(path.FullName, $"{category.Index}.csv")))
                .GroupBy(info => info.Id, info => $"{category.Index}.csv/{info.Name}", NotifyIdConflict)
                .ToDictionary(item => item.Item1, item => item.Item2);
        static Action<IEnumerable<ListInfoBase>> ConvertToCsv(this IEnumerable<Ktype> indices, string path) =>
            infos => File.WriteAllLines(path, [string.Join(',', indices),
                .. infos.Select(info => string.Join(',', indices.Select(info.GetString).Select(Normalize)))]);
        static void ConvertToStructure(this IEnumerable<ListInfoBase> infos, DirectoryInfo path) =>
            infos.GenerateHardMigration(path, ConvertToStructure);
        static Dictionary<int, HardMigrationInfo> ConvertToStructure(Category category, IEnumerable<ListInfoBase> infos, DirectoryInfo path) =>
            ConvertToStructure(category, infos, path, 0);
        static string ConvertToGroupIdentity(Category category, ListInfoBase info) =>
            JsonSerializer.Serialize(category.Entries
                .Where(entry => entry.Value is Vtype.Text && entry.Default.Length != 0)
                .Select(entry => new Tuple<Entry, string>(entry, Normalize(info.GetString(entry.Index))))
                .Where(tuple => !tuple.Item1.Default[0].Equals(tuple.Item2))
                .ToDictionary(tuple => tuple.Item1.Index, tuple => tuple.Item2), JsonOption);
        static Dictionary<int, HardMigrationInfo> ConvertToStructure(Category category, IEnumerable<ListInfoBase> infos, DirectoryInfo path, int index) =>
            infos.With(() => Directory.CreateDirectory(Path.Combine(path.FullName, category.Index.ToString())))
                .GroupBy(info => ConvertToGroupIdentity(category, info), (group, infos) => ConvertToStructure(category, index++, group, infos, path))
                .Aggregate((fst, snd) => fst.Concat(snd))
                .GroupBy(tuple => tuple.Item1, tuple => tuple.Item2, NotifyIdConflict)
                .ToDictionary(item => item.Item1, item => item.Item2);
        static IEnumerable<Tuple<int, string>> ConvertToStructure(Category category, int index, string group, IEnumerable<ListInfoBase> infos, DirectoryInfo path) =>
            Directory.CreateDirectory(Path.Combine(path.FullName, category.Index.ToString(), $"group{index}"))
                .With(subpath => File.WriteAllText(Path.Combine(subpath.FullName, "values.json"), group))
                .ConvertToStructure(category, $"{category.Index}/group{index}", infos);
        static IEnumerable<Tuple<int, string>> ConvertToStructure(this DirectoryInfo path, Category category, string prefix, IEnumerable<ListInfoBase> infos) =>
            infos.Select(info => new Tuple<int, string>(info.Id, path.ConvertToStructure(ConvertName(info.Name), prefix,
                category.Entries.Where(entry => entry.Default.Length == 0 || (entry.Value is not Vtype.Text))
                    .Select(entry => new Tuple<Entry, string>(entry, Normalize(info.GetString(entry.Index))))
                    .Where(tuple => tuple.Item1.Default.Length == 0 || !tuple.Item1.Default[0].Equals(tuple.Item2))
                    .ToDictionary(tuple => tuple.Item1.Index, tuple => tuple.Item2))));
        static string ConvertToStructure(this DirectoryInfo path, string name, string prefix, Dictionary<Ktype, string> values) =>
            $"{prefix}/{name}".With(() => Directory.CreateDirectory(Path.Combine(path.FullName, name))
                .With(subpath => File.WriteAllText(Path.Combine(subpath.FullName, "values.json"), JsonSerializer.Serialize(values, JsonOption))));
        static string ConvertName(string name) =>
            Path.GetInvalidFileNameChars().Aggregate(name, (name, ch) => name.Replace(ch, ' '), name => name);
    }
}