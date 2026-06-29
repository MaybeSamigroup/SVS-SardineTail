using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using UnityEngine;
#if Aicomi
using ILLGAMES.Unity;
#else
using ILLGames.Unity;
#endif
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;


namespace SardineTail
{
    public struct HardMigrationInfo
    {
        public string ModId { get; set; }
        public Version Version { get; set; }
    }

    file static class HardMigrations
    {
        static Dictionary<GameId, Dictionary<CatNo, Dictionary<int, ModInfo>>> Storage =
            Enum.GetValues<GameId>().ToDictionary(gameId => gameId,
                _ => Enum.GetValues<CatNo>().ToDictionary(cat => cat, cat => new Dictionary<int, ModInfo>()));

        internal static void Register(GameId gameId, CatNo categoryNo, int id, ModInfo mod) =>
            Storage[gameId][categoryNo][id] = mod;

        internal static bool TryGetValue(GameId gameId, CatNo categoryNo, int id, out ModInfo mod) =>
            Storage[gameId][categoryNo].TryGetValue(id, out mod);
    }
    public static partial class Packages
    {
        static Dictionary<GameId, Dictionary<string, ModPackage>> Storage =
            Enum.GetValues<GameId>().ToDictionary(gameId => gameId, _ => new Dictionary<string, ModPackage>());

        static Dictionary<GameId, Dictionary<CatNo, Dictionary<int, ModInfo>>> Indices =
            Enum.GetValues<GameId>().ToDictionary(gameId => gameId,
                _ => Enum.GetValues<CatNo>().ToDictionary(cat => cat, cat => new Dictionary<int, ModInfo>()));

        public static void Register(GameId gameId, string pkgId, ModPackage mods) =>
            Storage[gameId][pkgId] = mods;

        public static void Register(GameId gameId, CatNo categoryNo, int id, ModInfo mod) =>
            Indices[gameId][categoryNo][id] = mod;

        public static bool TryGetValue(GameId gameId, string pkgId, out ModPackage mods) =>
            Storage[gameId].TryGetValue(pkgId, out mods);

        public static bool TryGetValue(GameId gameId, CatNo categoryNo, int id, out ModInfo mod) =>
            Indices[gameId][categoryNo].TryGetValue(id, out mod);

        public static int TranslateId(GameId gameId, CatNo categoryNo, ModInfo mod, int id) =>
            HardMigrations.TryGetValue(gameId, categoryNo, id, out var mig)
                ? TryGetValue(gameId, mig.PkgId, out var migpkg)
                    ? migpkg.TranslateId(mig, id)
                    : id.With(() => Plugin.Instance.Log.LogMessage($"mod package missing ({gameId}): {mig.PkgId}"))
                : mod?.PkgId == null ? id
                : TryGetValue(gameId, mod.PkgId, out var modpkg)
                    ? modpkg.TranslateId(mod, id)
                    : id.With(() => Plugin.Instance.Log.LogMessage($"mod package missing ({gameId}): {mod.PkgId}"));

        internal static bool ResourceExists(GameId gameId, string pkgId, string path) =>
            TryGetValue(gameId, pkgId, out var pkg) && pkg.ResourceExists(path);

        internal static void InitializePackages(this GameId gameId, string path) =>
            StpPackage.Collect(gameId, Path.Combine(path, "sardines"))
                .Concat(DevPackage.Collect(gameId, Path.Combine(path, "UserData", "plugins", Plugin.Name, "packages")))
                .GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last())
                .ForEach(entry => Register(gameId, entry.Key, entry.Value.With(entry.Value.Initialize)));
    }

    internal static class AssetBundles
    {
        static Subject<UnityEngine.Object> PrefabLoad = new();

        static readonly Dictionary<string, AssetBundle> Cache = new()
        {
            [""] = new()
        };

        internal static bool TryGetValue(string id, out AssetBundle bundle) => Cache.TryGetValue(id, out bundle);

        internal static AssetBundle Register(string id, AssetBundle bundle) => Cache[id] = bundle;

        internal static string ToIdentity(Stream stream) =>
            F.ApplyDisposable(UnityFS.Extract, stream)
                .Try(Plugin.Instance.Log.LogError, out var results) ?
                    results.SelectMany(fs => fs.Identity).FirstOrDefault("") : "";

        internal static IObservable<GameObject> OnPrefabLoad => PrefabLoad.AsObservable().Select(obj => new GameObject(obj.Pointer));

        internal static NormalData ToNormalData(UnityEngine.Object asset) =>
            asset is null ? null : new NormalData(asset.Pointer);

        internal static NormalData ToNormalData(string[] items) =>
            items.Length switch
            {
                5 => Enum.TryParse<GameId>(items[0], out var gameId) && Packages.TryGetValue(gameId, items[1], out var pkg)
                    ? ToNormalData(pkg.GetAsset(items[2], items[3], Il2CppInterop.Runtime.Il2CppType.Of<NormalData>())) : null,
                _ => null
            };

        internal static UnityEngine.Object ToAsset(string[] items, Il2CppSystem.Type type) =>
            items.Length switch
            {
                3 => Enum.TryParse<GameId>(items[0], out var gameId) && Packages.TryGetValue(gameId, items[1], out var pkg)
                    ? pkg.GetTexture(Path.ChangeExtension(items[2], ".png")) : null,

                4 => Enum.TryParse<GameId>(items[0], out var gameId) && Packages.TryGetValue(gameId, items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], type) : null,

                5 => Enum.TryParse<GameId>(items[0], out var gameId) && Packages.TryGetValue(gameId, items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], type).With(PrefabLoad.OnNext) : null,

                _ => null
            };

        internal static UnityEngine.Object ToAsset(string[] items) =>
            items.Length switch
            {
                3 => Enum.TryParse<GameId>(items[0], out var gameId) && Packages.TryGetValue(gameId, items[1], out var pkg)
                    ? pkg.GetTexture(Path.ChangeExtension(items[2], ".png")) : null,

                4 => Enum.TryParse<GameId>(items[0], out var gameId) && Packages.TryGetValue(gameId, items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], Il2CppInterop.Runtime.Il2CppType.Of<Texture2D>()) : null,

                5 => Enum.TryParse<GameId>(items[0], out var gameId) && Packages.TryGetValue(gameId, items[1], out var pkg)
                    ? pkg.GetAsset(items[2], items[3], Il2CppInterop.Runtime.Il2CppType.Of<GameObject>()).With(PrefabLoad.OnNext) : null,

                _ => null
            };
    }

    public abstract partial class ModPackage
    {
        protected static readonly Texture2D DefaultTexture = new(0, 0);
        protected static IEnumerable<Version> ToVersion(string[] items) =>
            items.Length switch
            {
                0 => [],
                1 => [],
                _ => Version.TryParse(items[^1], out var version) ? [version] : [],
            };

        public GameId GameId { init; get; }

        public string PkgPath { init; get; }

        public string PkgId { init; get; }

        public Version PkgVersion { init; get; }

        internal abstract void Initialize();

        internal abstract bool ResourceExists(string path);

        internal abstract byte[] ToBytes(string path);

        internal abstract Stream ToStream(string path);

        internal abstract AssetBundle ToAssetBundle(string path);

        internal readonly Dictionary<string, int> ModToId = new();

        Dictionary<string, string> IdentityCache = new();

        internal Dictionary<Version, Dictionary<string, string>> SoftMigrations = new();

        internal ModPackage(GameId gameId, string path, string pkgId, Version version) =>
            (GameId, PkgPath, PkgId, PkgVersion) = (gameId, path, pkgId, version);

        internal void LoadHardMigrations(Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> info) =>
            info.ForEach(entry => entry.Value.ForEach(subentry =>
                HardMigrations.Register(GameId, entry.Key, subentry.Key, new ModInfo()
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

        internal string ToText(string path) =>
            Encoding.UTF8.GetString(ToBytes(path));

        internal Texture2D GetTexture(string path) =>
            ResourceExists(path) ? GetTexture(ToBytes(path)).With(WrapMode(path)) : DefaultTexture;

        internal UnityEngine.Object GetAsset(string bundle, string asset, Il2CppSystem.Type type) =>
            GetAssetBundle(bundle)?.LoadAsset(asset, type);

        Action<Texture2D> WrapMode(string path) =>
            WrapMode(ListInfoSpec.ToWrapModes(path.Split(Path.AltDirectorySeparatorChar)));

        Action<Texture2D> WrapMode((TextureWrapMode U, TextureWrapMode V, TextureWrapMode W) modes) =>
            t2d => (t2d.wrapModeU, t2d.wrapModeV, t2d.wrapModeW) = (modes.U, modes.V, modes.W);

        Texture2D GetTexture(byte[] bytes) =>
            new Texture2D(256, 256).With(t2d => ImageConversion.LoadImage(t2d, bytes));

        AssetBundle GetAssetBundle(string path) =>
            GetAssetBundle(path, AssetBundles.ToIdentity(ToStream(path)));

        string ToAssetBundleIdentity(string path) =>
            IdentityCache.TryGetValue(path, out var identity)
                ? identity : CacheAssetBundleIdentity(path);

        string CacheAssetBundleIdentity(string path) =>
            IdentityCache[path] = ResourceExists(path) ? AssetBundles.ToIdentity(ToStream(path)) : "";

        AssetBundle GetAssetBundle(string path, string identity) =>
            AssetBundles.TryGetValue(identity, out var cache) ? cache :
                AssetBundles.Register(identity, ToAssetBundle(path));
    }
    internal partial class DevPackage : ModPackage
    {
        DevPackage(GameId gameid, string path, string pkgId, Version version) : base(gameid, path, pkgId, version) { }

        static Func<GameId, string, string, Version, DevPackage> ToPackage =
            (gameId, root, path, version) => new DevPackage(gameId, path, ToPkgId(root, path), version);

        static Func<GameId, string, string, IEnumerable<DevPackage>> ToPackages =
            (gameId, root, path) => ToVersion(Path.GetRelativePath(root, path).Split('-'))
                .Select(ToPackage.Apply(gameId).Apply(root).Apply(path));

        internal static Func<GameId, string, IEnumerable<ModPackage>> Collect =
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

        internal override AssetBundle ToAssetBundle(string path) =>
            AssetBundle.LoadFromFile(Path.Combine(PkgPath, path));

        static Func<string, string, string> ToPkgId =
            (root, path) => string.Join('-', Path.GetRelativePath(root, path).Split('-')[0..^1]);
    }
    internal partial class StpPackage : ModPackage
    {
        internal ZipArchive Archive;

        StpPackage(GameId gameId, string path, string pkgId, Version version) : base(gameId, path, pkgId, version) { }

        static Func<GameId, string, Version, StpPackage> ToPackage =
            (gameId, path, version) => new StpPackage(gameId, path, ToPkgId(path), version);

        static Func<GameId, string, IEnumerable<ModPackage>> ToPackages =
            (gameId, path) => ToVersion(Path.GetFileNameWithoutExtension(path).Split('-'))
                .Select(ToPackage.Apply(gameId).Apply(path));

        internal static Func<GameId, string, IEnumerable<ModPackage>> Collect =
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

        internal override AssetBundle ToAssetBundle(string path) =>
            ToAssetBundle(Archive.GetEntry(path));

        AssetBundle ToAssetBundle(ZipArchiveEntry entry) =>
            entry.Length == entry.CompressedLength
                ? LoadAssetBundleFromStream(entry)
                : LoadAssetBundleFromBytes(entry);

        AssetBundle LoadAssetBundleFromStream(ZipArchiveEntry entry) =>
            AssetBundle.LoadFromStream(new ZipEntryWrapper(File.OpenRead(PkgPath), Offset(entry), entry.Length));

        AssetBundle LoadAssetBundleFromBytes(ZipArchiveEntry entry) =>
            AssetBundle.LoadFromMemory(ToBytes(entry));

        long Offset(ZipArchiveEntry entry) =>
            (long)typeof(ZipArchiveEntry).GetProperty("OffsetOfCompressedData",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetValue(entry);

        byte[] ToBytes(ZipArchiveEntry entry) =>
            EntryToBytes((int)entry.Length).ApplyDisposable(entry.Open())
                .Try(Plugin.Instance.Log.LogMessage, out var value) ? value : [];

        Func<Stream, byte[]> EntryToBytes(int length) =>
            stream => new BinaryReader(stream).ReadBytes(length);

        static Func<string, string> ToPkgId =>
            (path) => string.Join('-', Path.GetFileName(path).Split('-')[0..^1]);
    }
}