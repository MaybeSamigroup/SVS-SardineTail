using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Character;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using CoastalSmell;

namespace SardineTail
{
    static partial class Hooks
    {
        static void MaterialHelperLoadPatchMaterialPostfix() =>
            ModPackage.InitializePackages(Paths.GameRootPath);
        static Dictionary<string, MethodInfo[]> SpecPrefixes => new();
        static Dictionary<string, MethodInfo[]> SpecPostfixes => new()
        {
            [nameof(MaterialHelperLoadPatchMaterialPostfix)] = [
                typeof(HumanManager.MaterialHelper).GetMethod(
                    nameof(HumanManager.MaterialHelper.LoadPatchMaterial), 0, [typeof(string)])
            ],
            [nameof(LoadAssetWithoutTypePostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0, [typeof(string)])
            ],
        };
    }
    public partial class EntryWrapper : Il2CppSystem.IO.Stream
    {
        public override void SetLength(long value) =>
            throw new NotImplementedException();
    }
    internal static partial class CategoryExtension
    {
        internal const string AssetPath = "abdata";
        internal const string MainManifest = "abdata";
        internal static void Initialize() =>
            Plugin.HardmodConversion.Value.Maybe(Util<Manager.Game>.Hook.Apply(Convert).Apply(F.DoNothing));
    }
    [BepInDependency(VarietyOfScales.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class Plugin : BasePlugin
    {
        internal static ConfigEntry<bool> AicomiConversion;
        public const string Process = "SamabakeScramble";
        void GameSpecificInitialize()
        {
            AicomiConversion = Config.Bind("General", "Enable aicomi oriented hardmod conversion.", false);
        }
    }
}