using System.Collections.Generic;
using COM3D2.i18nEx.Core.Hooks;
using COM3D2.i18nEx.Core.TranslationManagers;
using COM3D2.i18nEx.Core.Util;
using I2.Loc;
using UnityEngine;

namespace COM3D2.i18nEx.Core
{
    public class Core : MonoBehaviour
    {
        internal static ScriptTranslationManager ScriptTranslate;
        internal static TextureReplaceManager TextureReplace;
        internal static I2TranslationManager I2Translation;
        private readonly List<TranslationManagerBase> managers = new List<TranslationManagerBase>();

        internal static ILogger Logger { get; private set; }
        public bool Initialized { get; private set; }

        public void Initialize(ILogger logger, string gameRoot)
        {
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

            foreach (var mgr in managers)
                mgr.LoadLanguage(Configuration.General.ActiveLanguage.Value);

            Configuration.General.ActiveLanguage.ValueChanged += s =>
            {
                foreach (var mgr in managers)
                    mgr.LoadLanguage(s);
            };
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