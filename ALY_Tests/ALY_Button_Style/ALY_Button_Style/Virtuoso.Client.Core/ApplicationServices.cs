#region Usings

using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Client.Infrastructure.Storage;

#endregion

namespace Virtuoso.Services
{
    public class ApplicationServices : IDisposable
    {
        LocalMessageReceiver incomingMessage;
        readonly bool IsRunningOutOfBrowser;

        public ApplicationServices(bool isRunningOutOfBrowser = true)
        {
            IsRunningOutOfBrowser = isRunningOutOfBrowser;

            if (DesignerProperties.IsInDesignTool)
            {
            }
        }

        public bool LockApplicationInstance(LogWriter logWriter)
        {
            bool _success = false;
            if (IsRunningOutOfBrowser)
            {
                var uniqueApplicationKey = Application.Current.Host.Source.ToString();

                try
                {
                    //Only allow single instance of application to run at a time

                    //Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + app name or tenant id
                    //really just app name or tenant id ought to be enough - though in development our local install will
                    //likely have tenant ids used in production.

                    //would probably still be best and simplest if tenant had a GUID....then could use that, would differ from
                    //production and development also, even if tenant ids where the same...GUIDs would never be the same, at least
                    //they'd be easier to change if development copied a production database - change the GUID on tenant table - 
                    //tenant IDs stay the same throughout the database.

                    //By using Host.Source - do not need tenant configuration - and this value is still unique to each
                    //installed application instance.

                    logWriter.Write(
                        string.Format("Local Message Key (application lock id): {0}", uniqueApplicationKey),
                        new[] { "ApplicationServices" }, //category
                        0, //priority
                        0, //eventid
                        TraceEventType.Information);

                    incomingMessage = new LocalMessageReceiver(uniqueApplicationKey);

                    incomingMessage.Listen(); //will throw exception if another instance is running
                    _success = true;
                }
                catch (ListenFailedException e)
                {
                    logWriter.Write(
                        string.Format("Local Message Key (application lock id): {0}", uniqueApplicationKey),
                        new[] { "ApplicationServices" }, //category
                        0, //priority
                        0, //eventid
                        TraceEventType.Information);

                    logWriter.Write(
                        string.Format("Could not lock application instance.  Error: {0}", e.ToString()),
                        new[] { "ApplicationServices" }, //category
                        0, //priority
                        0, //eventid
                        TraceEventType.Error);

                    _success = false;
                }
            }

            return _success;
        }

        public static void RestoreMainWindow()
        {
#if !OPENSILVER
            try
            {
                var window_state = VirtuosoStorageContext.LocalSettings.Get<UInt32?>("WindowState");
                if (window_state != null)
                {
                    switch (window_state)
                    {
                        case (UInt32)WindowState.Normal:
                            Application.Current.MainWindow.WindowState = WindowState.Normal;

                            var windowWidth = VirtuosoStorageContext.LocalSettings.Get<double?>("WindowWidth");
                            if (windowWidth != null) //set width/height before top/left
                            {
                                Application.Current.MainWindow.Width = windowWidth.Value;
                            }

                            var windowHeight = VirtuosoStorageContext.LocalSettings.Get<double?>("WindowHeight");
                            if (windowHeight != null) //set width/height before top/left
                            {
                                Application.Current.MainWindow.Height = windowHeight.Value;
                            }

                            var windowTop = VirtuosoStorageContext.LocalSettings.Get<double?>("WindowTop");
                            if (windowTop != null)
                            {
                                Application.Current.MainWindow.Top = windowTop.Value;
                            }

                            var windowLeft = VirtuosoStorageContext.LocalSettings.Get<double?>("WindowLeft");
                            if (windowLeft != null)
                            {
                                Application.Current.MainWindow.Left = windowLeft.Value;
                            }

                            break;
                        case (UInt32)WindowState.Maximized:
                            Application.Current.MainWindow.WindowState = WindowState.Maximized;
                            break;
                        case (UInt32)WindowState.Minimized:
                            Application.Current.MainWindow.WindowState = WindowState.Minimized;
                            break;
                    }
                }
            }
            catch (Exception)
            {
            }
#endif
        }

        public static void SaveMainWindow()
        {
#if !OPENSILVER
            try
            {
                VirtuosoStorageContext.LocalSettings.Put("WindowTop", Application.Current.MainWindow.Top);
                VirtuosoStorageContext.LocalSettings.Put("WindowLeft", Application.Current.MainWindow.Left);
                VirtuosoStorageContext.LocalSettings.Put("WindowWidth", Application.Current.MainWindow.Width);
                VirtuosoStorageContext.LocalSettings.Put("WindowHeight", Application.Current.MainWindow.Height);
                VirtuosoStorageContext.LocalSettings.Put("WindowState", Application.Current.MainWindow.WindowState);
            }
            catch (Exception)
            {
            }
#endif
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (incomingMessage != null)
            {
                incomingMessage.Dispose();
            }

            GC.SuppressFinalize(this);
        }
    }
}