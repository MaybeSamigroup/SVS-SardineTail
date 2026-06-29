using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using UnityEngine;
using Character;
#if Aicomi
using ILLGAMES.Unity;
#else
using ILLGames.Unity;
#endif
using HarmonyLib;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Configuration;
using CoastalSmell;
using Ktype = ChaListDefine.KeyType;

namespace SardineTail
{
    internal static partial class ModificationExtension
    {
        static UnityEngine.Object ToBodyAsset(ListInfoBase info, Ktype ab, Ktype data, Il2CppSystem.Type type) =>
            (info != null) &&
            info.TryGetValue(ab, out var bundle) &&
            info.TryGetValue(data, out var asset) &&
            info.TryGetValue(Ktype.MainManifest, out var manifest)
                ? ToBodyAsset(bundle, asset, manifest, type)
                : null;

        static NormalData ToBodyNormal(ListInfoBase info) =>
            info != null &&
            info.TryGetValue(Ktype.MainAB, out var bundle) &&
            info.TryGetValue(Ktype.MainData, out var asset) &&
            Plugin.AssetBundle.Equals(bundle) ? AssetBundles.ToNormalData(asset.Split(':')) : null;

        internal static IEnumerable<Renderer> ToOverrideRenderers(HumanBody body) =>
            ToOverrideRenderers(body.GetRefObject(Table.RefObjKey.S_Son))
                .Where(renderer => body.rendBody.material.shader.name.Equals(renderer?.material?.shader?.name));

        static IEnumerable<Renderer> ToOverrideRenderers(GameObject go) =>
            Enumerable.Range(0, go.transform.childCount)
                .Select(go.transform.GetChild).Where(tf => tf.name is "o_dan_f" or "o_dankon")
                .Select(tf => tf.gameObject.GetComponent<Renderer>()).Where(renderer => renderer != null);

        static Action<HumanDataBody, Material> ApplyColors =
            new Action<HumanDataBody, Material>[]
            {
                (data, material) => material.SetColor("_MainColor", data.skinMainColor),
                (data, material) => material.SetColor(ChaShader.Body.sHighlightColorID, data.skinHighlightColor),
                (data, material) => material.SetColor(ChaShader.Body.sShadowColorID, data.skinShadowColor),
            }.Concat(
                new int[] { ChaShader.Body.sDetailColor01, ChaShader.Body.sDetailColor02, ChaShader.Body.sDetailColor03 }
                .Select<int, Action<HumanDataBody, Material>>((id, idx) =>
                    (data, material) => (idx < data.skinDetailColors.Count)
                        .Maybe(F.Apply(material.SetColor, id, data.skinDetailColors[idx])))
            ).Aggregate((a, b) => a + b);
    }
    static partial class Hooks
    {
        const string BodyPrefabAB = "chara/body/body_00.unity3d";
        const string BodyPrefabM = "p_cm_sv_body_00";
        const string BodyPrefabF = "p_cf_sv_body_00";
        const string BodyTextureAB = "chara/body/bo_body_000_00.unity3d";
        const string BodyTexture = "cf_body_00_t";
        const string AicomiBodyShapeAnimeAB = "chara/list/customshape.unity3d";
        const string BodyShapeAnimeAB = "list/customshape.unity3d";
        const string BodyShapeAnime = "cf_anmShapeBody";
        static bool PreventRedirect = false;
        static void EnableRedirect() =>
            PreventRedirect = false;
        static void DisableRedirect() =>
            PreventRedirect = true;
        static Func<NormalData, NormalData> BustNormalOverrideProc = ModificationExtension.ToBodyNormal;
        static Func<NormalData, NormalData> BustNormalOverrideSkip = normalData => normalData;
        static Func<NormalData, NormalData> BustNormalOverride = BustNormalOverrideSkip;

        static void BustNormalInitializePrefix(ref NormalData normalData) =>
            (normalData = BustNormalOverride(normalData.With(DisableRedirect))).With(EnableRedirect);
#if Aicomi
        static void HumanBodyLoadPrefix(HumanBody __instance) =>
            BustNormalOverride = BustNormalOverrideProc.With(F.Apply(ModificationExtension.OverrideFigure, __instance._human));
#else
        static void HumanBodyLoadPrefix(HumanBody __instance) =>
            BustNormalOverride = BustNormalOverrideProc.With(F.Apply(ModificationExtension.OverrideFigure, __instance.human));
#endif
        static void HumanBodyLoadPostfix(HumanBody __instance) =>
            BustNormalOverride = BustNormalOverrideSkip.With(F.Apply(ModificationExtension.OverrideGraphic, __instance));
        static void HumanBodyCreateBodyTexturePostfix(HumanBody __instance) =>
            ModificationExtension.OverrideColors(__instance);

        static void LoadAssetPostfix(AssetBundle __instance, string name, Il2CppSystem.Type type, ref UnityEngine.Object __result) =>
            __result = PreventRedirect ? __result : ((__instance.name, name).With(DisableRedirect) switch
            {
                (Plugin.AssetBundle, _) => AssetBundles.ToAsset(name.Split(':'), type),
                (BodyPrefabAB, BodyPrefabM) => ModificationExtension.ToBodyPrefab(BodyPrefabM) ?? __result,
                (BodyPrefabAB, BodyPrefabF) => ModificationExtension.ToBodyPrefab(BodyPrefabF) ?? __result,
                (BodyTextureAB, BodyTexture) => ModificationExtension.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => ModificationExtension.ToBodyShapeAnime() ?? __result,
                (AicomiBodyShapeAnimeAB, BodyShapeAnime) => ModificationExtension.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result).With(EnableRedirect);

        static void LoadAssetWithoutTypePostfix(AssetBundle __instance, string name, ref UnityEngine.Object __result) =>
            __result = PreventRedirect ? __result : ((__instance.name, name).With(DisableRedirect) switch
            {
                (Plugin.AssetBundle, _) => AssetBundles.ToAsset(name.Split(':')),
                (BodyPrefabAB, BodyPrefabM) => ModificationExtension.ToBodyPrefab(BodyPrefabM) ?? __result,
                (BodyPrefabAB, BodyPrefabF) => ModificationExtension.ToBodyPrefab(BodyPrefabF) ?? __result,
                (BodyTextureAB, BodyTexture) => ModificationExtension.ToBodyTexture() ?? __result,
                (BodyShapeAnimeAB, BodyShapeAnime) => ModificationExtension.ToBodyShapeAnime() ?? __result,
                (AicomiBodyShapeAnimeAB, BodyShapeAnime) => ModificationExtension.ToBodyShapeAnime() ?? __result,
                _ => null
            } ?? __result).With(EnableRedirect);

        static Dictionary<string, MethodInfo[]> Prefixes => new()
        {
            [nameof(BustNormalInitializePrefix)] = [
                typeof(BustNormal).GetMethod(nameof(BustNormal.Initialize), 0, [typeof(GameObject), typeof(NormalData)])
            ],
            [nameof(HumanBodyLoadPrefix)] = [
                typeof(HumanBody).GetMethod(nameof(HumanBody.Load), 0, [typeof(Transform), typeof(string)])
            ]
        };

        static Dictionary<string, MethodInfo[]> Postfixes => new()
        {
            [nameof(LoadAssetPostfix)] = [
                typeof(AssetBundle).GetMethod(nameof(AssetBundle.LoadAsset), 0, [typeof(string), typeof(Il2CppSystem.Type)])
            ],
            [nameof(HumanBodyLoadPostfix)] = [
                typeof(HumanBody).GetMethod(nameof(HumanBody.Load), 0, [typeof(Transform), typeof(string)])
            ],
            [nameof(HumanBodyCreateBodyTexturePostfix)] = [
                typeof(HumanBody).GetMethod(nameof(HumanBody.CreateBodyTexture), 0, [])
            ]
        };

        static void ApplyPrefixes(Harmony hi) =>
            Prefixes.Concat(SpecPrefixes).ForEach(entry => entry.Value.ForEach(method =>
                hi.Patch(method, prefix: new HarmonyMethod(typeof(Hooks), entry.Key) { wrapTryCatch = true })));

        static void ApplyPostfixes(Harmony hi) =>
            Postfixes.Concat(SpecPostfixes).ForEach(entry => entry.Value.ForEach(method =>
                hi.Patch(method, postfix: new HarmonyMethod(typeof(Hooks), entry.Key) { wrapTryCatch = true })));

        internal static IDisposable Initialize() =>
            Disposable.Create(new Harmony($"Hooks.{Plugin.Name}").With(ApplyPrefixes).With(ApplyPostfixes).UnpatchSelf);
    }

    [BepInProcess(Process)]
    [BepInDependency(Fishbone.Plugin.Guid)]
    [BepInPlugin(Guid, Name, Version)]
    public partial class Plugin : BasePlugin
    {
        public const string Name = "SardineTail";
        public const string Version = "2.2.1";
        public const string Guid = $"{Process}.{Name}";
        internal const string AssetBundle = "sardinetail.unity3d";
        internal static ConfigEntry<bool> DevelopmentMode;
        internal static Plugin Instance;
        CompositeDisposable Subscriptions;
        public override void Load() =>
            Subscriptions = [Hooks.Initialize(), .. ModificationExtension.Initialize(), .. Initialize()];
        public override bool Unload() =>
            true.With(Subscriptions.Dispose) && base.Unload();
    }
}