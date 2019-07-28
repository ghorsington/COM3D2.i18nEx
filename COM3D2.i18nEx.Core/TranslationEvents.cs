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
            Core.Logger.LogInfo($"[{fileName}] \"{__result.Key}\" => \"{__result.Value}\"");
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
