using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Character;
using Fishbone;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal static partial class CategoryExtensions
    {
        internal static int CurrentGameID;
        internal static Action<ListInfoBase> RegisterMod(this Category category) =>
            info => Human.lstCtrl._table[CurrentGameID][category.Index].Add(info.Id, info);
        internal static void InitializeManifest(this int gameId, string manifest, string path) =>
            ((CurrentGameID, AssetBundleManager.ManifestBundlePack[manifest].AllAssetBundles) =
                (gameId, ExtendManifest(manifest))).With(path.InitializePackages);
        static string[] ExtendManifest(string manifest) =>
            AssetBundleManager.ManifestBundlePack[manifest].AllAssetBundles.Concat([Plugin.AssetBundle]).ToArray();
    }
    internal static partial class ModificationExtensions
    {
        static internal void Initialize()
        {
            Event.OnPreCharacterDeserialize +=
                (data, archive) => CharaMods.Load(archive)
                    .Apply(data.With(ModPackageExtensions.CaptureGameTag));
            Event.OnPreCoordinateDeserialize +=
                (_, data, limits, archive, current) => CoordMods
                    .ToMods(data.With(CoordMods.Load(archive).Apply(limits))).Save(current);
        }
    }
    internal static partial class ModPackageExtensions
    {
        static string GameTag;
        internal static void CaptureGameTag(HumanData data) =>
            GameTag = data.Tag;
        internal static UnityEngine.Object ToBodyPrefab() => OverrideBodyId < 100000000 ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(ref GameTag, CatNo.bo_body, OverrideBodyId),
                Ktype.MainAB, Ktype.MainData, Il2CppInterop.Runtime.Il2CppType.Of<GameObject>());
        internal static UnityEngine.Object ToBodyTexture() => OverrideBodyId < 100000000 ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(ref GameTag, CatNo.bo_body, OverrideBodyId),
                Ktype.MainTexAB, Ktype.MainTex, Il2CppInterop.Runtime.Il2CppType.Of<Texture2D>());
        internal static UnityEngine.Object ToBodyShapeAnime() => OverrideBodyId < 100000000 ? null :
            ToBodyAsset(Human.lstCtrl.GetListInfo(ref GameTag, CatNo.bo_body, OverrideBodyId),
                Ktype.ShapeAnimeAB, Ktype.ShapeAnime, Il2CppInterop.Runtime.Il2CppType.Of<TextAsset>());
    }
    static partial class Hooks
    {
        static void MaterialHelperLoadPatchMaterialPostfix(int gameID, string game) =>
            gameID.InitializeManifest(DigitalCraft.PathManager.Instance.GetMainManifestFromID(gameID), Path.Combine(game, ".."));
        static Dictionary<string, MethodInfo[]> SpecPrefixes => new();
        static Dictionary<string, MethodInfo[]> SpecPostfixes => new()
        {
            [nameof(MaterialHelperLoadPatchMaterialPostfix)] = [
                typeof(HumanManager.MaterialHelper).GetMethod(
                    nameof(HumanManager.MaterialHelper.LoadPatchMaterial), 0, [typeof(int), typeof(string), typeof(string)])
            ],
        };
    }
    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";
        public override void Load()
        {
            Patch = new Harmony($"{Name}.Hooks");
            Hooks.ApplyPatches(Patch);
            Instance = this;
            DevelopmentMode = Config.Bind("General", "Enable development package loading.", false);
            ModificationExtensions.Initialize();
        }
    }
}