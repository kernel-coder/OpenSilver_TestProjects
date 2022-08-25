#region Usings

using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Client.Core;

#endregion

namespace Virtuoso.Core.Services
{
    //This adapter is used by ViewModels - so that we inject an interface into them, this way they do not have to use
    //the static Logger.Current - keeps ViewModels testable.
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export(typeof(ILogger))]
    public class ApplicationLogWrapper : ILogger
    {
        LogWriter _logger { get; set; }

        public ApplicationLogWrapper()
        {
            _logger = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();

            if (System.Windows.Application.Current.IsRunningOutOfBrowserOrOpenSilver())
            {
#if OPENSILVER
                // Can set in client with virtuoso.setTraceFlag(true)
                IsTraceOn = Client.Infrastructure.Storage.VirtuosoStorageContext.LocalSettings.Get<bool>(Constants.Logging.TRACE_ON_BASE_FILE_NAME);
#else
                var have_trace_on_file = Directory.EnumerateFiles(ApplicationStoreInfo.GetUserStoreForApplication(),
                    String.Format("{0}*", Constants.Logging.TRACE_ON_BASE_FILE_NAME)).Count();
                if (have_trace_on_file > 0)
                {
                    IsTraceOn = true;
                }
                else
                {
                    IsTraceOn = false;
                }
#endif
            }
        }

        private bool IsTraceOn { get; }

        #region ILogger Members

        public void Fatal(string category, string message)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                message,
                new[] { category },
                0,
                0,
                TraceEventType.Critical);
        }

        public void Error(string category, string message)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                message,
                new[] { category },
                0,
                0,
                TraceEventType.Error);
        }

        public void Error(string category, Exception e)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                e.StackTrace,
                new[] { category },
                0,
                0,
                TraceEventType.Critical);

            _logger.Write(
                e.Message,
                new[] { category },
                0,
                0,
                TraceEventType.Critical);

            if (e.InnerException != null)
            {
                _logger.Write(
                    e.InnerException.Message,
                    new[] { category },
                    0,
                    0,
                    TraceEventType.Critical);
            }
        }

        public void Warning(string category, string message)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                message,
                new[] { category },
                0,
                0,
                TraceEventType.Warning);
        }

        public void Info(string category, string message)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                message,
                new[] { category },
                0,
                0,
                TraceEventType.Information);
        }

        public void Debug(string category, string message)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                message,
                new[] { category },
                0,
                0,
                TraceEventType.Verbose);
        }

        void ILogger.Log(TraceEventType level, string category, string message)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                message,
                new[] { category },
                0,
                0,
                level);
        }

        void ILogger.Log(TraceEventType level, string category, Exception exception)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                exception.StackTrace,
                new[] { category },
                0,
                0,
                level);

            _logger.Write(
                exception.Message,
                new[] { category },
                0,
                0,
                level);

            if (exception.InnerException != null)
            {
                _logger.Write(
                    exception.InnerException.Message,
                    new[] { category },
                    0,
                    0,
                    level);
            }
        }

        void ILogger.Log(TraceEventType level, string category, string message, Exception exception)
        {
            if (!IsTraceOn)
            {
                return;
            }

            _logger.Write(
                String.Format("{0}\n\tException: {1}", message, exception.ToString()),
                new[] { category },
                0,
                0,
                level);

            _logger.Write(
                String.Format("{0}\n\tException: {1}", message, exception.Message),
                new[] { category },
                0,
                0,
                level);

            if (exception.InnerException != null)
            {
                _logger.Write(
                    String.Format("{0}\n\tInner Exception{1}", message, exception.InnerException.Message),
                    new[] { category },
                    0,
                    0,
                    level);
            }
        }

        #endregion
    }
}