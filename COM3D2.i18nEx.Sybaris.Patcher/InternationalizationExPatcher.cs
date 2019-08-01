using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MethodAttributes = Mono.Cecil.MethodAttributes;

namespace COM3D2.i18nEx.Sybaris.Patcher
{
    public static class InternationalizationExPatcher
    {
        private const string MANAGED_ASSEMBLY = "COM3D2.i18nEx.Sybaris.Managed";
        public static readonly string[] TargetAssemblyNames = {"UnityEngine.dll"};


        public static void Patch(AssemblyDefinition ad)
        {
            AssemblyDefinition managedAd;
            try
            {
                managedAd = AssemblyDefinition.ReadAssembly(Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"{MANAGED_ASSEMBLY}.dll"));
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to run i18nEx patcher because {e}!");
                return;
            }

            var managedMD = managedAd.MainModule;
            var hookType = managedMD.GetType($"{MANAGED_ASSEMBLY}.Entrypoint");
            var hookMethod = hookType.Methods.FirstOrDefault(m => m.Name == "Start");

            var md = ad.MainModule;
            var application = md.GetType("UnityEngine.Application");
            var cctor = application.Methods.FirstOrDefault(m => m.Name == ".cctor");

            if (cctor == null)
            {
                cctor = new MethodDefinition(".cctor", MethodAttributes.Static
                                                       | MethodAttributes.Private
                                                       | MethodAttributes.HideBySig
                                                       | MethodAttributes.SpecialName
                                                       | MethodAttributes.RTSpecialName,
                    md.ImportReference(typeof(void)));

                var cil = cctor.Body.GetILProcessor();
                cil.Emit(OpCodes.Ret);
            }

            var il = cctor.Body.GetILProcessor();
            var ins = il.Body.Instructions.First();
                
            il.InsertBefore(ins, il.Create(OpCodes.Call, md.ImportReference(hookMethod)));
        }
    }
}