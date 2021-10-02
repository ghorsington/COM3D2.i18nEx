using System;

namespace COM3D2.i18nEx.Core.Util
{
    internal static class EnumConverter<T> where T : Enum
    {
        public static readonly Func<T, string> EnumToString = arg => arg.ToString();

        public static readonly Func<string, T> EnumFromString = s => (T)Enum.Parse(typeof(T), s, true);
    }
}
