using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using Character;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Fishbone;
using KeysDefs = Il2CppSystem.Collections.Generic.IReadOnlyList<ChaListDefine.KeyType>;
using KeysList = Il2CppSystem.Collections.Generic.List<ChaListDefine.KeyType>;
using ValsList = Il2CppSystem.Collections.Generic.List<string>;
using PkgEntry = System.Tuple<string[], System.IO.FileInfo>;
using ZipEntry = System.Tuple<string[], System.IO.Compression.ZipArchiveEntry>;
using Values = System.Collections.Generic.Dictionary<ChaListDefine.KeyType, string>;
using Mods = System.Collections.Generic.IEnumerable<System.Tuple<ChaListDefine.KeyType, string>>;
using Mod = System.Tuple<ChaListDefine.KeyType, string>;
using Resolution = System.Tuple<string, Character.ListInfoBase>;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    enum Vtype { Name, Store, Asset, Image, Text }
    struct Entry
    {
        internal Entry(Ktype index, Vtype value) =>
            (Index, Value, Children, Default) = (index, value, [], []);
        internal Entry(Ktype index, Vtype value, string defs) =>
            (Index, Value, Children, Default) = (index, value, [], [defs]);
        internal Entry(Ktype index, params Ktype[] children) =>
            (Index, Value, Children, Default) = (index, Vtype.Store, children, [Plugin.AssetBundle]);
        internal Ktype Index { init; get; }
        internal Vtype Value { init; get; }
        internal Ktype[] Children { init; get; }
        internal string[] Default { init; get; }
    }
    struct Category
    {
        internal CatNo Index { init; get; }
        internal Entry[] Entries { init; get; }
    }
    internal static partial class CategoryExtensions
    {
        internal static Resolution Resolve(this Category category, string pkgId, IEnumerable<string> paths, Mods values) =>
            category.Resolve(pkgId, string.Join(Path.AltDirectorySeparatorChar, paths), category.Complete(paths.Last(), values));
        static Mods Complete(this Category category, string name, Mods values) =>
            category.Entries.Where(entry => entry.Value is Vtype.Name)
                .Select<Entry, Mod>(entry => new(entry.Index, name)).Concat(values)
                .Concat(category.Entries.Where(entry => entry.Default is not [])
                    .Select<Entry, Mod>(entry => new(entry.Index, entry.Default.First())))
                    .DistinctBy(mod => mod.Item1);
        static Resolution Resolve(this Category category, string pkgId, string modId, Mods values) =>
            !category.Entries.All(entry => values.Select(value => value.Item1).Contains(entry.Index)) ? null :
                new(modId, category.Resolve(values.ToDictionary(value => value.Item1, value => value.Item2).With(pkgId.Resolve(category))));
        static ListInfoBase Resolve(this Category category, Values values) =>
            new ListInfoBase((int)category.Index, 0, new KeysDefs(category.ResolveKeys().Pointer), category.ResolveValues(values));
        static KeysList ResolveKeys(this Category category) =>
            new KeysList().With(s => s.Add(Ktype.ID)).With(s => category.Entries.Do(entry => s.Add(entry.Index)));
        static ValsList ResolveValues(this Category category, Values values) =>
            new ValsList()
                .With(s => s.Add(category.Index.AssignId().ToString()))
                .With(s => category.Entries.Do(entry => s.Add(values[entry.Index])));
        static Action<Values> Resolve(this string pkgId, Category category) =>
            values => category.Entries.Where(entry => entry.Value is Vtype.Store).Do(entry => pkgId.Resolve(entry, values));
        static void Resolve(this string pkgId, Entry entry, Values values) =>
            values[entry.Index] = values[entry.Index] switch
            {
                "0" when values.ContainsKey(Ktype.MainData) && values[Ktype.MainData].Split(':').Length == 3 => "0"
                    .With(() => values[Ktype.MainData].Split(':')[1]
                    .With(path => entry.Children
                        .Where(child => !values[child].IsNullOrEmpty() && !values[child].Equals("0"))
                        .Do(child => values[child] = $"{pkgId}:{path}:{values[child]}"))),
                Plugin.AssetBundle => Plugin.AssetBundle
                    .With(() => entry.Children
                        .Where(child => pkgId.AssetBundleExists(values[child]))
                        .Do(child => values[child] = $"{pkgId}:{values[child]}")),
                var path when pkgId.AssetBundleExists(path) => Plugin.AssetBundle
                    .With(() => entry.Children.Do(child => values[child] = $"{pkgId}:{path}:{values[child]}")),
                var path => path
            };
        static Mods Collect(this Entry entry, IEnumerable<string> paths, string name) =>
            entry.Value switch
            {
                Vtype.Image => Path.GetFileNameWithoutExtension(name)
                    .Equals(entry.Index.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? [new(entry.Index, string.Join(Path.AltDirectorySeparatorChar, [..paths, name]))] : [],
                _ => []
            };
        static Mods Collect(this Entry entry, Values values) =>
            entry.Value switch
            {
                Vtype.Name => [],
                _ => values.TryGetValue(entry.Index, out var value) ? [new(entry.Index, value)] : []
            };
        internal static Mods Collect(this Category category, IEnumerable<string> paths, string name) =>
            category.Entries.SelectMany(entry => entry.Collect(paths, name));
        internal static Mods Collect(this Category category, Values values) =>
            category.Entries.SelectMany(entry => entry.Collect(values));
        static IEnumerable<PkgEntry> EnumerateImages(this DirectoryInfo info) =>
            info.EnumerateFiles("*.png", SearchOption.AllDirectories).Select<FileInfo, PkgEntry>(
                entry => new(Path.GetRelativePath(info.FullName, entry.FullName).Split(Path.DirectorySeparatorChar), entry));
        static IEnumerable<PkgEntry> EnumerateValues(this DirectoryInfo info) =>
            info.EnumerateFiles("values.json", SearchOption.AllDirectories).Select<FileInfo, PkgEntry>(
                entry => new(Path.GetRelativePath(info.FullName, entry.FullName).Split(Path.DirectorySeparatorChar), entry));
        static IEnumerable<PkgEntry> EnumerateEntries(this DirectoryInfo info) =>
            info.Exists ? info.EnumerateImages().Concat(info.EnumerateValues()) : [];
        internal static Dictionary<Category, IEnumerable<PkgEntry>> Categories(this string path) =>
            All.ToDictionary(category => category, category =>
                new DirectoryInfo(Path.Combine(path, category.Index.ToString())).EnumerateEntries());
        internal static Dictionary<Category, IEnumerable<ZipEntry>> Categories(this ZipArchive archive) =>
            archive.Entries.Select(entry => new ZipEntry(entry.FullName.Split(Path.AltDirectorySeparatorChar), entry))
                .GroupBy(entry => Enum.TryParse<CatNo>(entry.Item1[0], out var value)
                    ? All.Where(category => category.Index == value).FirstOrDefault(NoCategory) : NoCategory)
                    .Where(group => !group.Key.Equals(NoCategory))
                    .ToDictionary(group => group.Key, group => group.Select(entry => new ZipEntry(entry.Item1[1..], entry.Item2)));
        internal static IEnumerable<Resolution> LoadCsvValues(this Category category, string pkgId, string input) =>
            category.LoadCsvValues(pkgId, input.Split('\n'));
        static IEnumerable<Resolution> LoadCsvValues(this Category category, string pkgId, string[] lines) =>
            category.CheckCsvHeaders(lines[0].Trim().Split(',')) ?
                lines[1..].Select(line => category.LoadCsvColumns(pkgId, line.Trim().Split(','))).Where(item => item != null) : [];
        static bool CheckCsvHeaders(this Category category, string[] values) =>
            Enumerable.Range(0, category.Entries.Length).All(index =>
                category.Entries[index].Index.ToString().Equals(values[index].Trim(), StringComparison.OrdinalIgnoreCase));
        static Resolution LoadCsvColumns(this Category category, string pkgId, string[] values) =>
            values.Length != category.Entries.Count() ? null :
                category.Resolve(pkgId, Enumerable.Range(0, category.Entries.Length)
                    .Select(index => new Mod(category.Entries[index].Index, Normalize(values[index]))));
        static Resolution Resolve(this Category category, string pkgId, Mods mods) =>
            category.Resolve(pkgId, $"{category.Index}.csv/{mods.First(mod => mod.Item1 is Ktype.Name).Item2}", mods);
        internal static Action<ListInfoBase> RegisterMod(this Category category) =>
            info => Human.lstCtrl._table[category.Index].Add(info.Id, info);
    }
    internal static partial class CategoryNoExtensions
    {
        static readonly Dictionary<CatNo, int> Identities =
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => 100000000 + new System.Random().Next(100));
        internal static int AssignId(this CatNo categoryNo) => Identities[categoryNo]++;
        internal static CatNo ToCategoryNo(this ChaFileDefine.ClothesKind value) =>
            value switch
            {
                ChaFileDefine.ClothesKind.top => CatNo.co_top,
                ChaFileDefine.ClothesKind.bot => CatNo.co_bot,
                ChaFileDefine.ClothesKind.bra => CatNo.co_bra,
                ChaFileDefine.ClothesKind.shorts => CatNo.co_shorts,
                ChaFileDefine.ClothesKind.gloves => CatNo.co_gloves,
                ChaFileDefine.ClothesKind.panst => CatNo.co_panst,
                ChaFileDefine.ClothesKind.socks => CatNo.co_socks,
                ChaFileDefine.ClothesKind.shoes => CatNo.co_shoes,
                _ => throw new ArgumentException()
            };
        internal static CatNo ToCategoryNo(this ChaFileDefine.HairKind value) =>
             value switch
             {
                 ChaFileDefine.HairKind.back => CatNo.bo_hair_b,
                 ChaFileDefine.HairKind.front => CatNo.bo_hair_f,
                 ChaFileDefine.HairKind.side => CatNo.bo_hair_s,
                 ChaFileDefine.HairKind.option => CatNo.bo_hair_o,
                 _ => throw new ArgumentException()
             };
    }
    internal abstract class CategoryCollector<T>
    {
        internal Category Index;
        internal string PkgId;
        internal CategoryCollector(Category index, string pkgId) => (Index, PkgId) = (index, pkgId);
        internal abstract IEnumerable<Resolution> Collect(IEnumerable<string> paths, Mods mods, T entries);
        internal IEnumerable<Resolution> Collect(T entries) => Collect([Index.Index.ToString()], [], entries);
    }
    internal class DirectoryCollector : CategoryCollector<IEnumerable<PkgEntry>>
    {
        internal DirectoryCollector(Category category, string pkgId) : base(category, pkgId) { }
        Mods Process(IEnumerable<string> paths, FileInfo entry) =>
            "values.json".Equals(entry.Name, StringComparison.OrdinalIgnoreCase)
                ? Index.Collect(JsonSerializer.Deserialize<Values>(entry.OpenRead()))
                : Index.Collect(paths, entry.Name);
        IEnumerable<Resolution> Verify(IEnumerable<string> paths, Mods mods, IEnumerable<PkgEntry> entries) =>
             Index.Resolve(PkgId, paths, mods) switch
             {
                 var item when item != null => [item],
                 _ => entries.GroupBy(entry => entry.Item1[0]).Select(
                     group => Collect(paths.Concat([group.Key]), mods, group
                          .Select(entry => new PkgEntry(entry.Item1[1..], entry.Item2)))).SelectMany(items => items)
             };
        internal override IEnumerable<Resolution> Collect(IEnumerable<string> paths, Mods mods, IEnumerable<PkgEntry> entries) =>
            Verify(paths, entries.Where(entry => entry.Item1.Length == 1)
                .SelectMany(entry => Process(paths, entry.Item2)).Concat(mods).DistinctBy(item => item.Item1),
                    entries.Where(entry => entry.Item1.Length > 1));
    }
    internal class ArchiveCollector : CategoryCollector<IEnumerable<ZipEntry>>
    {
        internal ArchiveCollector(Category index, string pkgId) : base(index, pkgId) { }
        Mods Process(IEnumerable<string> paths, ZipArchiveEntry entry) =>
            "values.json".Equals(entry.Name, StringComparison.OrdinalIgnoreCase)
                ? Index.Collect(JsonSerializer.Deserialize<Values>(entry.Open()))
                : Index.Collect(paths, entry.Name);
        IEnumerable<Resolution> Verify(IEnumerable<string> paths, Mods mods, IEnumerable<ZipEntry> entries) =>
           Index.Resolve(PkgId, paths, mods) switch
           {
               var item when item != null => [item],
               _ => entries.GroupBy(entry => entry.Item1[0]).Select(
                    group => Collect(paths.Concat([group.Key]), mods, group
                       .Select(entry => new ZipEntry(entry.Item1[1..], entry.Item2)))).SelectMany(items => items)
           };
        internal override IEnumerable<Resolution> Collect(IEnumerable<string> paths, Mods mods, IEnumerable<ZipEntry> entries) =>
             Verify(paths, entries.Where(entry => entry.Item1.Length == 1)
                 .SelectMany(entry => Process(paths, entry.Item2)).Concat(mods).DistinctBy(item => item.Item1),
                     entries.Where(entry => entry.Item1.Length > 1));
    }
    public struct HardMigrationInfo
    {
        public string ModId { get; set; }
        public Version Version { get; set; }
    }
    internal abstract class ModPackage
    {
        internal const string HARD_MIGRATION = "hardmig.json";
        internal const string SOFT_MIGRATION = "softmig.json";
        internal string PkgId;
        internal Version PkgVersion;
        internal ModPackage(string pkgId, Version version) => (PkgId, PkgVersion) = (pkgId, version);
        internal Dictionary<Version, Dictionary<string, string>> SoftMigrations = new();
        string SoftMigration(ModInfo info) =>
            SoftMigrations.Where(entry => entry.Key < info.PkgVersion).OrderBy(entry => entry.Key)
                .Aggregate(info.ModId, (modId, entry) => entry.Value.GetValueOrDefault(modId, modId));
        internal readonly Dictionary<string, int> ModToId = new();
        internal void Register(Category category, string modId, ListInfoBase info) =>
            ModToId.TryAdd(modId, info.Id).Either(
                () => Plugin.Instance.Log.LogMessage($"duplicate mod id detected. {PkgId}:{modId}"),
                () => category.Index.RegisterIdToMod(info.With(category.RegisterMod()).Id, new ModInfo
                {
                    PkgVersion = PkgVersion,
                    PkgId = PkgId,
                    ModId = modId,
                    Category = category.Index,
                })
            );

        internal void LoadHardMigration(Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> info) =>
            info.Do(entry => entry.Value.Do(subentry =>
                ModPackageExtensions.Register(entry.Key, subentry.Key, new ModInfo()
                {
                    PkgVersion = subentry.Value.Version,
                    PkgId = PkgId,
                    ModId = subentry.Value.ModId,
                    Category = entry.Key,
                })));
        internal int ToId(ModInfo info, int id) => ModToId.GetValueOrDefault(SoftMigration(info), id);
        internal readonly Dictionary<string, AssetBundle> Cache = new();
        internal void Unload() =>
            Cache.With(cache => cache.Values.Do(item => item.Unload(true))).Clear();
        internal UnityEngine.Object GetAsset(string bundle, string asset, Il2CppSystem.Type type) =>
            GetAssetBundle(bundle).LoadAsset(asset, type);
        internal Texture2D GetTexture(string path) =>
            LoadTexture(path)?.With(WrapMode(Path.GetFileNameWithoutExtension(path)));
        internal Texture2D ToTexture(byte[] bytes) =>
            new Texture2D(256, 256).With(t2d => ImageConversion.LoadImage(t2d, bytes));
        Action<Texture2D> WrapMode(string path) =>
            Path.GetFileNameWithoutExtension(path) switch
            {
                "MainTex" => _ => { }
                ,
                _ => t2d => t2d.With(t2d => t2d.wrapMode = t2d.wrapModeU = t2d.wrapModeV = t2d.wrapModeW = TextureWrapMode.Clamp)
            };
        internal AssetBundle GetAssetBundle(string path) =>
            Cache.GetValueOrDefault(path) ?? (Cache[path] = LoadAssetBundle(path));
        internal abstract void Initialize();
        internal abstract bool ResourceExists(string path);
        internal abstract AssetBundle LoadAssetBundle(string path);
        internal abstract Texture2D LoadTexture(string path);
    }
    internal class DirectoryPackage : ModPackage
    {
        string PkgPath;
        internal DirectoryPackage(string pkgId, Version version, string path) : base(pkgId, version) => PkgPath = path;
        internal override bool ResourceExists(string path) =>
            File.Exists(Path.Combine(PkgPath, path));
        internal override AssetBundle LoadAssetBundle(string path) =>
            File.Exists(Path.Combine(PkgPath, path)) ? AssetBundle.LoadFromFile(Path.Combine(PkgPath, path)) : new AssetBundle();
        internal override Texture2D LoadTexture(string path) =>
            File.Exists(Path.Combine(PkgPath, path)) ? ToTexture(File.ReadAllBytes(Path.Combine(PkgPath, path))) : null;
        void LoadHardMigration(string path) =>
            File.Exists(path).Maybe(() => LoadHardMigration(JsonSerializer
                .Deserialize<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>(File.ReadAllText(path))));
        void LoadSoftMigration(string path) =>
            File.Exists(path).Maybe(() => SoftMigrations =
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path))
                    .Where(item => Version.TryParse(item.Key, out var _))
                    .ToDictionary(item => Version.Parse(item.Key), item => item.Value));
        void LoadCsvFile(Category category, string path) =>
            File.Exists(path).Maybe(() => category.LoadCsvValues(PkgId, File.ReadAllText(path))
                .Do(value => Register(category, value.Item1, value.Item2)));
        void LoadCsvFiles() =>
            CategoryExtensions.All.Do(category =>
                LoadCsvFile(category, Path.Combine(PkgPath, $"{category.Index}.csv")));
        internal override void Initialize() =>
            PkgPath
                .With(() => LoadSoftMigration(Path.Combine(PkgPath, SOFT_MIGRATION)))
                .With(() => LoadHardMigration(Path.Combine(PkgPath, HARD_MIGRATION)))
                .With(LoadCsvFiles)
                .Categories().Do(entry => new DirectoryCollector(entry.Key, PkgId)
                    .Collect(entry.Value).Do(value => Register(entry.Key, value.Item1, value.Item2)));
    }
    internal class ArchivePackage : ModPackage
    {
        string ArchivePath;
        internal ArchivePackage(string pkgId, Version version, string path) : base(pkgId, version) => ArchivePath = path;
        internal override bool ResourceExists(string path) =>
            ZipFile.OpenRead(ArchivePath).GetEntry(Path.Combine(path)) != null;
        internal override AssetBundle LoadAssetBundle(string path) =>
            LoadAssetBundle(ZipFile.OpenRead(ArchivePath).GetEntry(path));
        internal override Texture2D LoadTexture(string path) =>
            LoadTexture(ZipFile.OpenRead(ArchivePath).GetEntry(path));
        AssetBundle LoadAssetBundle(ZipArchiveEntry entry) =>
            entry != null ? AssetBundle.LoadFromMemory(ToBytes(entry)) : new AssetBundle();
        Texture2D LoadTexture(ZipArchiveEntry entry) =>
            entry != null ? ToTexture(ToBytes(entry)) : null;
        byte[] ToBytes(ZipArchiveEntry entry) =>
            new BinaryReader(entry.Open()).ReadBytes((int)entry.Length);
        void LoadHardMigration(ZipArchiveEntry entry) =>
            (entry != null).Maybe(() => LoadHardMigration(JsonSerializer
                .Deserialize<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>(entry.Open())));
        void LoadSoftMigration(ZipArchiveEntry entry) =>
            (entry != null).Maybe(() => SoftMigrations =
                JsonSerializer.Deserialize<Dictionary<Version, Dictionary<string, string>>>(entry.Open()));
        void LoadCsvFile(Category category, ZipArchiveEntry entry) =>
            (entry != null).Maybe(() => category.LoadCsvValues(PkgId, System.Text.Encoding.UTF8.GetString(ToBytes(entry)))
                .Do(value => Register(category, value.Item1, value.Item2)));
        void LoadCsvFiles(ZipArchive archive) =>
            CategoryExtensions.All.Do(category =>
                LoadCsvFile(category, archive.GetEntry($"{category.Index}.csv")));
        internal override void Initialize() =>
            ZipFile.OpenRead(ArchivePath)
                .With(archive => LoadSoftMigration(archive.GetEntry(SOFT_MIGRATION)))
                .With(archive => LoadHardMigration(archive.GetEntry(HARD_MIGRATION)))
                .With(LoadCsvFiles)
                .Categories().Do(entry => new ArchiveCollector(entry.Key, PkgId)
                    .Collect(entry.Value).Do(value => Register(entry.Key, value.Item1, value.Item2)));
    }
    internal static class ModPackageExtensions
    {
        internal static bool AssetBundleExists(this string pkgId, string path) =>
            Packages[pkgId].ResourceExists(path);
        internal static ModInfo TranslateSoftMods(this CatNo categoryNo, int id) =>
            IdToMod.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var mod) ? mod : null;
        internal static ModInfo TranslateHardMods(this CatNo categoryNo, int id) =>
            HardMigrations.TryGetValue(categoryNo, out var mods) ? mods.GetValueOrDefault(id) : null;
        static Dictionary<CatNo, Dictionary<int, ModInfo>> IdToMod = new();
        internal static void RegisterIdToMod(this CatNo categoryNo, int id, ModInfo mod) =>
            (IdToMod[categoryNo] = IdToMod.TryGetValue(categoryNo, out var mods) ? mods : new()).Add(id, mod);
        static IEnumerable<Version> ToVersion(this string[] items) =>
            items.Length switch
            {
                0 => [],
                1 => [],
                _ => Version.TryParse(items[^1], out var version) ? [version] : [],
            };
        static string DirectoryId(string path) =>
            string.Join('-', Path.GetRelativePath(Plugin.DevelopmentPath, path).Split('-')[0..^1]);
        static string ArchiveId(string path) =>
            string.Join('-', Path.GetFileName(path).Split('-')[0..^1]);
        static IEnumerable<ModPackage> DirectoryToPackage(string path) =>
             Path.GetRelativePath(Plugin.DevelopmentPath, path).Split('-').ToVersion()
                .Select(version => new DirectoryPackage(DirectoryId(path), version, path));
        static IEnumerable<ModPackage> ArchiveToPackage(string path) =>
            Path.GetFileNameWithoutExtension(path).Split('-').ToVersion()
                .Select(version => new ArchivePackage(ArchiveId(path), version, path));
        static IEnumerable<ModPackage> ArchivePackages(string path) =>
            Directory.GetFiles(path).Where(IsArchivePackage)
                .SelectMany(ArchiveToPackage).Concat(Directory.GetDirectories(path).SelectMany(ArchivePackages));
        static IEnumerable<ModPackage> DirectoryPackages =>
            Plugin.DevelopmentMode.Value ?
                Directory.GetDirectories(Plugin.DevelopmentPath).SelectMany(DirectoryToPackage) : [];
        static bool IsArchivePackage(string path) =>
            ".stp".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        static Dictionary<string, ModPackage> Packages;
        internal static Dictionary<CatNo, Dictionary<int, ModInfo>> HardMigrations = new();
        internal static void Register(CatNo categoryNo, int id, ModInfo info) =>
            (HardMigrations.GetValueOrDefault(categoryNo) ?? (HardMigrations[categoryNo] = new())).TryAdd(id, info);
        internal static int ToId(this ModInfo info, CatNo categoryNo, int id) =>
            HardMigrations.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var mod)
                ? Packages[mod.PkgId].ToId(mod, id) : info == null ? id 
                : Packages.TryGetValue(info.PkgId, out var pkg) ? pkg.ToId(info, id)
                : id.With(() => Plugin.Instance.Log.LogMessage($"mod package missing: {info.PkgId}"));
        internal static void Initialize() =>
            ArchivePackages(Plugin.PackagePath).Concat(DirectoryPackages).GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last())
                .With(packages => Packages = packages)
                .Values.Do(item => item.Initialize());
        internal static UnityEngine.Object ToAsset(this Il2CppSystem.Type type, string[] items) =>
            items.Length switch
            {
                2 => Packages.GetValueOrDefault(items[0])?.GetTexture(Path.ChangeExtension(items[1], ".png")),
                3 => Packages.GetValueOrDefault(items[0])?.GetAsset(items[1], items[2], type),
                _ => null
            };
    }
    internal static class Hooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string), typeof(Il2CppSystem.Type))]
        static void LoadAssetPostfix(AssetBundle __instance, string name, Il2CppSystem.Type type, ref UnityEngine.Object __result) =>
            __result = !Plugin.AssetBundle.Equals(__instance.name) ? __result : type.ToAsset(name.Split(':')) ?? __result;
    }
    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        public const string Name = "SardineTail";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "0.9.0";
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static readonly string PackagePath = Path.Combine(Paths.GameRootPath, "sardines");
        internal static readonly string DevelopmentPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "packages");
        internal static readonly string ConversionsPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "hardmods");
        internal static Plugin Instance;
        internal static ConfigEntry<bool> DevelopmentMode;
        internal static ConfigEntry<bool> HardmodConversion;
        internal static ConfigEntry<bool> StructureConversion;
        private Harmony Patch;
        public override void Load() =>
            Patch = Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks")
                .With(() => Instance = this)
                .With(() => DevelopmentMode = Config.Bind("General", "Enable development package loading.", false))
                .With(() => HardmodConversion = Config.Bind("General", "Enable hardmod conversion at startup.", false))
                .With(() => StructureConversion = Config.Bind("General", "Convert hardmod into structured form.", false))
                .With(ModPackageExtensions.Initialize)
                .With(ModificationExtensions.Initialize)
                .With(CategoryExtensions.Initialize);
        public override bool Unload() =>
            true.With(Patch.UnpatchSelf) && base.Unload();
    }
}