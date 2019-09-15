using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using COM3D2.i18nEx.Core.Util;
using HarmonyLib;
using UnityEngine;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal enum TranslationsReroute
    {
        None,
        RouteToEnglish,
        RouteToJapanese
    }

    internal static class SubtitleHelper
    {
        private static readonly FieldInfo textField, displayTimeField, addDisplayTimeField, casinoTypeField;
        private static readonly Type subDataType;

        static SubtitleHelper()
        {
            subDataType = typeof(BaseKagManager).GetNestedType("SubtitleData");

            if (subDataType == null)
            {
                Core.Logger.LogError(
                    "BaseKagManager.SubtitleData class has not been found! Can't display subtitles! Check that you use the latest game version and latest i18nEx!");
                return;
            }

            textField = AccessTools.Field(subDataType, "text");
            displayTimeField = AccessTools.Field(subDataType, "displayTime");
            addDisplayTimeField = AccessTools.Field(subDataType, "addDisplayTime");
            casinoTypeField = AccessTools.Field(subDataType, "casinoType");
        }

        public static void SetSubtitleData(this SubtitleData subData, object kagSubtitleData)
        {
            if (subDataType == null)
                return;

            textField?.SetValue(kagSubtitleData, $"{subData.original}<E>{subData.translation}");
            displayTimeField?.SetValue(kagSubtitleData, subData.displayTime);
            addDisplayTimeField?.SetValue(kagSubtitleData, subData.addDisplayTime);
            casinoTypeField?.SetValue(kagSubtitleData, subData.isCasino);
        }
    }

    [Serializable]
    internal class SubtitleData
    {
        public int addDisplayTime = 0;
        public int displayTime = -1;
        public bool isCasino = false;
        public string original = string.Empty;
        public string translation = string.Empty;
        public string voice = string.Empty;
    }

    internal class ScriptTranslationFile
    {
        private const string VOICE_SUBTITLE_TAG = "@VoiceSubtitle";

        public ScriptTranslationFile(string fileName, string path)
        {
            FileName = fileName;
            FullPath = path;
        }

        public string FileName { get; }
        public string FullPath { get; }

        public Dictionary<string, string> Translations { get; } =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, SubtitleData> Subtitles { get; }
            = new Dictionary<string, SubtitleData>(StringComparer.InvariantCultureIgnoreCase);

        private void ParseVoiceSubtitle(string line)
        {
            try
            {
                var subData = JsonUtility.FromJson<SubtitleData>(line.Substring(VOICE_SUBTITLE_TAG.Length));
                Subtitles[subData.voice] = subData;
            }
            catch (Exception e)
            {
                Core.Logger.LogWarning($"Failed to load subtitle line from {FileName}. Reason: {e}\n Line: {line}");
            }
        }

        public void LoadTranslations()
        {
            Translations.Clear();
            using (var sr = new StreamReader(Core.TranslationLoader.OpenScriptTranslation(FullPath)))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith(";"))
                        continue;

                    if (trimmed.StartsWith("@VoiceSubtitle"))
                    {
                        ParseVoiceSubtitle(trimmed);
                        continue;
                    }

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

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
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

        public SubtitleData GetSubtitle(string fileName, string voiceName)
        {
            if (fileName == null || !translationFiles.ContainsKey(fileName))
                return null;
            if (!translationFileLookup.TryGetValue(fileName, out var tlNode))
            {
                tlNode = LoadFile(fileName);

                if (tlNode == null)
                    return null;
            }

            return tlNode.Value.Subtitles.TryGetValue(voiceName, out var subData) ? subData : null;
        }

        public string GetTranslation(string fileName, string text)
        {
            if (namesFile != null && namesFile.Translations.TryGetValue(text, out var nameTl))
                return nameTl;
            if (fileName == null || !translationFiles.ContainsKey(fileName))
                return NoTranslation(text);
            if (!translationFileLookup.TryGetValue(fileName, out var tlNode))
            {
                tlNode = LoadFile(fileName);

                if (tlNode == null)
                    return NoTranslation(text);
            }

            if (tlNode.Value.Translations.TryGetValue(text, out var tlText))
                return tlText;
            return NoTranslation(text);
        }

        private string NoTranslation(string inputText)
        {
            if (Configuration.ScriptTranslations.SendScriptToClipboard.Value)
                clipboardBuffer.AppendLine(inputText);
            return null;
        }

        private void Awake()
        {
            StartCoroutine(SendToClipboardRoutine());
        }

        private IEnumerator SendToClipboardRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds((float) Configuration.ScriptTranslations.ClipboardCaptureTime.Value);

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
                var tlPath = Path.Combine(Paths.TranslationsRoot, Configuration.General.ActiveLanguage.Value);
                var textTlPath = Path.Combine(tlPath, "Script");

                if (!Directory.Exists(textTlPath))
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

        public override void ReloadActiveTranslations()
        {
            LoadLanguage();
        }

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