using BepInEx;
using COM3D2.i18nEx.Core;
using UnityEngine;
using ILogger = COM3D2.i18nEx.Core.ILogger;

namespace COM3D2.i18nEx.BepInEx
{
    [BepInPlugin("horse.coder.com3d2.i18nex", "InternationaliazationEx", "1.0.0.0")]
    public class InternationalizationBepInEx : BaseUnityPlugin, ILogger
    {
        private GameObject managerObject;

        public void Awake()
        {
            DontDestroyOnLoad(this);

            managerObject = new GameObject("i18nExManager");
            DontDestroyOnLoad(managerObject);

            var core = managerObject.AddComponent<InternationalizationCore>();
            core.Initialize(this);
        }

        public void LogInfo(object data)
        {
            Logger.LogInfo(data);
        }

        public void LogWarning(object data)
        {
            Logger.LogWarning(data);
        }

        public void LogError(object data)
        {
            Logger.LogError(data);
        }
    }
}
