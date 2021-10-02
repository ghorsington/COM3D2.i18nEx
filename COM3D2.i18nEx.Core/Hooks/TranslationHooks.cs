using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using COM3D2.i18nEx.Core.Util;
using HarmonyLib;
using I2.Loc;
using UnityEngine;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class TranslationHooks
    {
        private static bool initialized;
        private static Harmony instance;

        public static void Initialize()
        {
            if (initialized)
                return;

            ScriptTranslationHooks.Initialize();
            TextureReplaceHooks.Initialize();
            UIFixes.Initialize();

            instance = Harmony.CreateAndPatchAll(typeof(TranslationHooks), "horse.coder.i18nex.hooks.base");

            initialized = true;
        }

        [HarmonyPatch(typeof(Product), nameof(Product.subTitleScenarioLanguage), MethodType.Getter)]
        [HarmonyPostfix]
        private static void SupportSubtitle(ref Product.Language __result)
        {
            // TODO: Might need smarter logic if/when game supports multiple TL
            __result = Product.Language.English;
        }

        [HarmonyPatch(typeof(Product), nameof(Product.IsSupportLanguage))]
        [HarmonyPrefix]
        private static bool OnIsSupportLanguage(ref bool __result)
        {
            // TODO: Might need smarter logic if/when game supports multiple TL
            __result = true;
            return false;
        }

        [HarmonyPatch(typeof(Product), nameof(Product.supportSubtitles), MethodType.Getter)]
        [HarmonyPostfix]
        private static void SupportSubtitle(ref bool __result)
        {
            __result = true;
        }

        [HarmonyPatch(typeof(Product), nameof(Product.supportMultiLanguage), MethodType.Getter)]
        [HarmonyPostfix]
        private static void SupportMultiLanguage(ref bool __result)
        {
            __result = true;
        }

        [HarmonyPatch(typeof(Product), nameof(Product.isJapan), MethodType.Getter)]
        [HarmonyPostfix]
        private static void IsJapan(ref bool __result)
        {
            __result = false;
        }

        [HarmonyPatch(typeof(SceneNetorareCheck), "Start")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> FixNTRCheckScene(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var ins in instructions)
                if (ins.opcode == OpCodes.Call && ins.operand is MethodInfo minfo && minfo.Name == "get_isJapan")
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                else
                    yield return ins;
        }

        [HarmonyPatch(typeof(SubtitleDisplayManager), nameof(SubtitleDisplayManager.messageBgAlpha), MethodType.Setter)]
        [HarmonyPrefix]
        private static bool OnGetConfigMessageAlpha(SubtitleDisplayManager __instance, ref float value)
        {
            var parent = __instance.transform.parent;
            if (Configuration.I2Translation.OverrideSubtitleOpacity.Value &&
                parent                                                    && parent.name == "YotogiPlayPanel")
            {
                if (Math.Abs(value - __instance.messageBgAlpha) < 0.001)
                    return false;
                value = Mathf.Clamp(Configuration.I2Translation.SubtitleOpacity.Value, 0f, 1f);
            }

            return true;
        }

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslation))]
        [HarmonyPostfix]
        private static void OnGetTranslation(ref string __result,
                                             string Term,
                                             bool FixForRTL,
                                             int maxLineLengthForRTL,
                                             bool ignoreRTLnumbers,
                                             bool applyParameters,
                                             GameObject localParametersRoot,
                                             string overrideLanguage)
        {
            if (overrideLanguage != "Japanese" &&
                (__result.IsNullOrWhiteSpace() || __result.IndexOf('/') >= 0 && Term.Contains(__result)))
                __result = LocalizationManager.GetTranslation(Term, FixForRTL, maxLineLengthForRTL, ignoreRTLnumbers,
                                                              applyParameters, localParametersRoot, "Japanese");
            else if (Configuration.I2Translation.VerboseLogging.Value)
                Core.Logger.LogInfo($"[I2Loc] Translating term \"{Term}\" => \"{__result}\"");
        }

        [HarmonyPatch(typeof(ConfigMgr), nameof(ConfigMgr.Update))]
        [HarmonyPrefix]
        private static bool OnConfigMgrUpdate()
        {
            return false;
        }
    }
}
