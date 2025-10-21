using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Character;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using CoastalSmell;

namespace SardineTail
{
    static partial class Hooks
    {
        static void MaterialHelperLoadPatchMaterialPostfix() =>
            ModPackage.InitializePackages(Paths.GameRootPath);

        static void ChaListControlLoadListInfoAllPostfix() =>
            Plugin.HardmodConversion.Value.Maybe(CategoryExtension.Convert);

        static Dictionary<string, MethodInfo[]> SpecPrefixes => new();
        static Dictionary<string, MethodInfo[]> SpecPostfixes => new()
        {
            [nameof(MaterialHelperLoadPatchMaterialPostfix)] = [
                typeof(HumanManager.MaterialHelper).GetMethod(
                    nameof(HumanManager.MaterialHelper.LoadPatchMaterial), 0, [typeof(string)])
            ],
            [nameof(ChaListControlLoadListInfoAllPostfix)] = [
                typeof(ChaListControl).GetMethod(
                    nameof(ChaListControl.LoadListInfoAll), 0, [])
            ]
        };
    }
    internal static partial class CategoryExtension
    {
        internal const string AssetPath = "lib";
        internal const string MainManifest = "lib000_03";
        internal static void Initialize() { } 
    }
    internal static partial class IOExtension
    {
        static void Translate(Material material, string original) =>
            (material.shader = original switch
            {
                "lif_main_acs" => Shader.Find("AC/acs"),
                "lif_main_acs_alpha" => Shader.Find("AC/acs_alpha"),
                "lif_main_cloth" => Shader.Find("AC/cloth"),
                "lif_main_cloth_socks" => Shader.Find("AC/cloth"),
                "lif_main_cloth_alpha" => Shader.Find("AC/cloth_alpha"),
                "lif_main_cloth_socks_alpha" => Shader.Find("AC/cloth_alpha"),
                "lif_main_eye" => Shader.Find("AC/eye"),
                "lif_main_eyebrow" => Shader.Find("AC/eyebrow"),
                "lif_main_eyelash_up" => Shader.Find("AC/eyelash_up"),
                "lif_main_eyelid" => Shader.Find("AC/eyelid"),
                "lif_main_hair" => Shader.Find("AC/hair"),
                "lif_main_hair_outline" => Shader.Find("AC/hair_outline"),
                "lif_main_nail" => Shader.Find("AC/nail"),
                "lif_main_skin_body" => Shader.Find("AC/skin_body"),
                "lif_main_skin_head" => Shader.Find("AC/skin_head"),
                "lif_namida" => Shader.Find("AC/sub/namida"),
                "lif_silhouette" => Shader.Find("AC/sub/silhouette"),
                "lif_sub_mnpb_urp" => Shader.Find("AC/sub/mnpb"),
                "lif_unlit2d" => Shader.Find("AC/sub/unlit2d"),
                "LIF/lif_main_acs" => Shader.Find("AC/acs"),
                "LIF/lif_main_acs_alpha" => Shader.Find("AC/acs_alpha"),
                "LIF/lif_main_cloth" => Shader.Find("AC/cloth"),
                "LIF/lif_main_cloth_socks" => Shader.Find("AC/cloth"),
                "LIF/lif_main_cloth_alpha" => Shader.Find("AC/cloth_alpha"),
                "LIF/lif_main_cloth_socks_alpha" => Shader.Find("AC/cloth_alpha"),
                "LIF/lif_main_eye" => Shader.Find("AC/eye"),
                "LIF/lif_main_eyebrow" => Shader.Find("AC/eyebrow"),
                "LIF/lif_main_eyelash_up" => Shader.Find("AC/eyelash_up"),
                "LIF/lif_main_eyelid" => Shader.Find("AC/eyelid"),
                "LIF/lif_main_hair" => Shader.Find("AC/hair"),
                "LIF/lif_main_hair_outline" => Shader.Find("AC/hair_outline"),
                "LIF/lif_main_nail" => Shader.Find("AC/nail"),
                "LIF/lif_main_skin_body" => Shader.Find("AC/skin_body"),
                "LIF/lif_main_skin_head" => Shader.Find("AC/skin_head"),
                "LIF/lif_namida" => Shader.Find("AC/sub/namida"),
                "LIF/lif_silhouette" => Shader.Find("AC/sub/silhouette"),
                "LIF/lif_sub_mnpb_urp" => Shader.Find("AC/sub/mnpb"),
                "LIF/lif_unlit2d" => Shader.Find("AC/sub/unlit2d"),
                _ => material.shader
            }).With(name => Plugin.Instance.Log.LogDebug($"shader translation: {original} => {material.shader.name}"));

        static void TranslateShader(Material material) => Translate(material, material.shader.name);

        static void TranslateShader(GameObject go) =>
            go.GetComponentsInChildren<Renderer>(true)
                .Select(renderer => renderer.material).ForEach(TranslateShader);

        internal static void TranslateShader(UnityEngine.Object prefab) =>
            TranslateShader(new GameObject(prefab.Pointer));

        internal static UnityEngine.Object TranslateShaderProc(UnityEngine.Object prefab) => prefab.With(TranslateShader);

        internal static UnityEngine.Object TranslateShaderSkip(UnityEngine.Object prefab) => prefab;

        internal static Func<UnityEngine.Object, UnityEngine.Object> PreprocessPrefab = TranslateShaderSkip;

    }
    public partial class Plugin : BasePlugin
    {
        public const string Process = "Aicomi";
        internal static ConfigEntry<bool> ShaderTranslation;
        void GameSpecificInitialize()
        {
            ShaderTranslation = Config.Bind("General", "Enable runtime shader translation (requires restart).", false);
            IOExtension.PreprocessPrefab = ShaderTranslation.Value ? IOExtension.TranslateShaderProc : IOExtension.TranslateShaderSkip;
        }
    }
}