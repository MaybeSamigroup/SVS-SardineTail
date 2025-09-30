using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using System.Linq;
using System.Collections.Generic;
using Character;
using BepInEx;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

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
        public IEnumerable<string> ToMainAssetBundle() =>
            Values.TryGetValue(Ktype.MainAB, out var bundle) ? [bundle] : [];
    }

    class ConvertPackage
    {
        public List<string> Bundles;
        public List<ConvertEntry> Entries;
        public ConvertPackage(List<string> bundles, List<ConvertEntry> entries) =>
            (Bundles, Entries) = (bundles, entries);
        public ConvertPackage(Dictionary<string, string> manifestMap, ConvertEntry entry) :
            this(entry.Bundles.Where(bundle => !manifestMap.ContainsKey(bundle)).ToList(), [entry])
        { }
        public bool Intersect(ConvertPackage package) =>
            package.Bundles.Count == 0 ? Bundles.Count == 0 : package.Bundles.Any(Bundles.Contains);
        public ConvertPackage Merge(IEnumerable<ConvertPackage> packages) =>
            new(Bundles.Concat(packages.SelectMany(package => package.Bundles))
                .Distinct().ToList(), Entries.Concat(packages.SelectMany(package => package.Entries)).ToList());
        internal void Archive() =>
            F.ApplyDisposable(Archive, new ZipArchive(File.OpenWrite(PackagePath), ZipArchiveMode.Create)).Try(Plugin.Instance.Log.LogError);
        void Archive(ZipArchive archive) =>
            archive.With(ArchiveBundles).With(ArchiveEntries);
        void ArchiveBundles(ZipArchive archive) =>
            Bundles.ForEach(bundle => ArchiveBundle(archive, bundle));
        void ArchiveBundle(ZipArchive archive, string bundle) =>
            archive.CreateEntryFromFile(CategoryExtension.ToBundlePath(bundle), bundle, CompressionLevel.NoCompression);
        void ArchiveEntries(ZipArchive archive) =>
            ArchiveHardMigration(Entries.GroupBy(entry => entry.Category)
                .ToDictionary(group => group.Key, group => ArchiveEntries(group).Apply(archive)
                    .Try(Plugin.Instance.Log.LogError, out var hardmig) ? hardmig : new()))
                    .ApplyDisposable(archive.CreateEntry("hardmig.json").Open()).Try(Plugin.Instance.Log.LogError);
        Action<Stream> ArchiveHardMigration(Dictionary<CatNo, Dictionary<string, HardMigrationInfo>> hardmigs) =>
            stream => JsonSerializer.Serialize(stream, hardmigs, CategoryExtension.JsonOption);
        Func<ZipArchive, Dictionary<string, HardMigrationInfo>> ArchiveEntries(IGrouping<CatNo, ConvertEntry> group) =>
            archive => group.With(ProcessEntries(ToEntryWriter(CategoryExtension.All[group.Key].Entries, archive)))
                .Select(entry => new Tuple<string, HardMigrationInfo>(entry.Id.ToString(),
                    new() { ModId = $"{group.Key}/{entry.Name}", Version = new(0, 0, 0) })).ToDictionary();
        Action<ConvertEntry> ToEntryWriter(Entry[] entries, ZipArchive archive) =>
            ProcessEntry.Apply(archive).Apply(entries);
        Action<ZipArchive, Entry[], ConvertEntry> ProcessEntry => (archive, entries, mod) =>
            ArchiveValues(entries
                .Where(entry => mod.Values.ContainsKey(entry.Index))
                .Where(entry => !entry.Default.Contains(mod.Values[entry.Index]))
                .ToDictionary(entry => entry.Index, entry => mod.Values[entry.Index]))
                .ApplyDisposable(archive.CreateEntry($"{mod.Category}/{mod.Name}/values.json").Open())
                .Try(Plugin.Instance.Log.LogInfo);
        Action<Stream> ArchiveValues(Dictionary<Ktype, string> values) =>
            stream => JsonSerializer.Serialize(stream, values, CategoryExtension.JsonOption);
        Action<IEnumerable<ConvertEntry>> ProcessEntryNames =>
            entries => entries.ForEach(entry => entry.Name = ConvertEntryName(entry.Name));
        Func<string, string> ConvertEntryName =
            input => Path.GetInvalidPathChars()
                .Concat(Path.GetInvalidFileNameChars())
                .Aggregate(input, (str, ch) => str.Replace(ch, ' '));
        Action<IEnumerable<ConvertEntry>> ProcessEntries(Action<ConvertEntry> writer) =>
            entries => entries.With(ProcessEntryNames).GroupBy(entry => entry.Name)
                .ForEach(dups => Process(dups, PreprocessModId(dups.Count()), writer));
        void Process(IEnumerable<ConvertEntry> entries, Action<int, ConvertEntry> preprocess, Action<ConvertEntry> writer) =>
            entries.ForEachIndex((mod, idx) => mod.With(preprocess.Apply(idx) + writer));
        Action<int, ConvertEntry> PreprocessModId(int count) =>
            count == 1 ? ((_, mod) => mod.Values[Ktype.Name] = mod.Name)
                : ((idx, mod) => mod.Name = mod.Values[Ktype.Name] = $"{mod.Name}({idx})");
        public string PackagePath =>
            Bundles.Count == 0
                ? Path.Combine(Plugin.ConvertPath, "ILLGames.ListMods-0.0.0.stp")
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
                .Select(suffix => Path.Combine(Plugin.ConvertPath, $"{name}{suffix}-0.0.0.stp"))
                .Where(path => !Directory.Exists(path)).First();
    }
    internal static partial class CategoryExtension
    {
        internal static void Initialize() =>
            Plugin.HardmodConversion.Value.Maybe(Util<Manager.Game>.Hook.Apply(Convert).Apply(F.DoNothing));
        internal static readonly JsonSerializerOptions JsonOption = new JsonSerializerOptions()
        { WriteIndented = true, Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };
        internal static string ToBundlePath(string bundle) =>
            Path.Combine([Paths.GameRootPath, MainManifest, .. bundle.Split(Path.AltDirectorySeparatorChar)]);
        static void Convert()
        {
            new string[] { Plugin.ConvertPath, Plugin.InvalidPath }
                .Where(Directory.Exists)
                .ForEach(path => Directory.Delete(path, true));
            new string[] { Plugin.ConvertPath, Plugin.InvalidPath }
                .ForEach(path => Directory.CreateDirectory(path));
            ProcessManifest(All.Values.SelectMany(Convert));
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
            Manifest = info.TryGetValue(Ktype.MainManifest, out var value) ? value : MainManifest,
            Values = category.Entries
                .Where(entry => Vtype.Name != entry.Value)
                .Where(entry => info.ContainsKey(entry.Index))
                .ToDictionary(entry => entry.Index, entry => info.GetString(entry.Index)),
            Bundles = groups.Where(group => group.Key).SelectMany(group => group).ToList(),
            Invalid = groups.Where(group => !group.Key).SelectMany(group => group).ToList()
        };
        static bool AbdataExists(string bundle) =>
            File.Exists(ToBundlePath(bundle));
        static void ProcessManifest(IEnumerable<ConvertEntry> entries) =>
            ProcessManifest(ToManifestMap(entries.Where(entry => entry.Id < 100)), entries.Where(entry => entry.Id >= 100));

        static Dictionary<string, string> ToManifestMap(IEnumerable<ConvertEntry> entries) =>
            ToManifestMap(
                entries.SelectMany(entry => entry.Bundles).Distinct(),
                entries.GroupBy(entry => entry.Manifest).ToDictionary(
                    group => group.Key,
                    group => group.SelectMany(entry => entry.ToMainAssetBundle()).Distinct().ToList()));

        static Dictionary<string, string> ToManifestMap(IEnumerable<string> bundles, Dictionary<string, List<string>> manifestToBundles) =>
            bundles.ToDictionary(
                bundle => bundle,
                bundle => manifestToBundles
                    .Where(entry => entry.Value.Contains(bundle))
                    .Select(entry => entry.Key).FirstOrDefault(MainManifest));
        static Action<IEnumerable<ConvertEntry>> PreprocessManifest(Dictionary<string, string> manifestMap) => 
            mods => mods.ForEach(
                mod => mod.Values[Ktype.MainManifest] = mod.ToMainAssetBundle()
                    .Where(manifestMap.ContainsKey).Select(bundle => manifestMap[bundle])
                    .Where(manifestMap.ContainsKey).Select(bundle => manifestMap[bundle])
                    .FirstOrDefault(MainManifest));
        static void ProcessManifest(Dictionary<string, string> manifestMap, IEnumerable<ConvertEntry> entries) =>
            entries.With(PreprocessManifest(manifestMap))
                .GroupBy(entry => entry.Invalid.Count == 0 && IsConvertible(entry))
                .ForEach(group => ForkValidAndInvalid(group.Key)(manifestMap, group));
#if SamabakeScramble
        static bool IsConvertible(ConvertEntry mod) =>
            !Plugin.AicomiConversion.Value || MainManifest.Equals(mod.Values.GetValueOrDefault(Ktype.MainManifest, MainManifest));
#else
        static bool IsConvertible(ConvertEntry mod) => true;
#endif
        static Action<Dictionary<string, string>, IEnumerable<ConvertEntry>> ForkValidAndInvalid(bool value) => value ? Convert : Invalid;
        static void Invalid(Dictionary<string, string> _, IEnumerable<ConvertEntry> entries) =>
            entries.GroupBy(entry => entry.Category)
                .ForEach(group => Invalid(Directory.CreateDirectory(Path.Combine(Plugin.InvalidPath, group.Key.ToString())), group));
        static void Convert(Dictionary<string, string> manifestMap, IEnumerable<ConvertEntry> entries) =>
            Convert(entries.Select(entry => new ConvertPackage(manifestMap, entry)));
        static void Convert(IEnumerable<ConvertPackage> entries)
        {
            List<ConvertPackage> packages = new();
            foreach (var entry in entries)
            {
                var results = packages.GroupBy(group => group.Intersect(entry));
                packages = results.Where(result => !result.Key).SelectMany(result => result)
                    .Append(entry.Merge(results.Where(result => result.Key).SelectMany(result => result))).ToList();
            }
            packages.ForEach(package => package.Archive());
        }
        static void Invalid(DirectoryInfo path, IEnumerable<ConvertEntry> entries) =>
            entries.ForEach(entry => File.WriteAllText(Path.Combine(path.FullName, $"{entry.Id}.json"), JsonSerializer.Serialize(entry, JsonOption)));
    }
}