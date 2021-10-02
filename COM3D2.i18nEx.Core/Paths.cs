using System.IO;

namespace COM3D2.i18nEx.Core
{
    internal static class Paths
    {
        public static string TranslationsRoot { get; private set; }
        public static string ConfigurationFilePath { get; private set; }

        public static void Initialize(string gameRoot)
        {
            Core.Logger.LogInfo("Initializing paths...");

            TranslationsRoot = Path.Combine(gameRoot, "i18nEx");
            ConfigurationFilePath = Path.Combine(TranslationsRoot, "configuration.ini");

            if (!Directory.Exists(TranslationsRoot))
            {
                Core.Logger.LogInfo($"No root path found. Creating one in {TranslationsRoot}");
                Directory.CreateDirectory(TranslationsRoot);
            }
        }
    }
}
