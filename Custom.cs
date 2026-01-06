using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if Aicomi
using ILLGAMES.Unity;
#else
using ILLGames.Unity;
#endif
using TMPro;
using Character;
using CharacterCreation;
using BepInEx;
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
        TMP_Dropdown PkgChoice;
        TMP_Dropdown ModChoice;
        List<(string, List<(string, int)>)> Options =
            Human.lstCtrl.GetCategoryInfo(CatNo.bo_body).Yield()
                .Where(entry => entry.Item1 >= ModInfo.MIN_ID)
                .Where(HumanCustom.Instance.IsMale()
                    ? entry => entry.Item2.GetInfoInt(Ktype.Sex) is 2
                    : entry => entry.Item2.GetInfoInt(Ktype.Sex) is 3)
                .Select(tuple => (tuple.Item2.Name, tuple.Item1))
                .GroupBy(entry => ModInfo.Map[CatNo.bo_body].ToMod(entry.Item2).PkgId)
                .Select(group => (group.Key, group.OrderBy(tuple => tuple.Item1).Prepend(("<current>", -1)).ToList()))
                .OrderBy(tuple => tuple.Item1)
                .Prepend(("<default>", [("<current>", -1), ("<default>", HumanCustom.Instance.IsMale() ? 0 : 1)])).ToList();
        (int, int) FigureIdToOption(int id) =>
            Options.Index().SelectMany(pkg => pkg.Item1.Item2.Index().Where(mod => id == mod.Item1.Item2).Select(mod => (pkg.Item2, mod.Item2))).FirstOrDefault((0, 0));
        void Update(Human human) =>
            FigureIdToOption(Extension<CharaMods, CoordMods>.Humans[human].FigureId).With(UpdatePkg).With(UpdateMod);
        void Update() => Update(HumanCustom.Instance.Human);
        void UpdatePkg((int, int) indices) => PkgChoice.SetValueWithoutNotify(indices.Item1);
        void UpdateMod((int, int) indices) => ModChoice.With(PopulateModOptions).SetValueWithoutNotify(indices.Item2);
        void ClearOptions(TMP_Dropdown ui) => ui.ClearOptions();
        void PopulatePkgOptions() =>
            PkgChoice.With(ClearOptions).AddOptions(Options.Select(tuple => tuple.Item1).AsIl2Cpp());
        void PopulateModOptions() =>
            ModChoice.With(ClearOptions).AddOptions(Options[PkgChoice.value].Item2.Select(tuple => tuple.Item1).AsIl2Cpp());
        void SubmitPkg() => ModChoice.With(PopulateModOptions).SetValueWithoutNotify(0);
        void SubmitMod() => (Extension<CharaMods, CoordMods>.Humans[HumanCustom.Instance.Human].FigureId = Options[PkgChoice.value].Item2[ModChoice.value].Item2).With(Extension.HumanCustomReload);
             
        void PreparePkgChoice(TMP_Dropdown ui) =>
            (PkgChoice = ui)
                .With(PopulatePkgOptions).OnValueChangedAsObservable()
                .Subscribe(_ => SubmitPkg());
        void PrepareModChoice(TMP_Dropdown ui) =>
            (ModChoice = ui)
                .With(PopulateModOptions).OnValueChangedAsObservable()
                .Subscribe(index => (index is not 0).Either(Update, SubmitMod));
        FigureChoice() =>
            Extension.OnLoadCustomChara.Subscribe(Update);
        FigureChoice(CharacterCreation.UI.ParameterWindow ui) =>
            ui.Content.With("BodyAsset".AsChild(
                UGUI.Size(height: 120) +
                UGUI.LayoutV(padding: UGUI.Offset(10, 6), childAlignment: TextAnchor.MiddleCenter) +
                "Body Pkg".AsChild(UGUI.Label(300, 24) + UGUI.Text(text: "Body Pkg:")) +
                "PkgChoice".AsChild(UGUI.Dropdown(300, 24, UGUI.Text(text: "<default>")) + UGUI.Component<TMP_Dropdown>(PreparePkgChoice)) +
                "Body Pkg".AsChild(UGUI.Label(300, 24) + UGUI.Text(text: "Body Mod:")) +
                "PkgChoice".AsChild(UGUI.Dropdown(300, 24, UGUI.Text(text: "<default>")) + UGUI.Component<TMP_Dropdown>(PrepareModChoice)) +
                new UIAction(go => ui.OnEnableAsObservable()
                    .Select(_ => (HumanCustom.Instance.NowCategory.Category, HumanCustom.Instance.NowCategory.Index))
                    .Select(nowCategory => nowCategory is (1, 0) or (1, 9)).Subscribe(go.SetActive))));

        internal static IDisposable Initialize() =>
            HumanCustomExtension.OnUIPrefab("editwindow.unity3d", "ParameterWindow")
                .Subscribe(UGUI.Component<CharacterCreation.UI.ParameterWindow>(cmp => new FigureChoice(cmp)).Invoke);
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

        internal static UnityEngine.Object ToBodyPrefab(string name) =>
            (FigureId < ModInfo.MIN_ID) ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(CatNo.bo_body, FigureId),
                Ktype.MainAB, Ktype.MainData, Il2CppInterop.Runtime.Il2CppType.Of<GameObject>())
                ?.With(obj => obj.name = name);

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
            (Extension<CharaMods, CoordMods>.Humans[HumanCustom.Instance.Human].FigureId, HumanCustom.Instance.IsMale()) switch
            {
                ( < ModInfo.MIN_ID, true) => 0,
                ( < ModInfo.MIN_ID, false) => 1,
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
    internal static partial class CategoryExtension
    {
        internal static IDisposable[] Initialize() => [
            SingletonInitializerExtension<CategoryEdit>.OnStartup.Subscribe(_ => FigureChoice.Initialize()),
            SingletonInitializerExtension<CategoryEdit>.OnDestroy.Subscribe(_ => IOExtension.InitializeFigureId()),
            Extension<CharaMods, CoordMods>.Translate<CharaMods>(Path.Combine(Plugin.Guid, "modifications.json"), mods => mods),
            Extension<CharaMods, CoordMods>.Translate<CoordMods>(Path.Combine(Plugin.Guid, "modifications.json"), mods => mods),
            Extension<CharaMods, CoordMods>.Translate<LegacyCharaMods>(Path.Combine(Plugin.Name, "modifications.json"), mods => mods),
            ..Extension.Register<CharaMods, CoordMods>(),
            Extension<CharaMods, CoordMods>.OnPreprocessChara.Subscribe(tuple => tuple.Item2.Apply(tuple.Item1)),
            Extension<CharaMods, CoordMods>.OnPreprocessCoord.Subscribe(tuple => tuple.Item2.Apply(tuple.Item1)),
            Extension.OnPrepareSaveChara.Subscribe(_ => IOExtension.SaveCustomChara()),
            Extension.OnPrepareSaveCoord.Subscribe(_ => IOExtension.SaveCustomCoord()),
            ..Extension.RegisterConversion<CharaMods, CoordMods>(),
        ];
    }

    public partial class Plugin : BasePlugin
    {
        internal static readonly string ConvertPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Name, "hardmods");
        internal static readonly string InvalidPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Name, "invalids");
    }
}