using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using COM3D2.i18nEx.Core.Util;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;


namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal enum TranslationsReroute
    {
        None,
        RouteToEnglish,
        RouteToJapanese
    }

    [Serializable]
    internal class SubtitleData
    {
        public int addDisplayTime;
        public int displayTime = -1;
        public bool isCasino;
        public string original = string.Empty;
        public int startTime;
        public string translation = string.Empty;
        public string voice = string.Empty;
    }

    internal class ScriptTranslationManager : TranslationManagerBase
    {
        private readonly StringBuilder clipboardBuffer = new();

        private readonly LinkedList<ScriptTranslationFile> translationFileCache =
            new();

        private readonly Dictionary<string, LinkedListNode<ScriptTranslationFile>> translationFileLookup =
            new();

        private readonly Dictionary<string, string> translationFiles =
            new(StringComparer.InvariantCultureIgnoreCase);





        private ScriptTranslationFile namesFile;

        private void Awake()
        {
            StartCoroutine(SendToClipboardRoutine());
        }

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
            var files2 = Core.TranslationLoader.GetScriptTranslationZipNames();

            if (files == null && files2 == null)
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
                    continue;
                }

                translationFiles[fileName] = file;
            }

            Core.Logger.LogInfo($"script count {translationFiles.Count}");

            if (files2 == null)
            {
                return;
            }

            ZipConstants.DefaultCodePage = Encoding.UTF8.CodePage;

            Core.Logger.LogInfo($"DefaultCodePage : {ZipConstants.DefaultCodePage}");

            foreach (string zipPath in files2)
            {

                ZipFile zip = new ZipFile(zipPath);
                {
                    Core.Logger.LogInfo($"zip : {zipPath} , {zip.Count}");
                    foreach (ZipEntry zfile in zip)
                    {
                        if (zfile.IsFile)
                        {
                            //var name= Path.GetFileName(file.Name);
                            var fileName = Path.GetFileNameWithoutExtension(zfile.Name);
                            //Core.Logger.LogInfo($"{fileName} , {zfile.Name} , {zfile.IsDirectory} , {zfile.IsFile} , {zfile.AESKeySize}");//  , {zip.FindEntry(name, true)}

                            if (translationFiles.ContainsKey(fileName))
                            {
                                Core.Logger.LogWarning(
                                                       $"Script translation file {fileName} is declared twice in different locations ({zfile} and {translationFiles[fileName]})");
                                continue;
                            }

                            ScriptTranslationFile.translationZips[fileName] = zip;
                            ScriptTranslationFile.translationZipEntrys[fileName] = zfile;

                            if (fileName == "__npc_names" && namesFile == null)
                            {
                                namesFile = new ScriptTranslationFile(fileName, zfile.Name);
                                namesFile.LoadTranslations();
                                continue;
                            }

                            translationFiles[fileName] = zfile.Name;

                            //zip.GetInputStream(zfile);
                        }
                    }


                }
            }

            Core.Logger.LogInfo($"script count {translationFiles.Count}");
        }




        public List<SubtitleData> GetSubtitle(string fileName, string voiceName)
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
