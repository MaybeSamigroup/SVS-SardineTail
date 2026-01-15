using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using UnityEngine;
using Character;
#if Aicomi
using ILLGAMES.Unity;
#else
using ILLGames.Unity;
#endif
using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using CoastalSmell;
using KeysDefs = Il2CppSystem.Collections.Generic.IReadOnlyList<ChaListDefine.KeyType>;
using KeysList = Il2CppSystem.Collections.Generic.List<ChaListDefine.KeyType>;
using ValsList = Il2CppSystem.Collections.Generic.List<string>;
using Values = System.Collections.Generic.Dictionary<ChaListDefine.KeyType, string>;
using Mods = System.Collections.Generic.IEnumerable<System.Tuple<ChaListDefine.KeyType, string>>;
using Mod = System.Tuple<ChaListDefine.KeyType, string>;
using Resolution = System.Tuple<string, Character.ListInfoBase>;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal static partial class CategoryExtension
    {
        static readonly Dictionary<CatNo, int> Identities =
#if DEBUG
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => ModInfo.MIN_ID + new System.Random().Next(0, 100));
#else
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => ModInfo.MIN_ID);
#endif
        internal static int AssignId(this CatNo categoryNo) => Identities[categoryNo]++;

        static KeysList ResolveKeys(this Category category) =>
            new KeysList()
                .With(s => s.Add(Ktype.ID))
                .With(s => category.Entries.ForEach(entry => s.Add(entry.Index)));

        static ValsList ResolveValues(this Category category, Values values) =>
            new ValsList()
                .With(s => s.Add(category.Index.AssignId().ToString()))
                .With(s => category.Entries.ForEach(entry => s.Add(values[entry.Index])));

        internal static ListInfoBase Resolve(this Category category, Values values) =>
            new ListInfoBase((int)category.Index, 0,
                new KeysDefs(category.ResolveKeys().Pointer), category.ResolveValues(values));
    }
    internal abstract class CategoryCollector<T>
    {
        internal const string HARD_MIGRATION = "hardmig.json";
        internal const string SOFT_MIGRATION = "softmig.json";
        internal int GameId;
        internal string PkgId;
        internal T Container;
        internal IEnumerable<ModNode<T>> Nodes;
        internal IEnumerable<ModLeaf<T>> Csvs;
        internal CategoryCollector(int gameId, string pkgId, T container) =>
            ((GameId, PkgId, Container) = (gameId, pkgId, container)).With(Initialize);
        void Initialize() =>
            (Nodes, Csvs) = Contents()
                .GroupBy(paths => paths[0]).Aggregate<IGrouping<string, string[]>, (IEnumerable<ModNode<T>>, IEnumerable<ModLeaf<T>>)>(([], []),
                    (tuple, group) => Enum.TryParse<CatNo>(group.Key, true, out var index) ?
                        new(tuple.Item1.Append(new ModNode<T>(this, CategoryExtension.All[index], group.Key, group)), tuple.Item2) :
                        new(tuple.Item1, tuple.Item2.Concat(CategoryExtension.All.Values
                            .Where(elm => $"{elm.Index}.csv".Equals(group.Key, StringComparison.OrdinalIgnoreCase))
                            .SelectMany(elm => group.Select(paths => new ModLeaf<T>(this, elm, paths))))));
        internal IEnumerable<IGrouping<Category, IEnumerable<Resolution>>> Resolve() =>
            Nodes.GroupBy(node => node.Category, node => node.Resolve())
                .Concat(Csvs.GroupBy(csv => csv.Category, csv => csv.ParseCsv()));
        internal abstract IEnumerable<string[]> Contents();
        internal abstract bool Exists(string path);
        internal abstract Mods GetValues(ModLeaf<T> leaf);
        internal abstract string GetContent(ModLeaf<T> leaf);
        internal abstract Dictionary<Version, Dictionary<string, string>> GetSoftMigrations();
        internal abstract Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> GetHardMigrations();
    }
    internal class ModLeaf<T>
    {
        internal Category Category;
        internal CategoryCollector<T> Root;
        internal string[] Paths;
        internal string ModId =>
            string.Join(Path.AltDirectorySeparatorChar, Paths);
        internal ModLeaf(CategoryCollector<T> root, Category category, string[] paths) =>
            (Root, Category, Paths) = (root, category, paths);
        internal Mods ToMods() =>
            "values.json".Equals(Paths[^1], StringComparison.OrdinalIgnoreCase) ? Root.GetValues(this)
                : Category.Entries.Where(entry => entry.Value is Vtype.Image)
                    .Where(entry => $"{entry.Index}.png".Equals(Paths[^1], StringComparison.OrdinalIgnoreCase))
                    .Select(entry => new Mod(entry.Index, string.Join(Path.AltDirectorySeparatorChar, Paths)));
        internal Values ToValues(Mods mods) =>
            Category.Entries.ToDictionary(entry => entry.Index,
                entry => entry.Default.Concat(mods.Where(mod => entry.Index == mod.Item1).Select(mod => mod.Item2)))
                    .Where(entry => entry.Value.Count() > 0).ToDictionary(entry => entry.Key, entry => entry.Value.Last());
        string NormalizedValue(Values mods, Entry entry) =>
            CategoryExtension.Normalize(mods.GetValueOrDefault(entry.Index, "0"));
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
            Category.Resolve(ToValues(Category.ResolutionPairs().SelectMany(pair => ResolveEntries(mods, pair.Key, pair.Value))));
        Resolution Resolve(Values mods) =>
            Category.Entries.All(entry => mods.ContainsKey(entry.Index))
                ? new(ModId, ResolveValues(mods)) : null;
        internal bool Resolve(Mods mods, out Resolution info) =>
            null != (info = Resolve(ToValues(mods)));
        internal IEnumerable<Resolution> ParseCsv() =>
            ParseCsv(Root.GetContent(this).Split('\n'));
        bool CheckCsvHeaders(string[] values) =>
            Category.Entries.Length == values.Length && Category.Entries.Index()
                .All(tuple => tuple.Item1.Index.ToString().Equals(values[tuple.Item2], StringComparison.OrdinalIgnoreCase));
        IEnumerable<Resolution> ParseCsv(string[] lines) =>
            CheckCsvHeaders(lines[0].Trim().Split(',')) ? lines[1..].SelectMany(line => CsvToMods(line.Trim().Split(','))) : [];
        IEnumerable<Resolution> CsvToMods(string[] values) =>
            Category.Entries.Length == values.Length ?
                [ResolveCsv(Category.Entries.Index().ToDictionary(tuple => tuple.Item1.Index, tuple => values[tuple.Item2]))] : [];
        Resolution ResolveCsv(Values values) =>
            new($"{Category.Index}.csv/{values[Ktype.Name]}", ResolveValues(values));
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
        internal ModNode(CategoryCollector<T> root, Category category, string[] paths, IEnumerable<ModLeaf<T>> inputs, int depth) : base(root, category, paths) =>
            (Leaves, Nodes) = inputs.GroupBy(leaf => leaf.Paths[depth])
                .Aggregate<IGrouping<string, ModLeaf<T>>, (IEnumerable<ModLeaf<T>>, IEnumerable<ModNode<T>>)>(([], []),
                    (tuple, group) => (
                        tuple.Item1.Concat(group.Where(leaf => leaf.Paths.Length == depth + 1)),
                        tuple.Item2.Append(new ModNode<T>(root, category, [.. paths, group.Key],
                            group.Where(leaf => leaf.Paths.Length > depth + 1), depth + 1))));
        internal ModNode(CategoryCollector<T> root, Category category, string path, IEnumerable<string[]> inputs) :
            this(root, category, [path], inputs.Where(paths => paths.Length > 1).Select(paths => new ModLeaf<T>(root, category, paths)), 1)
        { }
    }
    internal static partial class IOExtension
    {
        static Func<Stream, byte[]> ReadBytes(int length) =>
            stream => new BinaryReader(stream).ReadBytes(length);
        internal static byte[] ReadBytes(this Stream stream, int length) =>
            ReadBytes(length).ApplyDisposable(stream)
                .Try(Plugin.Instance.Log.LogMessage, out var bytes) ? bytes : [];
        internal static byte[] ReadAllBytes(this ZipArchiveEntry entry) =>
            entry.Open().ReadBytes((int)entry.Length);
        internal static string ReadAllText(this ZipArchiveEntry entry) =>
            System.Text.Encoding.UTF8.GetString(entry.ReadAllBytes());
        internal static T Parse<T>(this Stream stream) where T : new() =>
            Json<T>.Load(Plugin.Instance.Log.LogMessage, stream) ?? new();
    }
    internal class DevCollector : CategoryCollector<string>
    {
        internal DevCollector(int gameId, string pkgId, string path) : base(gameId, pkgId, path) { }
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
            File.OpenRead(Path.Combine([Container, .. leaf.Paths]))
                .Parse<Values>().Select(entry => new Mod(entry.Key, entry.Value));
        internal override Dictionary<Version, Dictionary<string, string>> GetSoftMigrations() =>
            Exists(SOFT_MIGRATION) ? File.OpenRead(Path.Combine(Container, SOFT_MIGRATION))
                .Parse<Dictionary<string, Dictionary<string, string>>>()
                .Where(entry => Version.TryParse(entry.Key, out _))
                .ToDictionary(entry => Version.Parse(entry.Key), entry => entry.Value) : new();
        internal override Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> GetHardMigrations() =>
            Exists(HARD_MIGRATION) ? File.OpenRead(Path.Combine(Container, HARD_MIGRATION))
                .Parse<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>() : new();
    }
    internal class StpCollector : CategoryCollector<ZipArchive>
    {
        internal StpCollector(int gameId, string pkgId, string path) : base(gameId, pkgId, new ZipArchive(File.OpenRead(path))) { }
        internal override IEnumerable<string[]> Contents() =>
            Container.Entries
                .Where(entry => !entry.FullName.EndsWith(Path.AltDirectorySeparatorChar))
                .Select(entry => entry.FullName.Split(Path.AltDirectorySeparatorChar));
        internal override bool Exists(string path) =>
            Container.GetEntry(path) != null;
        ZipArchiveEntry GetEntry(string[] paths) =>
            Container.GetEntry(string.Join(Path.AltDirectorySeparatorChar, paths));
        internal override string GetContent(ModLeaf<ZipArchive> leaf) =>
            GetEntry(leaf.Paths).ReadAllText();
        internal override Mods GetValues(ModLeaf<ZipArchive> leaf) =>
            GetEntry(leaf.Paths).Open().Parse<Values>().Select(entry => new Mod(entry.Key, entry.Value));
        internal override Dictionary<Version, Dictionary<string, string>> GetSoftMigrations() =>
            Exists(SOFT_MIGRATION) ? GetEntry([SOFT_MIGRATION]).Open()
                .Parse<Dictionary<string, Dictionary<string, string>>>()
                .Where(entry => Version.TryParse(entry.Key, out _))
                .ToDictionary(entry => Version.Parse(entry.Key), entry => entry.Value) : new();
        internal override Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> GetHardMigrations() =>
            Exists(HARD_MIGRATION) ? GetEntry([HARD_MIGRATION]).Open()
                .Parse<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>() : new();
    }
    public struct HardMigrationInfo
    {
        public string ModId { get; set; }
        public Version Version { get; set; }
    }

    internal abstract partial class ModPackage
    {
        static readonly Texture2D DefaultTexture = new(0, 0);
        static readonly Dictionary<string, AssetBundle> Cache = new()
        {
            [""] = new()
        };
        static Dictionary<int, Dictionary<string, ModPackage>> Packages =
            IDS.ToDictionary(gameId => gameId, _ => new Dictionary<string, ModPackage>());
        static Dictionary<int, Dictionary<CatNo, Dictionary<int, ModInfo>>> HardMigrations =
            IDS.ToDictionary(gameId => gameId, _ => Enum.GetValues<CatNo>().ToDictionary(cat => cat, cat => new Dictionary<int, ModInfo>()));
        static Dictionary<CatNo, Dictionary<int, ModInfo>> IdToMod =
            Enum.GetValues<CatNo>().ToDictionary(cat => cat, cat => new Dictionary<int, ModInfo>());
        static Action<CatNo, int, ModInfo> RegisterIdToMod =
            (categoryNo, id, mod) => IdToMod[categoryNo][id] = mod;
        static Action<int, CatNo, int, ModInfo> RegisterHardMigration =
            (gameId, categoryNo, id, mod) => HardMigrations[gameId][categoryNo][id] = mod; 
        internal static Func<int, string, string, bool> Exists =
            (gameId, pkgId, path) => Packages[gameId].TryGetValue(pkgId, out var pkg) && pkg.ResourceExists(path);
        internal static Func<CatNo, int, ModInfo> FromId =
            (categoryNo, id) => IdToMod[categoryNo].TryGetValue(id, out var mod) ? mod : null;
        internal static Func<int, CatNo, ModInfo, int, int> ToId =
            (gameId, categoryNo, mod, id) =>
                HardMigrations[gameId][categoryNo].TryGetValue(id, out var soft)
                    ? Packages[gameId][soft.PkgId].TranslateId(soft, id) : mod?.PkgId == null ? id
                    : Packages[gameId].TryGetValue(mod.PkgId, out var pkg) ? pkg.TranslateId(mod, id)
                    : id.With(() => Plugin.Instance.Log.LogMessage($"mod package missing ({gameId}): {mod.PkgId}"));

        internal static Func<string[], IEnumerable<Version>> ToVersion =>
            items => items.Length switch
            {
                0 => [],
                1 => [],
                _ => Version.TryParse(items[^1], out var version) ? [version] : [],
            };
        static NormalData ToNormalData(UnityEngine.Object asset) =>
            asset is null ? null : new NormalData(asset.Pointer);

        internal static NormalData ToNormalData(string[] items) =>
            items.Length switch
            {
                5 => int.TryParse(items[0], out var gameId) && Packages[gameId].TryGetValue(items[1], out var pkg)
                    ? ToNormalData(pkg.GetAsset(items[2], items[3], Il2CppInterop.Runtime.Il2CppType.Of<NormalData>())) : null,
                _ => null
            };

        static Subject<UnityEngine.Object> PrefabLoad = new();

        internal static IObservable<GameObject> OnPrefabLoad => PrefabLoad.AsObservable().Select(obj => new GameObject(obj.Pointer));

        internal static UnityEngine.Object ToAsset(string[] items, Il2CppSystem.Type type) =>
            items.Length switch
            {
                3 => int.TryParse(items[0], out var gameId) && Packages[gameId].TryGetValue(items[1], out var pkg)
                    ? pkg.GetTexture(Path.ChangeExtension(items[2], ".png")) : null,

                4 => int.TryParse(items[0], out var gameId) && Packages[gameId].TryGetValue(items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], type) : null,

                5 => int.TryParse(items[0], out var gameId) && Packages[gameId].TryGetValue(items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], type).With(PrefabLoad.OnNext) : null,

                _ => null
            };
        internal static UnityEngine.Object ToAsset(string[] items) =>
            items.Length switch
            {
                3 => int.TryParse(items[0], out var gameId) && Packages[gameId].TryGetValue(items[1], out var pkg)
                    ? pkg.GetTexture(Path.ChangeExtension(items[1], ".png")) : null,

                4 => int.TryParse(items[0], out var gameId) && Packages[gameId].TryGetValue(items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], Il2CppInterop.Runtime.Il2CppType.Of<Texture2D>()) : null,

                5 => int.TryParse(items[0], out var gameId) && Packages[gameId].TryGetValue(items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], Il2CppInterop.Runtime.Il2CppType.Of<GameObject>()).With(PrefabLoad.OnNext) : null,

                _ => null
            };
        internal static void InitializePackages(int gameId, string path) =>
            StpPackage.Collect(gameId, Path.Combine(path, "sardines"))
                .Concat(DevPackage.Collect(gameId, Path.Combine(path, "UserData", "plugins", Plugin.Name, "packages")))
                .GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last())
                .ForEach(entry => Packages[gameId][entry.Key] = entry.Value.With(entry.Value.Initialize));
    }

    internal abstract partial class ModPackage
    {
        internal int GameId;
        internal string PkgPath;
        internal string PkgId;
        internal Version PkgVersion;
        internal abstract void Initialize();
        internal abstract bool ResourceExists(string path);
        internal abstract byte[] ToBytes(string path);
        internal abstract string ToString(string path);
        internal abstract Stream ToStream(string path);
        internal abstract string ToAssetBundleIdentity(string path);
        internal abstract AssetBundle ToAssetBundle(string path);
        internal readonly Dictionary<string, int> ModToId = new();
        internal Dictionary<string, string> IdentityCache = new();
        internal Dictionary<Version, Dictionary<string, string>> SoftMigrations = new();
        internal ModPackage(int gameId, string path, string pkgId, Version version) =>
            (GameId, PkgPath, PkgId, PkgVersion) = (gameId, path, pkgId, version);
        internal void LoadHardMigrations(Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> info) =>
            info.ForEach(entry => entry.Value.ForEach(subentry =>
                RegisterHardMigration(GameId, entry.Key, subentry.Key, new ModInfo()
                {
                    PkgVersion = subentry.Value.Version,
                    PkgId = PkgId,
                    ModId = subentry.Value.ModId,
                    Category = entry.Key,
                })));
        string SoftMigration(ModInfo info) =>
            SoftMigrations.Where(entry => entry.Key > info.PkgVersion).OrderBy(entry => entry.Key)
                .Aggregate(info.ModId, (modId, entry) => entry.Value.GetValueOrDefault(modId, modId));
        void NotifyMissingModInfo(ModInfo info) =>
            Plugin.Instance.Log.LogMessage($"mod info missing:{info.PkgId}:{info.ModId}:{info.PkgVersion}");
        internal int TranslateId(ModInfo info, int oldId) =>
            ModToId.TryGetValue(SoftMigration(info), out var newId) ? newId : oldId.With(() => NotifyMissingModInfo(info));
        Texture2D GetTexture(string path) =>
            ResourceExists(path) ? GetTexture(ToBytes(path))
                .With(path.Split(Path.AltDirectorySeparatorChar).WrapMode) : DefaultTexture;
        Texture2D GetTexture(byte[] bytes) =>
            new Texture2D(256, 256).With(t2d => ImageConversion.LoadImage(t2d, bytes));
        AssetBundle GetAssetBundle(string path, string identity) =>
            Cache.TryGetValue(identity, out var cache) ? cache : CacheAssetBundle(path, identity);
        AssetBundle CacheAssetBundle(string path, string identity) =>
            Cache[identity] = ToAssetBundle(path);
        string GetAssetBundleIdentity(string path) =>
            IdentityCache.TryGetValue(path, out var identity) ? identity : CacheAssetBundleIdentity(path);
        string CacheAssetBundleIdentity(string path) =>
            ResourceExists(path) ? IdentityCache[path] = ToAssetBundleIdentity(path) : "";
        UnityEngine.Object GetAsset(string bundle, string asset, Il2CppSystem.Type type) =>
            GetAssetBundle(bundle, GetAssetBundleIdentity(bundle))?.LoadAsset(asset, type);
        internal void Unload() =>
            Cache.With(cache => cache.Values.ForEach(item => item.Unload(true))).Clear();
     }
    internal partial class DevPackage : ModPackage
    {
        DevPackage(int gameid, string path, string pkgId, Version version) : base(gameid, path, pkgId, version) { }

        static Func<int, string, string, Version, DevPackage> ToPackage =
            (gameId, root, path, version) => new DevPackage(gameId, path, ToPkgId(root, path), version);

        static Func<int, string, string, IEnumerable<DevPackage>> ToPackages =
            (gameId, root, path) => ToVersion(Path.GetRelativePath(root, path).Split('-'))
                .Select(ToPackage.Apply(gameId).Apply(root).Apply(path));

        internal static Func<int, string, IEnumerable<ModPackage>> Collect =
            (gameId, path) => Plugin.DevelopmentMode.Value
                ? Directory.GetDirectories(path).SelectMany(ToPackages.Apply(gameId).Apply(path))
                : Enumerable.Empty<ModPackage>();

        internal override void Initialize() =>
            Initialize(new DevCollector(GameId, PkgId, PkgPath));

        internal void Initialize(DevCollector collector)
        {
            SoftMigrations = collector.GetSoftMigrations();
            LoadHardMigrations(collector.GetHardMigrations());
            collector.Resolve()
                .ForEach(group => group.SelectMany(res => res)
                .ForEach(resolution => Register(group.Key, resolution.Item1, resolution.Item2)));
        }
        internal override bool ResourceExists(string path) =>
            File.Exists(Path.Combine(PkgPath, path));
        internal override Stream ToStream(string path) =>
            File.OpenRead(Path.Combine(PkgPath, path));
        internal override byte[] ToBytes(string path) =>
            File.ReadAllBytes(Path.Combine(PkgPath, path));
        internal override string ToString(string path) =>
            File.ReadAllText(Path.Combine(PkgPath, path));
        internal override string ToAssetBundleIdentity(string path) =>
            UnityFS.Extract(File.OpenRead(Path.Combine(PkgPath, path)))
                .SelectMany(fs => fs.Identity).FirstOrDefault(""); 
        internal override AssetBundle ToAssetBundle(string path) =>
            AssetBundle.LoadFromFile(Path.Combine(PkgPath, path));
        static Func<string, string, string> ToPkgId =
            (root, path) => string.Join('-', Path.GetRelativePath(root, path).Split('-')[0..^1]);
    }
    internal partial class StpPackage : ModPackage
    {
        internal ZipArchive Archive;
        StpPackage(int gameId, string path, string pkgId, Version version) : base(gameId, path, pkgId, version) { }
        static Func<int, string, Version, StpPackage> ToPackage =
            (gameId, path, version) => new StpPackage(gameId, path, ToPkgId(path), version);
        static Func<int, string, IEnumerable<ModPackage>> ToPackages =
            (gameId, path) => ToVersion(Path.GetFileNameWithoutExtension(path).Split('-'))
                .Select(ToPackage.Apply(gameId).Apply(path));
        internal static Func<int, string, IEnumerable<ModPackage>> Collect =
            (gameId, path) => new DirectoryInfo(path).GetFiles("*.stp", SearchOption.AllDirectories)
                .Select(info => info.FullName).SelectMany(ToPackages.Apply(gameId));
        internal override void Initialize() =>
            Initialize(new StpCollector(GameId, PkgId, PkgPath));
        internal void Initialize(StpCollector collector)
        {
            Archive = new ZipArchive(File.OpenRead(PkgPath));
            SoftMigrations = collector.GetSoftMigrations();
            LoadHardMigrations(collector.GetHardMigrations());
            collector.Resolve()
                .ForEach(group => group.SelectMany(res => res)
                .ForEach(resolution => Register(group.Key, resolution.Item1, resolution.Item2)));
        }
        internal override bool ResourceExists(string path) =>
            Archive.GetEntry(path) != null;
        internal override Stream ToStream(string path) =>
            Archive.GetEntry(path)?.Open();
        internal override byte[] ToBytes(string path) =>
            ToBytes(Archive.GetEntry(path));
        internal override string ToString(string path) =>
            System.Text.Encoding.UTF8.GetString(ToBytes(path));
        internal override string ToAssetBundleIdentity(string path) =>
            ToAssetBundleIdentity(Archive.GetEntry(path));
        string ToAssetBundleIdentity(ZipArchiveEntry entry) =>
            entry == null ? "" : UnityFS.Extract(entry.Open())
                .SelectMany(fs => fs.Identity).FirstOrDefault(""); 
        internal override AssetBundle ToAssetBundle(string path) =>
            ToAssetBundle(Archive.GetEntry(path));
        AssetBundle ToAssetBundle(ZipArchiveEntry entry) =>
            entry.Length == entry.CompressedLength ? LoadAssetBundleFromStream(entry) : LoadAssetBundleFromBytes(entry);
        AssetBundle LoadAssetBundleFromStream(ZipArchiveEntry entry) =>
            AssetBundle.LoadFromStream(new EntryWrapper(File.OpenRead(PkgPath), entry.EntryOffset(), entry.Length));
        AssetBundle LoadAssetBundleFromBytes(ZipArchiveEntry entry) =>
            AssetBundle.LoadFromMemory(ToBytes(entry));
        byte[] ToBytes(ZipArchiveEntry entry) =>
            EntryToBytes((int)entry.Length).ApplyDisposable(entry.Open())
                .Try(Plugin.Instance.Log.LogMessage, out var value) ? value : [];
        Func<Stream, byte[]> EntryToBytes(int length) =>
            stream => new BinaryReader(stream).ReadBytes(length);
        static Func<string, string> ToPkgId =>
            (path) => string.Join('-', Path.GetFileName(path).Split('-')[0..^1]);
    }
    internal static partial class IOExtension
    {
        static UnityEngine.Object ToBodyAsset(string bundle, string asset, string manifest, Il2CppSystem.Type type) =>
            Plugin.AssetBundle.Equals(bundle)
                ? ModPackage.ToAsset(asset.Split(':'), type)
                : AssetBundleManager.GetLoadedAssetBundle(bundle, manifest).Bundle.LoadAsset(asset, type);

        static UnityEngine.Object ToBodyAsset(ListInfoBase info, Ktype ab, Ktype data, Il2CppSystem.Type type) =>
            (info != null) &&
            info.TryGetValue(ab, out var bundle) &&
            info.TryGetValue(data, out var asset) &&
            info.TryGetValue(Ktype.MainManifest, out var manifest)
                ? ToBodyAsset(bundle, asset, manifest, type)
                : null;
        static NormalData ToBodyNormal(ListInfoBase info) =>
            info != null &&
            info.TryGetValue(Ktype.MainAB, out var bundle) &&
            info.TryGetValue(Ktype.MainData, out var asset) &&
            Plugin.AssetBundle.Equals(bundle) ? ModPackage.ToNormalData(asset.Split(':')) : null;

        internal static long EntryOffset(this ZipArchiveEntry entry) =>
            (long)typeof(ZipArchiveEntry).GetProperty("OffsetOfCompressedData",
                 BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetValue(entry);

        static IEnumerable<Renderer> ToOverrideRenderers(HumanBody body, GameObject go) =>
            Enumerable.Range(0, go.transform.childCount)
                .Select(index => go.transform.GetChild(index).gameObject.GetComponent<Renderer>())
                .Where(renderer => renderer != null & body.rendBody.material.shader.name.Equals(renderer?.material?.shader?.name));
        internal static void OverrideGraphic(HumanBody body, GameObject go) =>
#if Aicomi
            body._graphicDisposables.Add(body._graphic.AddEvent(ToOverrideRenderers(body, go).ToArray(), HumanGraphic.UpdateFlags.All)); 
#else
            body._graphicDisposables.Add(body.graphic.AddEvent(ToOverrideRenderers(body, go).ToArray(), HumanGraphic.UpdateFlags.All));
#endif
        internal static void OverrideColors(HumanBody body, GameObject go) =>
#if Aicomi
            ToOverrideRenderers(body, go).Select(renderer => renderer.material).ForEach(ApplyColors.Apply(body._fileBody));
#else
            ToOverrideRenderers(body, go).Select(renderer => renderer.material).ForEach(ApplyColors.Apply(body.fileBody));
#endif
        static Action<HumanDataBody, Material> ApplyColors =
            new Action<HumanDataBody, Material>[]
            {
                (data, material) => material.SetColor("_MainColor", data.skinMainColor),
                (data, material) => material.SetColor(ChaShader.Body.sHighlightColorID, data.skinHighlightColor),
                (data, material) => material.SetColor(ChaShader.Body.sShadowColorID, data.skinShadowColor),
            }.Concat(
                new int[] { ChaShader.Body.sDetailColor01, ChaShader.Body.sDetailColor02, ChaShader.Body.sDetailColor03 }
                .Select<int, Action<HumanDataBody, Material>>((id, idx) =>
                    (data, material) => (idx < data.skinDetailColors.Count)
                        .Maybe(F.Apply(material.SetColor, id, data.skinDetailColors[idx])))
            ).Aggregate((a, b) => a + b);
    }
    public partial class EntryWrapper : Il2CppSystem.IO.Stream
    {
        Stream Target;
        public long EntryOffset { get; init; }
        public long EntryLength { get; init; }
        public override long Length => EntryLength;
        public override long Position
        {
            get => Target.Position - EntryOffset;
            set => Target.Position = EntryOffset + Math.Min(value, EntryLength);
        }
        EntryWrapper() : base(ClassInjector.DerivedConstructorPointer<EntryWrapper>()) =>
            ClassInjector.DerivedConstructorBody(this);
        internal EntryWrapper(Stream target, long offset, long length) : this() =>
            (Target, EntryOffset, EntryLength, target.Position) = (target, offset, length, offset);
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override int Read(Il2CppStructArray<byte> buffer, int offset, int count) =>
            Target.Read(buffer.AsSpan()
                .Slice(offset, Position >= EntryLength ? 0 : Position + count < EntryLength ? count : (int)(EntryLength - Position)));
        public override long Seek(long offset, Il2CppSystem.IO.SeekOrigin origin) =>
            Target.Seek(origin switch
            {
                Il2CppSystem.IO.SeekOrigin.End => EntryOffset + EntryLength + offset,
                Il2CppSystem.IO.SeekOrigin.Begin => EntryOffset + offset,
                Il2CppSystem.IO.SeekOrigin.Current => offset,
                _ => throw new NotImplementedException()
            }, SeekOrigin.Begin) - EntryOffset;

        public override void Write(Il2CppStructArray<byte> buffer, int offset, int count) =>
            throw new NotImplementedException();
        public override void Flush() => Target.Flush();
        public override void Dispose() => Target.Dispose();
    }
    static partial class Hooks
    {
        const string BodyPrefabAB = "chara/body/body_00.unity3d";
        const string BodyPrefabM = "p_cm_sv_body_00";
        const string BodyPrefabF = "p_cf_sv_body_00";
        const string BodyTextureAB = "chara/body/bo_body_000_00.unity3d";
        const string BodyTexture = "cf_body_00_t";
#if Aicomi
        const string BodyShapeAnimeAB = "chara/list/customshape.unity3d";
#else
        const string BodyShapeAnimeAB = "list/customshape.unity3d";
#endif
        const string BodyShapeAnime = "cf_anmShapeBody";
        static bool PreventRedirect = false;
        static void EnableRedirect() =>
            PreventRedirect = false;
        static void DisableRedirect() =>
            PreventRedirect = true;
        static Func<NormalData, NormalData> BustNormalOverrideProc = IOExtension.ToBodyNormal;
        static Func<NormalData, NormalData> BustNormalOverrideSkip = normalData => normalData;
        static Func<NormalData, NormalData> BustNormalOverride = BustNormalOverrideSkip; 

        static void BustNormalInitializePrefix(ref NormalData normalData) =>
            (normalData = BustNormalOverride(normalData.With(DisableRedirect))).With(EnableRedirect);
#if Aicomi
        static void HumanBodyLoadPrefix(HumanBody __instance) =>
            BustNormalOverride = BustNormalOverrideProc.With(F.Apply(IOExtension.OverrideFigure, __instance._human));
#else
        static void HumanBodyLoadPrefix(HumanBody __instance) =>
            BustNormalOverride = BustNormalOverrideProc.With(F.Apply(IOExtension.OverrideFigure, __instance.human));
#endif
        static void HumanBodyLoadPostfix(HumanBody __instance) =>
            BustNormalOverride = BustNormalOverrideSkip.With(
                F.Apply(IOExtension.OverrideGraphic, __instance, __instance.GetRefObject(Table.RefObjKey.S_Son)));

        static void HumanBodyCreateBodyTexturePostfix(HumanBody __instance) =>
            IOExtension.OverrideColors(__instance, __instance.GetRefObject(Table.RefObjKey.S_Son));

        static void LoadAssetPostfix(AssetBundle __instance, string name, Il2CppSystem.Type type, ref UnityEngine.Object __result) =>
            __result = PreventRedirect ? __result : ((__instance.name, name).With(DisableRedirect) switch
            {
                (Plugin.AssetBundle, _) => ModPackage.ToAsset(name.Split(':'), type),
                (BodyPrefabAB, BodyPrefabM) => IOExtension.ToBodyPrefab(BodyPrefabM) ?? __result,
                (BodyPrefabAB, BodyPrefabF) => IOExtension.ToBodyPrefab(BodyPrefabF) ?? __result,
                (BodyTextureAB, BodyTexture) => IOExtension.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => IOExtension.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result).With(EnableRedirect);

        static void LoadAssetWithoutTypePostfix(AssetBundle __instance, string name, ref UnityEngine.Object __result) =>
            __result = PreventRedirect ? __result : ((__instance.name, name).With(DisableRedirect) switch
            {
                (Plugin.AssetBundle, _) => ModPackage.ToAsset(name.Split(':')),
                (BodyPrefabAB, BodyPrefabM) => IOExtension.ToBodyPrefab(BodyPrefabM) ?? __result,
                (BodyPrefabAB, BodyPrefabF) => IOExtension.ToBodyPrefab(BodyPrefabF) ?? __result,
                (BodyTextureAB, BodyTexture) => IOExtension.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => IOExtension.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result).With(EnableRedirect);

        static Dictionary<string, MethodInfo[]> Prefixes => new()
        {
            [nameof(BustNormalInitializePrefix)] = [
                typeof(BustNormal).GetMethod(nameof(BustNormal.Initialize), 0, [typeof(GameObject), typeof(NormalData)])
            ],
            [nameof(HumanBodyLoadPrefix)] = [
                typeof(HumanBody).GetMethod(nameof(HumanBody.Load), 0, [typeof(Transform), typeof(string)])
            ]
        };
        static Dictionary<string, MethodInfo[]> Postfixes => new()
        {
            [nameof(LoadAssetPostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0, [typeof(string), typeof(Il2CppSystem.Type)])
            ],
            [nameof(HumanBodyLoadPostfix)] = [
                typeof(HumanBody).GetMethod(nameof(HumanBody.Load), 0, [typeof(Transform), typeof(string)])
            ],
            [nameof(HumanBodyCreateBodyTexturePostfix)] = [
                typeof(HumanBody).GetMethod(nameof(HumanBody.CreateBodyTexture), 0, [])
            ]
        };
        static void ApplyPrefixes(Harmony hi) =>
            Prefixes.Concat(SpecPrefixes).ForEach(entry => entry.Value.ForEach(method =>
                hi.Patch(method, prefix: new HarmonyMethod(typeof(Hooks), entry.Key) { wrapTryCatch = true })));
        static void ApplyPostfixes(Harmony hi) =>
            Postfixes.Concat(SpecPostfixes).ForEach(entry => entry.Value.ForEach(method =>
                hi.Patch(method, postfix: new HarmonyMethod(typeof(Hooks), entry.Key) { wrapTryCatch = true })));
        internal static IDisposable Initialize() =>
            Disposable.Create(new Harmony($"Hooks.{Plugin.Name}").With(ApplyPrefixes).With(ApplyPostfixes).UnpatchSelf);
    }

    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        public const string Name = "SardineTail";
        public const string Version = "2.2.0";
        public const string Guid = $"{Process}.{Name}";
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static ConfigEntry<bool> DevelopmentMode;
        internal static Plugin Instance;
        CompositeDisposable Subscriptions; 
        static Plugin() =>
            ClassInjector.RegisterTypeInIl2Cpp<EntryWrapper>();
        public override void Load() =>
            Subscriptions = [Hooks.Initialize(), ..CategoryExtension.Initialize(), ..Initialize()];
        public override bool Unload() =>
            true.With(Subscriptions.Dispose) && base.Unload();
    }
}