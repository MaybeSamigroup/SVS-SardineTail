using HarmonyLib;
using BepInEx;
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
using Mods = System.Collections.Generic.Dictionary<ChaListDefine.KeyType, string>;
using Mod = System.Tuple<ChaListDefine.KeyType, string>;
using ZipEntry = System.Tuple<string[], System.IO.Compression.ZipArchiveEntry>;
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
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => 0);
        static readonly Dictionary<CatNo, IEnumerable<KeyTypeCollector>> Cache =
            Enum.GetValues<CatNo>().Select(Collectors)
                .Where(tuple => tuple.Item2.Count() > 0)
                .ToDictionary(item => item.Item1, item => item.Item2);
        static int AssignId(this CatNo categoryNo) => --Identities[categoryNo];
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
        CatNo Index;
        string PkgId;
        internal CategoryCollector(CatNo index, string pkgId) => (Index, PkgId) = (index, pkgId);
        internal IEnumerable<Mod> CollectMods(Mods mods) => Index.Collect(PkgId, mods);
        internal IEnumerable<Mod> CollectMods(string path) => Index.Collect(PkgId, path);
        internal bool Resolve(IEnumerable<string> modId, IEnumerable<Mod> mods) =>
            Index.Resolve(mods).With(result => result.Maybe(() => ModPackageExtensions.Register(Index, PkgId, modId, ToListInfoBase(mods))));
        ListInfoBase ToListInfoBase(IEnumerable<Mod> mods) =>
            Index.ToListInfoBase(mods.ToDictionary(item => item.Item1, item => item.Item2));
        internal IEnumerable<Mod> Defaults => Index.Defaults();
        internal abstract void Collect(T input);
    }
    internal class DirectoryCollector : CategoryCollector<string>
    {
        string PkgRoot;
        internal DirectoryCollector(CatNo index, string pkgId) : base(index, pkgId) { }
        IEnumerable<Mod> Process(string path) =>
            CollectMods(Path.GetRelativePath(PkgRoot, path)).Concat(
                "values.json".Equals(Path.GetFileName(path), StringComparison.OrdinalIgnoreCase)
                    ? CollectMods(JsonSerializer.Deserialize<Mods>(File.ReadAllText(path))) : []);
        void Collect(string path, IEnumerable<Mod> mods) =>
            Verify(path, Directory.GetFiles(path).SelectMany(Process).Concat(mods).DistinctBy(item => item.Item1));
        void Verify(string path, IEnumerable<Mod> mods) =>
            (!Resolve(Path.GetRelativePath(PkgRoot, path).Split(Path.DirectorySeparatorChar), mods))
                .Maybe(() => Directory.GetDirectories(path).Do(subpath => Collect(subpath, mods)));
        internal override void Collect(string path) => Collect(path.With(() => PkgRoot = Path.GetDirectoryName(path)), Defaults);
    }
    internal class ArchiveCollector : CategoryCollector<IEnumerable<ZipEntry>>
    {
        internal ArchiveCollector(CatNo index, string pkgId) : base(index, pkgId) { }
        IEnumerable<Mod> Process(ZipEntry entry) =>
            CollectMods(entry.Item2.FullName).Concat(
                "values.json".Equals(entry.Item2.Name, StringComparison.OrdinalIgnoreCase)
                    ? CollectMods(JsonSerializer.Deserialize<Mods>(entry.Item2.Open())) : []);
        void Collect(IEnumerable<string> paths, IEnumerable<Mod> mods, IEnumerable<ZipEntry> entries) =>
            Verify(paths, entries.Where(entry => entry.Item1.Length == 1)
                .SelectMany(Process).Concat(mods).DistinctBy(item => item.Item1),
                    entries.Where(entry => entry.Item1.Length > 1));
        void Verify(IEnumerable<string> paths, IEnumerable<Mod> mods, IEnumerable<ZipEntry> entries) =>
            (!Resolve(paths, mods)).Maybe(() => entries.GroupBy(entry => entry.Item1[0])
                .Do(group => Collect(paths.Concat([group.Key]),
                    mods, group.Select(entry => new ZipEntry(entry.Item1[1..], entry.Item2)))));
        internal override void Collect(IEnumerable<ZipEntry> entries) => Collect([], Defaults, entries);
    }
    internal abstract class ModPackage
    {
        internal string PkgId;
        internal Version PkgVersion;
        internal ModPackage(string pkgId, Version version) => (PkgId, PkgVersion) = (pkgId, version);
        internal readonly Dictionary<string, AssetBundle> Cache = new();
        internal void Unload() =>
            Cache.With(cache => cache.Values.Do(item => item.Unload(true))).Clear();
        internal UnityEngine.Object GetAsset(string bundle, string asset) =>
            GetAssetBundle(bundle).LoadAsset(asset);
        internal Texture2D GetTexture(string path) =>
            ToTexture(LoadBytes(path)).With(WrapMode(path));
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
        internal abstract byte[] LoadBytes(string path);
    }
    internal class DirectoryPackage : ModPackage
    {
        string PkgPath;
        internal DirectoryPackage(string pkgId, Version version, string path) : base(pkgId, version) => PkgPath = path;
        internal override AssetBundle LoadAssetBundle(string path) =>
            AssetBundle.LoadFromFile(Path.Combine(PkgPath, path));
        internal override byte[] LoadBytes(string path) =>
            File.ReadAllBytes(Path.Combine(PkgPath, path));
        internal override void Initialize() =>
            PkgPath.Categories().Do(entry => new DirectoryCollector(entry.Key, PkgId).Collect(entry.Value));
    }
    internal class ArchivePackage : ModPackage
    {
        string ArchivePath;
        internal ArchivePackage(string pkgId, Version version, string path) : base(pkgId, version) => ArchivePath = path;
        internal override AssetBundle LoadAssetBundle(string path) =>
            AssetBundle.LoadFromMemory(LoadBytes(ZipFile.OpenRead(ArchivePath).GetEntry(path)));
        internal override byte[] LoadBytes(string path) =>
            LoadBytes(ZipFile.OpenRead(ArchivePath).GetEntry(path));
        internal byte[] LoadBytes(ZipArchiveEntry entry) =>
            new BinaryReader(entry.Open()).ReadBytes((int)entry.Length);
        internal override void Initialize() =>
            ZipFile.OpenRead(ArchivePath).Categories().Do(group => new ArchiveCollector(group.Key, PkgId).Collect(group));
    }
    internal static class ModPackageExtensions
    {
        internal static Dictionary<CatNo, Dictionary<string, ListInfoBase>> Mods = new();
        internal static void Register(CatNo categoryNo, string pkgId, IEnumerable<string> modId, ListInfoBase info) =>
            (Mods[categoryNo] = Mods.TryGetValue(categoryNo, out var mods) ? mods : new())
                .Add(string.Join(':', [pkgId, .. modId]).With(Plugin.Instance.Log.LogDebug), info);
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
            Directory.GetDirectories(Plugin.DevelopmentPath).SelectMany(DirectoryToPackage);
        static bool IsArchivePackage(string path) =>
            ".stp".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        static Dictionary<string, ModPackage> Packages;
        static void Initialize(this Dictionary<string, ModPackage> packages) =>
            (Packages = packages).Values.Do(item => item.Initialize());
        internal static void Initialize() =>
            ArchivePackages(Plugin.PackagePath).Concat(DirectoryPackages).GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last()).Initialize();
        internal static UnityEngine.Object ToAsset(this string[] items) =>
            items.Length switch
            {
                2 => Packages.GetValueOrDefault(items[0])?.GetTexture(items[1]),
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
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static readonly string PackagePath = Path.Combine(Paths.GameRootPath, "sardines");
        internal static readonly string DevelopmentPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "packages");
        internal static Plugin Instance;
        public const string Process = "SamabakeScramble";
        public const string Name = "SardineTail";
        public const string Guid = $"{Process}.{Name}";
        public const string Version = "0.1.0";
        private Harmony Patch;
        public override void Load() =>
            Patch = Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks")
                .With(() => Instance = this)
                .With(ModPackageExtensions.Initialize);
        public override bool Unload() =>
            true.With(Patch.UnpatchSelf) && base.Unload();
    }
}