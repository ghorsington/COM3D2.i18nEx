﻿using System;
using System.Collections;
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
        private const int WIDTH = 200;
        private const int HEIGHT = 300;
        private const int MARGIN_X = 5;
        private const int MARGIN_TOP = 20;
        private const int MARGIN_BOTTOM = 5;

        private static readonly Regex textPattern = new Regex("text=\"(?<text>.*)\"");
        private static readonly Regex namePattern = new Regex("name=(?<name>.*)");
        private static readonly Encoding UTF8 = new UTF8Encoding(true);

        private readonly DumpOptions options = new DumpOptions();
        private bool displayGui;
        private bool dumping;

        private int translatedLines;

        private static void DumpI2Translations(LanguageSource src)
        {
            string i2Path = Path.Combine(TL_DIR, "UI");
            string sourcePath = Path.Combine(i2Path, src.name);
            if (!Directory.Exists(sourcePath))
                Directory.CreateDirectory(sourcePath);
            var categories = src.GetCategories(true);
            foreach (string category in categories)
            {
                string path = Path.Combine(sourcePath, $"{category}.csv");
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, src.Export_CSV(category), UTF8);
            }
        }

        private void OnGUI()
        {
            if (!displayGui)
                return;

            void Toggle(string text, ref bool toggle) { toggle = GUILayout.Toggle(toggle, text); }

            void Window(int id)
            {
                GUILayout.BeginArea(new Rect(MARGIN_X, MARGIN_TOP, WIDTH - MARGIN_X * 2,
                                             HEIGHT - MARGIN_TOP - MARGIN_BOTTOM));
                {
                    GUILayout.BeginVertical();
                    {
                        GUILayout.Label("Base dumps");
                        Toggle("Story scripts", ref options.dumpScripts);
                        Toggle("UI translations", ref options.dumpUITranslations);

                        GUILayout.Label("Advanced dumps");
                        Toggle(".menu item names", ref options.dumpItemNames);
                        Toggle("VIP event names", ref options.dumpVIPEvents);
                        Toggle("Yotogi skills", ref options.dumpYotogis);
                        Toggle("Personality names", ref options.dumpPersonalies);
                        Toggle("Event names", ref options.dumpEvents);

                        GUILayout.Label("Other");
                        Toggle("Skip translated items", ref options.skipTranslatedItems);

                        GUI.enabled = !dumping;
                        if (GUILayout.Button("Dump!"))
                        {
                            dumping = true;
                            StartCoroutine(DumpGame());
                        }

                        GUI.enabled = true;
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndArea();
            }

            GUI.Window(6969, new Rect(Screen.width - WIDTH, (Screen.height - HEIGHT) / 2f, WIDTH, HEIGHT), Window,
                       "EngExtract");
        }

        private IEnumerator DumpGame()
        {
            var opts = new DumpOptions(options);
            yield return null;
            Dump(opts);
            dumping = false;
        }

        private void Awake() { DontDestroyOnLoad(this); }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.D))
                displayGui = !displayGui;
        }

        private void DumpUI()
        {
            Debug.Log("Dumping UI localisation");

            var langs = LocalizationManager.GetAllLanguages();
            Debug.Log($"Currently {langs.Count} languages are known");
            foreach (string language in langs)
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
                string orig = txt.Substring(0, pos);
                string tl = txt.Substring(pos + 3).Replace("…", "...").Trim();
                return new KeyValuePair<string, string>(orig, tl);
            }

            return new KeyValuePair<string, string>(txt, string.Empty);
        }

        private static Dictionary<string, string> ParseTag(string line)
        {
            var result = new Dictionary<string, string>();
            var valueSb = new StringBuilder();
            var keySb = new StringBuilder();
            var captureValue = false;
            var quoted = false;
            var escapeNext = false;

            foreach (var c in line)
            {
                if (captureValue)
                {
                    if (valueSb.Length == 0 && c == '"')
                    {
                        quoted = true;
                        continue;
                    }

                    if (escapeNext)
                    {
                        escapeNext = false;
                        valueSb.Append(c);
                        continue;
                    }

                    if (c == '\\')
                        escapeNext = true;

                    if ((!quoted && char.IsWhiteSpace(c)) || (quoted && !escapeNext && c == '"'))
                    {
                        quoted = false;
                        result[keySb.ToString()] = valueSb.ToString();
                        keySb.Length = 0;
                        valueSb.Length = 0;
                        captureValue = false;
                    }

                    valueSb.Append(c);
                }
                else
                {
                    if (keySb.Length == 0 && char.IsWhiteSpace(c))
                        continue;

                    if (char.IsWhiteSpace(c) && keySb.Length != 0)
                    {
                        result[keySb.ToString()] = "true";
                        keySb.Length = 0;
                        continue;
                    }

                    if (c == '=')
                    {
                        captureValue = true;
                        continue;
                    }

                    keySb.Append(c);
                }
            }

            if (keySb.Length != 0)
                result[keySb.ToString()] = valueSb.Length == 0 ? "true" : valueSb.ToString();

            return result;
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

        private static T GetOrDefault<T>(Dictionary<string, T> dic, string key, T def)
        {
            return dic.TryGetValue(key, out var val) ? val : def;
        }

        private static Dictionary<string, string> NpcNames = new Dictionary<string, string>();
        private void ExtractTranslations(string fileName, string script)
        {
            string tlDir = Path.Combine(TL_DIR, "Script");
            string dir = Path.Combine(tlDir, Path.GetDirectoryName(fileName));
            string name = Path.GetFileNameWithoutExtension(fileName);

            Directory.CreateDirectory(dir);

            var lineList = new HashSet<string>();
            var lines = script.Split('\n');

            var sb = new StringBuilder();
            var captureTalk = false;
            var captureSubtitlePlay = false;
            SubtitleData subData = null;

            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.Length == 0)
                    continue;

                if (trimmedLine.StartsWith("@SubtitleDisplayForPlayVoice", StringComparison.InvariantCultureIgnoreCase))
                {
                    captureSubtitlePlay = true;
                    var sub = ParseTag(line.Substring("@SubtitleDisplayForPlayVoice".Length));
                    var text = SplitTranslation(sub["text"]);
                    subData = new SubtitleData
                    {
                        addDisplayTime = int.Parse(GetOrDefault(sub, "addtime", "0")),
                        displayTime = int.Parse(GetOrDefault(sub, "wait", "-1")),
                        original = text.Key,
                        translation = text.Value,
                        isCasino = sub.ContainsKey("mode_c")
                    };
                }
                else if (trimmedLine.StartsWith("@PlayVoice", StringComparison.InvariantCultureIgnoreCase) && captureSubtitlePlay)
                {
                    captureSubtitlePlay = false;
                    var data = ParseTag(line.Substring("@PlayVoice".Length));
                    if (!data.TryGetValue("voice", out var voiceName))
                    {
                        subData = null;
                        continue;
                    }
                    subData.voice = voiceName;
                    lineList.Add($"@VoiceSubtitle{JsonUtility.ToJson(subData, false)}");
                    subData = null;
                }
                else if (trimmedLine.StartsWith("@talk", StringComparison.InvariantCultureIgnoreCase))
                {
                    captureTalk = true;
                    var match = namePattern.Match(trimmedLine);
                    if (match.Success)
                    {
                        var m = match.Groups["name"];
                        var parts = SplitTranslation(m.Value.Trim('\"'));
                        if (parts.Key.StartsWith("[HF", StringComparison.InvariantCulture) ||
                            parts.Key.StartsWith("[SF", StringComparison.InvariantCulture))
                            continue;
                        NpcNames[parts.Key] = parts.Value;
                    }
                }
                else if (captureTalk)
                {
                    if (trimmedLine.StartsWith("@", StringComparison.InvariantCultureIgnoreCase))
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
                File.WriteAllLines(Path.Combine(dir, $"{name}.txt"), lineList.ToArray(), UTF8);
        }

        private void DumpScripts()
        {
            Debug.Log("Dumping game script translations...");
            Debug.Log("Getting all script files...");
            var scripts = GameUty.FileSystem.GetFileListAtExtension(".ks");
            Debug.Log($"Found {scripts.Length} scripts!");

            foreach (string scriptFile in scripts)
                using (var f = GameUty.FileOpen(scriptFile))
                {
                    string script = ShiftJisUtil.ToString(f.ReadAll());
                    Debug.Log(scriptFile);
                    ExtractTranslations(scriptFile, script);
                }

            string tlDir = Path.Combine(TL_DIR, "Script");
            string namesFile = Path.Combine(tlDir, "__npc_names.txt");
            File.WriteAllLines(namesFile, NpcNames.Select(n => $"{n.Key}\t{n.Value}").ToArray(), UTF8);
            NpcNames.Clear();
        }

        private void DumpScenarioEvents(DumpOptions opts)
        {
            string i2Path = Path.Combine(TL_DIR, "UI");
            string unitPath = Path.Combine(i2Path, "zzz_scenario_events");
            Directory.CreateDirectory(unitPath);

            Debug.Log("Getting scenario event data");

            var encoding = new UTF8Encoding(true);
            using (var sw = new StreamWriter(Path.Combine(unitPath, "SceneScenarioSelect.csv"), false, encoding))
                using (var f = GameUty.FileOpen("select_scenario_data.nei"))
                    using (var scenarioNei = new CsvParser())
                    {
                        sw.WriteLine("Key,Type,Desc,Japanese,English");

                        scenarioNei.Open(f);

                        for (var i = 1; i < scenarioNei.max_cell_y; i++)
                        {
                            if (!scenarioNei.IsCellToExistData(0, i))
                                continue;

                            int id = scenarioNei.GetCellAsInteger(0, i);
                            string name = scenarioNei.GetCellAsString(1, i);
                            string description = scenarioNei.GetCellAsString(2, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"SceneScenarioSelect/{id}/タイトル", out _))
                                continue;

                            string csvName = EscapeCSVItem(name);
                            string csvDescription = EscapeCSVItem(description);
                            sw.WriteLine($"{id}/タイトル,Text,,{csvName},{csvName}");
                            sw.WriteLine($"{id}/内容,Text,,{csvDescription},{csvDescription}");
                        }
                    }
        }

        private void DumpItemNames(DumpOptions opts)
        {
            string i2Path = Path.Combine(TL_DIR, "UI");
            string unitPath = Path.Combine(i2Path, "zzz_item_names");
            Directory.CreateDirectory(unitPath);

            var encoding = new UTF8Encoding(true);
            Debug.Log("Getting all .menu files (this might take a moment)...");
            var menus = GameUty.FileSystem.GetFileListAtExtension(".menu");

            Debug.Log($"Found {menus.Length} menus!");

            var swDict = new Dictionary<string, StreamWriter>();

            foreach (string menu in menus)
                using (var f = GameUty.FileOpen(menu))
                    using (var br = new BinaryReader(new MemoryStream(f.ReadAll())))
                    {
                        Debug.Log(menu);

                        br.ReadString();
                        br.ReadInt32();
                        br.ReadString();
                        string filename = Path.GetFileNameWithoutExtension(menu);
                        string name = br.ReadString();
                        string category = br.ReadString().ToLowerInvariant();
                        string info = br.ReadString();

                        if (!swDict.TryGetValue(category, out var sw))
                        {
                            swDict[category] = sw = new StreamWriter(Path.Combine(unitPath, $"{category}.csv"), false, encoding);
                            sw.WriteLine("Key,Type,Desc,Japanese,English");
                        }

                        if (opts.skipTranslatedItems &&
                            LocalizationManager.TryGetTranslation($"{category}/{filename}|name", out _))
                            continue;
                        sw.WriteLine($"{filename}|name,Text,,{EscapeCSVItem(name)},{EscapeCSVItem(name)}");
                        sw.WriteLine($"{filename}|info,Text,,{EscapeCSVItem(info)},{EscapeCSVItem(info)}");
                    }

            foreach (var keyValuePair in swDict)
                keyValuePair.Value.Dispose();
        }

        private void DumpPersonalityNames(DumpOptions opts)
        {
            string i2Path = Path.Combine(TL_DIR, "UI");
            string unitPath = Path.Combine(i2Path, "zzz_personalities");
            Directory.CreateDirectory(unitPath);

            Debug.Log("Getting personality names");

            var encoding = new UTF8Encoding(true);
            using (var sw = new StreamWriter(Path.Combine(unitPath, "MaidStatus.csv"), false, encoding))
                using (var f = GameUty.FileOpen("maid_status_personal_list.nei"))
                    using (var scenarioNei = new CsvParser())
                    {
                        sw.WriteLine("Key,Type,Desc,Japanese,English");

                        scenarioNei.Open(f);

                        for (var i = 1; i < scenarioNei.max_cell_y; i++)
                        {
                            if (!scenarioNei.IsCellToExistData(0, i))
                                continue;

                            string uniqueName = scenarioNei.GetCellAsString(1, i);
                            string displayName = scenarioNei.GetCellAsString(2, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"MaidStatus/性格タイプ/{uniqueName}", out _))
                                continue;

                            string csvName = EscapeCSVItem(displayName);
                            sw.WriteLine($"性格タイプ/{uniqueName},Text,,{csvName},{csvName}");
                        }
                    }
        }

        private void DumpYotogiData(DumpOptions opts)
        {
            string i2Path = Path.Combine(TL_DIR, "UI");
            string unitPath = Path.Combine(i2Path, "zzz_yotogi");
            Directory.CreateDirectory(unitPath);

            Debug.Log("Getting yotogi skills and commands");

            var encoding = new UTF8Encoding(true);
            using (var sw = new StreamWriter(Path.Combine(unitPath, "YotogiSkillName.csv"), false, encoding))
                using (var f = GameUty.FileOpen("yotogi_skill_list.nei"))
                    using (var scenarioNei = new CsvParser())
                    {
                        sw.WriteLine("Key,Type,Desc,Japanese,English");
                        scenarioNei.Open(f);

                        for (var i = 1; i < scenarioNei.max_cell_y; i++)
                        {
                            if (!scenarioNei.IsCellToExistData(0, i))
                                continue;

                            string skillName = scenarioNei.GetCellAsString(4, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"YotogiSkillName/{skillName}", out _))
                                continue;

                            string csvName = EscapeCSVItem(skillName);
                            sw.WriteLine($"{csvName},Text,,{csvName},{csvName}");
                        }
                    }

            var commandNames = new HashSet<string>();
            using (var sw = new StreamWriter(Path.Combine(unitPath, "YotogiSkillName.csv"), false, encoding))
                using (var f = GameUty.FileOpen("yotogi_skill_command_data.nei"))
                    using (var scenarioNei = new CsvParser())
                    {
                        sw.WriteLine("Key,Type,Desc,Japanese,English");
                        scenarioNei.Open(f);

                        for (var i = 0; i < scenarioNei.max_cell_y; i++)
                        {
                            if (!scenarioNei.IsCellToExistData(0, i))
                            {
                                i += 2;
                                continue;
                            }

                            string commandName = scenarioNei.GetCellAsString(2, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"YotogiSkillCommand/{commandName}", out _))
                                continue;

                            if (commandNames.Contains(commandName))
                                continue;

                            commandNames.Add(commandName);

                            string csvName = EscapeCSVItem(commandName);
                            sw.WriteLine($"{csvName},Text,,{csvName},{csvName}");
                        }
                    }
        }

        private void DumpVIPEvents(DumpOptions opts)
        {
            string i2Path = Path.Combine(TL_DIR, "UI");
            string unitPath = Path.Combine(i2Path, "zzz_vip_event");
            Directory.CreateDirectory(unitPath);

            Debug.Log("Getting VIP event names");

            var encoding = new UTF8Encoding(true);
            using (var sw = new StreamWriter(Path.Combine(unitPath, "SceneDaily.csv"), false, encoding))
                using (var f = GameUty.FileOpen("schedule_work_night.nei"))
                    using (var scenarioNei = new CsvParser())
                    {
                        sw.WriteLine("Key,Type,Desc,Japanese,English");
                        scenarioNei.Open(f);

                        for (var i = 1; i < scenarioNei.max_cell_y; i++)
                        {
                            if (!scenarioNei.IsCellToExistData(0, i))
                                continue;

                            string vipName = scenarioNei.GetCellAsString(1, i);
                            string vipDescription = scenarioNei.GetCellAsString(7, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"SceneDaily/スケジュール/項目/{vipName}", out _))
                                continue;

                            string csvName = EscapeCSVItem(vipName);
                            string csvDesc = EscapeCSVItem(vipDescription);
                            sw.WriteLine($"スケジュール/項目/{vipName},Text,,{csvName},{csvName}");
                            sw.WriteLine($"スケジュール/説明/{vipName},Text,,{csvDesc},{csvDesc}");
                        }
                    }
        }

        private string EscapeCSVItem(string str)
        {
            if (str.Contains("\n") || str.Contains("\"") || str.Contains(","))
                return $"\"{str.Replace("\"", "\"\"")}\"";
            return str;
        }

        private void Dump(DumpOptions opts)
        {
            Debug.Log("Dumping game localisation files! Please be patient!");

            if (opts.dumpUITranslations)
                DumpUI();

            if (opts.dumpScripts)
                DumpScripts();

            if (opts.dumpItemNames)
                DumpItemNames(opts);

            if(opts.dumpEvents)
                DumpScenarioEvents(opts);

            if(opts.dumpPersonalies)
                DumpPersonalityNames(opts);

            if(opts.dumpYotogis)
                DumpYotogiData(opts);

            if(opts.dumpVIPEvents)
                DumpVIPEvents(opts);

            if (opts.dumpScripts)
                Debug.Log($"Dumped {translatedLines} lines");
            Debug.Log($"Done! Dumped translations are located in {TL_DIR}. You can now close the game!");
            Debug.Log("IMPORTANT: Delete this plugin (EngExtract.dll) if you want to play the game normally!");
        }

        private class DumpOptions
        {
            public bool dumpEvents;
            public bool dumpItemNames;
            public bool dumpPersonalies;
            public bool dumpScripts = true;
            public bool dumpUITranslations = true;
            public bool dumpVIPEvents;
            public bool dumpYotogis;
            public bool skipTranslatedItems;
            public DumpOptions() { }

            public DumpOptions(DumpOptions other)
            {
                dumpScripts = other.dumpScripts;
                dumpUITranslations = other.dumpUITranslations;
                dumpItemNames = other.dumpItemNames;
                dumpVIPEvents = other.dumpVIPEvents;
                dumpYotogis = other.dumpYotogis;
                dumpPersonalies = other.dumpPersonalies;
                dumpEvents = other.dumpEvents;
                skipTranslatedItems = other.skipTranslatedItems;
            }
        }
    }
}


internal static class ShiftJisUtil
{
    [DllImport("kernel32.dll")]
    private static extern int MultiByteToWideChar(uint CodePage,
                                                  uint dwFlags,
                                                  [In] [MarshalAs(UnmanagedType.LPArray)] byte[] lpMultiByteStr,
                                                  int cbMultiByte,
                                                  [Out] [MarshalAs(UnmanagedType.LPArray)] byte[] lpWideCharStr,
                                                  int cchWideChar);

    public static string ToString(byte[] data)
    {
        // Can't get it to work with StringBuilder. Oh well, going the long route then...
        int needed = MultiByteToWideChar(932, 0, data, data.Length, null,
                                         0);
        var c = new byte[needed * 2];
        int sent = MultiByteToWideChar(932, 0, data, data.Length, c,
                                       needed * 2);
        return Encoding.Unicode.GetString(c);
    }
}