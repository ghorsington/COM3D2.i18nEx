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
            new(StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, List<SubtitleData>> Subtitles { get; }
            = new(StringComparer.InvariantCultureIgnoreCase);

        internal readonly static Dictionary<string, ZipFile> translationZips =
new(StringComparer.InvariantCultureIgnoreCase);
        internal readonly static Dictionary<string, ZipEntry> translationZipEntrys =
new(StringComparer.InvariantCultureIgnoreCase);

        private void ParseVoiceSubtitle(string line)
        {
            try
            {
                var subData = JsonUtility.FromJson<SubtitleData>(line.Substring(VOICE_SUBTITLE_TAG.Length));
                if (!Subtitles.TryGetValue(subData.voice, out var list))
                    Subtitles[subData.voice] = list = new List<SubtitleData>();
                list.Add(subData);
            }
            catch (Exception e)
            {
                Core.Logger.LogWarning($"Failed to load subtitle line from {FileName}. Reason: {e}\n Line: {line}");
            }
        }

        public void LoadTranslations()
        {
            Translations.Clear();

            Stream stream;
            if (translationZips.ContainsKey(FileName))
            {
                var zfile = translationZipEntrys[FileName];
                stream = translationZips[FileName].GetInputStream(translationZipEntrys[FileName]);                
                Core.Logger.LogInfo($"{FileName} , {zfile.Name} , {stream.Length}");//  , {zip.FindEntry(name, true)}
            }
            else
            {
                stream = Core.TranslationLoader.OpenScriptTranslation(FullPath);
            }
                        
            using (var sr = new StreamReader(stream))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var trimmed = line.Trim();
                    if (trimmed.Length == 0 || trimmed.StartsWith(";"))
                        continue;

                    if (trimmed.StartsWith(VOICE_SUBTITLE_TAG))
                    {
                        ParseVoiceSubtitle(trimmed);
                        continue;
                    }

                    var parts = line.Split(new[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                    var orig = parts[0].Unescape();
                    var tl = parts.Length > 1 ? parts[1].Unescape() : null;

                    Translations[orig] = tl;
                }
            }
        }
    }

}
