using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Harmony;
using COM3D2.i18nEx.Core.TranslationManagers;
using HarmonyLib;
using I2.Loc;
using MaidStatus;
using UnityEngine;
using UnityEngine.UI;

namespace COM3D2.i18nEx.Core.Hooks
{
    internal static class UIFixes
    {
        private static Harmony instance;
        private static bool initialized;
        private static readonly Dictionary<string, Font> customFonts = new Dictionary<string, Font>();

        public static void Initialize()
        {
            if (initialized)
                return;

            instance = HarmonyWrapper.PatchAll(typeof(UIFixes), "horse.coder.i18nex.ui_fixes");

            initialized = true;
        }

        [HarmonyPatch(typeof(CMSystem), "LoadIni")]
        [HarmonyPostfix]
        public static void PostLoadIni()
        {
            if (Configuration.General.FixSubtitleType.Value)
            {
                Configuration.ScriptTranslations.RerouteTranslationsTo.Value = TranslationsReroute.RouteToJapanese;
                Configuration.General.FixSubtitleType.Value = false;
                GameMain.Instance.CMSystem.SubtitleType = SubtitleDisplayManager.DisplayType.Original;
                GameMain.Instance.CMSystem.SaveIni();
                Core.Logger.LogInfo("Fixed game's subtitle type!");
            }
        }

        [HarmonyPatch(typeof(Status), nameof(Status.maxNameLength), MethodType.Getter)]
        [HarmonyPostfix]
        public static void GetMaxNameLength(ref int __result)
        {
            __result = int.MaxValue;
        }

        [HarmonyPatch(typeof(Text), "text", MethodType.Setter)]
        [HarmonyPrefix]
        public static void OnSetText(Text __instance, string value)
        {
            SetLoc(__instance.gameObject, value);
        }

        [HarmonyPatch(typeof(UILabel), "ProcessAndRequest")]
        [HarmonyPrefix]
        public static void OnProcessRequest(UILabel __instance)
        {
            SetLoc(__instance.gameObject, __instance.text);
        }

        private static void SetLoc(GameObject go, string text)
        {
            var loc = go.GetComponent<Localize>();
            if (loc != null || string.IsNullOrEmpty(text))
                return;

            var term = $"General/{text.Replace(" ", "_")}";
            if (Configuration.I2Translation.VerboseLogging.Value)
                Core.Logger.LogInfo($"Trying to localize with term {term}");
            loc = go.AddComponent<Localize>();
            loc.SetTerm(term);
        }

        [HarmonyPatch(typeof(Text), "OnEnable")]
        [HarmonyPrefix]
        public static void ChangeUEUIFont(Text __instance)
        {
            __instance.font = SwapFont(__instance.font);
        }

        [HarmonyPatch(typeof(UILabel), "ProcessAndRequest")]
        [HarmonyPrefix]
        public static void ChangeFont(UILabel __instance)
        {
            __instance.trueTypeFont = SwapFont(__instance.trueTypeFont);
        }

        private static Font SwapFont(Font originalFont)
        {
            if (originalFont == null)
                return null;

            var customFont = Configuration.I2Translation.CustomUIFont.Value.Trim();
            if (string.IsNullOrEmpty(customFont) || originalFont.name == customFont)
                return originalFont;

            var fontId = $"{customFont}#{originalFont.fontSize}";
            if (!customFonts.TryGetValue(fontId, out var font))
                font = customFonts[fontId] = Font.CreateDynamicFontFromOSFont(customFont, originalFont.fontSize);
            return font ?? originalFont;
        }

        [HarmonyPatch(typeof(SceneNetorareCheck), "Start")]
        [HarmonyPostfix]
        public static void LocalizeNTRScene(GameObject ___toggleParent)
        {
            Core.Logger.LogInfo("Fixing NTR check scene.");

            void Localize(string item)
            {
                var result = UTY.GetChildObject(___toggleParent, $"{item}/Result"); //.GetComponent<UILabel>();
                var title = UTY.GetChildObject(___toggleParent, $"{item}/Title");   //.GetComponent<UILabel>();

                var resultLoc = result.AddComponent<Localize>();
                resultLoc.SetTerm($"SceneNetorareCheck/{item}_Result");

                var titleLoc = title.AddComponent<Localize>();
                titleLoc.SetTerm($"SceneNetorareCheck/{item}_Title");
            }

            Localize("Toggle_LockUserDraftMaid");
            Localize("Toggle_IsComPlayer");
        }

        [HarmonyPatch(typeof(SystemShortcut), "OnClick_Info")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> LocalizeInfoText(IEnumerable<CodeInstruction> instructions)
        {
            var hasText = false;
            foreach (var codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Callvirt && codeInstruction.operand is MethodInfo minfo &&
                    minfo.Name             == "get_SysDlg")
                {
                    hasText = true;
                }
                else if (hasText)
                {
                    hasText = false;
                    var index = -1;
                    if (OpCodes.Ldloc_0.Value        <= codeInstruction.opcode.Value &&
                        codeInstruction.opcode.Value <= OpCodes.Ldloc_3.Value)
                        index = codeInstruction.opcode.Value - OpCodes.Ldloc_0.Value;
                    else if (codeInstruction.opcode == OpCodes.Ldloc_S || codeInstruction.opcode == OpCodes.Ldloc)
                        index = (int) codeInstruction.operand;

                    if (index < 0)
                    {
                        Core.Logger.LogError("Failed to patch info text localization! Please report this!");
                        yield return codeInstruction;
                        continue;
                    }

                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldloca, index);
                    yield return HarmonyWrapper.EmitDelegate<TranslateInfo>((ref string text) =>
                    {
                        if (LocalizationManager.TryGetTranslation("System/GameInfo_Description", out var tl))
                            text = string.Format(tl, Product.gameTitle, GameUty.GetBuildVersionText(),
                                                 GameUty.GetGameVersionText(), GameUty.GetLegacyGameVersionText());
                    });
                    yield return new CodeInstruction(OpCodes.Call,
                                                     AccessTools.PropertyGetter(
                                                                                typeof(GameMain),
                                                                                nameof(GameMain.Instance)));
                    yield return new CodeInstruction(OpCodes.Callvirt,
                                                     AccessTools.PropertyGetter(
                                                                                typeof(GameMain),
                                                                                nameof(GameMain.SysDlg)));
                }

                yield return codeInstruction;
            }
        }

        private delegate void TranslateInfo(ref string text);
    }
}