using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reactive.Linq; 
using UnityEngine;
using Character;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using CoastalSmell;

namespace SardineTail
{
    internal abstract partial class ModPackage
    {
        internal static readonly int[] IDS = [20];
    }
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
    }
    internal static partial class IOExtension
    {
        internal static void OverrideColors(HumanBody body) =>
            ToOverrideRenderers(body).Select(renderer => renderer.material).ForEach(ApplyColors.Apply(body.fileBody));
        internal static void OverrideGraphic(HumanBody body) =>
            body._graphicDisposables.Add(body.graphic.AddEvent(
                ToOverrideRenderers(body).ToArray(), HumanGraphic.UpdateFlags.All)); 
    }

    [BepInDependency(VarietyOfScales.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class Plugin : BasePlugin
    {
        public const string Process = "SamabakeScramble";
        internal static ConfigEntry<bool> HardmodConversion;
        internal static ConfigEntry<bool> AicomiConversion;
        public Plugin() : base() =>
            (Instance, DevelopmentMode, HardmodConversion, AicomiConversion) = (
                this,
                Config.Bind("General", "Enable development package loading.", false),
                Config.Bind("General", "Enable hardmod conversion at startup.", false),
                Config.Bind("General", "Enable aicomi oriented hardmod conversion.", false)
            );
        IDisposable[] Initialize() => [
            SingletonInitializerExtension<Manager.Game>.OnStartup
                .Where(_ => Config.Bind("General", "Enable hardmod conversion at startup.", false).Value)
                .FirstAsync().Subscribe(_ => CategoryExtension.Convert())
        ];
    }
}