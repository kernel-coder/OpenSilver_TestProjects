using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using OpenRiaServices.DomainServices.Client.ApplicationServices;
using System.Windows.Controls;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Utility;
using Virtuoso.Services.Authentication;
using Virtuoso.Client.Utils;

namespace Virtuoso.Core.Controls
{
    public partial class AuthenticatePopup : ChildWindow, INotifyPropertyChanged, INotifyDataErrorInfo
    {
        public AuthenticatePopup()
        {
            InitializeComponent();

            this._currentErrors = new Dictionary<string, List<string>>();

            Messenger.Default.Send<bool>(false, Constants.Application.Authenticated);

            DataContext = this;

            AsyncUtility.Run(async () =>
            {
                var infoMessage = await ApplicationMessaging.GetApplicationInfoMessage();
                if (string.IsNullOrEmpty(infoMessage) == false)
                {
                    this.txtDisplayArea.Visibility = System.Windows.Visibility.Visible;
                    this.txtDisplayArea.DisplayText = infoMessage;
                }
            });
        }

        #region Commands

        private int attemptedRetries = 0;
        private RelayCommand _ReAuthenticateCommand;

        public RelayCommand ReAuthenticateCommand
        {
            get
            {
                if (_ReAuthenticateCommand == null)
                {
                    _ReAuthenticateCommand = new RelayCommand(() =>
                        {
                            if (WebContext.Current.Authentication.IsLoggingIn) return;

                            BusyMessage = "Authenticating.  Please wait...";
                            IsReAuthenticating = true;
                            WebContext.Current.Authentication.Login(
                                new LoginParameters(WebContext.Current.User.MemberID.ToString(), ReAuthenticatePassword,
                                    false, null),
                                ReAuthenticate_Complete,
                                null);
                        },
                        () => { return (string.IsNullOrEmpty(ReAuthenticatePassword) == false); });
                }

                return _ReAuthenticateCommand;
            }
        }

        private void ReAuthenticate_Complete(LoginOperation loginOperation)
        {
            IsReAuthenticating = false;
            if (loginOperation.HasError)
            {
                loginOperation.MarkErrorAsHandled();
                if (this.attemptedRetries < 3)
                {
                    this.attemptedRetries++;
                    System.Threading.Thread.Sleep(1000);
                    _ReAuthenticateCommand.Execute(0);
                }
                else
                {
                    NavigateCloseDialog d = new NavigateCloseDialog();
                    if (d == null) return;
                    d.Width = double.NaN;
                    d.Height = double.NaN;
                    d.NoVisible = false;
                    d.ButtonYes.Content = "OK";
                    d.Title = "Error: Unable to contact server.";
                    d.ErrorMessage = "Unable to contact remote server. Please try again in a moment.";
                    d.HasCloseButton = false;
                    d.Show();

                    this.attemptedRetries = 0;
                }
            }
            else if (loginOperation.LoginSuccess)
            {
                //var _isOnline = EntityManager.Current.IsOnline;
                //if (_isOnline)
                //{
                //    //persist authentication cookie(s) to disk
                //    CookieSerializer.Save(ClientHttpAuthenticationUtility.GetCookies(System.Windows.Application.Current.Host.Source));
                //}

                Messenger.Default.Send<bool>(true, Constants.Application.Authenticated);

                var _authContext = WebContext.Current.Authentication as OfflineAuthentication;
                _authContext.StartInActivityTimer();

                this.DialogResult = true;
            }
            else
            {
                //InvalidLogin = true;
                AddErrorForProperty("ReAuthenticatePassword", "Invalid login");
            }
        }

        #endregion Commands

        #region Properties

        private string _BusyMessage;

        public string BusyMessage
        {
            get { return _BusyMessage; }
            set
            {
                _BusyMessage = value;
                RaisePropertyChanged("BusyMessage");
            }
        }

        private bool _IsReAuthenticating;

        public bool IsReAuthenticating
        {
            get { return _IsReAuthenticating; }
            set
            {
                _IsReAuthenticating = value;
                RaisePropertyChanged("IsReAuthenticating");
            }
        }

        string _ReAuthenticatePassword = String.Empty;

        public string ReAuthenticatePassword
        {
            get { return _ReAuthenticatePassword; }
            private set
            {
                if (_ReAuthenticatePassword != value)
                {
                    ClearErrorFromProperty("ReAuthenticatePassword");
                    _ReAuthenticatePassword = value;
                    ReAuthenticateCommand.RaiseCanExecuteChanged();
                    RaisePropertyChanged("ReAuthenticatePassword");
                }
            }
        }

        #endregion Properties

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any() == true);
                return (ret.Any() == true) ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any() == true)
                return _currentErrors[propertyName];
            else
                return null;
        }

        public bool HasErrors
        {
            get { return (_currentErrors.Where(c => c.Value.Any() == true).Any() == true); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }

        #endregion INotifyDataErrorInfo
    }
}