using System.Collections.Generic;
using System.IO;
using ExIni;

namespace COM3D2.i18nEx.Core.Loaders
{
    public interface ITranslationLoader
    {
        string CurrentLanguage { get; }

        void SelectLanguage(string name, string path, IniFile config);

        void UnloadCurrentTranslation();

        IEnumerable<string> GetScriptTranslationFileNames();

        IEnumerable<string> GetTextureTranslationFileNames();

        SortedDictionary<string, IEnumerable<string>> GetUITranslationFileNames();

        Stream OpenScriptTranslation(string path);

        Stream OpenTextureTranslation(string path);

        Stream OpenUiTranslation(string path);
    }
}