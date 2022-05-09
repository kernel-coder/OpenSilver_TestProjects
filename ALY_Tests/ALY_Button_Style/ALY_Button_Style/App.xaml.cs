using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace ALY_Button_Style
{
    public sealed partial class App : Application
    {
        public App()
        {
#if OPENSILVER
            App.Current.Host.Settings.EnableOptimizationWhereCollapsedControlsAreNotLoaded = true;
            App.Current.Host.Settings.EnableOptimizationWhereCollapsedControlsAreNotRendered = true;
#endif
            this.InitializeComponent();


            //File.ReadAllBytes("test.txt");
            //Directory.Delete("filename.txt");

#if OPENSILVER
            UpdateResDic(App.Current.Resources);
            var mainPage = new DataGridTest();
            Window.Current.Content = mainPage;
#else
            this.Startup += this.Application_Startup;
            this.UnhandledException += this.Application_UnhandledException;
#endif
        }

#if OPENSILVER
        private void UpdateResDic(ResourceDictionary dic)
        {
            if (dic.ContainsKey("HomeDataGridMarging"))
            {
                dic.Remove("HomeDataGridMarging");
                dic.Add("HomeDataGridMarging", new Thickness(0));
            }

            if (dic.ContainsKey("HomeDataGridColumnHeaderStyle"))
            {
                var headerStyle = dic["HomeDataGridColumnHeaderStyle"] as Style;
                foreach (Setter item in headerStyle.Setters)
                {
                    if (item.Property == DataGridColumnHeader.MarginProperty)
                    {
                        headerStyle.Setters.Remove(item);
                        Setter setter = new Setter(DataGridColumnHeader.MarginProperty, new Thickness(0));
                        headerStyle.Setters.Add(setter);
                        break;
                    }
                }
            }

            foreach (var mrd in dic.MergedDictionaries)
            {
                UpdateResDic(mrd);
            }
        }
#endif

        private void Application_Startup(object sender, StartupEventArgs e)
        {
#if !OPENSILVER
            this.RootVisual = new MainPage();
#endif
        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;
                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }

        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
            }
            catch (Exception)
            {
            }
        }
    }
}
