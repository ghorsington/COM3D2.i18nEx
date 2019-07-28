using System;
using System.Collections.Generic;
using System.IO;
using COM3D2.i18nEx.Core.Util;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal class ScriptTranslationFile
    {
        public string FileName { get; }
        public string FullPath { get; }

        public Dictionary<string, string> Translations { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public ScriptTranslationFile(string fileName, string path)
        {
            FileName = fileName;
            FullPath = path;
        }

        public void LoadTranslations()
        {
            using (var sr = new StreamReader(FullPath))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith(";"))
                        continue;

                    var parts = line.Split(new[] {'\t'}, StringSplitOptions.RemoveEmptyEntries);

                    var orig = parts[0].Unescape();
                    var tl = parts.Length > 1 ? parts[1].Unescape() : null;

                    Translations[orig] = tl;
                }
            }
        }
    }

    internal static class ScriptTranslationManager
    {
        private static string currentLanguage = "Unknown";

        private static Dictionary<string, string> translationFiles =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        private static LinkedList<ScriptTranslationFile> translationFileCache = new LinkedList<ScriptTranslationFile>();

        private static Dictionary<string, LinkedListNode<ScriptTranslationFile>> translationFileLookup =
            new Dictionary<string, LinkedListNode<ScriptTranslationFile>>();

        public static void Initialize()
        {
            LoadLanguage(Configuration.ActiveLanguage.Value);
        }

        public static void LoadLanguage(string language)
        {
            Core.Logger.LogInfo($"Loading script translations for language \"{language}\"");

            var tlPath = Path.Combine(Paths.TranslationsRoot, language);
            var textTlPath = Path.Combine(tlPath, "Script");
            if (!Directory.Exists(tlPath))
            {
                Core.Logger.LogWarning(
                    $"No Scripts translation folder found for language {language}. Skipping loading script translations...");
                currentLanguage = "Unknown";
                return;
            }

            currentLanguage = language;
            translationFiles.Clear();
            translationFileCache.Clear();
            translationFileLookup.Clear();

            if (!Directory.Exists(textTlPath))
                Directory.CreateDirectory(textTlPath);

            foreach (var file in Directory.GetFiles(textTlPath, "*.txt", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (translationFiles.ContainsKey(fileName))
                {
                    Core.Logger.LogWarning(
                        $"Script translation file {fileName} is declared twice in different locations ({file} and {translationFiles[fileName]})");
                    continue;
                }

                translationFiles[fileName] = file;
            }
        }

        public static string GetTranslation(string fileName, string text)
        {
            if (!translationFiles.ContainsKey(fileName))
                return null;
            if (!translationFileLookup.TryGetValue(fileName, out var tlNode))
            {
                tlNode = LoadFile(fileName);

                if (tlNode == null)
                    return null;
            }

            if (tlNode.Value.Translations.TryGetValue(text, out var tlText))
                return tlText;
            return null;
        }

        public static bool WriteTranslation(string fileName, string original, string translated)
        {
            if (!translationFiles.ContainsKey(fileName))
            {
                var tlPath = Path.Combine(Paths.TranslationsRoot, currentLanguage);
                var textTlPath = Path.Combine(tlPath, "Script");

                if(!Directory.Exists(textTlPath))
                    Directory.CreateDirectory(textTlPath);

                var scriptFilePath = Path.Combine(textTlPath, $"{fileName}.txt");
                File.WriteAllText(scriptFilePath, $"{original.Escape()}\t{translated.Escape()}");
                translationFiles.Add(fileName, scriptFilePath);
                return true;
            }

            var node = LoadFile(fileName);

            if (node.Value.Translations.ContainsKey(original))
                return false;

            node.Value.Translations.Add(original, translated);
            File.AppendAllText(translationFiles[fileName],
                $"{Environment.NewLine}{original.Escape()}\t{translated.Escape()}");
            return true;
        }

        private static LinkedListNode<ScriptTranslationFile> LoadFile(string fileName)
        {
            if (translationFileLookup.TryGetValue(fileName, out var node))
            {
                translationFileCache.Remove(node);
                translationFileCache.AddFirst(node);
                return node;
            }

            if (translationFileCache.Count == Configuration.MaxTranslationFilesCached.Value)
            {
                translationFileLookup.Remove(translationFileCache.Last.Value.FileName);
                translationFileCache.RemoveLast();
            }

            try
            {
                var file = new ScriptTranslationFile(fileName, translationFiles[fileName]);
                file.LoadTranslations();
                var result = translationFileCache.AddFirst(file);
                translationFileLookup.Add(fileName, result);
                return result;
            }
            catch (Exception e)
            {
                Core.Logger.LogWarning(
                    $"Failed to load translations for file {fileName} because: {e.Message}. Skipping file...");
                translationFiles.Remove(fileName);
                return null;
            }
        }
    }
}