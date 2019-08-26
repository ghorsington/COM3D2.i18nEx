using System;
using System.Collections.Generic;
using System.Text;

namespace COM3D2.i18nEx.Core.Util
{
    internal static class StringExtensions
    {
        public static string Splice(this string self, int start, int end)
        {
            if (start < 0)
                start += self.Length;
            if (end < 0)
                end += self.Length;

            return self.Substring(start, end - start + 1);
        }

        public static ulong KnuthHash(this string read)
        {
            var hashedValue = 3074457345618258791ul;
            foreach (char t in read)
            {
                hashedValue += t;
                hashedValue *= 3074457345618258799ul;
            }

            return hashedValue;
        }

        public static KeyValuePair<string, string> SplitTranslation(this string txt)
        {
            int pos;
            if ((pos = txt.IndexOf("<E>", StringComparison.InvariantCultureIgnoreCase)) > 0)
                return new KeyValuePair<string, string>(txt.Substring(0, pos), txt.Substring(pos + 3));
            return new KeyValuePair<string, string>(txt, string.Empty);
        }

        public static string Escape(this string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return txt;
            var stringBuilder = new StringBuilder(txt.Length + 2);
            foreach (char c in txt)
                switch (c)
                {
                    case '\0':
                        stringBuilder.Append(@"\0");
                        break;
                    case '\a':
                        stringBuilder.Append(@"\a");
                        break;
                    case '\b':
                        stringBuilder.Append(@"\b");
                        break;
                    case '\t':
                        stringBuilder.Append(@"\t");
                        break;
                    case '\n':
                        stringBuilder.Append(@"\n");
                        break;
                    case '\v':
                        stringBuilder.Append(@"\v");
                        break;
                    case '\f':
                        stringBuilder.Append(@"\f");
                        break;
                    case '\r':
                        stringBuilder.Append(@"\r");
                        break;
                    case '\'':
                        stringBuilder.Append(@"\'");
                        break;
                    case '\\':
                        stringBuilder.Append(@"\");
                        break;
                    case '\"':
                        stringBuilder.Append(@"\""");
                        break;
                    default:
                        stringBuilder.Append(c);
                        break;
                }

            return stringBuilder.ToString();
        }

        public static string Unescape(this string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return txt;
            var stringBuilder = new StringBuilder(txt.Length);
            for (var i = 0; i < txt.Length;)
            {
                int num = txt.IndexOf('\\', i);
                if (num < 0 || num == txt.Length - 1)
                    num = txt.Length;
                stringBuilder.Append(txt, i, num - i);
                if (num >= txt.Length)
                    break;
                char c = txt[num + 1];
                switch (c)
                {
                    case '0':
                        stringBuilder.Append('\0');
                        break;
                    case 'a':
                        stringBuilder.Append('\a');
                        break;
                    case 'b':
                        stringBuilder.Append('\b');
                        break;
                    case 't':
                        stringBuilder.Append('\t');
                        break;
                    case 'n':
                        stringBuilder.Append('\n');
                        break;
                    case 'v':
                        stringBuilder.Append('\v');
                        break;
                    case 'f':
                        stringBuilder.Append('\f');
                        break;
                    case 'r':
                        stringBuilder.Append('\r');
                        break;
                    case '\'':
                        stringBuilder.Append('\'');
                        break;
                    case '\"':
                        stringBuilder.Append('\"');
                        break;
                    case '\\':
                        stringBuilder.Append('\\');
                        break;
                    default:
                        stringBuilder.Append('\\').Append(c);
                        break;
                }

                i = num + 2;
            }

            return stringBuilder.ToString();
        }
    }
}