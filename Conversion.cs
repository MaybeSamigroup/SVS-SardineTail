using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Character;
using BepInEx;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;
using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace SardineTail
{
    class ConvertEntry
    {
        public string Name;
        public int Id { get; set; }
        public CatNo Category { get; set; }
        public string Manifest { get; set; } = "0";
        public List<string> Bundles { get; set; } = new();
        public List<string> Invalid { get; set; } = new();
        public Dictionary<Ktype, string> Values { get; set; } = new();
    }

    class ConvertPackage
    {
        public List<string> Bundles;
        public List<ConvertEntry> Entries;
        public ConvertPackage(List<string> bundles, List<ConvertEntry> entries) =>
            (Bundles, Entries) = (bundles, entries);
        public ConvertPackage(Dictionary<string, string> manifestMap, ConvertEntry entry) :
            this(entry.Bundles.Where(bundle => !manifestMap.ContainsKey(bundle)).ToList(), [entry]) { }
        public bool Intersect(ConvertPackage package) =>
            package.Bundles.Count == 0 ? Bundles.Count == 0 : package.Bundles.Any(Bundles.Contains);
        public ConvertPackage Merge(IEnumerable<ConvertPackage> packages) =>
            new(Bundles.Concat(packages.SelectMany(package => package.Bundles)).Distinct().ToList(), Entries.Concat(packages.SelectMany(package => package.Entries)).ToList());
        public string PackagePath =>
            Bundles.Count == 0
                ? Path.Combine(Plugin.ConvertPath, "ILLGames.Redefine-0.0.0")
                : Bundles.Where(bundle => !bundle.Contains("thumb"))
                    .Select(ToPath).Select(ToPluralPath)
                    .OrderBy(path => path.Length).First();
        string ToPath(string bundle) =>
            Path.GetFileNameWithoutExtension(string.Join('.',
                bundle.Split(Path.AltDirectorySeparatorChar)
                    .Where(path => !"MOD".Equals(path))
                    .Where(path => !"chara".Equals(path))));

        string ToPluralPath(string name) =>
            Enumerable.Range(0, 100).Select(index => index == 0 ? "" : $"({index})")
                .Select(suffix => Path.Combine(Plugin.ConvertPath, $"{name}{suffix}-0.0.0"))
                .Where(path => !Directory.Exists(path)).First();
    }
    internal static partial class CategoryExtension
    {
        internal static void Initialize() =>
            Plugin.HardmodConversion.Value.Maybe(Util<Manager.Game>.Hook.Apply(Convert).Apply(F.DoNothing));
        internal static readonly JsonSerializerOptions JsonOption = new JsonSerializerOptions()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

        static void Convert()
        {
            new string[] { Plugin.ConvertPath, Plugin.InvalidPath }
                .Where(Directory.Exists)
                .ForEach(path => Directory.Delete(path, true));
            new string[] { Plugin.ConvertPath, Plugin.InvalidPath }
                .ForEach(path => Directory.CreateDirectory(path));
            All.Values.SelectMany(Convert)
                .GroupBy(entry => entry.Invalid.Count > 0)
                .ForEach(group => ForkInvalidAndValid(group.Key)(group));
            Plugin.HardmodConversion.Value = false;
        }
        static IEnumerable<ConvertEntry> Convert(Category category) =>
            Human.lstCtrl._table[category.Index].Yield()
                .Where(tuple => ModInfo.Map[category.Index].ToMod(tuple.Item1) is null)
                .Select(tuple => Convert(category, tuple.Item1, tuple.Item2,
                    category.Entries
                        .Where(entry => entry.Value == Vtype.Store)
                        .Where(entry => tuple.Item2.ContainsKey(entry.Index))
                        .Select(entry => tuple.Item2.GetString(entry.Index))
                        .Where(value => !ListInfoBase.IsNoneOrEmpty(value)).Distinct()
                        .GroupBy(AbdataExists)));

        static ConvertEntry Convert(Category category, int id, ListInfoBase info, IEnumerable<IGrouping<bool, string>> groups) => new()
        {
            Id = id,
            Name = info.Name,
            Category = category.Index,
            Manifest = info.TryGetValue(Ktype.MainManifest, out var value) ? value : "0",
            Values = category.Entries
                .Where(entry => Vtype.Name != entry.Value)
                .Where(entry => info.ContainsKey(entry.Index))
                .ToDictionary(entry => entry.Index, entry => info.GetString(entry.Index)),
            Bundles = groups.Where(group => group.Key).SelectMany(group => group).ToList(),
            Invalid = groups.Where(group => !group.Key).SelectMany(group => group).ToList()
        };

        static bool AbdataExists(string bundle) =>
            File.Exists(Path.Combine([Paths.GameRootPath, "abdata", .. bundle.Split(Path.AltDirectorySeparatorChar)]));
        static Action<IEnumerable<ConvertEntry>> ForkInvalidAndValid(bool value) => value ? Invalid : Convert;

        static void Invalid(IEnumerable<ConvertEntry> entries) =>
            entries.GroupBy(entry => entry.Category)
                .ForEach(group => Invalid(Directory.CreateDirectory(Path.Combine(Plugin.InvalidPath, group.Key.ToString())), group));
        static void Convert(IEnumerable<ConvertEntry> entries) =>
            Convert(ToManifestMap(entries.Where(entry => entry.Id < 100)), entries.Where(entry => entry.Id >= 100));

        static Dictionary<string, string> ToManifestMap(IEnumerable<ConvertEntry> entries) =>
            ToManifestMap(
                entries.SelectMany(entry => entry.Bundles).Distinct(),
                entries.GroupBy(entry => entry.Manifest)
                    .ToDictionary(
                        group => group.Key,
                        group => group.SelectMany(entry => entry.Bundles).Distinct().ToList()));

        static Dictionary<string, string> ToManifestMap(IEnumerable<string> bundles, Dictionary<string, List<string>> manifestToBundles) =>
            bundles.ToDictionary(
                bundle => bundle,
                bundle => manifestToBundles
                    .Where(entry => entry.Value.Contains(bundle))
                    .Select(entry => entry.Key).FirstOrDefault("abdata"));

        static void Convert(Dictionary<string, string> manifestMap, IEnumerable<ConvertEntry> entries) =>
            Convert(manifestMap, entries.Select(entry => new ConvertPackage(manifestMap, entry)));
        static void Convert(Dictionary<string, string> manifestMap, IEnumerable<ConvertPackage> entries)
        {
            List<ConvertPackage> packages = new ();
            foreach (var entry in entries)
            {
                var results = packages.GroupBy(group => group.Intersect(entry));
                packages = results.Where(result => !result.Key).SelectMany(result => result)
                    .Append(entry.Merge(results.Where(result => result.Key).SelectMany(result => result))).ToList();
            }
            packages.ForEach(package => Package(manifestMap, package));
        }
        static void Package(Dictionary<string, string> manifestMap, ConvertPackage package)
        {
            var path = package.PackagePath;
            Directory.CreateDirectory(path);
            foreach (var bundle in package.Bundles)
            {
                var dest = Path.Combine([path, .. bundle.Split(Path.AltDirectorySeparatorChar)]);
                Directory.CreateDirectory(Path.GetDirectoryName(dest));
                File.Copy(Path.Combine([Paths.GameRootPath, "abdata", .. bundle.Split(Path.AltDirectorySeparatorChar)]), dest);
            }
            Dictionary<CatNo, Dictionary<string, HardMigrationInfo>> hardmigs = new();
            foreach (var group in package.Entries.GroupBy(entry => entry.Category))
            {
                Dictionary<string, HardMigrationInfo> hardmig = new();
                using (var stream = new StreamWriter(File.OpenWrite(Path.Combine(path, $"{group.Key}.csv")), new UTF8Encoding(false)))
                {
                    var defs = All[group.Key].Entries;
                    stream.WriteLine(string.Join(',', defs.Select(def => def.Index.ToString())));
                    foreach (var dups in group.GroupBy(mod => mod.Name))
                    {
                        var toModId = ToModId(dups.Count());
                        foreach (var (mod, idx) in dups.Select((mod, idx) => (mod, idx + 1)))
                        {
                            mod.Values[Ktype.Name] = mod.Name = toModId(mod, idx);
                            mod.Values[Ktype.MainManifest] = mod.Bundles.Where(manifestMap.ContainsKey).Select(bundle => manifestMap[bundle]).FirstOrDefault("abdata");
                            stream.WriteLine(string.Join(',', defs.Select(def => mod.Values.GetValueOrDefault(def.Index, def.Default.FirstOrDefault("0")))));
                            hardmig.Add(mod.Id.ToString(), new() { ModId = $"{group.Key}.csv/{mod.Name}", Version = new(0, 0, 0) });
                        }
                    }
                }
                hardmigs.Add(group.Key, hardmig);
            }
            File.WriteAllText(Path.Combine(path, "hardmig.json"), JsonSerializer.Serialize(hardmigs, JsonOption));
        }
        static Func<ConvertEntry, int, string> ToModId(int count) =>
            count > 1 ? ((entry, idx) => $"{entry.Name}({idx})") : ((entry, _) => entry.Name);
        static void Invalid(DirectoryInfo path, IEnumerable<ConvertEntry> entries) =>
            entries.ForEach(entry => File.WriteAllText(Path.Combine(path.FullName, $"{entry.Id}.json"), JsonSerializer.Serialize(entry, JsonOption)));
    }
}