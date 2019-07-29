using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal class TextureReplaceManager : TranslationManagerBase
    {
        public override void LoadLanguage(string language)
        {
            Core.Logger.LogInfo($"Loading texture replacements for language \"{language}\"");
        }

        public override void ReloadActiveTranslations()
        {
            throw new NotImplementedException();
        }
    }
}
