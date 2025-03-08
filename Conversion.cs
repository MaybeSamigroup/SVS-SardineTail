using HarmonyLib;
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using Character;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;
using Fishbone;
using BepInEx;

namespace SardineTail
{
    internal static partial class CategoryNoExtensions
    {
        internal static void Initialize() =>
            Util.Hook<Manager.Game>(() => Plugin.HardmodConversion.Value.Maybe(() => Convert(new(), new())), () => { });
        static IEnumerable<Ktype> Assets(CatNo cat, Ktype key) => (cat, key) switch
        {
            (CatNo.mt_body_detail, Ktype.MainAB) => [Ktype.NormallMapDetail, Ktype.LineMask],
            (CatNo.mt_body_paint, Ktype.MainAB) => [Ktype.PaintTex],
            (CatNo.mt_sunburn, Ktype.MainAB) => [Ktype.SunburnTex],
            (CatNo.mt_nip, Ktype.MainAB) => [Ktype.NipTex],
            (CatNo.mt_underhair, Ktype.MainAB) => [Ktype.UnderhairTex],
            (CatNo.mt_face_detail, Ktype.MainAB) => [Ktype.NormallMapDetail, Ktype.LineMask],
            (CatNo.mt_face_paint, Ktype.MainAB) => [Ktype.PaintTex],
            (CatNo.mt_eyeshadow, Ktype.MainAB) => [Ktype.EyeshadowTex],
            (CatNo.mt_cheek, Ktype.MainAB) => [Ktype.CheekTex],
            (CatNo.mt_lip, Ktype.MainAB) => [Ktype.LipTex],
            (CatNo.mt_lipline, Ktype.MainAB) => [Ktype.LiplineTex],
            (CatNo.mt_eyebrow, Ktype.MainAB) => [Ktype.EyebrowTex],
            (CatNo.mt_eye_white, Ktype.MainAB) => [Ktype.EyeWhiteTex],
            (CatNo.mt_eye, Ktype.MainAB) => [Ktype.EyeTex],
            (CatNo.mt_eyepipil, Ktype.MainAB) => [Ktype.EyepipilTex],
            (CatNo.mt_eye_gradation, Ktype.MainAB) => [Ktype.ColorMaskTex],
            (CatNo.mt_eye_hi_up, Ktype.MainAB) => [Ktype.EyeHiUpTex],
            (CatNo.mt_eyelid, Ktype.MainAB) => [Ktype.EyelidTex],
            (CatNo.mt_eyeline_up, Ktype.MainAB) => [Ktype.EyelineUpTex, Ktype.EyelineShadowTex],
            (CatNo.mt_eyeline_down, Ktype.MainAB) => [Ktype.EyelineDownTex],
            (CatNo.mt_nose, Ktype.MainAB) => [Ktype.NoseTex],
            (CatNo.mt_mole, Ktype.MainAB) => [Ktype.MoleTex],
            (CatNo.mt_pattern, Ktype.MainAB) => [Ktype.MainTex],
            (CatNo.mt_hairgloss, Ktype.MainAB) => [Ktype.MainTex],
            (_, Ktype.MainAB) => [Ktype.MainData],
            (_, Ktype.MainTexAB) => [Ktype.MainTex],
            (_, Ktype.MainTex02AB) => [Ktype.MainTex02],
            (_, Ktype.MainTex03AB) => [Ktype.MainTex03],
            (_, Ktype.ColorMaskAB) => [Ktype.ColorMaskTex],
            (_, Ktype.ColorMask02AB) => [Ktype.ColorMask02Tex],
            (_, Ktype.ColorMask03AB) => [Ktype.ColorMask03Tex],
            (_, Ktype.OverTopMaskAB) => [Ktype.OverTopMask],
            (_, Ktype.OverBotMaskAB) => [Ktype.OverBotMask],
            (_, Ktype.OverBraMaskAB) => [Ktype.OverBraMask],
            (_, Ktype.OverInnerMaskAB) => [Ktype.OverInnerMask],
            (_, Ktype.ThumbAB) => [Ktype.ThumbTex],
            (_, Ktype.PaintMaskAB) => [Ktype.PaintTex, Ktype.PaintMask],
            (_, Ktype.DentTexAB) => [Ktype.DentTex],
            (_, Ktype.ShapeAnimeAB) => [Ktype.ShapeAnime],
            _ => [],
        };
        static bool IsABKey(CatNo cat, Ktype key) => Assets(cat, key).Count() != 0;
        static HashSet<Ktype> GroupKeys = [
            Ktype.HideHair,
            Ktype.Kind,
            Ktype.NotBra,
            Ktype.Parent,
            Ktype.Possess,
            Ktype.SetHair,
            Ktype.Sex,
            Ktype.StateType,
            Ktype.NipHide,
            Ktype.KokanHide,
            Ktype.MabUV,
            Ktype.HideShorts,
            Ktype.HalfUndress,
            Ktype.Image,
            Ktype.Attribute,
            Ktype.Detail,
            Ktype.MoleLayoutID,
            Ktype.NailHide,
            Ktype.NoShake,
            Ktype.ShoesType,
            Ktype.WeightType,
            Ktype.SocksDent,
            Ktype.SkirtType,
            Ktype.Sort,
        ];
        static IEnumerable<Ktype> ABKeys(CatNo index) => Cache[index]
            .Where(item => IsABKey(index, item.KeyType)).Select(item => item.KeyType);
        static void Convert(HashSet<string> originalAB, List<Tuple<HashSet<string>, ListInfoBase>> dependencies)
        {
            foreach (var (catNo, entry) in Human.lstCtrl._table)
            {
                if (Cache.ContainsKey(catNo))
                {
                    foreach (var (id, info) in entry)
                    {
                        if (id > 100)
                        {
                            dependencies.Add(new(ABKeys(catNo).Select(info.GetString)
                                .Where(path => File.Exists(Path.Combine(Paths.GameRootPath, "abdata", path))).Distinct().ToHashSet(), info));
                        }
                        else if (id >= 0)
                        {
                            ABKeys(catNo).Select(info.GetString).Do(path => originalAB.Add(path));
                        }
                        foreach(var (key, val) in info.table) {
                            Plugin.Instance.Log.LogDebug($"{info.Id}:{key}:{val}");
                        }
                    }
                }
            }
            originalAB.Convert(0, dependencies.Select(item => item.Item1).Distinct()
                .Aggregate((IEnumerable<HashSet<string>>)[], (groups, abs) =>
                    groups.Where(group => !group.Any(abs.Contains))
                        .Append(groups.Where(group => group.Any(abs.Contains))
                            .Aggregate(abs.AsEnumerable(), (xs, ys) => xs.Union(ys)).ToHashSet()))
                                .GroupBy(group => group, group => group.Count() == 0
                                        ? dependencies.Where(item => item.Item1.All(originalAB.Contains)).Select(item => item.Item2)
                                        : dependencies.Where(item => item.Item1.Any(group.Except(originalAB).Contains)).Select(item => item.Item2)));
        }
        static Action<DirectoryInfo> CopyDependencies(this HashSet<string> abs, HashSet<string> originalAB) =>
            dir => abs.Except(originalAB)
                .Where(ab => File.Exists(Path.Combine(Paths.GameRootPath, "abdata", ab)))
                .Do(ab => Directory.CreateDirectory(Path.Combine(dir.FullName, Path.GetDirectoryName(ab)))
                    .With(subdir => File.Copy(Path.Combine(Paths.GameRootPath, "abdata", ab), Path.Combine(subdir.FullName, Path.GetFileName(ab)))));
        static void Convert(this HashSet<string> originalAB, int index, IEnumerable<IGrouping<HashSet<string>, IEnumerable<ListInfoBase>>> groups) =>
            groups.OrderBy(group => group.Key.Count).Do(group =>
                Directory.CreateDirectory(Path.Combine(Plugin.ConversionsPath, $"package{index++}-0.0.0"))
                    .With(group.Key.CopyDependencies(originalAB))
                    .ConvertCategory(originalAB, group.SelectMany(info => info).GroupBy(info => (CatNo)info.Category)));
        static string ToJson<T>(this T value) => JsonSerializer.Serialize(value, new JsonSerializerOptions() { WriteIndented = true });
        static void ConvertCategory(this DirectoryInfo dir, HashSet<string> originalAB, IEnumerable<IGrouping<CatNo, ListInfoBase>> groups) =>
            File.WriteAllText(Path.Combine(dir.FullName, "hardmig.json"), 
                groups.Select(group => dir.CreateSubdirectory(group.Key.ToString())
                    .ConvertGroups(0, originalAB, group.Key, group))
                    .ToDictionary(item => item.Item1, item => item.Item2).ToJson());
        static Tuple<CatNo, Dictionary<int, Dictionary<HardMigrationInfo, string>>> ConvertGroups(this DirectoryInfo dir,
            int index, HashSet<string> originalAB, CatNo catNo, IEnumerable<ListInfoBase> infos) =>
            new (catNo, infos .GroupBy(info => ConvertToGroup(catNo, info).ToJson())
                .SelectMany(group => dir.CreateSubdirectory($"group{index++}")
                    .With(group.Key.ToValues).ConvertMods(originalAB, catNo, group))
                    .DistinctBy(item => item.Item1)
                    .ToDictionary(item => item.Item1, item => Enum.GetValues<HardMigrationInfo>()
                    .ToDictionary(val => val, val => val switch {
                        HardMigrationInfo.Version => new Version(0,0,0).ToString(),
                        _ => TranslatePath(Path.GetRelativePath(dir.Parent.FullName, item.Item2))
                    })));
       static IEnumerable<Tuple<int, string>> ConvertMods(this DirectoryInfo dir,
            HashSet<string> originalAB, CatNo catNo, IEnumerable<ListInfoBase> infos) =>
            infos.Select(info => new Tuple<int, string>(info.Id,
                dir.CreateSubdirectory(ToName(info.GetString(Ktype.Name)))
                    .With(originalAB.ConvertToValues(catNo, info).ToJson().ToValues).FullName));
        static void ToValues(this string json, DirectoryInfo dir) =>
            File.WriteAllText(Path.Combine(dir.FullName, "values.json"), json);
        static string ToName(string name) =>
            Path.GetInvalidFileNameChars().Aggregate(name, (name, ch) => name.Replace(ch, ' '), name => name);
        static Dictionary<Ktype,string> ConvertToGroup(CatNo catNo, ListInfoBase info) =>
            Cache[catNo]
                .Where(item => GroupKeys.Contains(item.KeyType))
                .Where(item => info.ContainsKey(item.KeyType))
                .Where(item => !info.GetString(item.KeyType)?.Equals(item.Default.Select(def => def.Item2).FirstOrDefault()) ?? false)
                .Select(item => new Tuple<Ktype, string>(item.KeyType, info.GetString(item.KeyType)))
                .ToDictionary(item => item.Item1, item => item.Item2);
        static Dictionary<Ktype,string> ConvertToValues(this HashSet<string> originalAB, CatNo catNo, ListInfoBase info) =>
            Cache[catNo]
                .Where(item => !GroupKeys.Contains(item.KeyType))
                .Where(item => info.ContainsKey(item.KeyType))
                .Where(item => item is not Name && item is not Image && item is not Asset && item.KeyType != Ktype.MainManifest)
                .SelectMany(item => originalAB.ConvertToValues(info, item.KeyType, Assets(catNo, item.KeyType)))
                .ToDictionary(item => item.Item1, item => item.Item2);
        static IEnumerable<Tuple<Ktype, string>> ConvertToValues(this HashSet<string> originalAB, ListInfoBase info, Ktype key, IEnumerable<Ktype> subkeys) =>
            subkeys.Count() == 0 ? [new(key, info.GetString(key))] : info.GetString(key).Equals("0") ? [] : originalAB.Contains(info.GetString(key)) ?
                subkeys.Where(info.ContainsKey).Select(item => new Tuple<Ktype, string>(item, $":{info.GetString(item)}")).Concat([new(key, info.GetString(key))]) :
                subkeys.Where(info.ContainsKey).Where(item => !info.GetString(item)?.Equals("0") ?? false)
                    .Select(item => new Tuple<Ktype, string>(item, info.GetString(key).ToAssetPath(info.GetString(item))));
        static string ToAssetPath(this string bundle, string asset) => string.Join(':', [TranslatePath(bundle), asset]);
        static string TranslatePath(string path) =>
            string.Join(Path.AltDirectorySeparatorChar, path.Split(Path.DirectorySeparatorChar));
    }
}