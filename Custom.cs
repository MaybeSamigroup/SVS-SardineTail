using System;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using UnityEngine;
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
        internal static void InitializePackages(string path) =>
            InitializePackages(IDS[0], path);
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
    }

    class FigureChoice
    {
        TMP_Dropdown Choice;
        List<(int Id, string Name)> Options =
            Human.lstCtrl.GetCategoryInfo(CatNo.bo_body).Yield()
                .Where(entry => entry.Item1 >= ModInfo.MIN_ID)
                .Where(HumanCustom.Instance.IsMale()
                    ? entry => entry.Item2.GetInfoInt(Ktype.Sex) is 2
                    : entry => entry.Item2.GetInfoInt(Ktype.Sex) is 3)
                .Select(entry => (entry.Item1,
                    $"{ModPackage.FromId(CatNo.bo_body, entry.Item1).PkgId}:{entry.Item2.GetString(Ktype.Name)}"))
                .Prepend((0, "<default>")).ToList();

        int FigureIdToOption(int id) =>
            Options.Index()
                .Where(tuple => tuple.Value.Id == id)
                .Select(tuple => tuple.Index).FirstOrDefault(0);

        void Update(Human human) => Choice
            .SetValueWithoutNotify(FigureIdToOption(Extension<CharaMods, CoordMods>.Humans[human].FigureId(human)));

        void ClearOptions(TMP_Dropdown ui) => ui.ClearOptions();

        void PopulateOptions() =>
            Choice.With(ClearOptions).AddOptions(Options.Select(tuple => tuple.Name).AsIl2Cpp());

        void Submit(int index) => CharaMods.Store(HumanCustom.Instance.Human, Options[index].Id).With(Extension.HumanCustomReload);

        void PrepareChoice(TMP_Dropdown ui) =>
            (Choice = ui).With(PopulateOptions).OnValueChangedAsObservable().Subscribe(Submit);
        FigureChoice() =>
            Extension.OnLoadCustomChara.Subscribe(Update).With(subscription =>
                SingletonInitializerExtension<HumanCustom>.OnDestroy
                    .Subscribe(F.Ignoring<Unit>(subscription.Dispose)));

        FigureChoice(CharacterCreation.UI.ParameterWindow ui) : this() =>
            ui.Content.With("BodyAsset".AsChild(
                UGUI.Size(height: 60) +
                UGUI.LayoutV(padding: UGUI.Offset(10, 6), childAlignment: TextAnchor.MiddleCenter) +
                "Label".AsChild(UGUI.Label(300, 24) + UGUI.Text(text: "Body Model:")) +
                "Choice".AsChild(UGUI.Dropdown(300, 24, UGUI.Component<TMP_Dropdown>(PrepareChoice))) +
                new UIAction(go => ui.OnEnableAsObservable()
                    .Select(_ => (HumanCustom.Instance.NowCategory.Category, HumanCustom.Instance.NowCategory.Index))
                    .Select(nowCategory => nowCategory is (1, 0) or (1, 9)).Subscribe(go.SetActive))));

        static void Initialize(CompositeDisposable subscriptions) =>
            SingletonInitializerExtension<HumanCustom>.OnDestroy
                .Subscribe(F.Ignoring<Unit>(subscriptions.Dispose));

        internal static void Initialize() => Initialize([
            HumanCustomExtension.OnUIPrefab("editwindow.unity3d", "ParameterWindow")
                .Subscribe(UGUI.Component<CharacterCreation.UI.ParameterWindow>(cmp => new FigureChoice(cmp)).Invoke),
            Extension.OnLoadCustomChara
                .Select(human => Extension<CharaMods, CoordMods>.Humans[human].FigureId(human))
                .Where(id => IOExtension.FigureId != id)
                .Subscribe(id => (IOExtension.FigureId = id).With(Extension.HumanCustomReload))
        ]);
    }

    internal static partial class IOExtension
    {
        internal static int FigureId = -1;
        internal static void InitializeFigureId() => FigureId = -1;
        internal static void OverrideFigure(Human human) =>
            FigureId = Extension<CharaMods, CoordMods>.Humans[human].FigureId(human);
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
    }
    internal static partial class CategoryExtension
    {
        internal static IDisposable[] Initialize() => [
            SingletonInitializerExtension<HumanCustom>.OnStartup.Subscribe(_ => FigureChoice.Initialize()),
            SingletonInitializerExtension<HumanCustom>.OnDestroy.Subscribe(_ => IOExtension.InitializeFigureId()),
            Extension<CharaMods, CoordMods>.Translate<CoordMods>(Path.Combine(Plugin.Guid, "modifications.json"), mods => mods),
            Extension<CharaMods, CoordMods>.Translate<LegacyCharaMods>(Path.Combine(Plugin.Guid, "modifications.json"), mods => mods),
            Extension<CharaMods, CoordMods>.Translate<LegacyCharaMods>(Path.Combine(Plugin.Name, "modifications.json"), mods => mods),
            ..Extension.Register<CharaMods, CoordMods>(),
            Extension<CharaMods, CoordMods>.OnPreprocessChara.Subscribe(tuple => tuple.Value.Apply(tuple.Data)),
            Extension<CharaMods, CoordMods>.OnPreprocessCoord.Subscribe(tuple => tuple.Value.Apply(tuple.Data)),
            Extension.OnPrepareSaveChara.Subscribe(CharaMods.Store),
            Extension.OnPrepareSaveCoord.Subscribe(CharaMods.Store),
            ..Extension.RegisterConversion<CharaMods, CoordMods>(),
        ];
    }

    public partial class Plugin : BasePlugin
    {
        internal static readonly string ConvertPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Name, "hardmods");
        internal static readonly string InvalidPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Name, "invalids");
    }
}