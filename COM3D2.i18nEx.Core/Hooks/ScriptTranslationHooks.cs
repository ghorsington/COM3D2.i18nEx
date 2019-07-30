using System.Collections.Generic;
using System.IO;
using BepInEx.Harmony;
using COM3D2.i18nEx.Core.TranslationManagers;
using COM3D2.i18nEx.Core.Util;
using HarmonyLib;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class ScriptTranslationHooks
    {
        private static Harmony instance;
        private static string curScriptFileName = null;
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;

            instance = HarmonyWrapper.PatchAll(typeof(ScriptTranslationHooks), "horse.coder.com3d2.i18nex.hooks.scripts");
            initialized = true;
        }

        [HarmonyPatch(typeof(BaseKagManager), nameof(BaseKagManager.TagVRChoicesSet))]
        [HarmonyPatch(typeof(BaseKagManager), nameof(BaseKagManager.TagVRDialog))]
        [HarmonyPatch(typeof(ADVKagManager), nameof(ADVKagManager.TagChoicesRandomSet))]
        [HarmonyPatch(typeof(ADVKagManager), nameof(ADVKagManager.TagChoicesSet))]
        [HarmonyPatch(typeof(ADVKagManager), nameof(ADVKagManager.TagTalk))]
        [HarmonyPrefix]
        private static void LogScriptName(BaseKagManager __instance)
        {
            curScriptFileName = __instance.kag.GetCurrentFileName();
        }

        [HarmonyPatch(typeof(BaseKagManager), nameof(BaseKagManager.TagVRChoicesSet))]
        [HarmonyPatch(typeof(BaseKagManager), nameof(BaseKagManager.TagVRDialog))]
        [HarmonyPatch(typeof(ADVKagManager), nameof(ADVKagManager.TagChoicesRandomSet))]
        [HarmonyPatch(typeof(ADVKagManager), nameof(ADVKagManager.TagChoicesSet))]
        [HarmonyPatch(typeof(ADVKagManager), nameof(ADVKagManager.TagTalk))]
        [HarmonyPostfix]
        private static void ClearScriptName()
        {
            curScriptFileName = null;
        }

        [HarmonyPatch(typeof(ScriptManager), nameof(ScriptManager.ReplaceCharaName), typeof(string))]
        [HarmonyPrefix]
        private static void ReplaceCharaName(ref string text)
        {
            if (curScriptFileName == null)
                return;

            TranslateLine(curScriptFileName, ref text);
        }

        [HarmonyPatch(typeof(KagScript), "GetText")]
        [HarmonyPostfix]
        private static void KagScriptGetText(KagScript __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
                return;

            TranslateLine(__instance.GetCurrentFileName(), ref __result);
        }

        private static bool TranslateLine(string fileName, ref string text, bool stop = false)
        {
            var translationParts = text.SplitTranslation();

            ProcessTranslation(fileName, ref translationParts);

            if (!string.IsNullOrEmpty(translationParts.Value))
            {
                text = $"{translationParts.Key}<E>{translationParts.Value}";
                return true;
            }

            if (!stop)
            {
                var t = text.Replace("……", "…");
                if (t != text && TranslateLine(fileName, ref t, true))
                    text = t;
            }
            return false;
        }

        private static void ProcessTranslation(string fileName, ref KeyValuePair<string, string> translationPair)
        {
            if (string.IsNullOrEmpty(translationPair.Key))
                return;

            if (fileName == null)
            {
                Core.Logger.LogWarning("Found script with no name! Skipping...");
                return;
            }

            fileName = Path.GetFileNameWithoutExtension(fileName);
            var res = Core.ScriptTranslate.GetTranslation(fileName, translationPair.Key);

            if (!string.IsNullOrEmpty(res))
                translationPair = new KeyValuePair<string, string>(translationPair.Key, res);
            else if (Configuration.ScriptTranslations.DumpScriptTranslations.Value)
            {
                if (Core.ScriptTranslate.WriteTranslation(fileName, translationPair.Key, translationPair.Value))
                    Core.Logger.LogInfo($"[{fileName}] \"{translationPair.Key}\" => \"{translationPair.Value}\"");
            }
        }
    }
}