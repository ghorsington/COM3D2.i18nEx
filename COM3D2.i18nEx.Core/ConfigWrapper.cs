using System;
using System.ComponentModel;
using System.Linq;
using ExIni;

namespace COM3D2.i18nEx.Core
{
    internal class ConfigWrapper<T>
    {
        private readonly IniFile file;
        private readonly string savePath;
        private string section, key, description;
        private readonly IniKey iniKey;
        private readonly TypeConverter cvt;

        public ConfigWrapper(IniFile file, string savePath, string section, string key, string description = null,
            T defaultValue = default)
        {
            this.file = file;
            this.savePath = savePath;
            this.section = section;
            this.key = key;
            this.description = description;

            iniKey = file[section][key];
            iniKey.Comments.Comments = description?.Split('\n').ToList();

            cvt = TypeDescriptor.GetConverter(typeof(T));

            if (!cvt.CanConvertFrom(typeof(string)))
                throw new ArgumentException("Default TypeConverter can't convert from String");

            if (!cvt.CanConvertTo(typeof(string)))
                throw new ArgumentException("Default TypeConverter can't convert to String");

            if (iniKey.Value == null)
                Value = defaultValue;
        }

        public T Value
        {
            get => (T) cvt.ConvertFromInvariantString(iniKey.Value);
            set
            {
                iniKey.Value = cvt.ConvertToInvariantString(value);
                Save();
            }
        }

        private void Save()
        {
            file.Save(savePath);
        }
    }
}