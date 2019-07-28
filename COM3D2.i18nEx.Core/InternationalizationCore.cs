using UnityEngine;

namespace COM3D2.i18nEx.Core
{
    public class InternationalizationCore : MonoBehaviour
    {
        internal static ILogger Logger { get; private set; }

        public bool Initialized { get; private set; }

        public void Initialize(ILogger logger)
        {
            if (Initialized)
                return;

            Logger = logger;
            Logger.LogInfo("Initializing i18nEx...");

            TranslationEvents.Initialize();

            Logger.LogInfo("i18nEx initialized!");
            Initialized = true;
        }

        void Awake()
        {
            DontDestroyOnLoad(this);
        }
    }
}
