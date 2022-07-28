#region Usings

using System;
using System.Collections.Generic;
using System.Windows;
using Virtuoso.Services.Model;

#endregion

namespace Virtuoso.Core.Services
{
    public class VirtuosoLoggerNotFoundException : Exception
    {
        public VirtuosoLoggerNotFoundException(Guid guid)
            : base(string.Format("An attempt to unregister the logger with id {0} failed. The logger was not found.",
                guid))
        {
        }
    }

    public interface IVirtuosoLock
    {
        object Lock { get; }
    }

    public class LogManager : IVirtuosoLock
    {

        private readonly Dictionary<Guid, Action<VirtuosoLogLevel, DateTime, string, string, Exception>> _loggers
            = new Dictionary<Guid, Action<VirtuosoLogLevel, DateTime, string, string, Exception>>();

        public Guid RegisterLogger(Action<VirtuosoLogLevel, DateTime, string, string, Exception> logger)
        {
            var identifier = Guid.NewGuid();

            lock (Lock)
            {
                _loggers.Add(identifier, logger);
            }

            return identifier;
        }

        public void UnhookLogger(Guid guid)
        {
            if (!_loggers.ContainsKey(guid))
            {
                throw new VirtuosoLoggerNotFoundException(guid);
            }

            lock (Lock)
            {
                _loggers.Remove(guid);
            }
        }

        public void Log(VirtuosoLogLevel level, DateTime sent, string message, string tag, Exception exception)
        {
            if (!Deployment.Current.CheckAccess())
            {
                return;
            }

            var dispatcher = Deployment.Current.Dispatcher;

            if (!dispatcher.CheckAccess())
            {
                return;
            }

            lock (Lock)
            {
                foreach (var key in _loggers.Keys)
                {
                    var key1 = key;
                    dispatcher.BeginInvoke(() => _loggers[key1](level, sent, message, tag, exception));
                }
            }
        }

        private static readonly object _lock = new object();

        public object Lock => _lock;
    }
}