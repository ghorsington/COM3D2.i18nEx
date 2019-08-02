using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using I2.Loc;
using UnityEngine;
using UnityInjector;
using UnityInjector.Attributes;

namespace EngExtract
{
    [PluginName("Translation Extractor")]
    public class EngExtract : PluginBase
    {
        public const string TL_DIR = "COM3D2_Localisation";

        private static readonly Regex textPattern = new Regex("text=\"(?<text>.*)\"");
        private static readonly Regex namePattern = new Regex("name=\"(?<name>.*)\"");

        private int translatedLines;

        private static void DumpI2Translations(LanguageSource src)
        {
            var i2Path = Path.Combine(TL_DIR, "UI");
            var sourcePath = Path.Combine(i2Path, src.name);
            if (!Directory.Exists(sourcePath))
                Directory.CreateDirectory(sourcePath);
            var categories = src.GetCategories();
            foreach (var category in categories)
            {
                var path = Path.Combine(sourcePath, $"{category}.csv");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, src.Export_CSV(category));
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
                Dump();
        }

        private void DumpUI()
        {
            Debug.Log("Dumping UI localisation");

            var langs = LocalizationManager.GetAllLanguages();
            Debug.Log($"Currently {langs.Count} languages are known");
            foreach (var language in langs)
                Debug.Log($"{language}");

            Debug.Log($"Currently selected language is {LocalizationManager.CurrentLanguage}");
            Debug.Log($"There are {LocalizationManager.Sources.Count} language sources");

            foreach (var languageSource in LocalizationManager.Sources)
            {
                Debug.Log(
                    $"Dumping {languageSource.name} with languages: {string.Join(",", languageSource.mLanguages.Select(d => d.Name).ToArray())}. GSheets: {languageSource.HasGoogleSpreadsheet()}");
                DumpI2Translations(languageSource);
            }
        }

        private KeyValuePair<string, string> SplitTranslation(string txt)
        {
            int pos;
            if ((pos = txt.IndexOf("<E>", StringComparison.InvariantCultureIgnoreCase)) > 0)
            {
                translatedLines++;
                return new KeyValuePair<string, string>(txt.Substring(0, pos), txt.Substring(pos + 3));
            }

            return new KeyValuePair<string, string>(txt, string.Empty);
        }

        private void ExtractTranslations(string fileName, string script)
        {
            var tlDir = Path.Combine(TL_DIR, "Script");
            var dir = Path.Combine(tlDir, Path.GetDirectoryName(fileName));
            var name = Path.GetFileNameWithoutExtension(fileName);

            Directory.CreateDirectory(dir);

            var lineList = new HashSet<string>();
            var lines = script.Split('\n');

            var sb = new StringBuilder();
            var captureTalk = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length == 0)
                    continue;

                if (trimmedLine.StartsWith("@talk", StringComparison.InvariantCultureIgnoreCase))
                {
                    captureTalk = true;
                    var match = namePattern.Match(trimmedLine);
                    if (match.Success)
                    {
                        var m = match.Groups["name"];
                        var parts = SplitTranslation(m.Value);
                        if (parts.Key.StartsWith("[HF", StringComparison.InvariantCulture) ||
                            parts.Key.StartsWith("[SF", StringComparison.InvariantCulture))
                            continue;
                        lineList.Add($"{parts.Key}\t{parts.Value}");
                    }
                }
                else if (captureTalk)
                {
                    if (trimmedLine.StartsWith("@hitret", StringComparison.InvariantCultureIgnoreCase))
                    {
                        captureTalk = false;
                        var parts = SplitTranslation(sb.ToString());
                        sb.Length = 0;
                        lineList.Add($"{parts.Key}\t{parts.Value}");
                        continue;
                    }

                    sb.Append(trimmedLine);
                }
                else if (trimmedLine.StartsWith("@ChoicesSet", StringComparison.InvariantCultureIgnoreCase))
                {
                    var match = textPattern.Match(trimmedLine);
                    if (!match.Success)
                    {
                        Debug.Log($"[WARNING] Failed to extract line from \"{trimmedLine}\"");
                        continue;
                    }

                    var m = match.Groups["text"];
                    var parts = SplitTranslation(m.Value);
                    lineList.Add($"{parts.Key}\t{parts.Value}");
                }
            }

            if (lineList.Count != 0)
                File.WriteAllLines(Path.Combine(dir, $"{name}.txt"), lineList.ToArray());
        }

        private void DumpScripts()
        {
            Debug.Log("Dumping game script translations...");
            Debug.Log("Getting all script files...");
            var scripts = GameUty.FileSystem.GetFileListAtExtension(".ks");
            Debug.Log($"Found {scripts.Length} scripts!");

            foreach (var scriptFile in scripts)
                using (var f = GameUty.FileOpen(scriptFile))
                {
                    var script = ShiftJisUtil.ToString(f.ReadAll());
                    Debug.Log(scriptFile);
                    ExtractTranslations(scriptFile, script);
                }
        }

        private void Dump()
        {
            Debug.Log("Dumping game localisation files! Please be patient!");
            DumpUI();
            DumpScripts();
            Debug.Log($"Dumped {translatedLines} lines");
            Debug.Log($"Done! Dumped translations are located in {TL_DIR}. You can now close the game!");
            Debug.Log("IMPORTANT: Delete this plugin (EngExtract.dll) if you want to play the game normally!");
        }
    }
}


internal static class ShiftJisUtil
{
    [DllImport("kernel32.dll")]
    private static extern int MultiByteToWideChar(uint CodePage, uint dwFlags, [In] [MarshalAs(UnmanagedType.LPArray)]
        byte[] lpMultiByteStr, int cbMultiByte, [Out] [MarshalAs(UnmanagedType.LPArray)]
        byte[] lpWideCharStr, int cchWideChar);

    public static string ToString(byte[] data)
    {
        // Can't get it to work with StringBuilder. Oh well, going the long route then...
        var needed = MultiByteToWideChar(932, 0, data, data.Length, null, 0);
        var c = new byte[needed * 2];
        var sent = MultiByteToWideChar(932, 0, data, data.Length, c, needed * 2);
        return Encoding.Unicode.GetString(c);
    }
}