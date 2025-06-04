using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using UnityEngine;
using Character;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Encodings.Web;
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
                        .Where(child => !string.IsNullOrEmpty(values[child]) && !values[child].Equals("0"))
                        .Do(child => values[child] = $"{pkgId}:{path}:{values[child]}"))),
                Plugin.AssetBundle => Plugin.AssetBundle
                    .With(() => entry.Children
                        .Where(child => pkgId.ResourceExists(values[child]))
                        .Do(child => values[child] = $"{pkgId}:{values[child]}")),
                var path when pkgId.ResourceExists(path) => Plugin.AssetBundle
                    .With(() => entry.Children.Do(child => values[child] = $"{pkgId}:{path}:{values[child]}")),
                var path => path
            };
        static Mods Collect(this Entry entry, IEnumerable<string> paths, string name) =>
            entry.Value switch
            {
                Vtype.Image => Path.GetFileNameWithoutExtension(name)
                    .Equals(entry.Index.ToString(), StringComparison.OrdinalIgnoreCase)
                    ? [new(entry.Index, string.Join(Path.AltDirectorySeparatorChar, [.. paths, name]))] : [],
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
        static bool ValuesOrImage(ZipArchiveEntry entry) =>
           "values.json".Equals(entry.Name, StringComparison.OrdinalIgnoreCase) ||    
           ".png".Equals(Path.GetExtension(entry.Name), StringComparison.OrdinalIgnoreCase);
        internal static Dictionary<Category, IEnumerable<ZipEntry>> Categories(this ZipArchive archive) =>
            archive.Entries.Where(ValuesOrImage).Select(entry => new ZipEntry(entry.FullName.Split(Path.AltDirectorySeparatorChar), entry))
                .GroupBy(entry => Enum.TryParse<CatNo>(entry.Item1[0], out var value) ? value : CatNo.mt_dummy)
                .Where(group => All.Any(item => item.Index == group.Key))
                .ToDictionary(group => All.First(category => category.Index == group.Key),
                    group =>group.Select(entry => new ZipEntry(entry.Item1[1..], entry.Item2)));
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
        static string Normalize(string input) => string.IsNullOrEmpty(input) ? "0" : input;
    }
    internal static partial class CategoryNoExtensions
    {
        static readonly Dictionary<CatNo, int> Identities =
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => 100000000);
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
        internal abstract IEnumerable<Resolution> Collect(IEnumerable<string> paths, Mods mods, IEnumerable<T> entries);
        internal IEnumerable<Resolution> Collect(IEnumerable<T> entries) => Collect([Index.Index.ToString()], [], entries);
    }
    internal class DirectoryCollector : CategoryCollector<PkgEntry>
    {
        internal DirectoryCollector(Category category, string pkgId) : base(category, pkgId) { }
        Mods Process(IEnumerable<string> paths, FileInfo entry) =>
            "values.json".Equals(entry.Name, StringComparison.OrdinalIgnoreCase)
                ? Index.Collect(entry.OpenRead().TryWith(stream => stream.TryParse<Values>()))
                : Index.Collect(paths, entry.Name);
        IEnumerable<Resolution> Verify(IEnumerable<string> paths, Mods mods, IEnumerable<PkgEntry> entries) =>
           Index.Resolve(PkgId, paths, mods) switch
           {
               var item when item != null => [item],
               _ => entries.GroupBy(entry => entry.Item1[0], (path, group) => Collect(paths.Concat([path]),
                       mods, group.Select(entry => new PkgEntry(entry.Item1[1..], entry.Item2)))).SelectMany(items => items)
           };
        internal override IEnumerable<Resolution> Collect(IEnumerable<string> paths, Mods mods, IEnumerable<PkgEntry> entries) =>
            Verify(paths, entries.Where(entry => entry.Item1.Length == 1)
                .SelectMany(entry => Process(paths, entry.Item2)).Concat(mods).DistinctBy(item => item.Item1),
                    entries.Where(entry => entry.Item1.Length > 1));
    }
    internal class ArchiveCollector : CategoryCollector<ZipEntry>
    {
        internal ArchiveCollector(Category index, string pkgId) : base(index, pkgId) { }
        Mods Process(IEnumerable<string> paths, ZipArchiveEntry entry) =>
            "values.json".Equals(entry.Name, StringComparison.OrdinalIgnoreCase)
                ? Index.Collect(entry.Open().TryWith(stream => stream.TryParse<Values>()))
                : Index.Collect(paths, entry.Name);
        IEnumerable<Resolution> Verify(IEnumerable<string> paths, Mods mods, IEnumerable<ZipEntry> entries) =>
           Index.Resolve(PkgId, paths, mods) switch
           {
               var item when item != null => [item],
               _ => entries.GroupBy(entry => entry.Item1[0], (path, group) => Collect(paths.Concat([path]),
                       mods, group.Select(entry => new ZipEntry(entry.Item1[1..], entry.Item2)))).SelectMany(items => items)
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
            SoftMigrations.Where(entry => entry.Key > info.PkgVersion).OrderBy(entry => entry.Key)
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
        void LoadHardMigration(Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> info) =>
            info.Do(entry => entry.Value.Do(subentry =>
                ModPackageExtensions.Register(entry.Key, subentry.Key, new ModInfo()
                {
                    PkgVersion = subentry.Value.Version,
                    PkgId = PkgId,
                    ModId = subentry.Value.ModId,
                    Category = entry.Key,
                })));
        internal void LoadHardMigration(Stream stream) =>
            LoadHardMigration(stream.TryParse<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>());
        internal void LoadSoftMigration(Stream stream) =>
            SoftMigrations = stream.TryParse<Dictionary<string, Dictionary<string, string>>>()
                .Where(item => Version.TryParse(item.Key, out var _))
                .ToDictionary(item => Version.Parse(item.Key), item => item.Value);
        void NotifyMissingModInfo(ModInfo info) =>
            Plugin.Instance.Log.LogMessage($"mod info missing:{info.PkgId}:{info.ModId}:{info.PkgVersion}");
        internal int ToId(ModInfo info, int oldId) =>
            ModToId.TryGetValue(SoftMigration(info), out var newId) ? newId : oldId.With(() => NotifyMissingModInfo(info));
        internal readonly Dictionary<string, AssetBundle> Cache = new();
        internal void Unload() =>
            Cache.With(cache => cache.Values.Do(item => item.Unload(true))).Clear();
        internal UnityEngine.Object GetAsset(string bundle, string asset, Il2CppSystem.Type type) =>
            GetAssetBundle(bundle).LoadAsset(asset, type);
        internal UnityEngine.Object GetAsset(string bundle, string asset) =>
            GetAssetBundle(bundle).LoadAsset(asset);
        internal Texture2D GetTexture(string path) =>
            LoadTexture(path)?.With(path.Split(Path.AltDirectorySeparatorChar).WrapMode);
        internal Texture2D ToTexture(byte[] bytes) =>
            new Texture2D(256, 256).With(t2d => ImageConversion.LoadImage(t2d, bytes));
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
        void LoadHardMigration() =>
            Path.Combine(PkgPath, HARD_MIGRATION).With(path =>
                File.Exists(path).Maybe(() => File.OpenRead(path).TryWith(LoadHardMigration)));
        void LoadSoftMigration() =>
            Path.Combine(PkgPath, SOFT_MIGRATION).With(path =>
                File.Exists(path).Maybe(() => File.OpenRead(path).TryWith(LoadSoftMigration)));
        void LoadCsvFile(Category category, string path) =>
            File.Exists(path).Maybe(() => category.LoadCsvValues(PkgId, File.ReadAllText(path))
                .Do(value => Register(category, value.Item1, value.Item2)));
        void LoadCsvFiles() =>
            CategoryExtensions.All.Do(category =>
                LoadCsvFile(category, Path.Combine(PkgPath, $"{category.Index}.csv")));
        internal override void Initialize() =>
            PkgPath.With(LoadHardMigration).With(LoadSoftMigration).With(LoadCsvFiles)
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
            new BinaryReader(entry.Open()).TryWith(stream => stream.ReadBytes((int)entry.Length));
        void LoadHardMigration(ZipArchive archive) =>
            archive.GetEntry(HARD_MIGRATION).With(entry => (null != entry)
                .Maybe(() => entry.Open().TryWith(LoadHardMigration)));
        void LoadSoftMigration(ZipArchive archive) =>
            archive.GetEntry(SOFT_MIGRATION).With(entry => (null != entry)
                .Maybe(() => entry.Open().TryWith(LoadSoftMigration)));
        void LoadCsvFile(Category category, ZipArchiveEntry entry) =>
            (entry != null).Maybe(() => category.LoadCsvValues(PkgId, System.Text.Encoding.UTF8.GetString(ToBytes(entry)))
                .Do(value => Register(category, value.Item1, value.Item2)));
        void LoadCsvFiles(ZipArchive archive) =>
            CategoryExtensions.All.Do(category => LoadCsvFile(category, archive.GetEntry($"{category.Index}.csv")));
        internal override void Initialize() =>
            ZipFile.OpenRead(ArchivePath).TryWith(archive => archive
                .With(LoadHardMigration).With(LoadSoftMigration).With(LoadCsvFiles)
                .Categories().Do(entry => new ArchiveCollector(entry.Key, PkgId)
                    .Collect(entry.Value).Do(value => Register(entry.Key, value.Item1, value.Item2))));
    }
    internal static class ModPackageExtensions
    {
        internal static void TryWith<I>(this I input, Action<I> sideeffect) where I : IDisposable =>
            input.TryWith(input => true.With(() => sideeffect(input)));
        internal static O TryWith<I, O>(this I input, Func<I, O> sideeffect) where I : IDisposable
        {
            using (input) { return sideeffect(input); }
        }
        internal static readonly JsonSerializerOptions JsonOption = new JsonSerializerOptions()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
        internal static O TryParse<O>(this Stream input) where O : new()
        {
            try
            {
                return JsonSerializer.Deserialize<O>(input, JsonOption);
            }
            catch (Exception e)
            {
                Plugin.Instance.Log.LogMessage("Failed to parse json file:");
                Plugin.Instance.Log.LogMessage(e.StackTrace);
                return new();
            }
        }
        internal static bool ResourceExists(this string pkgId, string path) =>
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
        static string DirectoryId(string root, string path) =>
            string.Join('-', Path.GetRelativePath(root, path).Split('-')[0..^1]);
        static string ArchiveId(string path) =>
            string.Join('-', Path.GetFileName(path).Split('-')[0..^1]);
        static Func<string, IEnumerable<ModPackage>> DirectoryToPackage(string root) =>
             path => Path.GetRelativePath(root, path).Split('-').ToVersion()
                .Select(version => new DirectoryPackage(DirectoryId(root, path), version, path));
        static IEnumerable<ModPackage> ArchiveToPackage(string path) =>
            Path.GetFileNameWithoutExtension(path).Split('-').ToVersion()
                .Select(version => new ArchivePackage(ArchiveId(path), version, path));
        static IEnumerable<ModPackage> ArchivePackages(string path) =>
            Directory.GetFiles(path).Where(IsArchivePackage)
                .SelectMany(ArchiveToPackage).Concat(Directory.GetDirectories(path).SelectMany(ArchivePackages));
        static IEnumerable<ModPackage> DirectoryPackages(string path) =>
            Plugin.DevelopmentMode.Value ?
                Directory.GetDirectories(Path.Combine(path, "UserData", "plugins", Plugin.Guid, "packages"))
                    .SelectMany(DirectoryToPackage(path)) : [];
        static bool IsArchivePackage(string path) =>
            ".stp".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        static Dictionary<string, ModPackage> Packages = new ();
        internal static Dictionary<CatNo, Dictionary<int, ModInfo>> HardMigrations = new();
        internal static void Register(CatNo categoryNo, int id, ModInfo info) =>
            (HardMigrations.GetValueOrDefault(categoryNo) ?? (HardMigrations[categoryNo] = new())).TryAdd(id, info);
        internal static int ToId(this ModInfo info, CatNo categoryNo, int id) =>
            HardMigrations.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var mod)
                ? Packages[mod.PkgId].ToId(mod, id) : info == null ? id
                : Packages.TryGetValue(info.PkgId, out var pkg) ? pkg.ToId(info, id)
                : id.With(() => Plugin.Instance.Log.LogMessage($"mod package missing: {info.PkgId}"));
        internal static void InitializePackages(this string path) =>
            ArchivePackages(Path.Combine(path, "sardines")).Concat(DirectoryPackages(path)).GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last())
                .Select(entry => Packages[entry.Key] = entry.Value)
                .Do(item => item.Initialize());
        internal static UnityEngine.Object ToAsset(this string[] items, Il2CppSystem.Type type) =>
            items.Length switch
            {
                2 => Packages.GetValueOrDefault(items[0])?.GetTexture(Path.ChangeExtension(items[1], ".png")),
                3 => Packages.GetValueOrDefault(items[0])?.GetAsset(items[1], items[2], type),
                _ => null
            };
        internal static UnityEngine.Object ToAsset(this string[] items) =>
            items.Length switch
            {
                2 => Packages.GetValueOrDefault(items[0])?.GetTexture(Path.ChangeExtension(items[1], ".png")),
                3 => Packages.GetValueOrDefault(items[0])?.GetAsset(items[1], items[2]),
                _ => null
            };
    }
    static partial class Hooks
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string), typeof(Il2CppSystem.Type))]
        static void LoadAssetPostfix(AssetBundle __instance, string name, Il2CppSystem.Type type, ref UnityEngine.Object __result) =>
            __result = !Plugin.AssetBundle.Equals(__instance.name) ? __result : name.Split(':').ToAsset(type) ?? __result;
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AssetBundle), nameof(AssetBundle.LoadAsset), typeof(string))]
        static void LoadAssetWithoutTypePostfix(AssetBundle __instance, string name, ref UnityEngine.Object __result) =>
            __result = !Plugin.AssetBundle.Equals(__instance.name) ? __result : name.Split(':').ToAsset() ?? __result;
        [HarmonyPrefix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAsset), typeof(string), typeof(string), typeof(Il2CppSystem.Type), typeof(string))]
        [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.LoadAssetAsync), typeof(string), typeof(string), typeof(Il2CppSystem.Type), typeof(string))]
        [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.GetLoadedAssetBundle), typeof(string), typeof(string))]
        [HarmonyPatch(typeof(AssetBundleManager), nameof(AssetBundleManager.UnloadAssetBundle), typeof(string), typeof(bool), typeof(string), typeof(bool))]
        static void LoadAssetPrefix(string assetBundleName, ref string manifestAssetBundleName) =>
            manifestAssetBundleName = !Plugin.AssetBundle.Equals(assetBundleName) ? manifestAssetBundleName : "sv_abdata";
        
    }
    public partial class Plugin : BasePlugin
    {
        public const string Name = "SardineTail";
        public const string Version = "1.0.2";
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static Plugin Instance;
    }
}
