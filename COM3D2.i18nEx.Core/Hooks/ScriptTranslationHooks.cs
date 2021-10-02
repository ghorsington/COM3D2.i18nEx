using System.IO;
using System.Linq;
using COM3D2.i18nEx.Core.TranslationManagers;
using COM3D2.i18nEx.Core.Util;
using HarmonyLib;
using Scourt.Loc;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class ScriptTranslationHooks
    {
        private static Harmony instance;
        private static string curScriptFileName;
        private static bool initialized;
        private static string tlSeparator;

        private static string TlSeparator =>
            tlSeparator ??= LocalizationManager.ScriptTranslationMark
                                               .FirstOrDefault(kv => kv.Value == Product.subTitleScenarioLanguage).Key;

        public static void Initialize()
        {
            if (initialized)
                return;

            instance = Harmony.CreateAndPatchAll(typeof(ScriptTranslationHooks),
                                                 "horse.coder.com3d2.i18nex.hooks.scripts");
            initialized = true;
        }

        [HarmonyPatch(typeof(BaseKagManager), nameof(BaseKagManager.TagPlayVoice))]
        [HarmonyPrefix]
        private static void OnPlayVoice(BaseKagManager __instance,
                                        KagTagSupport tag_data,
                                        BaseKagManager.SubtitleData ___subtitle_data)
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
                var data = subData[0];
                ___subtitle_data.text = $"{data.original}<{TlSeparator}>{data.translation}";
                ___subtitle_data.displayTime = data.displayTime;
                ___subtitle_data.addDisplayTime = data.addDisplayTime;
                ___subtitle_data.casinoType = data.isCasino;
            }
            else
            {
                sub.autoDestroy = true;
                foreach (var subtitleData in subData)
                    sub.AddData($"{subtitleData.original}<{TlSeparator}>{subtitleData.translation}",
                                subtitleData.startTime,
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

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslationText), typeof(string))]
        [HarmonyPostfix]
        private static void OnGetTranslationText(ref LocalizationString __result)
        {
            if (!__result.IsEmpty(Product.subTitleScenarioLanguage))
                return;
            var orig = __result[Product.baseScenarioLanguage];
            if (!I2.Loc.LocalizationManager.TryGetTranslation($"SubMaid/{orig}/名前", out var tl))
                tl = Core.ScriptTranslate.GetTranslation(null, orig);
            var tls = __result.ToDictionary(kv => kv.Key, kv => kv.Value);
            tls[Product.subTitleScenarioLanguage] = tl;
            if (!string.IsNullOrEmpty(tl))
                foreach (var language in tls.Keys.ToList())
                    tls[language] = XUATInterop.MarkTranslated(tls[language]);
            __result = new LocalizationString(tls);
        }

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.GetTranslationText), typeof(string))]
        [HarmonyReversePatch]
        private static LocalizationString SplitTranslatedText(string baseText)
        {
            return null;
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
            var translationParts = SplitTranslatedText(text);

            ProcessTranslation(fileName, ref translationParts);

            var orig = translationParts[Product.baseScenarioLanguage];
            var tl = translationParts[Product.subTitleScenarioLanguage];

            if (!string.IsNullOrEmpty(tl))
            {
                text = $"{orig}<{TlSeparator}>{tl}";
                if (Configuration.ScriptTranslations.RerouteTranslationsTo.Value == TranslationsReroute.RouteToJapanese)
                    text = $"{tl}<{TlSeparator}>{tl}";
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
                    text = $"{orig}<{TlSeparator}>{orig}";
                    return true;
                }
            }

            return false;
        }

        private static void ProcessTranslation(string fileName, ref LocalizationString tlString)
        {
            // TODO: Support for multi language
            var orig = tlString[Product.baseScenarioLanguage];
            var tl = tlString[Product.subTitleScenarioLanguage];
            if (string.IsNullOrEmpty(orig))
            {
                if (Configuration.ScriptTranslations.VerboseLogging.Value)
                    Core.Logger.LogInfo(
                                        $"[Script] [{fileName}] \"{orig}\" => \"{tl}\"");
                return;
            }

            if (fileName == null)
            {
                if (Configuration.ScriptTranslations.VerboseLogging.Value)
                    Core.Logger.LogWarning("Found script with no name! Skipping...");
                return;
            }

            fileName = Path.GetFileNameWithoutExtension(fileName);
            var res = Core.ScriptTranslate.GetTranslation(fileName, orig);

            if (!string.IsNullOrEmpty(res))
            {
                var tls = tlString.ToDictionary(kv => kv.Key, kv => kv.Value);
                tls[Product.subTitleScenarioLanguage] = res;
                foreach (var language in tls.Keys.ToList())
                    tls[language] = XUATInterop.MarkTranslated(tls[language]);
                
                tlString = new LocalizationString(tls);
                if (Configuration.ScriptTranslations.VerboseLogging.Value)
                    Core.Logger.LogInfo(
                                        $"[Script] [{fileName}] \"{orig}\" => \"{res}\"");
            }
            else if (Configuration.ScriptTranslations.DumpScriptTranslations.Value)
            {
                if (Core.ScriptTranslate.WriteTranslation(fileName, orig, tl))
                    Core.Logger.LogInfo(
                                        $"[DUMP] [{fileName}] \"{orig}\" => \"{tl}\"");
            }
        }
    }
}
