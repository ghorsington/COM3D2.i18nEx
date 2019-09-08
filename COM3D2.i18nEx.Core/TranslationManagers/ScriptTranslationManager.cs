using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using COM3D2.i18nEx.Core.Util;
using UnityEngine;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal enum TranslationsReroute { None, RouteToEnglish, RouteToJapanese }

    internal class ScriptTranslationFile
    {
        public string FileName { get; }
        public string FullPath { get; }

        public Dictionary<string, string> Translations { get; } =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public ScriptTranslationFile(string fileName, string path)
        {
            FileName = fileName;
            FullPath = path;
        }

        public void LoadTranslations()
        {
            Translations.Clear();
            using (var sr = new StreamReader(Core.TranslationLoader.OpenScriptTranslation(FullPath)))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith(";"))
                        continue;

                    var parts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    string orig = parts[0].Unescape();
                    string tl = parts.Length > 1 ? parts[1].Unescape() : null;

                    Translations[orig] = tl;
                }
            }
        }
    }

    internal class ScriptTranslationManager : TranslationManagerBase
    {
        private readonly StringBuilder clipboardBuffer = new StringBuilder();

        private readonly LinkedList<ScriptTranslationFile> translationFileCache =
            new LinkedList<ScriptTranslationFile>();

        private readonly Dictionary<string, LinkedListNode<ScriptTranslationFile>> translationFileLookup =
            new Dictionary<string, LinkedListNode<ScriptTranslationFile>>();

        private readonly Dictionary<string, string> translationFiles =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        private ScriptTranslationFile namesFile;

        private void Update()
        {
            if (Configuration.ScriptTranslations.ReloadTranslationsKey.Value.IsPressed)
                ReloadActiveTranslations();
        }

        public override void LoadLanguage()
        {
            Core.Logger.LogInfo("Loading script translations");

            namesFile = null;
            translationFiles.Clear();
            translationFileCache.Clear();
            translationFileLookup.Clear();

            var files = Core.TranslationLoader.GetScriptTranslationFileNames();

            if (files == null)
            {
                Core.Logger.LogInfo("No script translation found! Skipping...");
                return;
            }

            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (translationFiles.ContainsKey(fileName))
                {
                    Core.Logger.LogWarning(
                        $"Script translation file {fileName} is declared twice in different locations ({file} and {translationFiles[fileName]})");
                    continue;
                }

                if (fileName == "__npc_names" && namesFile == null)
                {
                    namesFile = new ScriptTranslationFile(fileName, file);
                    namesFile.LoadTranslations();
                    Core.Logger.LogInfo("Loaded __npc_names! Got names:");
                    foreach (var namesFileTranslation in namesFile.Translations)
                        Core.Logger.LogInfo($"\"{namesFileTranslation.Key}\" => \"{namesFileTranslation.Value}\"");
                    continue;
                }

                translationFiles[fileName] = file;
            }
        }

        public string GetTranslation(string fileName, string text)
        {
            if (namesFile != null && namesFile.Translations.TryGetValue(text, out string nameTl))
                return nameTl;
            if (fileName == null || !translationFiles.ContainsKey(fileName))
                return NoTranslation(text);
            if (!translationFileLookup.TryGetValue(fileName, out var tlNode))
            {
                tlNode = LoadFile(fileName);

                if (tlNode == null)
                    return NoTranslation(text);
            }

            if (tlNode.Value.Translations.TryGetValue(text, out string tlText))
                return tlText;
            return NoTranslation(text);
        }

        private string NoTranslation(string inputText)
        {
            if (Configuration.ScriptTranslations.SendScriptToClipboard.Value)
                clipboardBuffer.AppendLine(inputText);
            return null;
        }

        private void Awake() { StartCoroutine(SendToClipboardRoutine()); }

        private IEnumerator SendToClipboardRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds((float)Configuration.ScriptTranslations.ClipboardCaptureTime.Value);

                if (clipboardBuffer.Length > 0)
                {
                    Clipboard.SetText(clipboardBuffer.ToString());
                    clipboardBuffer.Length = 0;
                }
            }
        }

        public bool WriteTranslation(string fileName, string original, string translated)
        {
            if (!translationFiles.ContainsKey(fileName))
            {
                string tlPath = Path.Combine(Paths.TranslationsRoot, Configuration.General.ActiveLanguage.Value);
                string textTlPath = Path.Combine(tlPath, "Script");

                if (!Directory.Exists(textTlPath))
                    Directory.CreateDirectory(textTlPath);

                string scriptFilePath = Path.Combine(textTlPath, $"{fileName}.txt");
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

        public override void ReloadActiveTranslations() { LoadLanguage(); }

        private LinkedListNode<ScriptTranslationFile> LoadFile(string fileName)
        {
            if (translationFileLookup.TryGetValue(fileName, out var node))
            {
                translationFileCache.Remove(node);
                translationFileCache.AddFirst(node);
                return node;
            }

            if (translationFileCache.Count == Configuration.ScriptTranslations.MaxTranslationFilesCached.Value)
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
                Core.Logger.LogError(
                    $"Failed to load translations for file {fileName} because: {e.Message}. Skipping file...");
                translationFiles.Remove(fileName);
                return null;
            }
        }
    }
}