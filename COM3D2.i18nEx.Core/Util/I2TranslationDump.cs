using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HarmonyLib;
using I2.Loc;

namespace COM3D2.i18nEx.Core.Util
{
    public static class I2TranslationDump
    {
        private static readonly HashSet<string> DumpedTerms = new();
        private static string extractPath;
        private static readonly Encoding Utf8 = new UTF8Encoding(true);

        private static bool initialized;

        public static void Initialize()
        {
            if (!Configuration.I2Translation.DumpTexts.Value)
                return;

            if (!initialized)
                Harmony.CreateAndPatchAll(typeof(I2TranslationDump));

            initialized = true;
            DumpedTerms.Clear();

            var langPath = Path.Combine(Paths.TranslationsRoot, Core.CurrentSelectedLanguage);
            var dumpPath = Path.Combine(langPath, "UI_Dump");
            extractPath = Path.Combine(dumpPath, DateTime.Now.ToString("yyyy_MM_dd__HHmmss"));

            Core.Logger.LogInfo($"[I2Loc] creating UI dumps to {extractPath}");
            Directory.CreateDirectory(extractPath);
        }

        private static bool SplitTerm(string term, out string mainCategory, out string rest)
        {
            var index = term.IndexOf('/');
            if (index == -1)
            {
                mainCategory = rest = null;
                return false;
            }

            mainCategory = term.Substring(0, index);
            rest = term.Substring(index + 1);
            return true;
        }

        private static string EscapeCsv(this string str, char delimiter = ',')
        {
            if (str.Contains("\n") || str.Contains(delimiter.ToString()))
                return $"\"{str.Replace("\"", "\"\"")}\"";
            return str;
        }

        [HarmonyPatch(typeof(LocalizationManager), nameof(LocalizationManager.TryGetTranslation))]
        [HarmonyPostfix]
        public static void PostTryGetTranslation(ref bool __result, string Term)
        {
            if (__result || DumpedTerms.Contains(Term))
                return;

            if (!SplitTerm(Term, out var mainCategory, out var restTerm))
                return;

            var csvPath = Path.Combine(extractPath, $"{mainCategory}.csv");
            if (!File.Exists(csvPath))
                File.WriteAllText(csvPath, "Key,Type,Desc,Japanese,English\n", Utf8);
            File.AppendAllText(csvPath, $"{restTerm.EscapeCsv()},Text,,,\n", Utf8);

            DumpedTerms.Add(Term);
        }
    }
}
