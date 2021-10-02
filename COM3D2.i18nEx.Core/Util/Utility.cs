using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace COM3D2.i18nEx.Core.Util
{
    public static class Utility
    {
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return str == null || str.All(char.IsWhiteSpace);
        }

        public static string ToLF(this string val)
        {
            var sb = new StringBuilder(val.Length);

            foreach (var c in val)
                if (c != '\r')
                    sb.Append(c);

            return sb.ToString();
        }

        public static string CombinePaths(string part1, string part2)
        {
            return Path.Combine(part1, part2);
        }

        public static string CombinePaths(params string[] parts)
        {
            if (parts.Length == 0)
                return null;
            if (parts.Length == 1)
                return parts[0];

            var result = parts[0];

            for (var i = 1; i < parts.Length; i++)
                result = Path.Combine(result, parts[i]);

            return result;
        }

        public static byte[] TexToPng(Texture2D tex)
        {
            if (tex.format == TextureFormat.DXT1 || tex.format == TextureFormat.DXT5)
                return DuplicateTextureToPng(tex);
            try
            {
                return tex.EncodeToPNG();
            }
            catch (Exception)
            {
                return DuplicateTextureToPng(tex);
            }
        }

        private static byte[] DuplicateTextureToPng(Texture2D tex)
        {
            var dup = Duplicate(tex);
            var res = dup.EncodeToPNG();
            Object.Destroy(dup);
            return res;
        }

        private static Texture2D Duplicate(Texture texture)
        {
            var render = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default,
                                                    RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, render);
            var previous = RenderTexture.active;
            RenderTexture.active = render;
            var result = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            result.ReadPixels(new Rect(0, 0, render.width, render.height), 0, 0);
            result.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(render);
            return result;
        }
    }
}
