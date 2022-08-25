#region Usings

using System;
using System.Diagnostics;
using System.Text;

#endregion

namespace Virtuoso.Core.Utility
{
    /// <summary>
    ///     Assist unwinding exceptions
    /// </summary>
    public static class ExceptionHelper
    {
        /// <summary>
        ///     Dumps an Exception and iterates inner exceptions
        /// </summary>
        /// <param name="ex">The exception to dump</param>
        public static string DumpEx(Exception ex)
        {
            if (!Debugger.IsAttached)
            {
                return string.Empty;
            }

            var curEx = ex;
            var sb = new StringBuilder();

            while (curEx != null)
            {
                var text =
                    string.Format(
                        "An error was encountered. Dumping message and stack trace with inner exceptions.{0}Error: {1}{0}Stack trace: {2}{0}",
                        Environment.NewLine,
                        ex.Message,
                        ex.StackTrace);

                Debug.WriteLine(text);
                sb.Append(text);
                curEx = ex.InnerException;
            }

            return sb.ToString();
        }
    }
}