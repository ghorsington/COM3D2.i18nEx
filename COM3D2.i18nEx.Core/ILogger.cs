namespace COM3D2.i18nEx.Core
{
    public interface ILogger
    {
        void LogInfo(object data);

        void LogWarning(object data);

        void LogError(object data);
    }
}
