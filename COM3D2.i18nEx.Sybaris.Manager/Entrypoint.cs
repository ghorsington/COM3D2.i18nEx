using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEngine;
using Debug = UnityEngine.Debug;
using ILogger = COM3D2.i18nEx.Core.ILogger;
using Object = UnityEngine.Object;

namespace COM3D2.i18nEx.Sybaris.Managed
{
    public static class Entrypoint
    {
        private static GameObject go;
        public static void Start()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveHarmony;
            go = new GameObject("i18nExManager");
            Object.DontDestroyOnLoad(go);
            var core = go.AddComponent<Core.Core>();
            core.Initialize(new UnityLogger(), Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName));
        }

        private static Assembly ResolveHarmony(object sender, ResolveEventArgs args)
        {
            var name = new AssemblyName(args.Name);

            switch (name.Name)
            {
                case "0Harmony" when name.Version == new Version(2, 0, 0, 0):
                    return Assembly.GetExecutingAssembly();
                case "BepInEx.Harmony" when name.Version == new Version(2, 0, 0, 0):
                    return Assembly.GetExecutingAssembly();
                default:
                    return null;
            }
        }
    }

    public class UnityLogger : ILogger
    {
        private const string PREFIX = "[i18nEx]";

        public void LogInfo(object data)
        {
            Debug.Log($"{PREFIX} {data}");
        }

        public void LogWarning(object data)
        {
            Debug.LogWarning($"{PREFIX} {data}");
        }

        public void LogError(object data)
        {
            Debug.LogError($"{PREFIX} {data}");
        }
    }
}
