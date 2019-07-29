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
            Translations.Clear();
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

    internal class ScriptTranslationManager : TranslationManagerBase
    {
        private static readonly Dictionary<string, string> TranslationFiles =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private static readonly LinkedList<ScriptTranslationFile> TranslationFileCache = new LinkedList<ScriptTranslationFile>();
        private static readonly Dictionary<string, LinkedListNode<ScriptTranslationFile>> TranslationFileLookup =
            new Dictionary<string, LinkedListNode<ScriptTranslationFile>>();

        void Update()
        {
            if(Configuration.ScriptTranslations.ReloadTranslationsKey.Value.IsPressed)
                ReloadActiveTranslations();
        }

        public override void LoadLanguage(string language)
        {
            Core.Logger.LogInfo($"Loading script translations for language \"{language}\"");

            var tlPath = Path.Combine(Paths.TranslationsRoot, language);
            var textTlPath = Path.Combine(tlPath, "Script");
            if (!Directory.Exists(tlPath))
            {
                Core.Logger.LogWarning(
                    $"No Scripts translation folder found for language {language}. Skipping loading script translations...");
                return;
            }

            TranslationFiles.Clear();
            TranslationFileCache.Clear();
            TranslationFileLookup.Clear();

            if (!Directory.Exists(textTlPath))
                Directory.CreateDirectory(textTlPath);

            foreach (var file in Directory.GetFiles(textTlPath, "*.txt", SearchOption.AllDirectories))
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (TranslationFiles.ContainsKey(fileName))
                {
                    Core.Logger.LogWarning(
                        $"Script translation file {fileName} is declared twice in different locations ({file} and {TranslationFiles[fileName]})");
                    continue;
                }

                TranslationFiles[fileName] = file;
            }
        }

        public string GetTranslation(string fileName, string text)
        {
            if (!TranslationFiles.ContainsKey(fileName))
                return null;
            if (!TranslationFileLookup.TryGetValue(fileName, out var tlNode))
            {
                tlNode = LoadFile(fileName);

                if (tlNode == null)
                    return null;
            }

            if (tlNode.Value.Translations.TryGetValue(text, out var tlText))
                return tlText;
            return null;
        }

        public bool WriteTranslation(string fileName, string original, string translated)
        {
            if (!TranslationFiles.ContainsKey(fileName))
            {
                var tlPath = Path.Combine(Paths.TranslationsRoot, Configuration.General.ActiveLanguage.Value);
                var textTlPath = Path.Combine(tlPath, "Script");

                if(!Directory.Exists(textTlPath))
                    Directory.CreateDirectory(textTlPath);

                var scriptFilePath = Path.Combine(textTlPath, $"{fileName}.txt");
                File.WriteAllText(scriptFilePath, $"{original.Escape()}\t{translated.Escape()}");
                TranslationFiles.Add(fileName, scriptFilePath);
                return true;
            }

            var node = LoadFile(fileName);

            if (node.Value.Translations.ContainsKey(original))
                return false;

            node.Value.Translations.Add(original, translated);
            File.AppendAllText(TranslationFiles[fileName],
                $"{Environment.NewLine}{original.Escape()}\t{translated.Escape()}");
            return true;
        }

        public override void ReloadActiveTranslations()
        {
            foreach (var scriptTranslationFile in TranslationFileCache)
            {
                Core.Logger.LogInfo($"Reloading translations for {scriptTranslationFile.FileName}.ks");
                scriptTranslationFile.LoadTranslations();
            }
        }

        private LinkedListNode<ScriptTranslationFile> LoadFile(string fileName)
        {
            if (TranslationFileLookup.TryGetValue(fileName, out var node))
            {
                TranslationFileCache.Remove(node);
                TranslationFileCache.AddFirst(node);
                return node;
            }

            if (TranslationFileCache.Count == Configuration.ScriptTranslations.MaxTranslationFilesCached.Value)
            {
                TranslationFileLookup.Remove(TranslationFileCache.Last.Value.FileName);
                TranslationFileCache.RemoveLast();
            }

            try
            {
                var file = new ScriptTranslationFile(fileName, TranslationFiles[fileName]);
                file.LoadTranslations();
                var result = TranslationFileCache.AddFirst(file);
                TranslationFileLookup.Add(fileName, result);
                return result;
            }
            catch (Exception e)
            {
                Core.Logger.LogWarning(
                    $"Failed to load translations for file {fileName} because: {e.Message}. Skipping file...");
                TranslationFiles.Remove(fileName);
                return null;
            }
        }
    }
}