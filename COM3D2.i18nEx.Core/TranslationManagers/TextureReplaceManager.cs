using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal static class TextureReplaceManager
    {
        public static void Initialize()
        {
            LoadLanguage(Configuration.General.ActiveLanguage.Value);
        }

        private static void LoadLanguage(string language)
        {
            Core.Logger.LogInfo($"Loading texture replacements for language \"{language}\"");
        }
    }
}
