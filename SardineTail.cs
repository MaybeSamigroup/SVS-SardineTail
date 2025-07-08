using HarmonyLib;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using UnityEngine;
using Character;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using CoastalSmell;
using Fishbone;
using CharaLimit = Character.HumanData.LoadLimited.Flags;
using CoordLimit = Character.HumanDataCoordinate.LoadLimited.Flags;
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
    public partial class ModInfo
    {
        public string PkgId { get; set; }
        public string ModId { get; set; }
        public CatNo Category { get; set; }
        public Version PkgVersion { get; set; }
    }
    public partial class HairsMods
    {
        public ModInfo HairGloss { get; set; }
        public Dictionary<ChaFileDefine.HairKind, ModInfo> Hairs { get; set; }
        internal partial void Apply(HumanDataHair data);
        internal static Func<HumanDataHair, HairsMods> ToMods;
    }
    public partial class ClothMods
    {
        public ModInfo Part { get; set; }
        public Dictionary<int, ModInfo> Paints { get; set; }
        public Dictionary<int, ModInfo> Patterns { get; set; }
        internal partial void Apply(ChaFileDefine.ClothesKind part, HumanDataClothes.PartsInfo data);
        internal static Func<HumanDataClothes, Dictionary<ChaFileDefine.ClothesKind, ClothMods>> ToMods;
    }
    public partial class AccessoryMods
    {
        public ModInfo Part { get; set; }
        public Dictionary<int, ModInfo> Patterns { get; set; }
        internal partial void Apply(HumanDataAccessory.PartsInfo data);
        internal static Func<HumanDataAccessory, Dictionary<int, AccessoryMods>> ToMods;
    }
    public partial class FaceMakeupMods
    {
        public ModInfo Eyeshadow { get; set; }
        public ModInfo Cheek { get; set; }
        public ModInfo Lip { get; set; }
        public Dictionary<int, ModInfo> Paints { get; set; }
        public Dictionary<int, ModInfo> Layouts { get; set; }
        internal partial void Apply(HumanDataFaceMakeup data);
        internal static Func<HumanDataFaceMakeup, FaceMakeupMods> ToMods;
    }
    public partial class BodyMakeupMods
    {
        public ModInfo Nail { get; set; }
        public ModInfo NailLeg { get; set; }
        public Dictionary<int, ModInfo> Paints { get; set; }
        public Dictionary<int, ModInfo> Layouts { get; set; }
        internal partial void Apply(HumanDataBodyMakeup data);
        internal static Func<HumanDataBodyMakeup, BodyMakeupMods> ToMods;
    }
    [BonesToStuck(Plugin.Name, "modifications.json")]
    public partial class CoordMods
    {
        public HairsMods Hairs { get; set; }
        public Dictionary<ChaFileDefine.ClothesKind, ClothMods> Clothes { get; set; }
        public Dictionary<int, AccessoryMods> Accessories { get; set; }
        public BodyMakeupMods BodyMakeup { get; set; }
        public FaceMakeupMods FaceMakeup { get; set; }
        internal partial void Apply(HumanDataCoordinate data);
        internal partial Action<HumanDataCoordinate> Apply(CoordLimit limits);
        internal partial void Save(ZipArchive archive);
        internal static Func<HumanDataCoordinate, CoordMods> ToMods;
        internal static Func<ZipArchive, CoordMods> Load;
    }
    [BonesToStuck(Plugin.Guid, "modifications.json")]
    public class LegacyCoordMods : CoordMods { }
    public partial class EyeMods
    {
        public ModInfo Eye { get; set; }
        public ModInfo Gradation { get; set; }
        public Dictionary<int, ModInfo> Highlights { get; set; }
        internal partial void Apply(HumanDataFace.PupilInfo data);
        internal static Func<HumanDataFace.PupilInfo, EyeMods> ToMods;
    }
    public partial class FaceMods
    {
        public ModInfo Head { get; set; }
        public ModInfo Detail { get; set; }
        public ModInfo Mole { get; set; }
        public ModInfo MoleLayout { get; set; }
        public ModInfo Nose { get; set; }
        public ModInfo LipLine { get; set; }
        public ModInfo Eyebrows { get; set; }
        public ModInfo Eyelid { get; set; }
        public ModInfo EyelineDown { get; set; }
        public ModInfo EyelineUp { get; set; }
        public ModInfo EyeWhite { get; set; }
        public Dictionary<int, EyeMods> Eyes { get; set; }
        internal partial void Apply(HumanDataFace data);
        internal static Func<HumanDataFace, FaceMods> ToMods;
    }
    public partial class BodyMods
    {
        public ModInfo Detail { get; set; }
        public ModInfo Sunburn { get; set; }
        public ModInfo Nip { get; set; }
        public ModInfo Underhair { get; set; }
        internal partial void Apply(HumanDataBody data);
        internal static Func<HumanDataBody, BodyMods> ToMods;
    }
    [BonesToStuck(Plugin.Name, "modifications.json")]
    public partial class CharaMods
    {
        public ModInfo Figure { get; set;  }
        public FaceMods Face { get; set; }
        public BodyMods Body { get; set; }
        public ModInfo Graphic { get; set; }
        public Dictionary<ChaFileDefine.CoordinateType, CoordMods> Coordinates { get; set; }
        internal partial void Apply(HumanData data);
        internal partial Action<HumanData> Apply(CharaLimit limits);
        internal partial void Save(ZipArchive archive);
        internal static Func<HumanData, CharaMods> ToMods;
        internal static Func<ZipArchive, CharaMods> Load;
    }
    [BonesToStuck(Plugin.Guid, "modifications.json")]
    public class LegacyCharaMods : CharaMods { }

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
                ? Index.Collect(entry.OpenRead().Parse<Values>())
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
                ? Index.Collect(entry.Open().Parse<Values>())
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
            LoadHardMigration(stream.Parse<Dictionary<CatNo, Dictionary<int, HardMigrationInfo>>>());
        internal void LoadSoftMigration(Stream stream) =>
            SoftMigrations = stream.Parse<Dictionary<string, Dictionary<string, string>>>()
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
        void LoadIfExists(string path, Action<Stream> action) =>
            File.Exists(path).Maybe(() => action(File.OpenRead(path)));
        void LoadHardMigration() =>
            LoadIfExists(Path.Combine(PkgPath, HARD_MIGRATION), LoadHardMigration);
        void LoadSoftMigration() =>
            LoadIfExists(Path.Combine(PkgPath, SOFT_MIGRATION), LoadSoftMigration); 
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
        void LoadIfExists(ZipArchiveEntry entry, Action<Stream> action) =>
            (entry != null).Maybe(() => action(entry.Open()));
        void LoadHardMigration(ZipArchive archive) =>
            LoadIfExists(archive.GetEntry(HARD_MIGRATION), LoadHardMigration);
        void LoadSoftMigration(ZipArchive archive) =>
            LoadIfExists(archive.GetEntry(SOFT_MIGRATION), LoadHardMigration);
        void LoadCsvFile(Category category, ZipArchiveEntry entry) =>
            (entry != null).Maybe(() => category.LoadCsvValues(PkgId, System.Text.Encoding.UTF8.GetString(ToBytes(entry)))
                .Do(value => Register(category, value.Item1, value.Item2)));
        void LoadCsvFiles(ZipArchive archive) =>
            CategoryExtensions.All.Do(category => LoadCsvFile(category, archive.GetEntry($"{category.Index}.csv")));
        byte[] ToBytes(ZipArchiveEntry entry) =>
            EntryToBytes((int)entry.Length).ApplyDisposable(entry.Open())
                .Try(Plugin.Instance.Log.LogMessage, out var value) ? value : [];
        Func<Stream, byte[]> EntryToBytes(int length) =>
            stream => new BinaryReader(stream).ReadBytes(length);
        internal override void Initialize() =>
            F.ApplyDisposable(Initialize, ZipFile.OpenRead(ArchivePath)).Try(Plugin.Instance.Log.LogMessage);
        internal void Initialize(ZipArchive archive) =>
             archive.With(LoadHardMigration).With(LoadSoftMigration).With(LoadCsvFiles)
                .Categories().Do(entry => new ArchiveCollector(entry.Key, PkgId)
                    .Collect(entry.Value).Do(value => Register(entry.Key, value.Item1, value.Item2)));
    }
    internal static partial class ModPackageExtensions
    {
        internal static T Parse<T>(this Stream stream) where T : new() =>
            Json<T>.Deserialize.ApplyDisposable(stream)
                .Try(Plugin.Instance.Log.LogMessage, out var value) ? value : new();
        internal static bool ResourceExists(this string pkgId, string path) =>
            Packages[pkgId].ResourceExists(path);
        static Dictionary<CatNo, Dictionary<int, ModInfo>> IdToMod = new();
        internal static void RegisterIdToMod(this CatNo categoryNo, int id, ModInfo mod) =>
            (IdToMod[categoryNo] = IdToMod.TryGetValue(categoryNo, out var mods) ? mods : new()).Add(id, mod);
        internal static ModInfo FromId(this CatNo categoryNo, int id) =>
            IdToMod.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var mod) ? mod : null;
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
                Directory.GetDirectories(path).SelectMany(DirectoryToPackage(path)) : [];
        static bool IsArchivePackage(string path) =>
            ".stp".Equals(Path.GetExtension(path), StringComparison.OrdinalIgnoreCase);
        static Dictionary<string, ModPackage> Packages = new();
        internal static Dictionary<CatNo, Dictionary<int, ModInfo>> HardMigrations = new();
        internal static void Register(CatNo categoryNo, int id, ModInfo info) =>
            (HardMigrations.GetValueOrDefault(categoryNo) ?? (HardMigrations[categoryNo] = new())).TryAdd(id, info);
        internal static int ToId(this CatNo categoryNo, ModInfo info, int id) =>
            HardMigrations.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var mod)
                ? Packages[mod.PkgId].ToId(mod, id) : info == null ? id
                : Packages.TryGetValue(info.PkgId, out var pkg) ? pkg.ToId(info, id)
                : id.With(() => Plugin.Instance.Log.LogMessage($"mod package missing: {info.PkgId}"));
        internal static void InitializePackages(this string path) =>
            ArchivePackages(Path.Combine(path, "sardines"))
                .Concat(DirectoryPackages(Path.Combine(path, "UserData", "plugins", Plugin.Guid, "packages"))).GroupBy(item => item.PkgId)
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
        internal static bool BypassFigure = false;
        internal static int OverrideBodyId = -1;
        static UnityEngine.Object ToBodyAsset(string bundle, string asset, Il2CppSystem.Type type) =>
            Plugin.AssetBundle.Equals(bundle) ? asset.Split(':').ToAsset(type) : null;
        static UnityEngine.Object ToBodyAsset(ListInfoBase info, Ktype ab, Ktype data, Il2CppSystem.Type type) =>
            info != null && info.TryGetValue(ab, out var bundle) && info.TryGetValue(data, out var asset) ? ToBodyAsset(bundle, asset, type) : null;
    }
    static partial class Hooks
    {
        const string BodyPrefabAB = "chara/body/body_00.unity3d";
        const string BodyPrefabM = "p_cm_sv_body_00";
        const string BodyPrefabF = "p_cf_sv_body_00";
        const string BodyTextureAB = "chara/body/bo_body_000_00.unity3d";
        const string BodyTexture = "cf_body_00_t";
        const string BodyShapeAnimeAB = "list/customshape.unity3d";
        const string BodyShapeAnime = "cf_anmShapeBody";
        static void LoadAssetPostfix(AssetBundle __instance, string name, Il2CppSystem.Type type, ref UnityEngine.Object __result) =>
            __result = (__instance.name, name) switch
            {
                (Plugin.AssetBundle, _) => name.Split(':').ToAsset(type),
                (BodyPrefabAB, BodyPrefabM) => ModPackageExtensions.ToBodyPrefab() ?? __result,
                (BodyPrefabAB, BodyPrefabF) => ModPackageExtensions.ToBodyPrefab() ?? __result,
                (BodyTextureAB, BodyTexture) => ModPackageExtensions.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => ModPackageExtensions.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result;
        static void LoadAssetWithoutTypePostfix(AssetBundle __instance, string name, ref UnityEngine.Object __result) =>
            __result = (__instance.name, name) switch
            {
                (Plugin.AssetBundle, _) => name.Split(':').ToAsset(),
                (BodyPrefabAB, BodyPrefabM) => ModPackageExtensions.ToBodyPrefab() ?? __result,
                (BodyPrefabAB, BodyPrefabF) => ModPackageExtensions.ToBodyPrefab() ?? __result,
                (BodyTextureAB, BodyTexture) => ModPackageExtensions.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => ModPackageExtensions.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result;
        static void LoadAssetPrefix(string assetBundleName, ref string manifestAssetBundleName) =>
            manifestAssetBundleName = !Plugin.AssetBundle.Equals(assetBundleName) ? manifestAssetBundleName : "sv_abdata";
        static Dictionary<string, MethodInfo[]> Postfixes => new()
        {
            [nameof(LoadAssetPostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0,
                    [typeof(string), typeof(Il2CppSystem.Type)])
            ],
            [nameof(LoadAssetWithoutTypePostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0,
                    [typeof(string)])
            ],
        };
        static Dictionary<string, MethodInfo[]> Prefixes => new()
        {
            [nameof(LoadAssetPrefix)] = [
                typeof(AssetBundleManager).GetMethod(nameof(AssetBundleManager.LoadAsset), 0,
                    [typeof(string), typeof(string), typeof(Il2CppSystem.Type), typeof(string)]),
                typeof(AssetBundleManager).GetMethod(nameof(AssetBundleManager.LoadAssetAsync), 0,
                    [typeof(string), typeof(string), typeof(Il2CppSystem.Type), typeof(string)]),
                typeof(AssetBundleManager).GetMethod(nameof(AssetBundleManager.GetLoadedAssetBundle), 0,
                    [typeof(string), typeof(string)]),
                typeof(AssetBundleManager).GetMethod(nameof(AssetBundleManager.UnloadAssetBundle), 0,
                    [typeof(string), typeof(bool), typeof(string), typeof(bool)]),
            ],
        };
        static void ApplyPrefixes(Harmony hi) =>
            Prefixes.Concat(SpecPrefixes).ForEach(entry => entry.Value.ForEach(method =>
                hi.Patch(method, prefix: new HarmonyMethod(typeof(Hooks), entry.Key) { wrapTryCatch = true })));
        static void ApplyPostfixes(Harmony hi) =>
            Postfixes.Concat(SpecPostfixes).ForEach(entry => entry.Value.ForEach(method =>
                hi.Patch(method, postfix: new HarmonyMethod(typeof(Hooks), entry.Key) { wrapTryCatch = true })));
        public static void ApplyPatches(Harmony hi) =>
            hi.With(ApplyPrefixes).With(ApplyPostfixes);
    }
    public partial class Plugin : BasePlugin
    {
        public const string Name = "SardineTail";
        public const string Version = "1.1.0";
        public const string Guid = $"{Process}.{Name}";
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static Plugin Instance;
        internal static ConfigEntry<bool> DevelopmentMode;
        private Harmony Patch;
        public override bool Unload() =>
            true.With(Patch.UnpatchSelf) && base.Unload();
    }
}
