using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if Aicomi
using R3;
using R3.Triggers;
using ILLGAMES.Unity;
#else
using UniRx;
using UniRx.Triggers;
using ILLGames.Unity;
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
        ChoiceList PkgChoice;
        TextMeshProUGUI CurrentPkg;
        Dictionary<string, ChoiceList> ModChoice;
        Dictionary<string, TextMeshProUGUI> CurrentMod;
        Dictionary<(string, string), int> ChoiceToId;
        Dictionary<int, (string, string)> IdToChoice;
        internal static void Initialize() =>
            new FigureChoice(CategoryEdit.Instance._parameterWindow.Content.gameObject);
        static Dictionary<int, (string, string)> Options =>
            Human.lstCtrl.GetCategoryInfo(CatNo.bo_body).Yield()
                .Where(entry => entry.Item1 >= ModInfo.MIN_ID)
                .Where(HumanCustom.Instance.IsMale()
                    ? entry => entry.Item2.GetInfoInt(Ktype.Sex) is 2
                    : entry => entry.Item2.GetInfoInt(Ktype.Sex) is 3)
                .Select(entry => (entry.Item1, (ModInfo.Map[CatNo.bo_body].ToMod(entry.Item1).PkgId, entry.Item2.Name)))
                .Append((HumanCustom.Instance.IsMale() ? 0 : 1, ("<default>", "<default>"))).Reverse()
                .ToDictionary(entry => entry.Item1, entry => (entry.Item2.Item1, entry.Item2.Item2)); 
        static (int, int) NowCategory =>
            (HumanCustom.Instance.NowCategory.Category, HumanCustom.Instance.NowCategory.Index);

        FigureChoice() =>
            Extension.OnLoadCustomChara += CheckFigure;

        FigureChoice(Dictionary<int, (string, string)> opts) : this() =>
            (IdToChoice, ChoiceToId, PkgChoice, ModChoice, CurrentMod) = (
                opts, opts.ToDictionary(entry => entry.Value, entry => entry.Key),
                new ChoiceList(300, 24, "Pkgs", opts.Values.Select(pair => pair.Item1).Distinct().ToArray()),
                opts.Values.GroupBy(pair => pair.Item1)
                    .ToDictionary(group => group.Key, group => 
                        new ChoiceList(300, 24, "Mods", group.Select(pair => pair.Item2).ToArray())), new ());

        FigureChoice(GameObject parent) : this(Options) =>
            new GameObject("BodyAssets")
                .With(UGUI.Go(parent: parent.transform))
                .With(UGUI.Cmp(UGUI.Layout(height: 120)))
                .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>(
                    padding: new RectOffset() { left = 10, right = 10, top = 6, bottom = 6 }, childAlignment: TextAnchor.MiddleCenter)))
                .With(UGUI.Label.Apply(300).Apply(24).Apply("Body Pkg"))
                .With(UGUI.Choice.Apply(300).Apply(24).Apply("Choice"))
                .With(UGUI.ModifyAt("Choice")(
                    UGUI.Cmp<LayoutElement>(ui => ui.minHeight = 24) +
                    UGUI.Cmp(UGUI.Fitter()) + PkgChoice.Assign +
                    UGUI.ModifyAt("Choice.State", "Choice.Label")(
                        UGUI.Cmp(UGUI.Text(text: IdToChoice[IOExtension.CustomFigureId].Item1)) +
                        UGUI.Cmp<TextMeshProUGUI>(ui => CurrentPkg = ui)) +
                    UGUI.Cmp(ObservePackageValueChanged)))
                .With(UGUI.Label.Apply(300).Apply(24).Apply("Body Mod"))
                .With(go => ModChoice.ForEach((pkg, choice) => go
                    .With(UGUI.Choice.Apply(300).Apply(24).Apply($"{pkg}.Choice"))
                    .With(UGUI.ModifyAt($"{pkg}.Choice")(
                        UGUI.Cmp<LayoutElement>(ui => ui.minHeight = 24) +
                        UGUI.Cmp(UGUI.Fitter()) + choice.Assign +
                        UGUI.ModifyAt($"{pkg}.Choice.State", $"{pkg}.Choice.Label")(
                            UGUI.Cmp(UGUI.Text(text: IdToChoice[IOExtension.CustomFigureId].Item2)) +
                            UGUI.Cmp<TextMeshProUGUI>(ui => CurrentMod[pkg] = ui)) +
                        UGUI.Cmp(ObserveFigureValueChanged)))))
                .With(ObserveParentEnable(parent))
                .With(ToggleModChoices)
                .OnDestroyAsObservable().Subscribe(F.Ignoring<Unit>(Dispose));
        void ToggleModChoices() =>
            CurrentMod.ForEach((pkg, ui) => ui.transform.parent.parent.gameObject.active = pkg == CurrentPkg.text);
        Action<Toggle> ObservePackageValueChanged =>
            ui => ui.OnValueChangedAsObservable().Subscribe(OnPackageChanged);
        Action<Toggle> ObserveFigureValueChanged =>
            ui => ui.OnValueChangedAsObservable().Subscribe(OnValueChanged);
        Action<bool> OnPackageChanged =>
            value => (!value).Maybe(ToggleModChoices);
        Action<bool> OnValueChanged =>
            value => (!value).Maybe(CheckFigure);
        void CheckFigure() =>
            ChoiceToId.TryGetValue((CurrentPkg.text,
                CurrentMod[CurrentPkg.text].text), out var id).Maybe(F.Apply(CheckFigure, id));
        void CheckFigure(int id) =>
            (Extension.Chara<CharaMods,CoordMods>().FigureId != id).Maybe(() => ApplyFigure(id));
        void ApplyFigure(int id) =>
            (Extension.Chara<CharaMods, CoordMods>().FigureId = id)
                .With(F.Apply(UpdateChoice, IdToChoice[id])).With(Extension.HumanCustomReload);
        Action<GameObject> ObserveParentEnable(GameObject parent) =>
            go => parent.With(UGUI.Cmp(ObserveOnEnable(go)));
        Action<ObservableEnableTrigger> ObserveOnEnable(GameObject go) =>
            trigger => trigger.OnEnableAsObservable().Subscribe(OnEnableParent(go));
        Action<Unit> OnEnableParent(GameObject go) =>
            _ => go.SetActive(NowCategory is (1, 0) or (1, 9));
        void Dispose() =>
            Extension.OnLoadCustomChara -= CheckFigure;
        void CheckFigure(Human _) =>
            IOExtension.ReloadingFigureId(out var id)
                .Maybe(F.Apply(UpdateChoice, IdToChoice[id]) + Extension.HumanCustomReload);
        void UpdateChoice((string, string) pair) =>
            (CurrentPkg.text = pair.Item1).With(() =>
                CurrentMod.Values.ForEach(ui => ui.text = pair.Item2));
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
        static NormalData ToBodyNormal(ListInfoBase info) =>
            info != null &&
            info.TryGetValue(Ktype.MainAB, out var bundle) &&
            info.TryGetValue(Ktype.MainData, out var asset) &&
            Plugin.AssetBundle.Equals(bundle) ? ModPackage.ToNormalData(asset.Split(':')) : null;

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

        internal static NormalData ToBodyNormal(NormalData original) =>
            (FigureId < ModInfo.MIN_ID) ? original :
            ToBodyNormal(Human.lstCtrl.GetListInfo(CatNo.bo_body, FigureId)) ?? original;

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
            Extension.OnPreprocessChara += Extension<CharaMods, CoordMods>
                .Translate<LegacyCharaMods>(Path.Combine(Name, "modifications.json"), mods => mods);

            Extension.Register<CharaMods, CoordMods>();
            Extension<CharaMods, CoordMods>.OnPreprocessChara += (data, mods) => mods.Apply(data);
            Extension<CharaMods, CoordMods>.OnPreprocessCoord += (data, mods) => mods.Apply(data);

            Extension.PrepareSaveChara += IOExtension.SaveCustomChara;
            Extension.PrepareSaveCoord += IOExtension.SaveCustomCoord;

            CategoryExtension.Initialize();
        }
    }
}