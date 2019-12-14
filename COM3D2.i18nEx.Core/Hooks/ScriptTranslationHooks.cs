using System.Collections.Generic;
using System.IO;
using BepInEx.Harmony;
using COM3D2.i18nEx.Core.TranslationManagers;
using COM3D2.i18nEx.Core.Util;
using HarmonyLib;
using I2.Loc;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class ScriptTranslationHooks
    {
        private static Harmony instance;
        private static string curScriptFileName;
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;

            instance = HarmonyWrapper.PatchAll(typeof(ScriptTranslationHooks),
                                               "horse.coder.com3d2.i18nex.hooks.scripts");
            initialized = true;
        }

        [HarmonyPatch(typeof(BaseKagManager), nameof(BaseKagManager.TagPlayVoice))]
        [HarmonyPrefix]
        private static void OnPlayVoice(BaseKagManager __instance, KagTagSupport tag_data, object ___subtitle_data)
        {
            __instance.CheckAbsolutelyNecessaryTag(tag_data, "playvoice", "voice");

            var voice = tag_data.GetTagProperty("voice").AsString();
            var subData =
                Core.ScriptTranslate.GetSubtitle(Path.GetFileNameWithoutExtension(__instance.kag.GetCurrentFileName()),
                                                 voice);

            if (subData == null)
                return;

            var sub = SubtitleMovieManager.GetGlobalInstance(false);
            sub.Clear();

            if (subData.Count == 1 && subData[0].startTime == 0)
            {
                subData[0].SetSubtitleData(___subtitle_data);
            }
            else
            {
                sub.autoDestroy = true;
                foreach (var subtitleData in subData)
                    sub.AddData($"{subtitleData.original}<E>{subtitleData.translation}", subtitleData.startTime,
                                subtitleData.displayTime);
                sub.Play();
            }
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
            if (string.IsNullOrEmpty(curScriptFileName))
                return;

            TranslateLine(curScriptFileName, ref text);
        }

        [HarmonyPatch(typeof(MessageClass), nameof(MessageClass.GetTranslationText))]
        [HarmonyPostfix]
        private static void OnGetTranslationText(ref KeyValuePair<string, string> __result)
        {
            if (!string.IsNullOrEmpty(__result.Key) && string.IsNullOrEmpty(__result.Value))
            {
                if (!LocalizationManager.TryGetTranslation($"SubMaid/{__result.Key}/名前", out var tl))
                    tl = Core.ScriptTranslate.GetTranslation(null, __result.Key);
                __result = new KeyValuePair<string, string>(__result.Key, tl);
            }
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
                if (Configuration.ScriptTranslations.RerouteTranslationsTo.Value == TranslationsReroute.RouteToJapanese)
                    text = $"{translationParts.Value}<E>{translationParts.Value}";
                return true;
            }

            if (!stop)
            {
                var t = text.Replace("……", "…");
                if (t != text && TranslateLine(fileName, ref t, true))
                {
                    text = t;
                    return true;
                }

                if (Configuration.ScriptTranslations.RerouteTranslationsTo.Value == TranslationsReroute.RouteToEnglish)
                {
                    text = $"{translationParts.Key}<E>{translationParts.Key}";
                    return true;
                }
            }

            return false;
        }

        private static void ProcessTranslation(string fileName, ref KeyValuePair<string, string> translationPair)
        {
            if (string.IsNullOrEmpty(translationPair.Key))
            {
                if (Configuration.ScriptTranslations.VerboseLogging.Value)
                    Core.Logger.LogInfo(
                                        $"[Script] [{fileName}] \"{translationPair.Key}\" => \"{translationPair.Value}\"");
                return;
            }

            if (fileName == null)
            {
                if (Configuration.ScriptTranslations.VerboseLogging.Value)
                    Core.Logger.LogWarning("Found script with no name! Skipping...");
                return;
            }

            fileName = Path.GetFileNameWithoutExtension(fileName);
            var res = Core.ScriptTranslate.GetTranslation(fileName, translationPair.Key);

            if (!string.IsNullOrEmpty(res))
            {
                translationPair = new KeyValuePair<string, string>(translationPair.Key, res);
                if (Configuration.ScriptTranslations.VerboseLogging.Value)
                    Core.Logger.LogInfo(
                                        $"[Script] [{fileName}] \"{translationPair.Key}\" => \"{translationPair.Value}\"");
            }
            else if (Configuration.ScriptTranslations.DumpScriptTranslations.Value)
            {
                if (Core.ScriptTranslate.WriteTranslation(fileName, translationPair.Key, translationPair.Value))
                    Core.Logger.LogInfo(
                                        $"[DUMP] [{fileName}] \"{translationPair.Key}\" => \"{translationPair.Value}\"");
            }
        }
    }
}