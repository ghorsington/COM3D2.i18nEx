using System;
using BepInEx.Harmony;
using HarmonyLib;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class TranslationHooks
    {
        public static void Initialize()
        {
            ScriptTranslationHooks.Initialize();
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
