using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Character;
using CoastalSmell;
using Resolution = System.Tuple<string, Character.ListInfoBase>;
using KeysDefs = Il2CppSystem.Collections.Generic.IReadOnlyList<ChaListDefine.KeyType>;
using KeysList = Il2CppSystem.Collections.Generic.List<ChaListDefine.KeyType>;
using ValsList = Il2CppSystem.Collections.Generic.List<string>;
using Values = System.Collections.Generic.Dictionary<ChaListDefine.KeyType, string>;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;
using Mods = System.Collections.Generic.IEnumerable<System.Tuple<ChaListDefine.KeyType, string>>;
using Mod = System.Tuple<ChaListDefine.KeyType, string>;

namespace SardineTail
{
    file static partial class ParserExtension
    {
        static readonly Dictionary<CatNo, int> Identities =
#if DEBUG
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => ModInfo.MIN_ID + new System.Random().Next(0, 100));
#else
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => ModInfo.MIN_ID);
#endif
        static int AssignId(this CatNo categoryNo) => Identities[categoryNo]++;

        static KeysList ResolveKeys(this CategorySpec category) =>
            new KeysList()
                .With(s => s.Add(Ktype.ID))
                .With(s => category.Entries.ForEach(entry => s.Add(entry.Index)));

        static ValsList ResolveValues(this CategorySpec category, Values values) =>
            new ValsList()
                .With(s => s.Add(category.Index.AssignId().ToString()))
                .With(s => category.Entries.ForEach(entry => s.Add(values[entry.Index])));

        internal static string Normalize(string input) =>
            string.IsNullOrWhiteSpace(input) ? "0" : input.Trim();

        internal static ListInfoBase Resolve(this CategorySpec category, Values values) =>
            new ListInfoBase((int)category.Index, 0,
                new KeysDefs(category.ResolveKeys().Pointer), category.ResolveValues(values));
    }

    internal abstract class CategoryCollector<T>
    {
        internal const string HARD_MIGRATION = "hardmig.json";
        internal const string SOFT_MIGRATION = "softmig.json";
        internal GameId GameId;
        internal string PkgId;
        internal T Container;
        internal IEnumerable<ModNode<T>> Nodes;
        internal IEnumerable<ModLeaf<T>> Csvs;
        internal CategoryCollector(GameId gameId, string pkgId, T container) =>
            ((GameId, PkgId, Container) = (gameId, pkgId, container)).With(Initialize);
        void Initialize() =>
            (Nodes, Csvs) = Contents()
                .GroupBy(paths => paths[0]).Aggregate<IGrouping<string, string[]>, (IEnumerable<ModNode<T>>, IEnumerable<ModLeaf<T>>)>(([], []),
                    (tuple, group) => Enum.TryParse<CatNo>(group.Key, true, out var index) ?
                        new(tuple.Item1.Append(new ModNode<T>(this, GameId.ToSpec(index), group.Key, group)), tuple.Item2) :
                        new(tuple.Item1, tuple.Item2.Concat(GameId.ToSpecs()
                            .Where(elm => $"{elm.Index}.csv".Equals(group.Key, StringComparison.OrdinalIgnoreCase))
                            .SelectMany(elm => group.Select(paths => new ModLeaf<T>(this, elm, paths))))));
        internal IEnumerable<IGrouping<CategorySpec, IEnumerable<Resolution>>> Resolve() =>
            Nodes.GroupBy(node => node.CategorySpec, node => node.Resolve())
                .Concat(Csvs.GroupBy(csv => csv.CategorySpec, csv => csv.ParseCsv()));
        internal abstract IEnumerable<string[]> Contents();
        internal abstract bool Exists(string path);
        internal abstract Mods GetValues(ModLeaf<T> leaf);
        internal abstract string GetContent(ModLeaf<T> leaf);
        internal abstract Dictionary<Version, Dictionary<string, string>> GetSoftMigrations();
        internal abstract Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> GetHardMigrations();
    }
    internal class ModLeaf<T>
    {
        internal CategorySpec CategorySpec;
        internal CategoryCollector<T> Root;
        internal string[] Paths;
        internal string ModId =>
            string.Join(Path.AltDirectorySeparatorChar, Paths);
        internal ModLeaf(CategoryCollector<T> root, CategorySpec category, string[] paths) =>
            (Root, CategorySpec, Paths) = (root, category, paths);
        internal Mods ToMods() =>
            "values.json".Equals(Paths[^1], StringComparison.OrdinalIgnoreCase) ? Root.GetValues(this)
                : CategorySpec.Entries.Where(entry => entry.Value is Vtype.Image)
                    .Where(entry => $"{entry.Index}.png".Equals(Paths[^1], StringComparison.OrdinalIgnoreCase))
                    .Select(entry => new Mod(entry.Index, string.Join(Path.AltDirectorySeparatorChar, Paths)));
        internal Values ToValues(Mods mods) =>
            CategorySpec.Entries.ToDictionary(entry => entry.Index,
                entry => entry.Default.Concat(mods.Where(mod => entry.Index == mod.Item1).Select(mod => mod.Item2)))
                    .Where(entry => entry.Value.Count() > 0).ToDictionary(entry => entry.Key, entry => entry.Value.Last());
        string NormalizedValue(Values mods, Entry entry) =>
            ParserExtension.Normalize(mods.GetValueOrDefault(entry.Index, "0"));

        internal string ResolveImage(string path) =>
            path is "0" ? "0" : Root.Exists(path) ? $"{Root.GameId}:{Root.PkgId}:{path}" : path;
        internal string ResolveImage(string bundle, string path) =>
            path is "0" ? "0" : Root.Exists(path) ? $"{Root.GameId}:{Root.PkgId}:{path}" : $"{Root.GameId}:{Root.PkgId}:{bundle}:{path}";
        internal string ResolveAsset(string bundle, string path) =>
            path is "0" ? "0" : $"{Root.GameId}:{Root.PkgId}:{bundle}:{path}:";
        Mod ResolveEntry(Values mods, Entry entry) =>
            entry.Value switch
            {
                Vtype.Asset => new(entry.Index, NormalizedValue(mods, entry)),
                Vtype.Image => new(entry.Index, ResolveImage(NormalizedValue(mods, entry))),
                _ => new(entry.Index, NormalizedValue(mods, entry))
            };
        Mod ResolveEntry(Values mods, string path, Entry entry) =>
            entry.Value switch
            {
                Vtype.Asset => new(entry.Index, ResolveAsset(path, NormalizedValue(mods, entry))),
                Vtype.Image => new(entry.Index, ResolveImage(path, NormalizedValue(mods, entry))),
                _ => new(entry.Index, NormalizedValue(mods, entry))
            };
        // Resolves children entries based on the path value
        // If path is "0" (default), resolve each child without bundle
        // If path doesn't exist, resolve children with the path as bundle name
        // Otherwise, resolve children with the path as bundle and use default asset bundle for parent
        Mods ResolveChildren(Values mods, Ktype key, string path, IEnumerable<Entry> children) =>
            path is "0" ? children.Select(child => ResolveEntry(mods, child)).Append(new Mod(key, "0")) :
                !Root.Exists(path)
                    ? children.Select(child => ResolveEntry(mods, child)).Append(new Mod(key, path))
                    : children.Select(child => ResolveEntry(mods, path, child)).Append(new Mod(key, Plugin.AssetBundle));
        Mods ResolveEntries(Values mods, Entry parent, IEnumerable<Entry> children) =>
            children.Count() is 0 ? [ResolveEntry(mods, parent)] :
                ResolveChildren(mods, parent.Index, NormalizedValue(mods, parent), children);
        ListInfoBase ResolveValues(Values mods) =>
            CategorySpec.Resolve(ToValues(CategorySpec.ResolutionPairs.SelectMany(pair => ResolveEntries(mods, pair.Key, pair.Value))));
        Resolution Resolve(Values mods) =>
            CategorySpec.Entries.All(entry => mods.ContainsKey(entry.Index))
                ? new(ModId, ResolveValues(mods)) : null;
        internal bool Resolve(Mods mods, out Resolution info) =>
            null != (info = Resolve(ToValues(mods)));
        internal IEnumerable<Resolution> ParseCsv() =>
            ParseCsv(Root.GetContent(this).Split('\n'));
        bool CheckCsvHeaders(string[] values) =>
            CategorySpec.Entries.Length == values.Length && CategorySpec.Entries.Index()
                .All(tuple => tuple.Item1.Index.ToString().Equals(values[tuple.Item2], StringComparison.OrdinalIgnoreCase));
        IEnumerable<Resolution> ParseCsv(string[] lines) =>
            CheckCsvHeaders(lines[0].Trim().Split(',')) ? lines[1..].SelectMany(line => CsvToMods(line.Trim().Split(','))) : [];
        IEnumerable<Resolution> CsvToMods(string[] values) =>
            CategorySpec.Entries.Length == values.Length ?
                [ResolveCsv(CategorySpec.Entries.Index().ToDictionary(tuple => tuple.Item1.Index, tuple => values[tuple.Item2]))] : [];
        Resolution ResolveCsv(Values values) =>
            new($"{CategorySpec.Index}.csv/{values[Ktype.Name]}", ResolveValues(values));
    }
    internal class ModNode<T> : ModLeaf<T>
    {
        internal IEnumerable<ModLeaf<T>> Leaves;
        internal IEnumerable<ModNode<T>> Nodes;
        internal IEnumerable<Resolution> Resolve() =>
            Resolve([]);
        IEnumerable<Resolution> Resolve(Mods parent) =>
            Resolve(parent, Leaves.SelectMany(leaf => leaf.ToMods()));
        IEnumerable<Resolution> Resolve(Mods parent, Mods leaves) =>
            Resolve(parent.Concat(leaves).Append(new Mod(Ktype.Name, Paths[^1])), out var info)
                ? [info] : Nodes.SelectMany(node => node.Resolve(parent.Concat(leaves)));
        internal ModNode(CategoryCollector<T> root, CategorySpec category, string[] paths, IEnumerable<ModLeaf<T>> inputs, int depth) : base(root, category, paths) =>
            (Leaves, Nodes) = inputs.GroupBy(leaf => leaf.Paths[depth])
                .Aggregate<IGrouping<string, ModLeaf<T>>, (IEnumerable<ModLeaf<T>>, IEnumerable<ModNode<T>>)>(([], []),
                    (tuple, group) => (
                        tuple.Item1.Concat(group.Where(leaf => leaf.Paths.Length == depth + 1)),
                        tuple.Item2.Append(new ModNode<T>(root, category, [.. paths, group.Key],
                            group.Where(leaf => leaf.Paths.Length > depth + 1), depth + 1))));
        internal ModNode(CategoryCollector<T> root, CategorySpec category, string path, IEnumerable<string[]> inputs) :
            this(root, category, [path], inputs.Where(paths => paths.Length > 1).Select(paths => new ModLeaf<T>(root, category, paths)), 1)
        { }
    }
    internal class DevCollector : CategoryCollector<string>
    {
        internal DevCollector(GameId gameId, string pkgId, string path) : base(gameId, pkgId, path) { }
        internal override IEnumerable<string[]> Contents() =>
            Contents(new DirectoryInfo(Container));
        IEnumerable<string[]> Contents(DirectoryInfo info) =>
            EnumeratePaths(info, "*.csv").Concat(EnumeratePaths(info, "*.png")).Concat(EnumeratePaths(info, "values.json"));
        IEnumerable<string[]> EnumeratePaths(DirectoryInfo info, string pattern) =>
            info.EnumerateFiles(pattern, SearchOption.AllDirectories).Select(info =>
                Path.GetRelativePath(Container, info.FullName).Split(Path.DirectorySeparatorChar));
        internal override bool Exists(string path) =>
            File.Exists(Path.Combine([Container, .. path.Split(Path.AltDirectorySeparatorChar)]));
        internal override string GetContent(ModLeaf<string> leaf) =>
            File.ReadAllText(Path.Combine([Container, .. leaf.Paths]));
        internal override Mods GetValues(ModLeaf<string> leaf) =>
            Json<Values>.Load(Plugin.Instance.Log.LogError,
                File.OpenRead(Path.Combine([Container, .. leaf.Paths])))
                .Select(entry => new Mod(entry.Key, entry.Value));
        internal override Dictionary<Version, Dictionary<string, string>> GetSoftMigrations() =>
            Exists(SOFT_MIGRATION) ?
                Json<Dictionary<string, Dictionary<string, string>>>
                    .Load(Plugin.Instance.Log.LogError, File.OpenRead(Path.Combine(Container, SOFT_MIGRATION)))
                    .Where(entry => Version.TryParse(entry.Key, out _))
                    .ToDictionary(entry => Version.Parse(entry.Key), entry => entry.Value) : new();
        internal override Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> GetHardMigrations() =>
            Exists(HARD_MIGRATION) ?
                Json<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>
                    .Load(Plugin.Instance.Log.LogError, File.OpenRead(Path.Combine(Container, HARD_MIGRATION))) : new();
    }
    internal class StpCollector : CategoryCollector<ZipArchive>
    {
        internal StpCollector(GameId gameId, string pkgId, string path) : base(gameId, pkgId, new ZipArchive(File.OpenRead(path))) { }
        internal override IEnumerable<string[]> Contents() =>
            Container.Entries
                .Where(entry => !entry.FullName.EndsWith(Path.AltDirectorySeparatorChar))
                .Select(entry => entry.FullName.Split(Path.AltDirectorySeparatorChar));
        internal override bool Exists(string path) =>
            Container.GetEntry(path) != null;
        ZipArchiveEntry GetEntry(string[] paths) =>
            Container.GetEntry(string.Join(Path.AltDirectorySeparatorChar, paths));
        internal override string GetContent(ModLeaf<ZipArchive> leaf) =>
            GetContent(GetEntry(leaf.Paths));

        string GetContent(ZipArchiveEntry entry) =>
            Encoding.UTF8.GetString(entry.Open().ReadBytes((int)entry.Length));

        internal override Mods GetValues(ModLeaf<ZipArchive> leaf) =>
            Json<Values>.Load(Plugin.Instance.Log.LogError, GetEntry(leaf.Paths).Open())
                .Select(entry => new Mod(entry.Key, entry.Value));

        internal override Dictionary<Version, Dictionary<string, string>> GetSoftMigrations() =>
            Exists(SOFT_MIGRATION) ? Json<Dictionary<string, Dictionary<string, string>>>
                .Load(Plugin.Instance.Log.LogError, GetEntry([SOFT_MIGRATION]).Open())
                .Where(entry => Version.TryParse(entry.Key, out _))
                .ToDictionary(entry => Version.Parse(entry.Key), entry => entry.Value) : new();
        internal override Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> GetHardMigrations() =>
            Exists(HARD_MIGRATION) ? Json<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>
                .Load(Plugin.Instance.Log.LogError, GetEntry([HARD_MIGRATION]).Open()) : new();
    }
}
