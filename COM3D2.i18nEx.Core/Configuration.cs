using System;
using System.IO;
using COM3D2.i18nEx.Core.Util;
using ExIni;
using UnityEngine;

namespace COM3D2.i18nEx.Core
{
    internal static class Configuration
    {
        private static readonly IniFile configFile = File.Exists(Paths.ConfigurationFilePath) ? IniFile.FromFile(Paths.ConfigurationFilePath) : new IniFile();

        public static void Reload()
        {
            configFile.Merge(IniFile.FromFile(Paths.TranslationsRoot));
        }

        private static ConfigWrapper<T> Wrap<T>(string section, string key, string description = "", T @default = default, Func<T, string> toStringConvert = null, Func<string, T> fromStringConvert = null)
        {
            return new ConfigWrapper<T>(configFile, Paths.ConfigurationFilePath, section, key, description, @default, toStringConvert, fromStringConvert);
        }


        public static readonly General GeneralConfig = new General();
        public static readonly ScriptTranslations ScriptTranslationsConfig = new ScriptTranslations();

        internal class General
        {
            public ConfigWrapper<string> ActiveLanguage = Wrap(
                "General",
                "ActiveLanguage",
                "Currently selected language",
                "English"
            );
        }

        internal class ScriptTranslations
        {
            public ConfigWrapper<int> MaxTranslationFilesCached = Wrap(
                "ScriptTranslations", 
                "CacheSize",
                "Specifies how many text translation files should be kept in memory at once\nHaving bigger cache can improve performance at the cost of memory usage",
                1);

            public ConfigWrapper<bool> DumpScriptTranslations = Wrap(
                "ScriptTranslations",
                "DumpUntranslatedLines",
                "If enabled, dumps untranslated script lines (along with built-in translations, if present).",
                false);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                "ScriptTranslations",
                "ReloadTranslationsKey",
                "The key (or key combination) to reload currently loaded or cached translations.",
                new KeyCommand(KeyCode.LeftAlt, KeyCode.F12),
                KeyCommand.KeyCommandToString,
                KeyCommand.KeyCommandFromString);
        }
    }
}
