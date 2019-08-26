﻿using System.IO;
using System.Linq;
using COM3D2.i18nEx.Core.Util;
using I2.Loc;
using UnityEngine;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal class I2TranslationManager : TranslationManagerBase
    {
        private readonly GameObject go = new GameObject();

        public override void LoadLanguage()
        {
            DontDestroyOnLoad(go);

            Core.Logger.LogInfo("Loading UI translations");
            LoadTranslations();
        }

        private void LoadTranslations()
        {
            var files = Core.TranslationLoader.GetUITranslationFileNames();

            if (files == null)
            {
                Core.Logger.LogInfo("No UI translations found! Skipping...");
                return;
            }

            var source = go.GetComponent<LanguageSource>() ?? go.AddComponent<LanguageSource>();
            source.name = "i18nEx";
            source.ClearAllData();

            foreach (var kv in files)
            {
                string unit = kv.Key;
                var tlFiles = kv.Value;

                if (Configuration.I2Translation.VerboseLogging.Value)
                    Core.Logger.LogInfo($"Loading unit {unit}");

                foreach (string tlFile in tlFiles)
                {
                    string categoryName = tlFile.Replace("\\", "/").Splice(0, -5);

                    if (Configuration.I2Translation.VerboseLogging.Value)
                        Core.Logger.LogInfo($"Loading category {categoryName}");

                    string csvFile;
                    using (var f = new StreamReader(Core.TranslationLoader.OpenUiTranslation($"{unit}/{tlFile}")))
                        csvFile = f.ReadToEnd().ToLF();
                    source.Import_CSV(categoryName, csvFile, eSpreadsheetUpdateMode.Merge);
                }
            }

            Core.Logger.LogInfo(
                $"Loaded the following languages: {string.Join(",", source.mLanguages.Select(d => d.Name).ToArray())}");

            LocalizationManager.LocalizeAll(true);
        }

        private void Update()
        {
            if (Configuration.I2Translation.ReloadTranslationsKey.Value.IsPressed)
                ReloadActiveTranslations();
        }

        public override void ReloadActiveTranslations()
        {
            Core.Logger.LogInfo("Reloading current I2 translations");
            LoadTranslations();
        }
    }
}