using System;
using System.ComponentModel;
using System.Linq;
using ExIni;

namespace COM3D2.i18nEx.Core.Util
{
    internal class ConfigWrapper<T>
    {
        private readonly IniFile file;
        private readonly string savePath;
        private readonly IniKey iniKey;
        private readonly Func<T, string> toStringConvert;
        private readonly Func<string, T> fromStringConvert;
        private readonly T defaultValue;
        private T prevValue;
        private string prevValueRaw;

        public ConfigWrapper(IniFile file, string savePath, string section, string key, string description = null,
            T defaultValue = default, Func<T, string> toStringConvert = null, Func<string, T> fromStringConvert = null)
        {
            this.file = file;
            this.savePath = savePath;
            this.defaultValue = defaultValue;

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
            get
            {
                try
                {
                    if (iniKey.RawValue != prevValueRaw)
                    {
                        UnloadValue();
                        prevValue = fromStringConvert(iniKey.Value);
                    }
                }
                catch (Exception)
                {
                    Value = defaultValue;
                }

                return prevValue;
            }
            set
            {
                iniKey.Value = toStringConvert(value);
                UnloadValue();
                prevValue = value;
                prevValueRaw = iniKey.RawValue;
                Save();
            }
        }

        private void UnloadValue()
        {
            if(prevValue != null && prevValue is IDisposable disposable)
                disposable.Dispose();
        }

        private void Save()
        {
            file.Save(savePath);
        }
    }
}