using System;
using System.Linq;
using HarmonyLib;
using MonoMod.Utils;

namespace COM3D2.i18nEx.Core.Util
{
    public static class XUATInterop
    {
        private static MarkTranslatedDelegate markTranslated;
        private static bool initialized;

        private static bool Initialize()
        {
            if (initialized)
                return markTranslated != null;

            initialized = true;

            var xuatAssembly = AppDomain.CurrentDomain.GetAssemblies()
                                        .FirstOrDefault(a => a.GetName().Name == "XUnity.AutoTranslator.Plugin.Core");
            if (xuatAssembly == null)
            {
                Core.Logger.LogInfo("No XUAT detected, skipping interop");
                return false;
            }

            var langHelper = xuatAssembly.GetType("XUnity.AutoTranslator.Plugin.Core.Utilities.LanguageHelper");
            if (langHelper == null)
            {
                Core.Logger.LogWarning("Could not find LanguageHelper; skipping XUAT interop");
                return false;
            }

            var makeRedirectedMethod = AccessTools.Method(langHelper, "MakeRedirected");
            if (makeRedirectedMethod == null)
            {
                Core.Logger.LogWarning("Could not find LanguageHelper.MakeRedirected; skipping XUAT interop");
                return false;
            }

            markTranslated = makeRedirectedMethod.CreateDelegate<MarkTranslatedDelegate>();
            Core.Logger.LogInfo("Found XUAT; enabled interop");
            return true;
        }

        public static string MarkTranslated(string text)
        {
            return !Initialize() ? text : markTranslated(text);
        }

        private delegate string MarkTranslatedDelegate(string text);
    }
}
