using COM3D2.i18nEx.Core.Hooks;
using COM3D2.i18nEx.Core.TranslationManagers;
using COM3D2.i18nEx.Core.Util;
using UnityEngine;

namespace COM3D2.i18nEx.Core
{
    public class Core : MonoBehaviour
    {
        internal static ILogger Logger { get; private set; }

        public bool Initialized { get; private set; }

        public void Initialize(ILogger logger, string gameRoot)
        {
            if (Initialized)
                return;

            Logger = logger;
            Logger.LogInfo("Initializing i18nEx...");

            Paths.Initialize(gameRoot);

            ScriptTranslationManager.Initialize();
            TextureReplaceManager.Initialize();

            TranslationHooks.Initialize();

            Logger.LogInfo("i18nEx initialized!");
            Initialized = true;
        }

        void Awake()
        {
            DontDestroyOnLoad(this);
        }

        void Update()
        {
            KeyCommandHandler.UpdateState();

            if(Configuration.ScriptTranslationsConfig.ReloadTranslationsKey.Value.IsPressed)
                ScriptTranslationManager.ReloadActiveTranslations();
        }
    }
}
