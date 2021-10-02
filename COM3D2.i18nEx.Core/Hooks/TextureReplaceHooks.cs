using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class TextureReplaceHooks
    {
        private const string FONT_TEX_NAME = "Font Texture";
        private static bool initialized;
        private static Harmony instance;
        private static readonly byte[] EmptyBytes = new byte[0];
        private static string previousTexName;

        public static void Initialize()
        {
            if (initialized)
                return;

            instance = Harmony.CreateAndPatchAll(typeof(TextureReplaceHooks), "horse.coder.i18nex.hooks.textures");
            initialized = true;
        }


        [HarmonyPatch(typeof(FileSystemArchive), nameof(FileSystemArchive.IsExistentFile))]
        [HarmonyPatch(typeof(FileSystemWindows), nameof(FileSystemWindows.IsExistentFile))]
        [HarmonyPostfix]
        private static void IsExistentFileCheck(ref bool __result, string file_name)
        {
            if (file_name == null ||
                (!Path.GetExtension(file_name)?.Equals(".tex", StringComparison.InvariantCultureIgnoreCase) ?? true))
                return;

            if (!string.IsNullOrEmpty(file_name) &&
                Core.TextureReplace.ReplacementExists(Path.GetFileNameWithoutExtension(file_name)))
                __result = true;
        }

        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
        [HarmonyPrefix]
        private static bool LoadTexture(ref TextureResource __result,
                                        AFileSystemBase f_fileSystem,
                                        string f_strFileName,
                                        bool usePoolBuffer)
        {
            var fileName = Path.GetFileNameWithoutExtension(f_strFileName);
            if (string.IsNullOrEmpty(fileName))
                return true;

            var skipLogging = true;
            if (Configuration.TextureReplacement.VerboseLogging.Value && previousTexName != f_strFileName)
            {
                Core.Logger.LogInfo($"[COM3D2_TEX] {f_strFileName}");
                previousTexName = f_strFileName;
                skipLogging = false;
            }

            var newTex = Core.TextureReplace.GetReplacementTextureBytes(fileName, "tex", skipLogging);

            if (newTex == null)
                return true;

            if (Configuration.TextureReplacement.VerboseLogging.Value)
                Core.Logger.LogInfo($"Replacing {f_strFileName}");

            __result = new TextureResource(1, 1, TextureFormat.ARGB32, __result?.uvRects, newTex);

            return false;
        }

        [HarmonyPatch(typeof(ImportCM), nameof(ImportCM.LoadTexture))]
        [HarmonyPostfix]
        private static void OnTexLoaded(ref TextureResource __result,
                                        AFileSystemBase f_fileSystem,
                                        string f_strFileName,
                                        bool usePoolBuffer)
        {
            if (!Configuration.TextureReplacement.DumpTextures.Value ||
                Configuration.TextureReplacement.SkipDumpingCMTextures.Value)
                return;
            var tex = __result.CreateTexture2D();
            Core.TextureReplace.DumpTexture(Path.GetFileNameWithoutExtension(f_strFileName), tex);
        }

        [HarmonyPatch(typeof(UIWidget), nameof(UIWidget.mainTexture), MethodType.Getter)]
        [HarmonyPatch(typeof(UI2DSprite), nameof(UI2DSprite.mainTexture), MethodType.Getter)]
        [HarmonyPostfix]
        private static void GetMainTexturePost(UIWidget __instance, ref Texture __result)
        {
            Texture tex;

            switch (__instance)
            {
                case UI2DSprite sprite:
                    tex = sprite.sprite2D?.texture;
                    break;
                default:
                    tex = __instance.material?.mainTexture;
                    break;
            }

            if (tex      == null || string.IsNullOrEmpty(tex.name) || tex.name.StartsWith("i18n_") ||
                tex.name == FONT_TEX_NAME)
                return;

            var skipLogging = true;
            if (Configuration.TextureReplacement.VerboseLogging.Value && previousTexName != tex?.name)
            {
                Core.Logger.LogInfo($"[{__instance.GetType().Name}] {tex?.name}");
                previousTexName = tex?.name;
                skipLogging = false;
            }

            var newData =
                Core.TextureReplace.GetReplacementTextureBytes(tex.name, __instance.GetType().Name, skipLogging);

            if (newData == null)
            {
                if (Configuration.TextureReplacement.DumpTextures.Value)
                    Core.TextureReplace.DumpTexture(tex.name, tex);
                return;
            }

            if (Configuration.TextureReplacement.VerboseLogging.Value)
                Core.Logger.LogInfo($"Replacing {tex?.name}");

            if (tex is Texture2D tex2d)
            {
                tex2d.LoadImage(EmptyBytes);
                tex2d.LoadImage(newData);
                tex2d.name = $"i18n_{tex2d}";
            }
            else
            {
                Core.Logger.LogError($"Texture {tex.name} is of type {tex.GetType().FullName} and not tex2d!");
            }
        }

        [HarmonyPatch(typeof(UITexture), nameof(UITexture.mainTexture), MethodType.Getter)]
        [HarmonyPostfix]
        private static void GetMainTexturePostTex(UITexture __instance, ref Texture __result, ref Texture ___mTexture)
        {
            var tex = ___mTexture ?? __instance.material?.mainTexture;

            if (tex == null || string.IsNullOrEmpty(tex.name) || tex.name.StartsWith("i18n_"))
                return;

            var skipLogging = true;
            if (Configuration.TextureReplacement.VerboseLogging.Value && previousTexName != tex?.name)
            {
                Core.Logger.LogInfo($"[{__instance.GetType().Name}] {tex?.name}");
                previousTexName = tex?.name;
                skipLogging = false;
            }

            var newData = Core.TextureReplace.GetReplacementTextureBytes(tex.name, "UITexture", skipLogging);

            if (newData == null)
            {
                if (Configuration.TextureReplacement.DumpTextures.Value)
                    Core.TextureReplace.DumpTexture(tex.name, tex);
                return;
            }

            if (Configuration.TextureReplacement.VerboseLogging.Value)
                Core.Logger.LogInfo($"Replacing {tex?.name}");

            if (tex is Texture2D tex2d)
            {
                tex2d.LoadImage(EmptyBytes);
                tex2d.LoadImage(newData);
                tex2d.name = $"i18n_{tex2d}";
            }
            else
            {
                Core.Logger.LogError($"Texture {tex.name} is of type {tex.GetType().FullName} and not tex2d!");
            }
        }

        [HarmonyPatch(typeof(Image), nameof(Image.sprite), MethodType.Setter)]
        [HarmonyPrefix]
        private static void SetSprite(ref Sprite value)
        {
            if (value == null || value.texture == null || string.IsNullOrEmpty(value.texture.name) ||
                value.texture.name.StartsWith("i18n_"))
                return;

            var skipLogging = true;
            if (Configuration.TextureReplacement.VerboseLogging.Value && previousTexName != value?.texture?.name)
            {
                Core.Logger.LogInfo($"[UnityEngine.UI.Image] {value?.texture?.name}");
                previousTexName = value?.texture?.name;
                skipLogging = false;
            }

            var newData = Core.TextureReplace.GetReplacementTextureBytes(value.texture.name, "Image", skipLogging);

            if (newData == null)
            {
                if (Configuration.TextureReplacement.DumpTextures.Value)
                    Core.TextureReplace.DumpTexture(value.texture.name, value.texture);
                return;
            }

            if (Configuration.TextureReplacement.VerboseLogging.Value)
                Core.Logger.LogInfo($"Replacing {value?.texture?.name}");

            value.texture.LoadImage(EmptyBytes);
            value.texture.LoadImage(newData);
            value.texture.name = $"i18n_{value.texture.name}";
        }

        [HarmonyPatch(typeof(MaskableGraphic), "OnEnable")]
        [HarmonyPrefix]
        private static void OnMaskableGraphicEnable(MaskableGraphic __instance)
        {
            // Force replacement of Images
            if (__instance is not Image img || img.sprite == null)
                return;
            var tmp = img.sprite;
            img.sprite = tmp;
        }
    }
}
