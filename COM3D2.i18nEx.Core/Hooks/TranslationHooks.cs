using System.Collections.Generic;
using System.IO;
using BepInEx.Harmony;
using COM3D2.i18nEx.Core.TranslationManagers;
using COM3D2.i18nEx.Core.Util;
using HarmonyLib;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class TranslationHooks
    {
        private static Harmony instance;
        private static bool initialized;

        public static void Initialize()
        {
            if (initialized)
                return;
            instance = HarmonyWrapper.PatchAll(typeof(TranslationHooks), "horse.coder.com3d2.i18nex");
            initialized = true;
        }

        [HarmonyPatch(typeof(KagScript), "GetText")]
        [HarmonyPostfix]
        private static void KagScriptGetText(KagScript __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result))
                return;

            var fileName = __instance.GetCurrentFileName();
            var translationParts = __result.SplitTranslation();

            ProcessTranslation(fileName, ref translationParts);

            if (!string.IsNullOrEmpty(translationParts.Value))
                __result = $"{translationParts.Key}<E>{translationParts.Value}";
        }


        private static void ProcessTranslation(string fileName, ref KeyValuePair<string, string> __result)
        {
            if (string.IsNullOrEmpty(__result.Key))
                return;

            if (fileName == null)
            {
                Core.Logger.LogWarning("Found script with no name! Skipping...");
                return;
            }

            fileName = Path.GetFileNameWithoutExtension(fileName);
            var res = ScriptTranslationManager.GetTranslation(fileName, __result.Key);

            if (!string.IsNullOrEmpty(res))
                __result = new KeyValuePair<string, string>(__result.Key, res);
            else if (Configuration.DumpScriptTranslations.Value)
            {
                if (ScriptTranslationManager.WriteTranslation(fileName, __result.Key, __result.Value))
                    Core.Logger.LogInfo($"[{fileName}] \"{__result.Key}\" => \"{__result.Value}\"");
            }
        }

        [HarmonyPatch(typeof(MessageClass), nameof(MessageClass.GetTranslationText))]
        [HarmonyPostfix]
        private static void GetTranslationText(ref KeyValuePair<string, string> __result)
        {
            Core.Logger.LogInfo($"\"{__result.Key}\" => \"{__result.Value}\"");
        }

        //[HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
        //[HarmonyPrefix]
        //private static bool LoadTexture(ref TextureResource __result, AFileSystemBase f_fileSystem, string f_strFileName, bool usePoolBuffer)
        //{
        //    Core.Logger.LogInfo($"[Texture] \"{f_strFileName}\"");
        //    return true;
        //}

        //[HarmonyPatch(typeof(UIWidget), nameof(UIWidget.mainTexture), MethodType.Getter)]
        //[HarmonyPrefix]
        //private static bool GetMainTexture(UIWidget __instance, ref Texture __result)
        //{
        //    Core.Logger.LogInfo($"[UIWidget:{__instance.GetType().FullName}] \"{__instance.name}\"");
        //    return true;
        //}

        //[HarmonyPatch(typeof(UIWidget), nameof(UIWidget.mainTexture), MethodType.Getter)]
        //[HarmonyPatch(typeof(UI2DSprite), nameof(UIWidget.mainTexture), MethodType.Getter)]
        //[HarmonyPostfix]
        //private static void GetMainTexturePost(UIWidget __instance, ref Texture __result)
        //{
        //    Texture tex;

        //    switch (__instance)
        //    {
        //        case UI2DSprite sprite:
        //            tex = sprite.sprite2D?.texture;
        //            break;
        //        default:
        //            tex = __instance.material?.mainTexture;
        //            break;
        //    }

        //    Core.Logger.LogInfo($"[UIWidget:{__instance.GetType().FullName}] mat: \"{__instance.material?.name}\" tex: \"{tex?.name}\"");
        //}

        //[HarmonyPatch(typeof(UITexture), nameof(UIWidget.mainTexture), MethodType.Getter)]
        //[HarmonyPostfix]
        //private static void GetMainTexturePostTex(UIWidget __instance, ref Texture __result, ref Texture ___mTexture)
        //{
        //    Core.Logger.LogInfo($"[UIWidget:{__instance.GetType().FullName}] mat: \"{__instance.material?.name}\" tex: \"{___mTexture?.name ?? __instance.material?.mainTexture?.name}\"");
        //}
    }
}
