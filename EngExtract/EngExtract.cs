using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static readonly Dictionary<string, string> NpcNames = new Dictionary<string, string>();

        private readonly DumpOptions options = new DumpOptions();
        private bool displayGui;
        private bool dumping;


        private readonly HashSet<string> filesToSkip = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

        private int translatedLines;

        private static void DumpI2Translations(LanguageSource src)
        {
            var i2Path = Path.Combine(TL_DIR, "UI");
            var sourcePath = Path.Combine(i2Path, src.name);
            if (!Directory.Exists(sourcePath))
                Directory.CreateDirectory(sourcePath);
            var categories = src.GetCategories(true);
            foreach (var category in categories)
            {
                var path = Path.Combine(sourcePath, $"{category}.csv");
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
                var orig = txt.Substring(0, pos);
                var tl = txt.Substring(pos + 3).Replace("…", "...").Trim();
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

                    if (!quoted && char.IsWhiteSpace(c) || quoted && !escapeNext && c == '"')
                    {
                        quoted = false;
                        result[keySb.ToString()] = valueSb.ToString();
                        keySb.Length = 0;
                        valueSb.Length = 0;
                        captureValue = false;
                        continue;
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

            if (keySb.Length != 0)
                result[keySb.ToString()] = valueSb.Length == 0 ? "true" : valueSb.ToString();

            return result;
        }

        private static T GetOrDefault<T>(Dictionary<string, T> dic, string key, T def)
        {
            return dic.TryGetValue(key, out var val) ? val : def;
        }

        private void ExtractTranslations(string fileName, string script)
        {
            var tlDir = Path.Combine(TL_DIR, "Script");
            var dir = Path.Combine(tlDir, Path.GetDirectoryName(fileName));
            var name = Path.GetFileNameWithoutExtension(fileName);

            if (filesToSkip.Contains(name))
                return;

            Directory.CreateDirectory(dir);

            var lineList = new HashSet<string>();
            var lines = script.Split('\n');

            var sb = new StringBuilder();
            var captureTalk = false;
            var captureSubtitlePlay = false;
            SubtitleData subData = null;

            var captureSubtitlesList = new List<KeyValuePair<string, string>>();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Length == 0)
                    continue;

                if (trimmedLine.StartsWith("@LoadSubtitleFile", StringComparison.InvariantCultureIgnoreCase))
                {
                    var sub = ParseTag(trimmedLine.Substring("@LoadSubtitleFile".Length));
                    var subFileName = sub["file"];

                    filesToSkip.Add(subFileName);

                    using (var f = GameUty.FileOpen($"{subFileName}.ks"))
                    {
                        var parseTalk = false;
                        var subSb = new StringBuilder();
                        foreach (var subLine in NUty.SjisToUnicode(f.ReadAll()).Split('\n').Select(s => s.Trim())
                                                    .Where(s => s.Length != 0))
                            if (subLine.StartsWith("@talk", StringComparison.InvariantCultureIgnoreCase))
                                parseTalk = true;
                            else if (subLine.StartsWith("@hitret", StringComparison.InvariantCultureIgnoreCase) &&
                                     parseTalk)
                            {
                                parseTalk = false;
                                var parts = SplitTranslation(subSb.ToString());
                                captureSubtitlesList.Add(parts);
                                subSb.Length = 0;
                            }
                            else
                                subSb.Append(subLine);
                    }
                }
                else if (trimmedLine.StartsWith("@SubtitleDisplayForPlayVoice",
                                                StringComparison.InvariantCultureIgnoreCase))
                {
                    captureSubtitlePlay = true;
                    var sub = ParseTag(trimmedLine.Substring("@SubtitleDisplayForPlayVoice".Length));
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
                else if (trimmedLine.StartsWith("@PlayVoice", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (captureSubtitlePlay)
                    {
                        captureSubtitlePlay = false;
                        var data = ParseTag(trimmedLine.Substring("@PlayVoice".Length));
                        if (!data.TryGetValue("voice", out var voiceName))
                        {
                            subData = null;
                            continue;
                        }

                        subData.voice = voiceName;
                        lineList.Add($"@VoiceSubtitle{JsonUtility.ToJson(subData, false)}");
                        subData = null;
                    }
                    else if (captureSubtitlesList.Count > 0)
                    {
                        var subTl = captureSubtitlesList[0];
                        captureSubtitlesList.RemoveAt(0);

                        subData = new SubtitleData
                        {
                            addDisplayTime = 0,
                            displayTime = -1,
                            isCasino = false,
                            original = subTl.Key,
                            translation = subTl.Value
                        };

                        var data = ParseTag(trimmedLine.Substring("@PlayVoice".Length));
                        if (!data.TryGetValue("voice", out var voiceName))
                        {
                            subData = null;
                            continue;
                        }

                        subData.voice = voiceName;
                        lineList.Add($"@VoiceSubtitle{JsonUtility.ToJson(subData, false)}");
                        subData = null;
                    }
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

            foreach (var scriptFile in scripts)
                using (var f = GameUty.FileOpen(scriptFile))
                {
                    var script = NUty.SjisToUnicode(f.ReadAll());
                    Debug.Log(scriptFile);
                    ExtractTranslations(scriptFile, script);
                }

            var tlDir = Path.Combine(TL_DIR, "Script");
            var namesFile = Path.Combine(tlDir, "__npc_names.txt");
            File.WriteAllLines(namesFile, NpcNames.Select(n => $"{n.Key}\t{n.Value}").ToArray(), UTF8);
            NpcNames.Clear();
            filesToSkip.Clear();
        }

        private void DumpScenarioEvents(DumpOptions opts)
        {
            var i2Path = Path.Combine(TL_DIR, "UI");
            var unitPath = Path.Combine(i2Path, "zzz_scenario_events");
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

                            var id = scenarioNei.GetCellAsInteger(0, i);
                            var name = scenarioNei.GetCellAsString(1, i);
                            var description = scenarioNei.GetCellAsString(2, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"SceneScenarioSelect/{id}/タイトル", out _))
                                continue;

                            var csvName = EscapeCSVItem(name);
                            var csvDescription = EscapeCSVItem(description);
                            sw.WriteLine($"{id}/タイトル,Text,,{csvName},{csvName}");
                            sw.WriteLine($"{id}/内容,Text,,{csvDescription},{csvDescription}");
                        }
                    }
        }

        private void DumpItemNames(DumpOptions opts)
        {
            var i2Path = Path.Combine(TL_DIR, "UI");
            var unitPath = Path.Combine(i2Path, "zzz_item_names");
            Directory.CreateDirectory(unitPath);

            var encoding = new UTF8Encoding(true);
            Debug.Log("Getting all .menu files (this might take a moment)...");
            var menus = GameUty.FileSystem.GetFileListAtExtension(".menu");

            Debug.Log($"Found {menus.Length} menus!");

            var swDict = new Dictionary<string, StreamWriter>();

            foreach (var menu in menus)
                using (var f = GameUty.FileOpen(menu))
                    using (var br = new BinaryReader(new MemoryStream(f.ReadAll())))
                    {
                        Debug.Log(menu);

                        br.ReadString();
                        br.ReadInt32();
                        br.ReadString();
                        var filename = Path.GetFileNameWithoutExtension(menu);
                        var name = br.ReadString();
                        var category = br.ReadString().ToLowerInvariant();
                        var info = br.ReadString();

                        if (!swDict.TryGetValue(category, out var sw))
                        {
                            swDict[category] =
                                sw = new StreamWriter(Path.Combine(unitPath, $"{category}.csv"), false, encoding);
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
            var i2Path = Path.Combine(TL_DIR, "UI");
            var unitPath = Path.Combine(i2Path, "zzz_personalities");
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

                            var uniqueName = scenarioNei.GetCellAsString(1, i);
                            var displayName = scenarioNei.GetCellAsString(2, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"MaidStatus/性格タイプ/{uniqueName}", out _))
                                continue;

                            var csvName = EscapeCSVItem(displayName);
                            sw.WriteLine($"性格タイプ/{uniqueName},Text,,{csvName},{csvName}");
                        }
                    }
        }

        private void DumpYotogiData(DumpOptions opts)
        {
            var i2Path = Path.Combine(TL_DIR, "UI");
            var unitPath = Path.Combine(i2Path, "zzz_yotogi");
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

                            var skillName = scenarioNei.GetCellAsString(4, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"YotogiSkillName/{skillName}", out _))
                                continue;

                            var csvName = EscapeCSVItem(skillName);
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

                            var commandName = scenarioNei.GetCellAsString(2, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"YotogiSkillCommand/{commandName}", out _))
                                continue;

                            if (commandNames.Contains(commandName))
                                continue;

                            commandNames.Add(commandName);

                            var csvName = EscapeCSVItem(commandName);
                            sw.WriteLine($"{csvName},Text,,{csvName},{csvName}");
                        }
                    }
        }

        private void DumpVIPEvents(DumpOptions opts)
        {
            var i2Path = Path.Combine(TL_DIR, "UI");
            var unitPath = Path.Combine(i2Path, "zzz_vip_event");
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

                            var vipName = scenarioNei.GetCellAsString(1, i);
                            var vipDescription = scenarioNei.GetCellAsString(7, i);

                            if (opts.skipTranslatedItems &&
                                LocalizationManager.TryGetTranslation($"SceneDaily/スケジュール/項目/{vipName}", out _))
                                continue;

                            var csvName = EscapeCSVItem(vipName);
                            var csvDesc = EscapeCSVItem(vipDescription);
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

            if (opts.dumpEvents)
                DumpScenarioEvents(opts);

            if (opts.dumpPersonalies)
                DumpPersonalityNames(opts);

            if (opts.dumpYotogis)
                DumpYotogiData(opts);

            if (opts.dumpVIPEvents)
                DumpVIPEvents(opts);

            if (opts.dumpScripts)
                Debug.Log($"Dumped {translatedLines} lines");
            Debug.Log($"Done! Dumped translations are located in {TL_DIR}. You can now close the game!");
            Debug.Log("IMPORTANT: Delete this plugin (EngExtract.dll) if you want to play the game normally!");
        }

        [Serializable]
        internal class SubtitleData
        {
            public int addDisplayTime;
            public int displayTime = -1;
            public bool isCasino;
            public string original = string.Empty;
            public string translation = string.Empty;
            public string voice = string.Empty;
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