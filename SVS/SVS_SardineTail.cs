using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UniRx.Triggers;
using TMPro;
using Character;
using CharacterCreation;
using Fishbone;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal static partial class CategoryExtensions
    {
        internal static Action<ListInfoBase> RegisterMod(this Category category) =>
             info => Human.lstCtrl._table[category.Index].Add(info.Id, info);
    }
    internal static partial class ModificationExtensions
    {
        internal static void Initialize()
        {
            Event.OnActorSerialize +=
                (actor, archive) => CharaMods.ToMods(actor.charFile).Save(archive);
            Event.OnCharacterSerialize +=
                (data, archive) => CharaMods.ToMods(data).Save(archive);
            Event.OnCoordinateSerialize +=
                (data, archive) => CoordMods.ToMods(data).Save(archive);
            Event.OnActorDeserialize +=
                (actor, archive) => CharaMods.Load(archive).Apply(actor.charFile);
            Event.OnPreActorHumanize +=
                (_, data, archive) => CharaMods.Load(archive).ApplyFigure(data);
            Event.OnPreCharacterDeserialize +=
                (data, limits, archive, current) => CharaMods
                    .ToMods(data.With(CharaMods.Load(archive).Apply(limits))).Save(current);
            Event.OnPreCoordinateDeserialize +=
                (_, data, limits, archive, current) => CoordMods
                    .ToMods(data.With(CoordMods.Load(archive).Apply(limits))).Save(current);
            Util<HumanCustom>.Hook(Extensions.InitializeOverrideBody, F.DoNothing);
            Util<CategoryEdit>.Hook(BodyChoice.Initialize, F.DoNothing);
        }
    }
    internal static partial class Extensions
    {
        internal static void InitializeOverrideBody() =>
            OverrideBodyId = HumanCustom.Instance.IsMale() ? 0 : 1;
        static UnityEngine.Object ToBodyAsset(string bundle, string asset, string manifest, Il2CppSystem.Type type) =>
            Plugin.AssetBundle.Equals(bundle) ? ModPackage.ToAsset(asset.Split(':'), type) :
                AssetBundleManager.LoadAssetBundle(bundle, manifest).Bundle.LoadAsset(asset, type);
        static UnityEngine.Object ToBodyAsset(ListInfoBase info, Ktype ab, Ktype data, Il2CppSystem.Type type) =>
            info != null &&
                info.TryGetValue(ab, out var bundle) &&
                info.TryGetValue(data, out var asset) &&
                info.TryGetValue(Ktype.MainManifest, out var manifest) ? ToBodyAsset(bundle, asset, manifest, type) : null;
        internal static UnityEngine.Object ToBodyPrefab() => OverrideBodyId < 100000000 ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(CatNo.bo_body, OverrideBodyId),
                Ktype.MainAB, Ktype.MainData, Il2CppInterop.Runtime.Il2CppType.Of<GameObject>());
        internal static UnityEngine.Object ToBodyTexture() => OverrideBodyId < 100000000 ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(CatNo.bo_body, OverrideBodyId),
                Ktype.MainTexAB, Ktype.MainTex, Il2CppInterop.Runtime.Il2CppType.Of<Texture2D>());
        internal static UnityEngine.Object ToBodyShapeAnime() => OverrideBodyId < 100000000 ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(CatNo.bo_body, OverrideBodyId),
                Ktype.ShapeAnimeAB, Ktype.ShapeAnime, Il2CppInterop.Runtime.Il2CppType.Of<TextAsset>());
    }
    class BodyChoice
    {
        internal static void Initialize() =>
            Util.OnCustomHumanReady(Create);
        static void Create() =>
            Event.OnPostCharacterDeserialize +=
                new BodyChoice(CategoryEdit.Instance._parameterWindow.Content.gameObject).OnDeserialize;
        static Func<Tuple<int, ListInfoBase>, bool> GenderFilter(int value) =>
            tuple => value == tuple.Item2.GetInfoInt(Ktype.Sex);
        static (int, int) NowCategory =>
            (HumanCustom.Instance.NowCategory.Category, HumanCustom.Instance.NowCategory.Index);
        Dictionary<int, string> IdToName;
        Dictionary<string, int> NameToId;
        ChoiceList Options;
        TextMeshProUGUI Current;
        BodyChoice(Dictionary<int, string> idToName) =>
            IdToName = idToName;
        BodyChoice(Dictionary<string, int> nameToId) : this(nameToId.ToDictionary(entry => entry.Value, entry => entry.Key)) =>
            (NameToId, Options) = (nameToId, new ChoiceList(300, 24, "Bodies", nameToId.OrderBy(entry => entry.Value).Select(entry => entry.Key).ToArray()));
        BodyChoice(GameObject parent) :
            this(Human.lstCtrl.GetCategoryInfo(CatNo.bo_body).Yield()
                .Where(HumanCustom.Instance.IsMale() ? GenderFilter(2) : GenderFilter(3))
                .ToDictionary(tuple => tuple.Item1 < 2 ? "default" : tuple.Item2.Name, tuple => tuple.Item1)) =>
            new GameObject("BodyAssets")
                .With(UGUI.Go(parent: parent.transform))
                .With(UGUI.Cmp(UGUI.Layout(height: 60)))
                .With(UGUI.Cmp(UGUI.LayoutGroup<VerticalLayoutGroup>(
                        padding: new(10, 10, 6, 6), childAlignment: TextAnchor.MiddleCenter)))
                .With(UGUI.Label.Apply(300).Apply(24).Apply("Body"))
                .With(UGUI.Choice.Apply(300).Apply(24).Apply("Choice"))
                .With(UGUI.ModifyAt("Choice")(
                        UGUI.Cmp<LayoutElement>(ui => ui.minHeight = 24) +
                        UGUI.Cmp(UGUI.Fitter()) + Options.Assign +
                        UGUI.ModifyAt("Choice.State", "Choice.Label")(
                            UGUI.Cmp(UGUI.Text(text: "default")) +
                            UGUI.Cmp<TextMeshProUGUI>(ui => Current = ui)) +
                        UGUI.Cmp(ObserveValueChanged)))
                .With(ObserveParentEnable(parent))
                .OnDestroyAsObservable().Subscribe(F.Ignoring<Unit>(Dispose));
        Action<Toggle> ObserveValueChanged =>
            ui => ui.OnValueChangedAsObservable().Subscribe(OnValueChanged);
        Action<bool> OnValueChanged =>
            value => (!value && Extensions.OverrideBodyId != NameToId[Current.text]).Maybe(SetBody(NameToId[Current.text]));
        Action SetBody(int id) => () =>
            ((Extensions.OverrideBodyId, Extensions.BypassFigure) = (id, true)).With(Event.HumanCustomReload);
        Action GetBody(int id) => () =>
            (id != NameToId[Current.text]).Maybe(F.Apply(Current.SetText, IdToName[id], true) + Util.DoNextFrame.Apply(SetBody(id)));
        Action<GameObject> ObserveParentEnable(GameObject parent) =>
            go => parent.With(UGUI.Cmp(ObserveOnEnable(go)));
        Action<ObservableEnableTrigger> ObserveOnEnable(GameObject go) =>
            trigger => trigger.OnEnableAsObservable().Subscribe(OnEnableParent(go));
        Action<Unit> OnEnableParent(GameObject go) =>
            _ => go.SetActive(NowCategory is (1, 0) or (1, 9));
        void OnDeserialize(Human human, HumanData.LoadLimited.Flags limits, ZipArchive archive, ZipArchive storage) =>
            Extensions.BypassFigure = Extensions.BypassFigure
                ? false : false.With(GetBody(Extensions.OverrideBodyId));
        void Dispose() =>
            Event.OnPostCharacterDeserialize -= OnDeserialize;
    }
    static partial class Hooks
    {
        static Dictionary<string, MethodInfo[]> SpecPrefixes => new();
        static Dictionary<string, MethodInfo[]> SpecPostfixes => new();
    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        internal static readonly string ConversionsPath = Path.Combine(Paths.GameRootPath, "UserData", "plugins", Guid, "hardmods");
        internal static ConfigEntry<bool> HardmodConversion;
        internal static ConfigEntry<bool> StructureConversion;
        public override void Load()
        {
            Hooks.ApplyPatches(Patch = new Harmony($"{Name}.Hooks"));
            Instance = this;
            DevelopmentMode = Config.Bind("General", "Enable development package loading.", false);
            HardmodConversion = Config.Bind("General", "Enable hardmod conversion at startup.", false);
            StructureConversion = Config.Bind("General", "Convert hardmod into structured form.", false);
            ModPackage.InitializePackages(Paths.GameRootPath);
            ModificationExtensions.Initialize();
            CategoryExtensions.Initialize();
        }
    }
}