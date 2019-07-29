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
            TextureReplaceHooks.Initialize();
        }
    }
}
