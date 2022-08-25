using System;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.Events;
using System.Diagnostics;
using Virtuoso.Helpers;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Log;
using OpenRiaServices.DomainServices.Client;
using System.ComponentModel.DataAnnotations;

namespace Virtuoso.Core.Controls
{
    public partial class ErrorWindow : ChildWindow
    {
        Exception Exception { get; set; }
        ErrorEventArgs ErrorEventArgs { get; set; }
        MultiErrorEventArgs MultiErrorEventArgs { get; set; }

        public ErrorWindow(string message, Exception errorDetails)
        {
            InitializeComponent();
            Exception = errorDetails;
            ErrorEventArgs = null;
            MultiErrorEventArgs = null;
            ErrorText.Visibility = Visibility.Visible;
            IntroductoryText.Text = message;
            ErrorText.Text = errorDetails.ToString() + GetEntitiesInError(errorDetails);
            this.entityErrorsContainer.Visibility = Visibility.Collapsed;
            this.multiErrorExceptions.Visibility = Visibility.Collapsed;
            this.multiEntityErrors.Visibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(ErrorText.Text) == false)
            {
                ErrorText.Visibility = Visibility.Visible;
            }
            this.Closed += new EventHandler(ErrorWindow_Closed);
        }
        private string GetEntitiesInError(Exception ex)
        {
            SubmitOperationException soe = ex as SubmitOperationException;
            if (soe?.EntitiesInError == null) return null;
            string CR = char.ToString('\r');
            string entityID = null;
            string eieText = null;
            foreach (Entity e in soe.EntitiesInError)
            {
                entityID = e.GetType().Name + ((e.GetIdentity() == null) ? "" : entityID = entityID + " key=" + e.GetIdentity().ToString());
                foreach (ValidationResult vr in e.ValidationErrors)
                {
                    eieText = eieText + "    " +  entityID + " : " + vr.ErrorMessage + CR;
                }
            }
            if (eieText == null) return null;
            return CR + CR + "Entities In Error:" + CR + eieText;
        }


        public ErrorWindow(string message, ErrorEventArgs errorDetails)
        {
            InitializeComponent();
            ErrorEventArgs = errorDetails;
            MultiErrorEventArgs = null;
            ErrorText.Visibility = Visibility.Visible;
            IntroductoryText.Text = message;
            ErrorText.Text = errorDetails.Error.ToString();
            this.multiErrorExceptions.Visibility = Visibility.Collapsed;
            this.multiEntityErrors.Visibility = Visibility.Collapsed;
            if (string.IsNullOrEmpty(ErrorText.Text) == false)
            {
                ErrorText.Visibility = Visibility.Visible;
            }
            if (errorDetails.EntityValidationResults == null || errorDetails.EntityValidationResults.Count <= 0)
            {
                entityErrorsContainer.Visibility = Visibility.Collapsed;
            }
            else
            {
                entityErrorsContainer.Visibility = Visibility.Visible;
                //this.entityErrors.ItemsSource = errorDetails.EntityErrors;
                entityErrors.ItemsSource = errorDetails.EntityValidationResults;
            }
            this.Closed += new EventHandler(ErrorWindow_Closed);
        }

        protected ErrorWindow(string message, MultiErrorEventArgs errorDetails)
        {
            //FYI: MultiErrorEventArgs
            //      List<Exception> _errors = new List<Exception>();
            //      List<string> _entity_errors = new List<string>();
            InitializeComponent();
            ErrorEventArgs = null;
            MultiErrorEventArgs = errorDetails;
            IntroductoryText.Text = message;
            ErrorText.Visibility = Visibility.Collapsed;
            //ErrorText.Text = errorDetails.Error.ToString();

            if (errorDetails.Errors.Count > 0)
            {
                multiErrorExceptions.Visibility = Visibility.Visible;
                multiErrorExceptions.ItemsSource = errorDetails.Errors;
            }
            if (errorDetails.EntityErrors.Count > 0)
            {
                //this.multiErrorExceptions
                multiEntityErrors.Visibility = Visibility.Visible;
                multiEntityErrors.ItemsSource = errorDetails.EntityErrors;
            }
            this.Closed += new EventHandler(ErrorWindow_Closed);
        }

        #region Factory Methods
        public static void CreateNew(string message, ErrorEventArgs errorDetails)
        {
            var window = new ErrorWindow(message, errorDetails);
            window.Show();
        }

        public static void CreateNew(string message, MultiErrorEventArgs errorDetails)
        {
            var window = new ErrorWindow(message, errorDetails);
            window.Show();
        }

        public static void CreateNew(string message, Exception errorDetails)
        {
            var window = new ErrorWindow(message, errorDetails);
            window.Show();
        }
        #endregion


        private void SendDetailButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        void ErrorWindow_Closed(object sender, EventArgs e)
        {
            SendExceptionDetails();
        }


        public void SendExceptionDetails()
        {
            var errorDetailHelper = new ErrorDetailHelper();

            var errorDetaiLog = new ErrorDetailLog();

            if (EntityManager.Current.IsOnline)
            {
                if (ErrorEventArgs != null)
                {
                    var errorDetail = errorDetailHelper.GetErrorDetail(ErrorEventArgs, this.CommentTextBox.Text);
                    errorDetaiLog.Add(errorDetail, true);
                }
                if (MultiErrorEventArgs != null)
                {
                    var errorDetail = errorDetailHelper.GetErrorDetail(MultiErrorEventArgs, this.CommentTextBox.Text);
                    errorDetaiLog.Add(errorDetail, true);
                }
                if (this.Exception != null)
                {
                    var errorDetail = errorDetailHelper.GetErrorDetail(this.Exception, this.CommentTextBox.Text);
                    errorDetaiLog.Add(errorDetail, true);
                }
                
            }
            else
            {
                if (ErrorEventArgs != null)
                {
                    var errorDetail = errorDetailHelper.GetErrorDetail(ErrorEventArgs, this.CommentTextBox.Text);
                    errorDetaiLog.SaveToDisk(errorDetail);
                }
                if (MultiErrorEventArgs != null)
                {
                    var errorDetail = errorDetailHelper.GetErrorDetail(MultiErrorEventArgs, this.CommentTextBox.Text);
                    errorDetaiLog.SaveToDisk(errorDetail);
                }
                if (this.Exception != null)
                {
                    var errorDetail = errorDetailHelper.GetErrorDetail(this.Exception, this.CommentTextBox.Text);
                    errorDetaiLog.SaveToDisk(errorDetail);
                }
            }
        } //public void SendExceptionDetails()
    } //public partial class ErrorWindow : ChildWindow
}