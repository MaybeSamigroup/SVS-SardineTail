using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if Aicomi
using R3;
using R3.Triggers;
#else
using UniRx;
using UniRx.Triggers;
#endif
using TMPro;
using Character;
using CharacterCreation;
using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using Fishbone;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal abstract partial class ModPackage
    {
        internal ModPackage(string path, string pkgId, Version version) =>
            (PkgPath, PkgId, PkgVersion) = (path, pkgId, version);

        internal void Register(Category category, string modId, ListInfoBase info) =>
            (ModToId.TryAdd(modId, info.Id) && Human.lstCtrl._table[category.Index].TryAdd(info.Id, info))
            .Either(
                () => Plugin.Instance.Log.LogMessage($"duplicate mod id detected. {PkgId}:{modId}"),
                () => RegisterIdToMod(category.Index, info.Id, new ModInfo
                {
                    PkgVersion = PkgVersion,
                    PkgId = PkgId,
                    ModId = modId,
                    Category = category.Index,
                })
            );

        internal static Action<string> InitializePackages = path =>
            StpPackage.Collect(Path.Combine(path, "sardines"))
                .Concat(DevPackage.Collect(Path.Combine(path, "UserData", "plugins", Plugin.Name, "packages")))
                .GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last())
                .ForEach(entry => Packages[entry.Key] = entry.Value.With(entry.Value.Initialize));

    }

    internal partial class DevPackage : ModPackage
    {
        DevPackage(string path, string pkgId, Version version) : base(path, pkgId, version) { }

        static Func<string, string, Version, DevPackage> ToPackage =
            (root, path, version) => new DevPackage(path, ToPkgId(root, path), version);

        static Func<string, string, IEnumerable<DevPackage>> ToPackages =
            (root, path) => ToVersion(Path.GetRelativePath(root, path).Split('-'))
                .Select(ToPackage.Apply(root).Apply(path));

        internal static Func<string, IEnumerable<ModPackage>> Collect = path =>
            Plugin.DevelopmentMode.Value
                ? Directory.GetDirectories(path).SelectMany(ToPackages.Apply(path))
                : Enumerable.Empty<ModPackage>();
    }

    internal partial class StpPackage : ModPackage
    {
        StpPackage(string path, string pkgId, Version version) : base(path, pkgId, version) { }

        static Func<string, Version, StpPackage> ToPackage =
            (path, version) => new StpPackage(path, ToPkgId(path), version);

        static Func<string, IEnumerable<ModPackage>> ToPackages =
            path => ToVersion(Path.GetFileNameWithoutExtension(path).Split('-'))
                .Select(ToPackage.Apply(path));

        internal static Func<string, IEnumerable<ModPackage>> Collect = path =>
            new DirectoryInfo(path).GetFiles("*.stp", SearchOption.AllDirectories)
                .Select(info => info.FullName).SelectMany(ToPackages);
    }

    class FigureChoice
    {
        Dictionary<int, string> IdToName;
        Dictionary<string, int> NameToId;
        ChoiceList Options;
        TextMeshProUGUI Current;
        internal static void Initialize() =>
            new FigureChoice(CategoryEdit.Instance._parameterWindow.Content.gameObject);

        static Func<Tuple<int, ListInfoBase>, bool> GenderFilter(int value) =>
            tuple => value == tuple.Item2.GetInfoInt(Ktype.Sex);

        static Dictionary<string, int> GenderOptions =>
            Human.lstCtrl.GetCategoryInfo(CatNo.bo_body).Yield()
                .Where(HumanCustom.Instance.IsMale() ? GenderFilter(2) : GenderFilter(3))
                .ToDictionary(tuple => tuple.Item1 < 2 ? "default" : tuple.Item2.Name, tuple => tuple.Item1);

        static (int, int) NowCategory =>
            (HumanCustom.Instance.NowCategory.Category, HumanCustom.Instance.NowCategory.Index);

        FigureChoice() =>
            Extension.OnLoadCustomChara += CheckFigure;
        
        FigureChoice(Dictionary<string, int> nameToId) : this() =>
            (NameToId, IdToName, Options) = (nameToId, nameToId.ToDictionary(entry => entry.Value, entry => entry.Key),
                new ChoiceList(300, 24, "Bodies", nameToId.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray()));

        FigureChoice(GameObject parent) : this(GenderOptions) =>
            new GameObject("BodyAssets")
                .With(UGUI.Go(parent: parent.transform))
                .With(UGUI.Cmp(UGUI.Layout(height: 60)))
                .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>(
                    padding: new RectOffset() { left = 10, right = 10, top = 6, bottom = 6 }, childAlignment: TextAnchor.MiddleCenter)))
                .With(UGUI.Label.Apply(300).Apply(24).Apply("Body"))
                .With(UGUI.Choice.Apply(300).Apply(24).Apply("Choice"))
                .With(UGUI.ModifyAt("Choice")(
                    UGUI.Cmp<LayoutElement>(ui => ui.minHeight = 24) +
                    UGUI.Cmp(UGUI.Fitter()) + Options.Assign +
                    UGUI.ModifyAt("Choice.State", "Choice.Label")(
                        UGUI.Cmp(UGUI.Text(text: IdToName[IOExtension.CustomFigureId])) +
                        UGUI.Cmp<TextMeshProUGUI>(ui => Current = ui)) +
                    UGUI.Cmp(ObserveValueChanged)))
                .With(ObserveParentEnable(parent))
                .OnDestroyAsObservable().Subscribe(F.Ignoring<Unit>(Dispose));

        Action<Toggle> ObserveValueChanged =>
            ui => ui.OnValueChangedAsObservable().Subscribe(OnValueChanged);

        Action<bool> OnValueChanged =>
            value => (!value).Maybe(CheckFigure(NameToId[Current.text]));

        Action CheckFigure(int id) => () =>
            (Extension.Chara<CharaMods,CoordMods>().FigureId != id).Maybe(ApplyFigure(id));

        Action ApplyFigure(int id) => () =>
            (Extension.Chara<CharaMods,CoordMods>().FigureId = id).With(Extension.HumanCustomReload);

        Action<GameObject> ObserveParentEnable(GameObject parent) =>
            go => parent.With(UGUI.Cmp(ObserveOnEnable(go)));

        Action<ObservableEnableTrigger> ObserveOnEnable(GameObject go) =>
            trigger => trigger.OnEnableAsObservable().Subscribe(OnEnableParent(go));

        Action<Unit> OnEnableParent(GameObject go) =>
            _ => go.SetActive(NowCategory is (1, 0) or (1, 9));

        void Dispose() =>
            Extension.OnLoadCustomChara -= CheckFigure;

         void CheckFigure(Human _) =>
            IOExtension.ReloadingFigureId(out var id).Maybe(F.Apply(UpdateFigure, id));

        void UpdateFigure(int id) =>
            (Current.text = IdToName[id]).With(Extension.HumanCustomReload);
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

        internal static UnityEngine.Object ToBodyPrefab() =>
            (FigureId < ModInfo.MIN_ID) ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(CatNo.bo_body, FigureId),
                Ktype.MainAB, Ktype.MainData, Il2CppInterop.Runtime.Il2CppType.Of<GameObject>());

        internal static UnityEngine.Object ToBodyTexture() =>
            (FigureId < ModInfo.MIN_ID) ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(CatNo.bo_body, FigureId),
                Ktype.MainTexAB, Ktype.MainTex, Il2CppInterop.Runtime.Il2CppType.Of<Texture2D>());

        internal static UnityEngine.Object ToBodyShapeAnime() =>
            (FigureId < ModInfo.MIN_ID) ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(CatNo.bo_body, FigureId),
                Ktype.ShapeAnimeAB, Ktype.ShapeAnime, Il2CppInterop.Runtime.Il2CppType.Of<TextAsset>());

        internal static int CustomFigureId =>
            (Extension.Chara<CharaMods, CoordMods>().FigureId, HumanCustom.Instance.IsMale()) switch
            {
                (< ModInfo.MIN_ID, true) => 0,
                (< ModInfo.MIN_ID, false) => 1,
                (var figureId, _) => figureId
            };

        internal static bool ReloadingFigureId(out int figureId) =>
            (FigureId, figureId = CustomFigureId) is not (
                 < ModInfo.MIN_ID,
                 < ModInfo.MIN_ID
            ) && FigureId != figureId;

        internal static void SaveCustomChara() =>
            CharaMods.Store(HumanCustom.Instance.Human);

        internal static void SaveCustomCoord() =>
            CharaMods.Store(HumanCustom.Instance.Human);

        internal static void InitializeFigureId() => FigureId = -1;
    }
    public partial class Plugin : BasePlugin
    {
        internal static readonly string ConvertPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Name, "hardmods");
        internal static readonly string InvalidPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Name, "invalids");
        internal static ConfigEntry<bool> HardmodConversion;
        public override void Load()
        {
            Hooks.ApplyPatches(Patch = new Harmony($"{Name}.Hooks"));
            Instance = this;
            DevelopmentMode = Config.Bind("General", "Enable development package loading.", false);
            HardmodConversion = Config.Bind("General", "Enable hardmod conversion at startup.", false);
            GameSpecificInitialize();

            Util<CategoryEdit>.Hook(FigureChoice.Initialize, IOExtension.InitializeFigureId);

            Extension.OnPreprocessChara += Extension<CharaMods, CoordMods>
                .Translate<CharaMods>(Path.Combine(Guid, "modifications.json"), mods => mods);
            Extension.OnPreprocessCoord += Extension<CharaMods, CoordMods>
                .Translate<CoordMods>(Path.Combine(Guid, "modifications.json"), mods => mods);

            Extension.Register<CharaMods, CoordMods>();
            Extension<CharaMods, CoordMods>.OnPreprocessChara += (data, mods) => mods.Apply(data);
            Extension<CharaMods, CoordMods>.OnPreprocessCoord += (data, mods) => mods.Apply(data);

            Extension.PrepareSaveChara += IOExtension.SaveCustomChara;
            Extension.PrepareSaveCoord += IOExtension.SaveCustomCoord;

            CategoryExtension.Initialize();
        }
    }
}