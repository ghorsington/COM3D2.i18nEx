using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal abstract class TranslationManagerBase : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(this);
            OnAwake();
        }

        protected virtual void OnAwake() { }

        public abstract void LoadLanguage(string langName);

        public abstract void ReloadActiveTranslations();
    }
}
