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

    [BepInDependency(VarietyOfScales.Plugin.Guid, BepInDependency.DependencyFlags.SoftDependency)]
    public partial class Plugin : BasePlugin
    {
        public const GameId GAME_ID = GameId.SVS;
        public const string Process = "SamabakeScramble";
        internal static ConfigEntry<bool> HardmodConversion;
        internal static ConfigEntry<bool> CommonConversion;
        public Plugin() : base() =>
            (Instance, DevelopmentMode, HardmodConversion, CommonConversion) = (
                this,
                Config.Bind("General", "Enable development package loading.", false),
                Config.Bind("General", "Enable hardmod conversion at startup.", false),
                Config.Bind("General", "Enable MainManifest restricted hardmod conversion.", false)
            );
        IDisposable[] Initialize() => [
            AssetBundles.OnPrefabLoad.Where(_ => Config
                .Bind("General", "Enable runtime shader translation.", false).Value).Subscribe(TranslateShader),
            SingletonInitializerExtension<Manager.Game>.OnStartup
                .Where(_ => Config.Bind("General", "Enable hardmod conversion at startup.", false).Value)
                .FirstAsync().Subscribe(_ => ModificationExtension.Convert())
        ];

        static void TranslateShader(GameObject go) =>
            go.GetComponentsInChildren<Renderer>(true)
                .Select(renderer => renderer.material).ForEach(TranslateShader);

        static void TranslateShader(Material material) => Translate(material, material.shader.name);

        static void Translate(Material material, string original) =>
            (material.shader = original switch
            {
                "lif_main_acs" => Shader.Find("LIF/lif_main_acs"),
                "lif_main_acs_alpha" => Shader.Find("LIF/lif_main_acs_alpha"),
                "lif_main_cloth" => Shader.Find("LIF/lif_main_cloth"),
                "lif_main_cloth_socks" => Shader.Find("LIF/lif_main_cloth_socks"),
                "lif_main_cloth_alpha" => Shader.Find("LIF/lif_main_cloth_alpha"),
                "lif_main_cloth_socks_alpha" => Shader.Find("LIF/lif_main_cloth_socks_alpha"),
                "lif_main_eye" => Shader.Find("LIF/lif_main_eye"),
                "lif_main_eyebrow" => Shader.Find("LIF/lif_main_eyebrow"),
                "lif_main_eyelash_up" => Shader.Find("LIF/lif_main_eyelash_up"),
                "lif_main_eyelid" => Shader.Find("LIF/lif_main_eyelid"),
                "lif_main_hair" => Shader.Find("LIF/lif_main_hair"),
                "lif_main_hair_outline" => Shader.Find("LIF/lif_main_hair_outline"),
                "lif_main_nail" => Shader.Find("LIF/lif_main_nail"),
                "lif_main_skin_body" => Shader.Find("LIF/lif_main_skin_body"),
                "lif_main_skin_head" => Shader.Find("LIF/lif_main_skin_head"),
                "lif_namida" => Shader.Find("LIF/lif_namida"),
                "lif_silhouette" => Shader.Find("LIF/lif_silhouette"),
                "lif_sub_mnpb_urp" => Shader.Find("LIF/lif_sub_mnpb_urp"),
                "lif_unlit2d" => Shader.Find("LIF/lif_unlit2d"),
                "AC/acs" => Shader.Find("LIF/lif_main_acs"),
                "AC/acs_alpha" => Shader.Find("LIF/lif_main_acs_alpha"),
                "AC/cloth" => Shader.Find("LIF/lif_main_cloth"),
                "AC/cloth_alpha" => Shader.Find("LIF/lif_main_cloth_alpha"),
                "AC/eye" => Shader.Find("LIF/lif_main_eye"),
                "AC/eyebrow" => Shader.Find("LIF/lif_main_eyebrow"),
                "AC/eyelash_up" => Shader.Find("LIF/lif_main_eyelash_up"),
                "AC/eyelid" => Shader.Find("LIF/lif_main_eyelid"),
                "AC/hair" => Shader.Find("LIF/lif_main_hair"),
                "AC/hair_outline" => Shader.Find("LIF/lif_main_hair_outline"),
                "AC/nail" => Shader.Find("LIF/lif_main_nail"),
                "AC/skin_body" => Shader.Find("LIF/lif_main_skin_body"),
                "AC/skin_head" => Shader.Find("LIF/lif_main_skin_head"),
                "AC/sub/namida" => Shader.Find("LIF/lif_namida"),
                "AC/sub/silhouette" => Shader.Find("LIF/lif_silhouette"),
                "AC/sub/mnpb" => Shader.Find("LIF/lif_sub_mnpb_urp"),
                "AC/sub/unlit2d" => Shader.Find("LIF/lif_unlit2d"),
                _ => material.shader
            }).With(name => Instance.Log.LogDebug($"shader translation: {original} => {material.shader.name}"));
    }
}