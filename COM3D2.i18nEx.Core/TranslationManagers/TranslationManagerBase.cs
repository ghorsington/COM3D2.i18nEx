using UnityEngine;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal abstract class TranslationManagerBase : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
            OnAwake();
        }

        protected virtual void OnAwake()
        {
        }

        public abstract void LoadLanguage(string langName);

        public abstract void ReloadActiveTranslations();
    }
}