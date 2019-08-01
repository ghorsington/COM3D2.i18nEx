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
        private Texture2D replacement;

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
            replacement = null;
            Data = File.ReadAllBytes(FullPath);
        }
    }

    internal class TextureReplaceManager : TranslationManagerBase
    {
        private readonly HashSet<string> dumpedItems = new HashSet<string>();
        private readonly LinkedList<TextureReplacement> texReplacementCache = new LinkedList<TextureReplacement>();

        private readonly Dictionary<string, LinkedListNode<TextureReplacement>> texReplacementLookup =
            new Dictionary<string, LinkedListNode<TextureReplacement>>(StringComparer.InvariantCultureIgnoreCase);

        private readonly Dictionary<string, string> textureReplacements =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);


        public override void LoadLanguage(string language)
        {
            var tlPath = Path.Combine(Paths.TranslationsRoot, language);
            var texPath = Path.Combine(tlPath, "Textures");
            Core.Logger.LogInfo($"Loading texture replacements for language \"{language}\"");

            if (!Directory.Exists(tlPath))
            {
                Core.Logger.LogWarning(
                    $"No translation folder found for language {language}. Skipping loading texture translations...");
                return;
            }

            textureReplacements.Clear();
            texReplacementLookup.Clear();
            texReplacementCache.Clear();

            if (!Directory.Exists(texPath))
            {
                Directory.CreateDirectory(texPath);
                return;
            }

            foreach (var file in Directory.GetFiles(texPath, "*.png", SearchOption.AllDirectories))
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

        void Update()
        {
            if(Configuration.TextureReplacement.ReloadTranslationsKey.Value.IsPressed)
                ReloadActiveTranslations();
        }

        public override void ReloadActiveTranslations()
        {
            foreach (var textureReplacement in texReplacementCache)
            {
                Debug.Log($"Reloading texture {textureReplacement.Name}.png");
                textureReplacement.Load();
            }
        }

        public byte[] GetReplacementTextureBytes(string texName, string tag = null)
        {
            return GetReplacement(texName, tag)?.Data;
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

        private TextureReplacement GetReplacement(string texName, string tag = null)
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