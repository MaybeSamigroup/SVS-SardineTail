using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Character;
using ILLGames.Unity;
using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Runtime;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Fishbone;
using CoastalSmell;
using CatNo = ChaListDefine.CategoryNo;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal abstract partial class ModPackage
    {
        int GameId;

        internal ModPackage(int gameId, string path, string pkgId, Version version) =>
            (GameId, PkgPath, PkgId, PkgVersion) = (gameId, path, pkgId, version);

        internal void Register(Category category, string modId, ListInfoBase info) =>
            (ModToId.TryAdd(modId, info.Id) && Human.lstCtrl._table[GameId][category.Index].TryAdd(info.Id, info))
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

        internal static Action<int, string> InitializePackages = (gameId, path) =>
            StpPackage.Collect(gameId, Path.Combine(path, "sardines"))
                .Concat(DevPackage.Collect(gameId, Path.Combine(path, "UserData", "plugins", Plugin.Name, "packages")))
                .GroupBy(item => item.PkgId)
                .ToDictionary(group => group.Key, group => group.OrderBy(item => item.PkgVersion).Last())
                .ForEach(entry => Packages[entry.Key] = entry.Value.With(entry.Value.Initialize));
    }

    internal partial class DevPackage : ModPackage
    {
        DevPackage(int gameId, string path, string pkgId, Version version) : base(gameId, path, pkgId, version) { }

        static Func<int, string, string, Version, DevPackage> ToPackage =
            (gameId, root, path, version) => new DevPackage(gameId, path, ToPkgId(root, path), version);

        static Func<int, string, string, IEnumerable<DevPackage>> ToPackages =
            (gameId, root, path) => ToVersion(Path.GetRelativePath(root, path).Split('-'))
                .Select(ToPackage.Apply(gameId).Apply(root).Apply(path));

        internal static Func<int, string, IEnumerable<ModPackage>> Collect = (gameId, path) =>
            Plugin.DevelopmentMode.Value
                ? Directory.GetDirectories(path).SelectMany(ToPackages.Apply(gameId).Apply(path))
                : Enumerable.Empty<ModPackage>();
    }

    internal partial class StpPackage : ModPackage
    {
        StpPackage(int gameId, string path, string pkgId, Version version) : base(gameId, path, pkgId, version) { }

        static Func<int, string, Version, StpPackage> ToPackage =
            (gameId, path, version) => new StpPackage(gameId, path, ToPkgId(path), version);

        static Func<int, string, IEnumerable<ModPackage>> ToPackages =
            (gameId, path) => ToVersion(Path.GetFileNameWithoutExtension(path).Split('-'))
                .Select(ToPackage.Apply(gameId).Apply(path));

        internal static Func<int, string, IEnumerable<ModPackage>> Collect = (gameId, path) =>
            new DirectoryInfo(path).GetFiles("*.stp", SearchOption.AllDirectories)
                .Select(info => info.FullName).SelectMany(ToPackages.Apply(gameId));
    }

    internal static partial class IOExtension
    {
        internal static void InitializeManifest(this int gameId, string path, string manifest) =>
            AssetBundleManager.ManifestBundlePack[manifest].AllAssetBundles =
                ExtendManifest(manifest).With(ModPackage.InitializePackages.Apply(gameId).Apply(path));

        static string[] ExtendManifest(string manifest) =>
            AssetBundleManager.ManifestBundlePack[manifest].AllAssetBundles
                .Concat([Plugin.AssetBundle]).ToArray();

        internal static LoadedAssetBundle ToAssetBundle(string bundle)
        {
            DigitalCraft.PathManager.Instance
                .GetManifestAndGamePath(ref GameTag, ref bundle, out var manifest, out var path);
            return AssetBundleManager.LoadAssetBundle(ref path, bundle, manifest);
        }

        static UnityEngine.Object ToBodyAsset(string bundle, string asset, Il2CppSystem.Type type) =>
            Plugin.AssetBundle.Equals(bundle)
                ? ModPackage.ToAsset(asset.Split(':'), type)
                : ToAssetBundle(bundle).Bundle.LoadAsset(asset, type);

        static UnityEngine.Object ToBodyAsset(ListInfoBase info, Ktype ab, Ktype data, Il2CppSystem.Type type) =>
            info != null &&
            info.TryGetValue(ab, out var bundle) &&
            info.TryGetValue(data, out var asset)
                ? ToBodyAsset(bundle, asset, type)
                : null;

        static NormalData ToBodyNormal(ListInfoBase info) =>
            info != null &&
            info.TryGetValue(Ktype.MainAB, out var bundle) &&
            info.TryGetValue(Ktype.MainData, out var asset) &&
            Plugin.AssetBundle.Equals(bundle) ? ModPackage.ToNormalData(asset.Split(':')) : null;

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

        internal static unsafe Span<byte> AsSpan(this Il2CppStructArray<byte> array) =>
            new Span<byte>(IntPtr.Add(array.Pointer, sizeof(Il2CppObject) + sizeof(void*) + sizeof(nuint)).ToPointer(), array.Length);
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
    internal static partial class CategoryExtension
    {
        internal const string MainManifest = "abdata";
    }
    [BepInDependency(VarietyOfScales.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";

        public override void Load()
        {
            Patch = new Harmony($"{Name}.Hooks");
            Hooks.ApplyPatches(Patch);
            Instance = this;
            DevelopmentMode = Config.Bind("General", "Enable development package loading.", false);

            Extension.Register<CharaMods, CoordMods>();
            Extension<CharaMods, CoordMods>.OnPreprocessChara += (data, mods) => mods.Apply(data);
            Extension<CharaMods, CoordMods>.OnPreprocessCoord += (data, mods) => mods.Apply(data);
            Extension.OnLoadChara += CharaMods.Store;
            Extension.OnLoadCoord += CoordMods.Store;
        }
    }
}