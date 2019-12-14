using System;
using System.Collections.Generic;
using System.IO;
using COM3D2.i18nEx.Core.Util;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace COM3D2.i18nEx.Core.TranslationManagers
{
    internal class TextureReplacement
    {
        public TextureReplacement(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
        }

        public string Name { get; }

        public string FullPath { get; }

        public byte[] Data { get; set; }

        public void Load()
        {
            using (var s = Core.TranslationLoader.OpenTextureTranslation(FullPath))
            {
                Data = new byte[s.Length];
                s.Read(Data, 0, Data.Length);
            }
        }
    }

    internal class TextureReplaceManager : TranslationManagerBase
    {
        private readonly HashSet<string> dumpedItems = new HashSet<string>();
        private readonly HashSet<string> missingTextures = new HashSet<string>();
        private readonly LinkedList<TextureReplacement> texReplacementCache = new LinkedList<TextureReplacement>();

        private readonly Dictionary<string, LinkedListNode<TextureReplacement>> texReplacementLookup =
            new Dictionary<string, LinkedListNode<TextureReplacement>>(StringComparer.InvariantCultureIgnoreCase);

        private readonly Dictionary<string, string> textureReplacements =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);


        public override void LoadLanguage()
        {
            Core.Logger.LogInfo("Loading texture replacements");

            missingTextures.Clear();
            textureReplacements.Clear();
            texReplacementLookup.Clear();
            texReplacementCache.Clear();

            var files = Core.TranslationLoader.GetTextureTranslationFileNames();

            if (files == null)
            {
                Core.Logger.LogInfo("No textures found! Skipping...");
                return;
            }

            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);

                if (textureReplacements.ContainsKey(name))
                {
                    Core.Logger.LogWarning(
                                           $"Found duplicate replacements for texture \"{name}\". Please name all your textures uniquely. If there are name collisions, name them by hash.");
                    continue;
                }

                textureReplacements[name] = file;
            }
        }

        private void Update()
        {
            if (Configuration.TextureReplacement.ReloadTranslationsKey.Value.IsPressed)
                ReloadActiveTranslations();

            if (Configuration.I2Translation.PrintFontNamesKey.Value.IsPressed)
                Core.Logger.LogInfo($"Supported fonts:\n{string.Join("\n", Font.GetOSInstalledFontNames())}");
        }

        public bool ReplacementExists(string texName)
        {
            return textureReplacements.ContainsKey(texName);
        }

        public override void ReloadActiveTranslations()
        {
            LoadLanguage();
        }

        public byte[] GetReplacementTextureBytes(string texName, string tag = null, bool skipLogging = false)
        {
            return GetReplacement(texName, tag, skipLogging)?.Data;
        }

        public void DumpTexture(string texName, Texture tex)
        {
            if (dumpedItems.Contains(texName))
                return;

            if (!(tex is Texture2D tex2d))
                return;

            var dumpPath = Utility.CombinePaths(Paths.TranslationsRoot, Configuration.General.ActiveLanguage.Value,
                                                "Textures", "Dumped");

            if (!Directory.Exists(dumpPath))
                Directory.CreateDirectory(dumpPath);

            Core.Logger.LogInfo($"[DUMP] {texName}.png");
            var p = Path.Combine(dumpPath, $"{texName}.png");
            File.WriteAllBytes(p, Utility.TexToPng(tex2d));
            dumpedItems.Add(texName);
        }

        private TextureReplacement GetReplacement(string texName, string tag = null, bool skipLogging = false)
        {
            var hash = $"{texName}:{tag}".KnuthHash().ToString("X16");
            string[] lookupNames =
            {
                texName,
                hash,
                $"{texName}@{SceneManager.GetActiveScene().buildIndex}",
                $"{hash}@{SceneManager.GetActiveScene().buildIndex}"
            };

            foreach (var lookupName in lookupNames)
            {
                if (Configuration.TextureReplacement.VerboseLogging.Value && !skipLogging)
                    Core.Logger.LogInfo($"Trying with name {lookupName}.png");
                if (!textureReplacements.ContainsKey(lookupName))
                    continue;
                return LoadReplacement(lookupName);
            }

            return null;
        }

        private TextureReplacement LoadReplacement(string name)
        {
            if (texReplacementLookup.TryGetValue(name, out var node))
            {
                texReplacementCache.Remove(node);
                texReplacementCache.AddFirst(node);
                return node.Value;
            }

            if (texReplacementLookup.Count == Configuration.TextureReplacement.MaxTexturesCached.Value)
            {
                node = texReplacementCache.Last;
                texReplacementCache.RemoveLast();
                texReplacementLookup.Remove(node.Value.Name);
            }

            try
            {
                var newNode = new TextureReplacement(name, textureReplacements[name]);
                newNode.Load();
                node = texReplacementCache.AddFirst(newNode);
                texReplacementLookup.Add(name, node);
                return newNode;
            }
            catch (Exception e)
            {
                Core.Logger.LogError($"Failed to load texture \"{name}\" because: {e.Message}");
                textureReplacements.Remove(name);
                return null;
            }
        }
    }
}