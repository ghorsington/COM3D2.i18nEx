using System.Collections.Generic;
using BepInEx.Harmony;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.i18nEx.Core
{
    internal static class TranslationEvents
    {
        private static Harmony instance;
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;
            instance = HarmonyWrapper.PatchAll(typeof(TranslationEvents), "horse.coder.com3d2.i18nex");
            initialized = true;
        }

        [HarmonyPatch(typeof(MessageClass), nameof(MessageClass.GetTranslationText))]
        [HarmonyPostfix]
        private static void GetTranslationText(ref KeyValuePair<string, string> __result)
        {
            var fileName = GameMain.Instance.ScriptMgr.adv_kag.kag.GetCurrentFileName();
            InternationalizationCore.Logger.LogInfo($"[{fileName}] \"{__result.Key}\" => \"{__result.Value}\"");
        }

        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
        [HarmonyPrefix]
        private static bool LoadTexture(ref TextureResource __result, AFileSystemBase f_fileSystem, string f_strFileName, bool usePoolBuffer)
        {
            InternationalizationCore.Logger.LogInfo($"[Texture] \"{f_strFileName}\"");
            return false;
        }

        [HarmonyPatch(typeof(UIWidget), nameof(UIWidget.mainTexture), MethodType.Getter)]
        [HarmonyPrefix]
        private static bool GetMainTexture(UIWidget __instance, ref Texture __result)
        {
            InternationalizationCore.Logger.LogInfo($"[UIWidget:{__instance.GetType().FullName}] \"{__instance.name}\"");
            return false;
        }
    }
}
