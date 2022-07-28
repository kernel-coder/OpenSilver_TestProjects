#region Usings

using System;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Browser;
using Virtuoso.Client.Core;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Events;
using Virtuoso.Core.Log;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Helpers
{
    public class ErrorDetailHelper
    {
        private const string ErrorMsg = "Can't Retrieve {0}";
        private readonly Assembly _currentAssm;

        public ErrorDetailHelper()
        {
            _currentAssm = Assembly.GetExecutingAssembly();
        }

        public ErrorDetail GetErrorDetail(Exception exception, string comment)
        {
            try
            {
                var errorDetail = new ErrorDetail();

                errorDetail.Message = GetAllExceptionMessages(exception);
                errorDetail.StackTrace = GetAllExceptionStackTraces(exception);
                errorDetail.Comment = comment;

                errorDetail.Online = GetIsOnline();

                GetSystemDetails(errorDetail);

                return errorDetail;
            }
            catch
            {
                return null;
            }
        }

        public ErrorDetail GetErrorDetail(ErrorEventArgs errorEventArgs, string comment)
        {
            try
            {
                var errorDetail = new ErrorDetail();

                errorDetail.Online = GetIsOnline();

                errorDetail.Message = String.Empty;
                errorDetail.StackTrace = String.Empty;

                errorDetail.Message += GetAllExceptionMessages(errorEventArgs.Error);
                errorDetail.Message += "\r\n\r\n";
                errorDetail.StackTrace += GetAllExceptionStackTraces(errorEventArgs.Error);
                errorDetail.StackTrace += "\r\n\r\n";

                foreach (var entityValidationResult in errorEventArgs.EntityValidationResults)
                {
                    errorDetail.Message += entityValidationResult.ErrorMessage;
                    errorDetail.Message += "\r\n\r\n";
                }

                foreach (var entityError in errorEventArgs.EntityErrors)
                {
                    errorDetail.Message += entityError;
                    errorDetail.Message += "\r\n\r\n";
                }

                errorDetail.Comment = comment;

                GetSystemDetails(errorDetail);

                return errorDetail;
            }
            catch
            {
                return null;
            }
        }

        public ErrorDetail GetErrorDetail(MultiErrorEventArgs multiErrorEventArgs, string comment)
        {
            try
            {
                var errorDetail = new ErrorDetail();

                errorDetail.Online = GetIsOnline();

                errorDetail.Message = String.Empty;
                errorDetail.StackTrace = String.Empty;

                foreach (var error in multiErrorEventArgs.Errors)
                {
                    errorDetail.Message += GetAllExceptionMessages(error);
                    errorDetail.Message += "\r\n\r\n";
                    errorDetail.StackTrace += GetAllExceptionStackTraces(error);
                    errorDetail.StackTrace += "\r\n\r\n";
                }

                foreach (var entityError in multiErrorEventArgs.EntityErrors)
                {
                    errorDetail.Message += entityError;
                    errorDetail.Message += "\r\n\r\n";
                }

                errorDetail.Comment = comment;

                GetSystemDetails(errorDetail);

                return errorDetail;
            }
            catch
            {
                return null;
            }
        }

        public ErrorDetail GetErrorDetailandLocation(Exception exception, string location)
        {
            try
            {
                var errorDetail = new ErrorDetail();
                GetSystemDetails(errorDetail);

                errorDetail.Online = GetIsOnline();

                errorDetail.Message = GetAllExceptionMessages(exception);
                errorDetail.StackTrace = GetAllExceptionStackTraces(exception);
                errorDetail.Comment = "";
                try
                {
                    if (ApplicationStoreInfo.ApplicationNavBookmark != null)
                    {
                        errorDetail.Location = String.Format("({0}->{1})",
                            ApplicationStoreInfo.ApplicationNavBookmark.NavigationLocation, location);
                        errorDetail.ClientDateTime = ApplicationStoreInfo.ApplicationNavBookmark.ClientDateTime;
                    }
                    else
                    {
                        var serviceLocator = VirtuosoContainer.Current;
                        var navSvc = serviceLocator.GetInstance<Core.Navigation.INavigationService>();
                        var currentURI = navSvc.CurrentSource.ToString();

                        errorDetail.Location = "(" + currentURI + ")->" + location;
                        errorDetail.ClientDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    }
                }
                catch (Exception)
                {
                    errorDetail.Location = "n/a";
                    errorDetail.ClientDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }

                return errorDetail;
            }
            catch
            {
                return null;
            }
        }

        public void SendExceptionDetails(Exception exception, string location)
        {
            var errorDetail = GetErrorDetailandLocation(exception, location);

            var errorDetaiLog = new ErrorDetailLog();

            if (EntityManager.Current.IsOnline)
            {
                errorDetaiLog.Add(errorDetail);
            }
            else
            {
                errorDetaiLog.SaveToDisk(errorDetail);
            }
        }

        public void SendExceptionDetails(ErrorEventArgs errorEventArgs, string location, bool useMessageBox = true)
        {
            var errorDetailHelper = new ErrorDetailHelper();
            var errorDetaiLog = new ErrorDetailLog();
            string comment = "Location: " + location;

            if (EntityManager.Current.IsOnline)
            {
                var errorDetail = errorDetailHelper.GetErrorDetail(errorEventArgs, comment);
                errorDetaiLog.Add(errorDetail, false);

                if (useMessageBox)
                {
                    MessageBox.Show(errorDetail.Message);
                }
                else
                {
                    Core.Controls.ErrorWindow.CreateNew(location, errorEventArgs);
                }
            }
            else
            {
                var errorDetail = errorDetailHelper.GetErrorDetail(errorEventArgs, comment);
                errorDetaiLog.SaveToDisk(errorDetail);
                if (useMessageBox)
                {
                    MessageBox.Show(errorDetail.Message);
                }
                else
                {
                    Core.Controls.ErrorWindow.CreateNew(location, errorEventArgs);
                }
            }
        }

        #region PRIVATE_METHODS

        private bool? GetIsOnline()
        {
            try
            {
                bool? online = EntityManager.Current.IsOnline;
                return online;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void GetSystemDetails(ErrorDetail errorDetail)
        {
            var an = new AssemblyName(_currentAssm.FullName);
            errorDetail.AssemblyVersion = an.Version.ToString();
            try
            {
                Analytics _analytics = new Analytics(); //must run on UI thread
                errorDetail.AverageProcessLoad = _analytics.AverageProcessLoad;
                errorDetail.AverageProcessorLoad = _analytics.AverageProcessorLoad;
            }
            catch (Exception)
            {
            }

            try
            {
                errorDetail.Location = ApplicationStoreInfo.ApplicationNavBookmark.NavigationLocation;
                errorDetail.ClientDateTime = ApplicationStoreInfo.ApplicationNavBookmark.ClientDateTime;
            }
            catch (Exception)
            {
                try
                {
                    var serviceLocator = VirtuosoContainer.Current;
                    var navSvc = serviceLocator.GetInstance<Core.Navigation.INavigationService>();
                    var currentURI = navSvc.CurrentSource.ToString();

                    errorDetail.Location = currentURI;
                    errorDetail.ClientDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }
                catch (Exception)
                {
                    errorDetail.Location = "n/a";
                    errorDetail.ClientDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                }
            }

            try
            {
                errorDetail.ClrVersion = Environment.Version.ToString();
            }
            catch
            {
                errorDetail.ClrVersion = string.Format(ErrorMsg, "CLR Version");
            }

            errorDetail.CurrentUri = Application.Current.IsRunningOutOfBrowser
                ? Application.Current.Host.Source.Host
                : HtmlPage.Document.DocumentUri.Host;

            errorDetail.OSName = GetOsName();
            try
            {
                errorDetail.OSVersion = Environment.OSVersion.ToString();
            }
            catch
            {
                errorDetail.OSVersion = string.Format(ErrorMsg, "Operating System Version");
            }

            try
            {
                errorDetail.ProcessorCount = Environment.ProcessorCount;
            }
            catch
            {
                errorDetail.ProcessorCount = 0;
            }

            try
            {
                errorDetail.UserLanguage = CultureInfo.CurrentCulture.Name;
            }
            catch
            {
                errorDetail.UserLanguage = string.Format(ErrorMsg, "Current Culture");
            }

            try
            {
                errorDetail.UserName = UserCache.Current.GetCurrentUserProfile().UserName;
            }
            catch
            {
                errorDetail.UserName = string.Format(ErrorMsg, "UserName");
            }
        }

        private string GetOsName()
        {
            try
            {
                string ret = string.Empty;

                switch (Environment.OSVersion.Version.Major)
                {
                    case 7:
                        ret = "Windows 8";
                        break;
                    case 6:
                        if (Environment.OSVersion.Version.Minor == 0)
                        {
                            ret = "Windows Vista";
                        }
                        else if (Environment.OSVersion.Version.Minor == 1)
                        {
                            ret = "Windows 7";
                        }

                        break;
                    case 5:
                        if (Environment.OSVersion.Version.Minor == 0)
                        {
                            ret = "Windows 2000";
                        }
                        else if (Environment.OSVersion.Version.Minor == 1)
                        {
                            ret = "Windows XP";
                        }

                        break;
                    case 4:
                        ret = "Windows NT";
                        break;
                    default:
                        ret = "Windows 98";
                        break;
                }

                return ret;
            }
            catch
            {
                return string.Format(ErrorMsg, "Operating System Name");
            }
        }

        private static string GetAllExceptionMessages(Exception ex)
        {
            string message = string.Empty;
            Exception innerException = ex;

            do
            {
                if (message != string.Empty)
                {
                    message = message + "\r\n\r\n" + (string.IsNullOrEmpty(innerException.Message)
                        ? string.Empty
                        : innerException.Message);
                }
                else
                {
                    message = message + (string.IsNullOrEmpty(innerException.Message)
                        ? string.Empty
                        : innerException.Message);
                }

                innerException = innerException.InnerException;
            } while (innerException != null);

            return message;
        }

        private static string GetAllExceptionStackTraces(Exception ex)
        {
            string stackTrace = string.Empty;
            Exception innerException = ex;

            do
            {
                if (stackTrace != string.Empty)
                {
                    stackTrace = stackTrace + "\r\n\r\n" + (string.IsNullOrEmpty(innerException.StackTrace)
                        ? string.Empty
                        : innerException.StackTrace);
                }
                else
                {
                    stackTrace = stackTrace + (string.IsNullOrEmpty(innerException.StackTrace)
                        ? string.Empty
                        : innerException.StackTrace);
                }

                innerException = innerException.InnerException;
            } while (innerException != null);

            return stackTrace;
        }

        #endregion PRIVATE_METHODS
    }

    public static class ErrorDetailLogger
    {
        private static ErrorDetailHelper _ErrorDetailHelper = new ErrorDetailHelper();

        public static void LogDetails(Exception exceptionDetails, string location)
        {
            var errorDetailHelper = new ErrorDetailHelper();
            errorDetailHelper.SendExceptionDetails(exceptionDetails, location);
        }

        public static void LogDetails(Exception exceptionDetails, bool displayMessage, string location,
            bool useMessageBox = true)
        {
            var errorDetailHelper = new ErrorDetailHelper();
            errorDetailHelper.SendExceptionDetails(exceptionDetails, location);

            if (displayMessage)
            {
                var error_detail = errorDetailHelper.GetErrorDetailandLocation(exceptionDetails, location);
                if (useMessageBox)
                {
                    MessageBox.Show(error_detail.Message);
                }
                else
                {
                    Core.Controls.ErrorWindow.CreateNew(location, exceptionDetails);
                }
            }
        }

        public static void LogDetails<T>(EntityEventArgs<T> e, bool displayMessage, string location,
            bool useMessageBox = true)
        {
            LogDetailsInternal(e, displayMessage, location, useMessageBox);
        }

        public static void LogDetails<T>(MultiErrorEventArgs e, bool displayMessage, string location)
        {
            foreach (var item in e.Errors)
            {
                var error = new EntityEventArgs<T>(item);
                LogDetailsInternal(error, displayMessage, location);
            }

            foreach (var item in e.EntityErrors)
            {
                var errors = new EntityEventArgs<T>(new Exception(item));
                LogDetailsInternal(errors, displayMessage, location);
            }
        }

        private static void LogDetailsInternal<T>(EntityEventArgs<T> e, bool displayMessage, string location,
            bool useMessageBox = true)
        {
            if (e.EntityErrors.Count == 0)
            {
                LogDetails(e.Error, location);
                if (displayMessage)
                {
                    if (useMessageBox)
                    {
                        MessageBox.Show(e.Error.Message);
                    }
                    else
                    {
                        Core.Controls.ErrorWindow.CreateNew(location, e);
                    }
                }
            }
            else
            {
                var errorDetailHelper = new ErrorDetailHelper();
                errorDetailHelper.SendExceptionDetails(e, location);
            }
        }
    }
}