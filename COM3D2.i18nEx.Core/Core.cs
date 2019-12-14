using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using COM3D2.i18nEx.Core.Hooks;
using COM3D2.i18nEx.Core.Loaders;
using COM3D2.i18nEx.Core.TranslationManagers;
using COM3D2.i18nEx.Core.Util;
using ExIni;
using I2.Loc;
using UnityEngine;

namespace COM3D2.i18nEx.Core
{
    public class Core : MonoBehaviour
    {
        private const int MIN_SUPPORTED_VERSION = 1320;
        internal static ScriptTranslationManager ScriptTranslate;
        internal static TextureReplaceManager TextureReplace;
        internal static I2TranslationManager I2Translation;
        private readonly List<TranslationManagerBase> managers = new List<TranslationManagerBase>();

        public static ILogger Logger { get; private set; }

        public bool Initialized { get; private set; }

        internal static ITranslationLoader TranslationLoader { get; private set; }

        private int GameVersion => (int) typeof(Misc).GetField(nameof(Misc.GAME_VERSION)).GetValue(null);

        public void Initialize(ILogger logger, string gameRoot)
        {
            if (GameVersion < MIN_SUPPORTED_VERSION)
            {
                logger.LogWarning(
                                  $"This version of i18nEx core supports only game versions {MIN_SUPPORTED_VERSION} or newer. Detected game version: {GameVersion}");
                Destroy(this);
                return;
            }

            if (Initialized)
                return;

            Logger = logger;
            Logger.LogInfo("Initializing i18nEx...");

            Paths.Initialize(gameRoot);
            InitializeTranslationManagers();
            TranslationHooks.Initialize();

            Logger.LogInfo("i18nEx initialized!");
            Initialized = true;
        }

        private T RegisterTranslationManager<T>() where T : TranslationManagerBase
        {
            var res = gameObject.AddComponent<T>();
            managers.Add(res);
            return res;
        }

        private void InitializeTranslationManagers()
        {
            ScriptTranslate = RegisterTranslationManager<ScriptTranslationManager>();
            TextureReplace = RegisterTranslationManager<TextureReplaceManager>();
            I2Translation = RegisterTranslationManager<I2TranslationManager>();

            LoadLanguage(Configuration.General.ActiveLanguage.Value);
            Configuration.General.ActiveLanguage.ValueChanged += LoadLanguage;
        }

        private void LoadLanguage(string langName)
        {
            var tlLang = Path.Combine(Paths.TranslationsRoot, langName);

            if (!Directory.Exists(tlLang))
            {
                Logger.LogWarning($"No translations for language \"{langName}\" was found!");
                return;
            }

            TranslationLoader?.UnloadCurrentTranslation();

            var iniFile = LoadLanguageConfig(tlLang);

            TranslationLoader =
                iniFile == null ? new BasicTranslationLoader() : GetLoader(iniFile["Info"]["Loader"].Value);

            Logger.LogInfo($"Selecting language for {TranslationLoader}");
            TranslationLoader.SelectLanguage(langName, tlLang, iniFile);

            foreach (var mgr in managers)
                mgr.LoadLanguage();
        }

        private ITranslationLoader GetLoader(string loaderName)
        {
            if (string.IsNullOrEmpty(loaderName))
                return new BasicTranslationLoader();

            var loadersPath = Path.Combine(Paths.TranslationsRoot, "loaders");
            if (!Directory.Exists(loadersPath))
                Directory.CreateDirectory(loadersPath);

            loaderName = loaderName.Trim();

            if (loaderName == "BasicLoader")
                return new BasicTranslationLoader();

            var loaderPath = Path.Combine(loadersPath, $"{loaderName}.dll");
            if (!File.Exists(loaderPath))
                return new BasicTranslationLoader();

            try
            {
                var ass = Assembly.LoadFile(loaderPath);
                var loader = ass.GetTypes().FirstOrDefault(t => t.GetInterface(nameof(ITranslationLoader)) != null);

                Logger.LogInfo($"Invoking loader {loader}");

                if (loader != null)
                    return Activator.CreateInstance(loader) as ITranslationLoader;

                Logger.LogWarning(
                                  $"Loader \"{loaderName}.dll\" doesn't contain any translation loader implementations!");
                return new BasicTranslationLoader();
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to load translation loader \"{loaderName}.dll\". Reason: {e.Message}");
                return new BasicTranslationLoader();
            }
        }

        private IniFile LoadLanguageConfig(string tlPath)
        {
            var iniFile = Path.Combine(tlPath, "config.ini");
            if (!File.Exists(iniFile))
                return null;
            try
            {
                return IniFile.FromFile(iniFile);
            }
            catch (Exception e)
            {
                Logger.LogWarning($"Failed to read config.ini. Reason: {e.Message}");
                return null;
            }
        }

        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            KeyCommandHandler.UpdateState();

            if (Configuration.General.ReloadConfigKey.Value.IsPressed)
                Configuration.Reload();

            if (Configuration.General.ReloadTranslationsKey.Value.IsPressed)
                foreach (var mgr in managers)
                    mgr.ReloadActiveTranslations();

            if (Input.GetKey(KeyCode.Keypad0))
                foreach (var languageSource in LocalizationManager.Sources)
                    Logger.LogInfo($"Got source {languageSource}");
        }
    }
}