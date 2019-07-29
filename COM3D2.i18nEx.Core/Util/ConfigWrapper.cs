using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using ExIni;

namespace COM3D2.i18nEx.Core.Util
{
    internal class ConfigWrapper<T>
    {
        private readonly IniFile file;
        private readonly string savePath;
        private string section, key, description;
        private readonly IniKey iniKey;
        private readonly Func<T, string> toStringConvert;
        private readonly Func<string, T> fromStringConvert;

        public ConfigWrapper(IniFile file, string savePath, string section, string key, string description = null,
            T defaultValue = default, Func<T, string> toStringConvert = null, Func<string, T> fromStringConvert = null)
        {
            this.file = file;
            this.savePath = savePath;
            this.section = section;
            this.key = key;
            this.description = description;

            iniKey = file[section][key];
            iniKey.Comments.Comments = description?.Split('\n').ToList();

            var cvt = TypeDescriptor.GetConverter(typeof(T));

            if (fromStringConvert == null && !cvt.CanConvertFrom(typeof(string)))
                throw new ArgumentException("Default TypeConverter can't convert from String");

            if (toStringConvert == null && !cvt.CanConvertTo(typeof(string)))
                throw new ArgumentException("Default TypeConverter can't convert to String");

            this.toStringConvert = toStringConvert ?? (v => cvt.ConvertToInvariantString(v));
            this.fromStringConvert = fromStringConvert ?? (v => (T) cvt.ConvertFromInvariantString(v));

            if (iniKey.Value == null)
                Value = defaultValue;
        }

        public T Value
        {
            get => fromStringConvert(iniKey.Value);
            set
            {
                iniKey.Value = toStringConvert(value);
                Save();
            }
        }

        private void Save()
        {
            file.Save(savePath);
        }
    }
}