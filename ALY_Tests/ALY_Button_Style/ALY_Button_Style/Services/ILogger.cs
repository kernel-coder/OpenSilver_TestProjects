#region Usings

using System;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;

#endregion

namespace Virtuoso.Core.Services
{
    public interface ILogger
    {
        void Fatal(string category, string message);
        void Error(string category, string message);
        void Error(string category, Exception e);
        void Warning(string category, string message);
        void Info(string category, string message);
        void Debug(string category, string message);

        void Log(TraceEventType level, string category, string message);
        void Log(TraceEventType level, string category, Exception exception);
        void Log(TraceEventType level, string category, string message, Exception exception);
    }
}