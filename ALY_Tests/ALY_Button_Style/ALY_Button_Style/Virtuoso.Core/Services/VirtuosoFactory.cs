namespace Virtuoso.Core.Services
{
    public static class VirtuosoFactory
    {
        private static readonly LogManager _logManager;

        static VirtuosoFactory()
        {
            _logManager = new LogManager();
        }

        public static LogManager GetLogger()
        {
            return _logManager;
        }
    }
}