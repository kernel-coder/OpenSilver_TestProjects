using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Virtuoso.Services.Model
{
    /// <summary>
    ///     Log levels for Virtuoso
    /// </summary>
    public enum VirtuosoLogLevel
    {
        //ALL = Int32.MinValue,  //client code doesn't use this value
        ALL = 0,
        TRACE = 10000,
        DEBUG = 20000,
        INFO = 30000,
        WARN = 40000,
        ERROR = 50000,
        FATAL = 60000,
        //OFF = Int32.MaxValue  //client code doesn't use this value
        OFF = 100000

        //DEBUG - The DEBUG Level designates fine-grained informational events that are most useful to debug an application. 
        //INFO – The INFO level designates informational messages that highlight the progress of the application at coarse-grained level. 
        //WARN – The WARN level designates potentially harmful situations. 
        //ERROR – The ERROR level designates error events that might still allow the application to continue running.

        //TRACE - The TRACE Level designates finer-grained informational events than the DEBUG 
        //FATAL – The FATAL level designates very severe error events that will presumably lead the application to abort. 

        //In addition, there are two special levels of logging available: (descriptions borrowed from the log4j API http://jakarta.apache.org/log4j/docs/api/index.html): 
        //ALL -The ALL Level has the lowest possible rank and is intended to turn on all logging. 
        //OFF – The OFF Level has the highest possible rank and is intended to turn off logging.
    }

    public static class LogLevelConverter
    {
        public static VirtuosoLogLevel IntToLogLevel(int intLevel)
        {
            VirtuosoLogLevel ConvertedValue = (VirtuosoLogLevel)Enum.ToObject(typeof(VirtuosoLogLevel), intLevel);
            Boolean blnIsExist = Enum.IsDefined(typeof(VirtuosoLogLevel), ConvertedValue);
            if (blnIsExist)
                return ConvertedValue;
            else
                return VirtuosoLogLevel.INFO;
        }
    }

}
