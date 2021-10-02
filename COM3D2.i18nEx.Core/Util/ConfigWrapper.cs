using System;
using System.ComponentModel;
using System.Linq;
using ExIni;

namespace COM3D2.i18nEx.Core.Util
{
    internal interface IReloadable
    {
        void Reload();
    }

    internal class ConfigWrapper<T> : IReloadable
    {
        private readonly T defaultValue;
        private readonly IniFile file;
        private readonly Func<string, T> fromStringConvert;
        private readonly IniKey iniKey;
        private readonly string savePath;
        private readonly Func<T, string> toStringConvert;
        private T prevValue;
        private string prevValueRaw;

        public ConfigWrapper(IniFile file,
                             string savePath,
                             string section,
                             string key,
                             string description = null,
                             T defaultValue = default,
                             Func<T, string> toStringConvert = null,
                             Func<string, T> fromStringConvert = null)
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

            this.toStringConvert = toStringConvert     ?? (v => cvt.ConvertToInvariantString(v));
            this.fromStringConvert = fromStringConvert ?? (v => (T)cvt.ConvertFromInvariantString(v));

            if (iniKey.Value == null)
            {
                Value = defaultValue;
            }
            else
            {
                prevValueRaw = iniKey.RawValue;
                try
                {
                    prevValue = this.fromStringConvert(iniKey.Value);
                }
                catch (Exception)
                {
                    Value = defaultValue;
                }
            }
        }

        public T Value
        {
            get => prevValue;
            set
            {
                var val = toStringConvert(value);
                if (val == prevValueRaw)
                    return;

                iniKey.Value = val;
                UnloadValue();
                prevValue = value;
                prevValueRaw = iniKey.RawValue;
                Save();

                ValueChanged?.Invoke(value);
            }
        }

        public void Reload()
        {
            try
            {
                if (iniKey.RawValue == prevValueRaw)
                    return;
                UnloadValue();
                prevValue = fromStringConvert(iniKey.Value);
                ValueChanged?.Invoke(prevValue);
            }
            catch (Exception)
            {
                Value = defaultValue;
            }
        }

        public event Action<T> ValueChanged;

        private void UnloadValue()
        {
            if (prevValue != null && prevValue is IDisposable disposable)
                disposable.Dispose();
        }

        private void Save()
        {
            file.Save(savePath);
        }
    }
}
