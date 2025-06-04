using HarmonyLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System;
using System.IO;
using System.Linq;
using Character;
using Fishbone;

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
                (data, archive) => archive.Load(data);
            Event.OnPreCoordinateDeserialize +=
                (_, data, limits, archive, current) => data.With(archive.Load(limits)).With(current.Save);
        }
    }
    internal static partial class Hooks
    {
        [HarmonyPostfix]
        [HarmonyWrapSafe]
        [HarmonyPatch(typeof(HumanManager.MaterialHelper), nameof(HumanManager.MaterialHelper.LoadPatchMaterial))]
        static void MaterialHelperLoadPatchMaterialPostfix(int gameID, string game) =>
            gameID.InitializeManifest(DigitalCraft.PathManager.Instance.GetMainManifestFromID(gameID), Path.Combine(game, ".."));
    }
    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "DigitalCraft";
        public const string Guid = $"{Process}.{Name}";
        internal static ConfigEntry<bool> DevelopmentMode;
        private Harmony Patch;
        public override void Load() =>
            Patch = Harmony.CreateAndPatchAll(typeof(Hooks), $"{Name}.Hooks")
                .With(() => Instance = this)
                .With(() => DevelopmentMode = Config.Bind("General", "Enable development package loading.", false))
                .With(ModificationExtensions.Initialize);
        public override bool Unload() =>
            true.With(Patch.UnpatchSelf) && base.Unload();
    }
}