using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Character;
using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using CoastalSmell;
using Fishbone;
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
            Enum.GetValues<CatNo>().ToDictionary(item => item, item => ModInfo.MIN_ID + new System.Random().Next(0, 100));

        internal static int AssignId(this CatNo categoryNo) => Identities[categoryNo]++;

        static KeysList ResolveKeys(this Category category) =>
            new KeysList()
                .With(s => s.Add(Ktype.ID))
                .With(s => category.Entries.Do(entry => s.Add(entry.Index)));

        static ValsList ResolveValues(this Category category, Values values) =>
            new ValsList()
                .With(s => s.Add(category.Index.AssignId().ToString()))
                .With(s => category.Entries.Do(entry => s.Add(values[entry.Index])));

        internal static ListInfoBase Resolve(this Category category, Values values) =>
            new ListInfoBase((int)category.Index, 0, new KeysDefs(category.ResolveKeys().Pointer), category.ResolveValues(values));
    }
    internal abstract class CategoryCollector<T>
    {
        internal const string HARD_MIGRATION = "hardmig.json";
        internal const string SOFT_MIGRATION = "softmig.json";
        internal string PkgId;
        internal T Container;
        internal IEnumerable<ModNode<T>> Nodes;
        internal IEnumerable<ModLeaf<T>> Csvs;
        internal CategoryCollector(string pkgId, T container) =>
            ((PkgId, Container) = (pkgId, container)).With(Initialize);
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
            path is "0" ? "0" : Root.Exists(path) ? $"{Root.PkgId}:{path}" : path;
        internal string ResolveImage(string bundle, string path) =>
            path is "0" ? "0" : Root.Exists(path) ? $"{Root.PkgId}:{path}" : $"{Root.PkgId}:{bundle}:{path}";
        internal string ResolveAsset(string bundle, string path) =>
            path is "0" ? "0" : $"{Root.PkgId}:{bundle}:{path}:";
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
        internal DevCollector(string pkgId, string path) : base(pkgId, path) { }
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
        internal StpCollector(string pkgId, string path) : base(pkgId, new ZipArchive(File.OpenRead(path))) { }
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
        internal static readonly AssetBundle DefaultBundle = new();
        internal string PkgPath;
        internal string PkgId;
        internal Version PkgVersion;
        internal abstract void Initialize();
        internal abstract bool ResourceExists(string path);
        internal abstract byte[] ToBytes(string path);
        internal abstract string ToString(string path);
        internal abstract Stream ToStream(string path);
        internal abstract AssetBundle ToAssetBundle(string path);
        internal readonly Dictionary<string, int> ModToId = new();
        internal readonly Dictionary<string, AssetBundle> Cache = new();
        internal Dictionary<Version, Dictionary<string, string>> SoftMigrations = new();
        internal void LoadHardMigrations(Dictionary<CatNo, Dictionary<int, HardMigrationInfo>> info) =>
            info.Do(entry => entry.Value.Do(subentry =>
                RegisterHardMigration(entry.Key, subentry.Key, new ModInfo()
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
        AssetBundle GetAssetBundle(string path) =>
            Cache.TryGetValue(path, out var cache) ? cache : CacheAssetBundle(path);
        AssetBundle CacheAssetBundle(string path) =>
            ResourceExists(path) ? Cache[path] = ToAssetBundle(path) : DefaultBundle;
        UnityEngine.Object GetAsset(string bundle, string asset, Il2CppSystem.Type type) =>
            GetAssetBundle(bundle).LoadAsset(asset, type);
        internal void Unload() =>
            Cache.With(cache => cache.Values.Do(item => item.Unload(true))).Clear();
        internal static Func<string[], IEnumerable<Version>> ToVersion =>
            items => items.Length switch
            {
                0 => [],
                1 => [],
                _ => Version.TryParse(items[^1], out var version) ? [version] : [],
            };
        static Dictionary<string, ModPackage> Packages = new();
        static Dictionary<CatNo, Dictionary<int, ModInfo>> IdToMod = new();
        static Dictionary<CatNo, Dictionary<int, ModInfo>> HardMigrations = new();
        static Action<CatNo, int, ModInfo> RegisterIdToMod =
            (categoryNo, id, mod) => (IdToMod[categoryNo] = IdToMod.TryGetValue(categoryNo, out var mods) ? mods : new()).Add(id, mod);
        static Action<CatNo, int, ModInfo> RegisterHardMigration =
            (categoryNo, id, info) => (HardMigrations.GetValueOrDefault(categoryNo) ?? (HardMigrations[categoryNo] = new())).TryAdd(id, info);
        internal static Func<string, string, bool> Exists =
            (pkgId, path) => Packages.TryGetValue(pkgId, out var pkg) && pkg.ResourceExists(path);
        internal static Func<CatNo, int, ModInfo> FromId =
            (categoryNo, id) => IdToMod.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var mod) ? mod : null;
        internal static Func<CatNo, ModInfo, int, int> ToId =
            (categoryNo, info, id) =>
                HardMigrations.TryGetValue(categoryNo, out var mods) && mods.TryGetValue(id, out var soft)
                    ? Packages[soft.PkgId].TranslateId(soft, id) : info?.PkgId == null ? id
                    : Packages.TryGetValue(info.PkgId, out var pkg) ? pkg.TranslateId(info, id)
                    : id.With(() => Plugin.Instance.Log.LogMessage($"mod package missing: {info.PkgId}"));
        internal static UnityEngine.Object ToAsset(string[] items, Il2CppSystem.Type type) =>
            items.Length switch
            {
                2 => Packages.TryGetValue(items[0], out var modPkg)
                    ? modPkg.GetTexture(Path.ChangeExtension(items[1], ".png")) : null,
                3 => Packages.TryGetValue(items[0], out var modPkg)
                    ? modPkg.GetAsset(items[1], items[2], type) : null,
                4 => Packages.TryGetValue(items[0], out var modPkg)
                    ? modPkg.GetAsset(items[1], items[2], type) : null,
                _ => null
            };
        internal static UnityEngine.Object ToAsset(string[] items) =>
            items.Length switch
            {
                2 => Packages.TryGetValue(items[0], out var modPkg)
                    ? modPkg.GetTexture(Path.ChangeExtension(items[1], ".png")) : null,
                3 => Packages.TryGetValue(items[0], out var modPkg)
                    ? modPkg.GetAsset(items[1], items[2], Il2CppInterop.Runtime.Il2CppType.Of<Texture2D>()) : null,
                4 => Packages.TryGetValue(items[0], out var modPkg)
                    ? modPkg.GetAsset(items[1], items[2], Il2CppInterop.Runtime.Il2CppType.Of<GameObject>()) : null,
                _ => null
            };
    }
    internal partial class DevPackage : ModPackage
    {
        internal override void Initialize() =>
            Initialize(new DevCollector(PkgId, PkgPath));
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
        internal override AssetBundle ToAssetBundle(string path) =>
            AssetBundle.LoadFromFile(Path.Combine(PkgPath, path));
        static Func<string, string, string> ToPkgId =
            (root, path) => string.Join('-', Path.GetRelativePath(root, path).Split('-')[0..^1]);
    }
    internal partial class StpPackage : ModPackage
    {
        internal ZipArchive Archive;
        internal override void Initialize() =>
            Initialize(new StpCollector(PkgId, PkgPath));
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
        internal override AssetBundle ToAssetBundle(string path) =>
            ToAssetBundle(Archive.GetEntry(path));
        AssetBundle ToAssetBundle(ZipArchiveEntry entry) =>
            entry == null ? DefaultBundle : entry.Length == entry.CompressedLength
                ? LoadAssetBundleFromStream(entry) : LoadAssetBundleFromBytes(entry);
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
        static int FigureId = -1;
        static string GameTag;
        internal static void OverrideFigure(Human human) =>
            (GameTag, FigureId) = (human.data.Tag, Extension.Chara<CharaMods, CoordMods>(human).FigureId);
        internal static long EntryOffset(this ZipArchiveEntry entry) =>
            (long)typeof(ZipArchiveEntry).GetProperty("OffsetOfCompressedData",
                 BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetValue(entry);
    }
    public class EntryWrapper : Il2CppSystem.IO.Stream
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
            Target.Read(((Span<byte>)buffer)
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
        public void SetLength(long value) =>
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
        const string BodyShapeAnimeAB = "list/customshape.unity3d";
        const string BodyShapeAnime = "cf_anmShapeBody";
        static bool PreventRedirect = false;
        static void EnableRedirect() =>
            PreventRedirect = false;
        static void DisableRedirect() =>
            PreventRedirect = true;
        static void HumanBodyLoadPrefix(HumanBody __instance) =>
            IOExtension.OverrideFigure(__instance.human);
        static void LoadAssetPostfix(AssetBundle __instance, string name, Il2CppSystem.Type type, ref UnityEngine.Object __result) =>
            __result = PreventRedirect ? __result : ((__instance.name, name).With(DisableRedirect) switch
            {
                (Plugin.AssetBundle, _) => ModPackage.ToAsset(name.Split(':'), type),
                (BodyPrefabAB, BodyPrefabM) => IOExtension.ToBodyPrefab() ?? __result,
                (BodyPrefabAB, BodyPrefabF) => IOExtension.ToBodyPrefab() ?? __result,
                (BodyTextureAB, BodyTexture) => IOExtension.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => IOExtension.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result).With(EnableRedirect);

        static void LoadAssetWithoutTypePostfix(AssetBundle __instance, string name, ref UnityEngine.Object __result) =>
            __result = PreventRedirect ? __result : ((__instance.name, name).With(DisableRedirect) switch
            {
                (Plugin.AssetBundle, _) => ModPackage.ToAsset(name.Split(':')),
                (BodyPrefabAB, BodyPrefabM) => IOExtension.ToBodyPrefab() ?? __result,
                (BodyPrefabAB, BodyPrefabF) => IOExtension.ToBodyPrefab() ?? __result,
                (BodyTextureAB, BodyTexture) => IOExtension.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => IOExtension.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result).With(EnableRedirect);
        static Dictionary<string, MethodInfo[]> Prefixes => new()
        {
            [nameof(HumanBodyLoadPrefix)] = [
                typeof(HumanBody).GetMethod(nameof(HumanBody.Load), 0, [typeof(Transform), typeof(string)])
            ]
        };
        static Dictionary<string, MethodInfo[]> Postfixes => new()
        {
            [nameof(LoadAssetPostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0, [typeof(string), typeof(Il2CppSystem.Type)])
            ],
            [nameof(LoadAssetWithoutTypePostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0, [typeof(string)])
            ]
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
    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInDependency(VarietyOfScales.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        public const string Name = "SardineTail";
        public const string Version = "2.0.0";
        public const string Guid = $"{Process}.{Name}";
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static Plugin Instance;
        internal static ConfigEntry<bool> DevelopmentMode;
        private Harmony Patch;
        static Plugin() =>
            ClassInjector.RegisterTypeInIl2Cpp<EntryWrapper>();
        public override bool Unload() =>
            true.With(Patch.UnpatchSelf) && base.Unload();
    }
}