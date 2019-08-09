using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using COM3D2.i18nEx.Core.Util;
using I2.Loc;
using UnityEngine;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal class I2TranslationManager : TranslationManagerBase
    {
        private readonly GameObject go = new GameObject();


        public override void LoadLanguage(string langName)
        {
            DontDestroyOnLoad(go);

            Core.Logger.LogInfo($"Loading UI translations for language \"{langName}\"");

            var tlPath = Path.Combine(Paths.TranslationsRoot, langName);
            var textTlPath = Path.Combine(tlPath, "UI");
            if (!Directory.Exists(textTlPath))
            {
                Core.Logger.LogWarning(
                    $"No UI translations folder found for language {langName}. Skipping loading script translations...");
                return;
            }

            LoadTranslations(langName);
        }

        private void LoadTranslations(string lang)
        {
            var tlPath = Path.Combine(Paths.TranslationsRoot, lang);
            var textTlPath = Path.Combine(tlPath, "UI");
            var source = go.GetComponent<LanguageSource>() ?? go.AddComponent<LanguageSource>();
            source.name = "i18nEx";
            source.ClearAllData();
            foreach (var directory in Directory.GetDirectories(textTlPath).OrderByDescending(s => s, StringComparer.InvariantCultureIgnoreCase))
            {
                var fullDir = Path.GetFullPath(directory);

                if (Configuration.I2Translation.VerboseLogging.Value)
                    Core.Logger.LogInfo($"Loading unit {fullDir}");

                foreach (var file in Directory.GetFiles(fullDir, "*.csv", SearchOption.AllDirectories))
                {

                    var categoryName = Path.GetFullPath(file).Substring(fullDir.Length + 1).Replace(".csv", "").Replace("\\", "/");

                    if(Configuration.I2Translation.VerboseLogging.Value)
                        Core.Logger.LogInfo($"Loading category {categoryName}");

                    source.Import_CSV(categoryName, File.ReadAllText(file).ToLF(), eSpreadsheetUpdateMode.Merge);
                }
            }

            Core.Logger.LogInfo(
                $"Loaded the following languages: {string.Join(",", source.mLanguages.Select(d => d.Name).ToArray())}");

            LocalizationManager.LocalizeAll(true);
        }

        void Update()
        {
            if(Configuration.I2Translation.ReloadTranslationsKey.Value.IsPressed)
                ReloadActiveTranslations();
        }

        public override void ReloadActiveTranslations()
        {
            Core.Logger.LogInfo("Reloading current I2 translations");
            LoadTranslations(Configuration.General.ActiveLanguage.Value);
        }
    }
}