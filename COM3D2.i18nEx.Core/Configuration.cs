using System;
using System.Collections.Generic;
using System.IO;
using COM3D2.i18nEx.Core.Util;
using ExIni;
using UnityEngine;

namespace COM3D2.i18nEx.Core
{
    internal static class Configuration
    {
        private static readonly IniFile configFile = File.Exists(Paths.ConfigurationFilePath)
            ? IniFile.FromFile(Paths.ConfigurationFilePath)
            : new IniFile();

        private static readonly List<IReloadable> reloadableWrappers = new List<IReloadable>();

        public static readonly GeneralConfig General = new GeneralConfig();
        public static readonly ScriptTranslationsConfig ScriptTranslations = new ScriptTranslationsConfig();
        public static readonly TextureReplacementConfig TextureReplacement = new TextureReplacementConfig();
        public static readonly I2TranslationConfig I2Translation = new I2TranslationConfig();

        public static void Reload()
        {
            configFile.Merge(IniFile.FromFile(Paths.TranslationsRoot));
            foreach (var reloadableWrapper in reloadableWrappers)
                reloadableWrapper.Reload();
        }

        private static ConfigWrapper<T> Wrap<T>(string section, string key, string description = "",
            T @default = default, Func<T, string> toStringConvert = null, Func<string, T> fromStringConvert = null)
        {
            var res = new ConfigWrapper<T>(configFile, Paths.ConfigurationFilePath, section, key, description, @default,
                toStringConvert, fromStringConvert);

            reloadableWrappers.Add(res);

            return res;
        }

        internal class GeneralConfig
        {
            public ConfigWrapper<string> ActiveLanguage = Wrap(
                "General",
                "ActiveLanguage",
                "Currently selected language",
                "English"
            );

            public ConfigWrapper<KeyCommand> ReloadConfigKey = Wrap(
                "General",
                "ReloadConfigKey",
                "The key to reload current configuration file",
                new KeyCommand(KeyCode.LeftControl, KeyCode.F12),
                KeyCommand.KeyCommandToString,
                KeyCommand.KeyCommandFromString);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                "General",
                "ReloadConfigKey",
                "The key to reload current configuration file",
                new KeyCommand(KeyCode.LeftAlt, KeyCode.F12),
                KeyCommand.KeyCommandToString,
                KeyCommand.KeyCommandFromString);
        }

        internal class ScriptTranslationsConfig
        {
            public ConfigWrapper<bool> DumpScriptTranslations = Wrap(
                "ScriptTranslations",
                "DumpUntranslatedLines",
                "If enabled, dumps untranslated script lines (along with built-in translations, if present).",
                false);

            public ConfigWrapper<bool> VerboseLogging = Wrap(
                "ScriptTranslations",
                "VerboseLogging",
                "If enabled, logs precise translation info\nUseful if you're writing new translations.",
                false);

            public ConfigWrapper<int> MaxTranslationFilesCached = Wrap(
                "ScriptTranslations",
                "CacheSize",
                "Specifies how many text translation files should be kept in memory at once\nHaving bigger cache can improve performance at the cost of memory usage",
                1);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                "ScriptTranslations",
                "ReloadTranslationsKey",
                "The key (or key combination) to reload currently loaded or cached translations.",
                new KeyCommand(KeyCode.LeftAlt, KeyCode.Keypad1),
                KeyCommand.KeyCommandToString,
                KeyCommand.KeyCommandFromString);
        }

        internal class TextureReplacementConfig
        {
            public ConfigWrapper<bool> DumpTextures = Wrap(
                "TextureReplacement",
                "DumpOriginalTextures",
                "If enabled, dumps textures that have no replacements.",
                false);

            public ConfigWrapper<int> MaxTexturesCached = Wrap(
                "TextureReplacement",
                "CacheSize",
                "Specifies how many texture replacements should be kept in memory at once\nHaving bigger cache can improve performance at the cost of memory usage",
                10);

            public ConfigWrapper<bool> SkipDumpingCMTextures = Wrap(
                "TextureReplacement",
                "SkipDumpingCMTextures",
                "If `DumpOriginalTextures` is enabled, setting this to `True` will disable dumping game's own .tex files\nUse this if you don't want to dump all in-game textures.",
                false);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                "TextureReplacement",
                "ReloadTranslationsKey",
                "The key (or key combination) to reload currently cached textures.",
                new KeyCommand(KeyCode.LeftAlt, KeyCode.Keypad2),
                KeyCommand.KeyCommandToString,
                KeyCommand.KeyCommandFromString);

            public ConfigWrapper<bool> VerboseLogging = Wrap(
                "TextureReplacement",
                "VerboseLogging",
                "If enabled, logs precise texture replacement info\nUseful if you're writing new translations.",
                false);
        }

        internal class I2TranslationConfig
        {
            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                "I2Translation",
                "ReloadTranslationsKey",
                "The key (or key combination) to reload currently cached translations.",
                new KeyCommand(KeyCode.LeftAlt, KeyCode.Keypad3),
                KeyCommand.KeyCommandToString,
                KeyCommand.KeyCommandFromString);
        }
    }
}