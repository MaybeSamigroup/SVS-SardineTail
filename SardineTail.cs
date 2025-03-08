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
using ZipEntry = System.Tuple<string[], System.IO.Compression.ZipArchiveEntry>;
using Mods = System.Collections.Generic.Dictionary<ChaListDefine.KeyType, string>;
using Mod = System.Tuple<ChaListDefine.KeyType, string>;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal class KeyTypeCollector
    {
        internal IEnumerable<Mod> Default { get; init; }
        internal Ktype KeyType { get; init; }
        internal KeyTypeCollector(Ktype keyType) => (KeyType, Default) = (keyType, []);
        internal KeyTypeCollector(Ktype keyType, string value) => (KeyType, Default) = (keyType, [new(keyType, value)]);
        internal virtual IEnumerable<Mod> Apply(string pkgId, string path) => [];
        internal virtual IEnumerable<Mod> Apply(string pkgId, Mods values) => [];
    }
    internal class Name : KeyTypeCollector
    {
        internal Name() : base(Ktype.Name) { }
        internal override IEnumerable<Mod> Apply(string _, string path) =>
            GetName(Path.GetDirectoryName(path));
        IEnumerable<Mod> GetName(string path) =>
            GetName(Path.GetDirectoryName(path), path);
        IEnumerable<Mod> GetName(string parent, string path) =>
            parent.IsNullOrEmpty() ? [] : [new(KeyType, Path.GetRelativePath(parent, path))];
    }
    internal class Text : KeyTypeCollector
    {
        internal Text(Ktype keyType) : base(keyType) { }
        internal Text(Ktype keyType, string value) : base(keyType, value) { }
        internal override IEnumerable<Mod> Apply(string _, Mods values) =>
            values.TryGetValue(KeyType, out var value) ? [new(KeyType, value)] : [];
    }
    internal class Image : KeyTypeCollector
    {
        internal Image(Ktype keyType) : base(keyType) { }
        internal Image(Ktype keyType, string value) : base(keyType, value) { }
        internal override IEnumerable<Mod> Apply(string pkgId, string path) =>
            Path.GetFileName(path).ToLower().Equals($"{KeyType.ToString().ToLower()}.png")
                ? [new(KeyType, $"{pkgId}:{path}")] : [];
        internal override IEnumerable<Mod> Apply(string pkgId, Mods values) =>
            values.TryGetValue(KeyType, out var value) ? [new(KeyType, ':' == value[0] ? value[1..] : $"{pkgId}:{value}")] : [];
    }
    internal class Asset : KeyTypeCollector
    {
        internal Asset(Ktype keyType) : base(keyType) { }
        internal override IEnumerable<Mod> Apply(string pkgId, Mods values) =>
            values.TryGetValue(KeyType, out var value) ? [new(KeyType, ':' == value[0] ? value[1..] : $"{pkgId}:{value}")] : [];
    }
    internal static partial class CategoryNoExtensions
    {
        static readonly Dictionary<CatNo, int> Identities =
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => -new System.Random().Next());
        static readonly Dictionary<CatNo, IEnumerable<KeyTypeCollector>> Cache =
            Enum.GetValues<CatNo>().Select(Collectors)
                .Where(tuple => tuple.Item2.Count() > 0)
                .Where(tuple => ModPackageExtensions.HardMigrations.TryAdd(tuple.Item1, new ()))
                .ToDictionary(item => item.Item1, item => item.Item2);
        static int AssignId(this CatNo categoryNo) => --Identities[categoryNo];
       internal static CatNo ToCategoryNo(this ChaFileDefine.ClothesKind value) =>
            value switch {
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
            value switch {
                ChaFileDefine.HairKind.back => CatNo.bo_hair_b,
                ChaFileDefine.HairKind.front => CatNo.bo_hair_f,
                ChaFileDefine.HairKind.side => CatNo.bo_hair_s,
                ChaFileDefine.HairKind.option => CatNo.bo_hair_o,
                _ => throw new ArgumentException()
            };
 
        internal static IEnumerable<Mod> Defaults(this CatNo categoryNo) =>
            Cache[categoryNo].SelectMany(item => item.Default);
        internal static IEnumerable<Mod> Collect(this CatNo categoryNo, string pkgId, string path) =>
            Cache[categoryNo].SelectMany(item => item.Apply(pkgId, path));
        internal static IEnumerable<Mod> Collect(this CatNo categoryNo, string pkgId, Mods values) =>
            Cache[categoryNo].SelectMany(item => item.Apply(pkgId, values));
        internal static bool Resolve(this CatNo categoryNo, IEnumerable<Mod> mods) =>
            Cache[categoryNo].Count() == mods.Count();
        internal static ListInfoBase ToListInfoBase(this CatNo categoryNo, Mods mods) =>
            new ListInfoBase((int)categoryNo, 0,
                new KeysDefs(new KeysList()
                    .With(s => s.Add(Ktype.ID))
                    .With(s => Cache[categoryNo].Do(item => s.Add(item.KeyType))).Pointer),
                new ValsList()
                    .With(s => s.Add(categoryNo.AssignId().ToString()))
                    .With(s => Cache[categoryNo].Do(item => s.Add(mods[item.KeyType]))))
                    .With(info => Human.lstCtrl._table[categoryNo].Add(info.Id, info));
        internal static Dictionary<CatNo, string> Categories(this string path) =>
            Directory.GetDirectories(path)
                .Select(subpath => Path.GetRelativePath(Path.GetDirectoryName(subpath), subpath))
                .GroupBy(name => Enum.TryParse<CatNo>(name, out var value) ? value : CatNo.mt_dummy)
                .Where(group => Cache.ContainsKey(group.Key)).ToDictionary(group => group.Key, group => Path.Combine(path, group.First()));
        internal static IEnumerable<IGrouping<CatNo, ZipEntry>> Categories(this ZipArchive archive) =>
            archive.Entries.Select(entry => new ZipEntry(entry.FullName.Split(Path.AltDirectorySeparatorChar), entry))
                .GroupBy(entry => Enum.TryParse<CatNo>(entry.Item1[0], out var value) ? value : CatNo.mt_dummy)
                .Where(group => Cache.ContainsKey(group.Key));
    }
    internal abstract class CategoryCollector<T>
    {
        internal CatNo Index;
        internal string PkgId;
        internal CategoryCollector(CatNo index, string pkgId) => (Index, PkgId) = (index, pkgId);
        internal bool Resolve(IEnumerable<string> modId, IEnumerable<Mod> mods) =>
            Index.Resolve(mods).With(result => result.Maybe(() => ModPackageExtensions.Register(Index, PkgId, modId, ToListInfoBase(mods))));
        ListInfoBase ToListInfoBase(IEnumerable<Mod> mods) =>
            Index.ToListInfoBase(mods.ToDictionary(item => item.Item1, item => item.Item2));
        internal abstract void Collect(T input);
    }
    internal class DirectoryCollector : CategoryCollector<string>
    {
        string PkgRoot;
        internal DirectoryCollector(CatNo index, string pkgId) : base(index, pkgId) { }
        IEnumerable<Mod> Process(string path) =>
            Index.Collect(PkgId, Path.GetRelativePath(PkgRoot, path)).Concat(
                "values.json".Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase)
                    ? Index.Collect(PkgId, JsonSerializer.Deserialize<Mods>(File.ReadAllText(path))) : []);
        void Collect(string path, IEnumerable<Mod> mods) =>
            Verify(path, Directory.GetFiles(path).SelectMany(Process).Concat(mods).DistinctBy(item => item.Item1));
        void Verify(string path, IEnumerable<Mod> mods) =>
            (!Resolve(Path.GetRelativePath(PkgRoot, path).Split(Path.DirectorySeparatorChar), mods))
                .Maybe(() => Directory.GetDirectories(path).Do(subpath => Collect(subpath, mods)));
        internal override void Collect(string path) => Collect(path.With(() => PkgRoot = Path.GetDirectoryName(path)), Index.Defaults());
    }
    internal class ArchiveCollector : CategoryCollector<IEnumerable<ZipEntry>>
    {
        internal ArchiveCollector(CatNo index, string pkgId) : base(index, pkgId) { }
        IEnumerable<Mod> Process(ZipEntry entry) =>
            Index.Collect(PkgId, entry.Item2.FullName).Concat(
                "values.json".Equals(entry.Item2.Name, StringComparison.OrdinalIgnoreCase)
                    ? Index.Collect(PkgId, JsonSerializer.Deserialize<Mods>(entry.Item2.Open())) : []);
        void Collect(IEnumerable<string> paths, IEnumerable<Mod> mods, IEnumerable<ZipEntry> entries) =>
            Verify(paths, entries.Where(entry => entry.Item1.Length == 1)
                .SelectMany(Process).Concat(mods).DistinctBy(item => item.Item1),
                    entries.Where(entry => entry.Item1.Length > 1));
        void Verify(IEnumerable<string> paths, IEnumerable<Mod> mods, IEnumerable<ZipEntry> entries) =>
            (!Resolve(paths, mods)).Maybe(() => entries.GroupBy(entry => entry.Item1[0])
                .Do(group => Collect(paths.Concat([group.Key]),
                    mods, group.Select(entry => new ZipEntry(entry.Item1[1..], entry.Item2)))));
        internal override void Collect(IEnumerable<ZipEntry> entries) => Collect([], Index.Defaults(), entries);
    }
    internal enum HardMigrationInfo { ModId, Version }
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
        internal ModInfo Register(string modId, int id) => new ModInfo {
            PkgId = PkgId,
            PkgVersion = PkgVersion,
            ModId = modId.With(() => ModToId.Add(modId, id))
        };

        internal int ToId(ModInfo info, int id) => ModToId.GetValueOrDefault(SoftMigration(info), id);
        internal readonly Dictionary<string, AssetBundle> Cache = new();
        internal void Unload() =>
            Cache.With(cache => cache.Values.Do(item => item.Unload(true))).Clear();
        internal UnityEngine.Object GetAsset(string bundle, string asset) =>
            GetAssetBundle(bundle).LoadAsset(asset);
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
        internal abstract AssetBundle LoadAssetBundle(string path);
        internal abstract Texture2D LoadTexture(string path);
    }
    internal class DirectoryPackage : ModPackage
    {
        string PkgPath;
        internal DirectoryPackage(string pkgId, Version version, string path) : base(pkgId, version) => PkgPath = path;
        internal override AssetBundle LoadAssetBundle(string path) =>
            File.Exists(Path.Combine(PkgPath, path)) ? AssetBundle.LoadFromFile(Path.Combine(PkgPath, path)) : new AssetBundle();
        internal override Texture2D LoadTexture(string path) =>
            File.Exists(Path.Combine(PkgPath, path)) ? ToTexture(File.ReadAllBytes(Path.Combine(PkgPath, path))) : null;
        void LoadHardMigration(string path) =>
            File.Exists(path).Maybe(() => JsonSerializer
                .Deserialize<Dictionary<CatNo, Dictionary<int, Dictionary<HardMigrationInfo, string>>>>(File.ReadAllText(path))
                .Do(entry => ModPackageExtensions.HardMigrations.TryGetValue(entry.Key, out var mods).Maybe(() =>
                    entry.Value.Do(subentry => mods.TryAdd(subentry.Key, new ModInfo()
                    {
                        PkgId = PkgId,
                        PkgVersion = Version.Parse(subentry.Value[HardMigrationInfo.Version]),
                        ModId = subentry.Value[HardMigrationInfo.ModId],
                    })))));
        void LoadSoftMigration(string path) =>
            File.Exists(path).Maybe(() => SoftMigrations =
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(path))
                    .Where(item => Version.TryParse(item.Key, out var _))
                    .ToDictionary(item => Version.Parse(item.Key), item => item.Value));
        internal override void Initialize() =>
            PkgPath
                .With(() => LoadSoftMigration(Path.Combine(PkgPath, SOFT_MIGRATION)))
                .With(() => LoadHardMigration(Path.Combine(PkgPath, HARD_MIGRATION)))
                .Categories().Do(entry => new DirectoryCollector(entry.Key, PkgId).Collect(entry.Value));
    }
    internal class ArchivePackage : ModPackage
    {
        string ArchivePath;
        internal ArchivePackage(string pkgId, Version version, string path) : base(pkgId, version) => ArchivePath = path;
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
            (entry != null).Maybe(() => JsonSerializer
                .Deserialize<Dictionary<CatNo, Dictionary<int, Dictionary<HardMigrationInfo, string>>>>(entry.Open())
                .Do(entry => ModPackageExtensions.HardMigrations.TryGetValue(entry.Key, out var mods).Maybe(() =>
                    entry.Value.Do(subentry => mods.TryAdd(subentry.Key, new ModInfo()
                    {
                        PkgId = PkgId,
                        PkgVersion = Version.Parse(subentry.Value[HardMigrationInfo.Version]),
                        ModId = subentry.Value[HardMigrationInfo.ModId],
                    })))));
        void LoadSoftMigration(ZipArchiveEntry entry) =>
            (entry != null).Maybe(() => SoftMigrations =
                JsonSerializer.Deserialize<Dictionary<Version, Dictionary<string, string>>>(entry.Open()));
        internal override void Initialize() =>
            ZipFile.OpenRead(ArchivePath)
                .With(archive => LoadSoftMigration(archive.GetEntry(SOFT_MIGRATION)))
                .With(archive => LoadHardMigration(archive.GetEntry(HARD_MIGRATION)))
                .Categories().Do(group => new ArchiveCollector(group.Key, PkgId).Collect(group));
    }
    internal static class ModPackageExtensions
    {
        internal static void Register(CatNo categoryNo, string pkgId, IEnumerable<string> modId, ListInfoBase info) =>
            RegisterIdToMod(categoryNo, info.Id, Packages[pkgId].Register(string.Join(Path.AltDirectorySeparatorChar, modId), info.Id));
        internal static ModInfo ToModInfo(this ChaFileDefine.HairKind value, int id) =>
            value.ToCategoryNo().ToModInfo(id);
        internal static ModInfo ToModInfo(this ChaFileDefine.ClothesKind value, int id) =>
            value.ToCategoryNo().ToModInfo(id);
        internal static ModInfo ToModInfo(this HumanDataAccessory.PartsInfo value, int id) =>
            ((CatNo)value.type).ToModInfo(id);
        internal static ModInfo ToModInfo(this CatNo categoryNo, int id) =>
            id < 0 ? IdToMod[categoryNo].GetValueOrDefault(id) : null;
        static Dictionary<CatNo, Dictionary<int, ModInfo>> IdToMod = new();
        static void RegisterIdToMod(CatNo categoryNo, int id, ModInfo mod) =>
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
        internal static int ToId(this ModInfo info, CatNo categoryNo, int id) =>
            HardMigrations.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var mod)
                ? Packages[mod.PkgId].ToId(mod, id) : info != null ? Packages[info.PkgId].ToId(info, id) : id;
                
        internal static void Initialize() =>
            ArchivePackages(Plugin.PackagePath).Concat(DirectoryPackages).GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last())
                .With(packages => Packages = packages)
                .Values.Do(item => item.Initialize());
        internal static UnityEngine.Object ToAsset(this string[] items) =>
            items.Length switch
            {
                2 => Packages.GetValueOrDefault(items[0])?.GetTexture(Path.ChangeExtension(items[1], ".png")),
                3 => Packages.GetValueOrDefault(items[0])?.GetAsset(items[1], items[2]),
                _ => null
            };
    }
    internal static class Hooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string), typeof(Il2CppSystem.Type))]
        static void LoadAssetPostfix(AssetBundle __instance, string name, ref UnityEngine.Object __result) =>
            __result = !Plugin.AssetBundle.Equals(__instance.name) ? __result : name.Split(':').ToAsset() ?? __result;
    }
    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        public const string Name = "SardineTail";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "0.5.0";
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static readonly string PackagePath = Path.Combine(Paths.GameRootPath, "sardines");
        internal static readonly string DevelopmentPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "packages");
        internal static readonly string ConversionsPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "hardmods");
        internal static Plugin Instance;
        internal static ConfigEntry<bool> DevelopmentMode;
        internal static ConfigEntry<bool> HardmodConversion;
        private Harmony Patch;
        public override void Load() =>
            Patch = Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks")
                .With(() => Instance = this)
                .With(() => DevelopmentMode = Config.Bind("General", "Enable development package loading.", false))
                .With(() => HardmodConversion = Config.Bind("General", "Enable hardmod conversion at startup.", false))
                .With(ModPackageExtensions.Initialize)
                .With(ModificationExtensions.Initialize)
                .With(CategoryNoExtensions.Initialize);
        public override bool Unload() =>
            true.With(Patch.UnpatchSelf) && base.Unload();
    }
}