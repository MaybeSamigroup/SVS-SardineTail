using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Character;
using ILLGames.Unity;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Fishbone;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    public abstract partial class ModPackage
    {
        internal void Register(CategorySpec category, string modId, ListInfoBase info) =>
            (ModToId.TryAdd(modId, info.Id) && Human.lstCtrl._table[(int)GameId][category.Index].TryAdd(info.Id, info))
            .Either(
                () => Plugin.Instance.Log.LogMessage($"duplicate mod id detected. {PkgId}:{modId}"),
                () => Packages.Register(GameId, category.Index, info.Id, new ModInfo
                {
                    PkgVersion = PkgVersion,
                    PkgId = PkgId,
                    ModId = modId,
                    Category = category.Index,
                })
            );
    }

    internal static partial class ModificationExtension
    {
        internal static GameId ToGameId(this HumanData data) => GameIdSpec.FromCharaTag(data.Tag);

        internal static GameId ToGameId(this HumanDataCoordinate data) => GameIdSpec.FromClothesTag(data.Tag);

        internal static void InitializeManifest(this int gameId, string path, string manifest) =>
            AssetBundleManager.ManifestBundlePack[manifest].AllAssetBundles =
                ExtendManifest(manifest).With(F.Apply(Packages.InitializePackages, (GameId)gameId, path));

        static string[] ExtendManifest(string manifest) =>
            AssetBundleManager.ManifestBundlePack[manifest].AllAssetBundles.Concat([Plugin.AssetBundle]).ToArray();

        internal static int FigureId = -1;
        static string GameTag;
        internal static void OverrideFigure(Human human) =>
            (GameTag, FigureId) = (human.data.Tag, Extension<CharaMods, CoordMods>.Humans[human].FigureId(human));

        static UnityEngine.Object ToBodyAsset(string bundle, string asset, string _, Il2CppSystem.Type type) =>
            Plugin.AssetBundle.Equals(bundle)
                ? AssetBundles.ToAsset(asset.Split(':'), type)
                : AssetBundleManager.GetLoadedAssetBundle(bundle,
                    DigitalCraft.PathManager.Instance.GetMainManifestFromTag(ref GameTag)).Bundle.LoadAsset(asset, type);

        internal static UnityEngine.Object ToBodyPrefab(string name) =>
            (FigureId < ModInfo.MIN_ID) ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(ref GameTag, CatNo.bo_body, FigureId),
                Ktype.MainAB, Ktype.MainData, Il2CppInterop.Runtime.Il2CppType.Of<GameObject>())
                ?.With(obj => obj.name = name);

        internal static UnityEngine.Object ToBodyTexture() =>
            (FigureId < ModInfo.MIN_ID) ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(ref GameTag, CatNo.bo_body, FigureId),
                Ktype.MainTexAB, Ktype.MainTex, Il2CppInterop.Runtime.Il2CppType.Of<Texture2D>());

        internal static UnityEngine.Object ToBodyShapeAnime() =>
            (FigureId < ModInfo.MIN_ID) ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(ref GameTag, CatNo.bo_body, FigureId),
                Ktype.ShapeAnimeAB, Ktype.ShapeAnime, Il2CppInterop.Runtime.Il2CppType.Of<TextAsset>());

        internal static NormalData ToBodyNormal(NormalData original) =>
            (FigureId < ModInfo.MIN_ID) ? original :
            ToBodyNormal(Human.lstCtrl.GetListInfo(ref GameTag, CatNo.bo_body, FigureId)) ?? original;

        static Dictionary<Renderer, Material> MaterialBuffer = new();
        static void StoreMaterial(Renderer rend) =>
            MaterialBuffer[rend] = UnityEngine.Object.Instantiate(rend.material);
        static Action<Renderer> ApplyColorsToRenderer(HumanBody body) =>
            rend => ApplyColors.Apply(body.fileBody).Apply(rend.material);
        static Action<Renderer> OverrideColorsActions(HumanBody body) =>
            body.human.data.IsAC ? ApplyColorsToRenderer(body) + StoreMaterial : ApplyColorsToRenderer(body);
        static IEnumerable<Renderer> RenderersToOverrideGraphic(HumanBody body) =>
            body.human.data.IsAC ? ToOverrideRenderers(body.GetRefObject(Table.RefObjKey.S_Son))
                .Where(rend => MaterialBuffer.Remove(rend, out var material) && true.With(() => rend.material = material)) : ToOverrideRenderers(body);
        internal static void OverrideColors(HumanBody body) =>
            ToOverrideRenderers(body).ForEach(OverrideColorsActions(body));
        internal static void OverrideGraphic(HumanBody body) =>
            body._graphicDisposables.Add(body.graphic.AddEvent(RenderersToOverrideGraphic(body).ToArray(), HumanGraphic.UpdateFlags.All));

        internal static IDisposable[] Initialize() => [
            Extension<CharaMods, CoordMods>
                .Translate<LegacyCharaMods>(Path.Combine(Plugin.Name, "modifications.json"), mods => mods),
            ..Extension.Register<CharaMods, CoordMods>(),
            Extension<CharaMods, CoordMods>.OnPreprocessChara.Subscribe(tuple => tuple.Item2.Apply(tuple.Item1)),
            Extension<CharaMods, CoordMods>.OnPreprocessCoord.Subscribe(tuple => tuple.Item2.Apply(tuple.Item1)),
            Extension.OnLoadChara.Subscribe(CharaMods.Store),
            Extension.OnLoadCoord.Subscribe(CoordMods.Store),
        ];
    }

    static partial class Hooks
    {
        static void MaterialHelperLoadPatchMaterialPostfix(int gameID, string game) =>
            gameID.InitializeManifest(Path.Combine(game, ".."), DigitalCraft.PathManager.Instance.GetMainManifestFromID(gameID));
        static Dictionary<string, MethodInfo[]> SpecPrefixes => new();
        static Dictionary<string, MethodInfo[]> SpecPostfixes => new()
        {
            [nameof(MaterialHelperLoadPatchMaterialPostfix)] = [
                typeof(HumanManager.MaterialHelper).GetMethod(
                    nameof(HumanManager.MaterialHelper.LoadPatchMaterial), 0, [typeof(int), typeof(string), typeof(string), typeof(bool)])
            ],
            [nameof(LoadAssetWithoutTypePostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0, [typeof(string)])
            ],
        };
    }

    [BepInDependency(VarietyOfScales.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";
        public Plugin() : base() =>
            (Instance, DevelopmentMode) =
                (this, DevelopmentMode = Config.Bind("General", "Enable development package loading.", false));

        IDisposable[] Initialize() => [];
    }
}