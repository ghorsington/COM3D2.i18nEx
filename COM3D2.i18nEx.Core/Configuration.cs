using System;
using System.Collections.Generic;
using System.IO;
using COM3D2.i18nEx.Core.TranslationManagers;
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

        private static readonly List<IReloadable> reloadableWrappers = new();

        public static readonly GeneralConfig General = new();
        public static readonly ScriptTranslationsConfig ScriptTranslations = new();
        public static readonly TextureReplacementConfig TextureReplacement = new();
        public static readonly I2TranslationConfig I2Translation = new();

        static Configuration()
        {
            var scriptSection = configFile["ScriptTranslations"];
            if (scriptSection.HasKey("InsertJapaneseTextIntoEnglishText"))
            {
                var key = scriptSection["InsertJapaneseTextIntoEnglishText"];
                if (bool.TryParse(key.Value, out var val))
                    ScriptTranslations.RerouteTranslationsTo.Value =
                        val ? TranslationsReroute.RouteToEnglish : TranslationsReroute.None;
                scriptSection.DeleteKey("InsertJapaneseTextIntoEnglishText");
                configFile.Save(Paths.ConfigurationFilePath);
            }
        }

        public static void Reload()
        {
            configFile.Merge(IniFile.FromFile(Paths.ConfigurationFilePath));
            foreach (var reloadableWrapper in reloadableWrappers)
                reloadableWrapper.Reload();
        }

        private static ConfigWrapper<T> Wrap<T>(string section,
                                                string key,
                                                string description = "",
                                                T @default = default,
                                                Func<T, string> toStringConvert = null,
                                                Func<string, T> fromStringConvert = null)
        {
            var res = new ConfigWrapper<T>(configFile, Paths.ConfigurationFilePath, section, key, description,
                                           @default, toStringConvert, fromStringConvert);

            reloadableWrappers.Add(res);

            return res;
        }

        internal class GeneralConfig
        {
            public ConfigWrapper<string> ActiveLanguage = Wrap(
                                                               "General", "ActiveLanguage",
                                                               "Currently selected language", "English");

            public ConfigWrapper<bool> FixSubtitleType = Wrap(
                                                              "General", "FixGameSubtitleType",
                                                              "DO NOT TOUCH: If enabled, i18nEx will reset game subtitle type to Japanese on the next game run",
                                                              true);

            public ConfigWrapper<KeyCommand> ReloadConfigKey = Wrap(
                                                                    "General", "ReloadConfigKey",
                                                                    "The key to reload current configuration file",
                                                                    new KeyCommand(KeyCode.LeftControl, KeyCode.F12),
                                                                    KeyCommand.KeyCommandToString,
                                                                    KeyCommand.KeyCommandFromString);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                                                                          "General", "ReloadConfigKey",
                                                                          "The key to reload current configuration file",
                                                                          new KeyCommand(KeyCode.LeftAlt, KeyCode.F12),
                                                                          KeyCommand.KeyCommandToString,
                                                                          KeyCommand.KeyCommandFromString);
        }

        internal class ScriptTranslationsConfig
        {
            public ConfigWrapper<double> ClipboardCaptureTime = Wrap(
                                                                     "ScriptTranslations", "ClipboardCaptureTime",
                                                                     "If `SendScriptToClipboard` is enabled, specifies the time to wait before sending all input to clipboard.",
                                                                     0.25);

            public ConfigWrapper<bool> DumpScriptTranslations = Wrap(
                                                                     "ScriptTranslations", "DumpUntranslatedLines",
                                                                     "If enabled, dumps untranslated script lines (along with built-in translations, if present).",
                                                                     false);

            public ConfigWrapper<int> MaxTranslationFilesCached = Wrap(
                                                                       "ScriptTranslations", "CacheSize",
                                                                       "Specifies how many text translation files should be kept in memory at once\nHaving bigger cache can improve performance at the cost of memory usage",
                                                                       1);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                                                                          "ScriptTranslations", "ReloadTranslationsKey",
                                                                          "The key (or key combination) to reload all translations.",
                                                                          new KeyCommand(KeyCode.LeftAlt,
                                                                           KeyCode.Keypad1),
                                                                          KeyCommand.KeyCommandToString,
                                                                          KeyCommand.KeyCommandFromString);

            public ConfigWrapper<TranslationsReroute> RerouteTranslationsTo = Wrap(
             "ScriptTranslations",
             "RerouteTranslationsTo",
             "Allows you to route both English and Japanese translations into a single textbox instead of viewing both\nSupports the following values:\nNone -- Disabled. English text is written into English textbox; Japanese into Japanese\nRouteToEnglish -- Puts Japanese text into English textbox if there is no translation text available\nRouteToJapanese -- Puts translations into Japanese textbox if there is a translation available",
             TranslationsReroute.RouteToJapanese,
             EnumConverter<TranslationsReroute>
                .EnumToString,
             EnumConverter<TranslationsReroute>
                .EnumFromString);

            public ConfigWrapper<bool> SendScriptToClipboard = Wrap(
                                                                    "ScriptTranslations", "SendToClipboard",
                                                                    "If enabled, sends untranslated story text to clipboard.",
                                                                    false);

            public ConfigWrapper<bool> VerboseLogging = Wrap("ScriptTranslations", "VerboseLogging",
                                                             "If enabled, logs precise translation info\nUseful if you're writing new translations.",
                                                             false);
        }

        internal class TextureReplacementConfig
        {
            public ConfigWrapper<bool> DumpTextures = Wrap("TextureReplacement", "DumpOriginalTextures",
                                                           "If enabled, dumps textures that have no replacements.",
                                                           false);

            public ConfigWrapper<int> MaxTexturesCached = Wrap("TextureReplacement", "CacheSize",
                                                               "Specifies how many texture replacements should be kept in memory at once\nHaving bigger cache can improve performance at the cost of memory usage",
                                                               10);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                                                                          "TextureReplacement", "ReloadTranslationsKey",
                                                                          "The key (or key combination) to reload all translations.",
                                                                          new KeyCommand(KeyCode.LeftAlt,
                                                                           KeyCode.Keypad2),
                                                                          KeyCommand.KeyCommandToString,
                                                                          KeyCommand.KeyCommandFromString);

            public ConfigWrapper<bool> SkipDumpingCMTextures = Wrap("TextureReplacement", "SkipDumpingCMTextures",
                                                                    "If `DumpOriginalTextures` is enabled, setting this to `True` will disable dumping game's own .tex files\nUse this if you don't want to dump all in-game textures.",
                                                                    true);

            public ConfigWrapper<bool> VerboseLogging = Wrap("TextureReplacement", "VerboseLogging",
                                                             "If enabled, logs precise texture replacement info\nUseful if you're writing new translations.",
                                                             false);
        }

        internal class I2TranslationConfig
        {
            public ConfigWrapper<string> CustomUIFont = Wrap("I2Translation", "CustomUIFont",
                                                             "If specified, replaces the UI font with this one.\nIMPORTANT: The font **must** be installed on your machine and it **must** be a TrueType font.",
                                                             "");

            public ConfigWrapper<bool> DumpTexts = Wrap("I2Translation", "DumpUntranslatedUITexts",
                                                        "If enabled, dumps untranslated UI texts",
                                                        false);

            public ConfigWrapper<bool> OverrideSubtitleOpacity = Wrap("I2Translation", "OverrideSubtitleOpacity",
                                                                      "If enabled, allows to change subtitle box opacity without affecting other elements.",
                                                                      false);

            public ConfigWrapper<KeyCommand> PrintFontNamesKey = Wrap("I2Translation", "PrintFontNamesKey",
                                                                      "The key (or key combination) do display all supported UI fonts in the console.",
                                                                      new KeyCommand(KeyCode.LeftAlt, KeyCode.F11),
                                                                      KeyCommand.KeyCommandToString,
                                                                      KeyCommand.KeyCommandFromString);

            public ConfigWrapper<KeyCommand> ReloadTranslationsKey = Wrap(
                                                                          "I2Translation", "ReloadTranslationsKey",
                                                                          "The key (or key combination) to reload all translations.",
                                                                          new KeyCommand(KeyCode.LeftAlt,
                                                                           KeyCode.Keypad3),
                                                                          KeyCommand.KeyCommandToString,
                                                                          KeyCommand.KeyCommandFromString);

            public ConfigWrapper<float> SubtitleOpacity = Wrap("I2Translation", "SubtitleOpacity",
                                                               "If OverrideSubtitleOpacity is true, specifies opacity of the subtitle box. Must be a decimal between 0 (transparent) and 1 (opaque).",
                                                               1.0f);

            public ConfigWrapper<bool> VerboseLogging = Wrap("I2Translation", "VerboseLogging",
                                                             "If enabled, logs precise I2Loc loading and translation info\nUseful if you're debugging.",
                                                             false);
        }
    }
}
