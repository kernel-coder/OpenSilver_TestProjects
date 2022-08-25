#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media.Animation;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Client.Utils;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Navigation;
using Virtuoso.Core.Utility;
using Virtuoso.Metrics;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public interface IScreen
    {
        bool CanExit();
    }

    public interface INavigateClose
    {
        void NavigateClose();
    }

    public enum AnimationControlType
    {
        StartAnimation = 0,
        StopAnimation
    }

    public enum ViewModelMode
    {
        ADD = 0,
        EDIT
    }

    public class CodeDesc
    {
        private string code;
        private string desc;

        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        public string Description
        {
            get { return desc; }
            set { desc = value; }
        }

        public static string GetDescription(List<CodeDesc> errCodeDescList, string Code)
        {
            string result = null;

            var x = errCodeDescList.Where(Desc => Desc.Code == Code).Select(s => s.Description).ToArray();
            if (x.Count() == 1)
            {
                result = x[0];
            }

            return result;
        }
    }

    public interface IAnimation
    {
        event EventHandler StartAnimation;
        event EventHandler StopAnimation;
        RelayCommand CloseAnimatedViewCommand { get; set; }
        RelayCommand AnimationCompletedCommand { get; set; }
    }

    public abstract class ViewModelBase : GenericBase, IScreen, IAnimation
    {
        protected List<QueuedLogData> MetricQueue { get; set; }
        protected MetricsTimerHelper ScreenBusyEvent { get; set; }
        public CorrelationIDHelper CorrelationIDHelper { get; set; }

        #region IAnimation

        public event EventHandler StartAnimation;
        public event EventHandler StopAnimation;
        public event EventHandler ShowTimedMessageClosed;

        void OnStartAnimation()
        {
            if (StartAnimation != null)
            {
                StartAnimation(this, EventArgs.Empty);
            }
        }

        void OnStopAnimation()
        {
            if (StopAnimation != null)
            {
                StopAnimation(this, EventArgs.Empty);
            }
        }

        void RaiseAnimationEvent(AnimationControlType AnimationControlType = AnimationControlType.StartAnimation)
        {
            if (AnimationControlType == AnimationControlType.StartAnimation)
            {
                OnStartAnimation();
            }
            else if (AnimationControlType == AnimationControlType.StopAnimation)
            {
                OnStopAnimation();
            }
        }

        public RelayCommand CloseAnimatedViewCommand { get; set; }
        public RelayCommand AnimationCompletedCommand { get; set; }
        public string SavedDataType { get; set; }

        private KeyTime _messageDuration;

        public KeyTime MessageDuration
        {
            get { return _messageDuration; }
            set
            {
                _messageDuration = value;
                this.RaisePropertyChangedLambda(p => p.MessageDuration);
            }
        }

        private string _timedMessage = "Default message";

        public string TimedMessage
        {
            get { return _timedMessage; }
            set
            {
                _timedMessage = value;
                this.RaisePropertyChangedLambda(p => p.TimedMessage);
            }
        }

        protected virtual void DataSaved()
        {
            RaiseAnimationEvent();
        }

        protected virtual void ShowTimedMessage(string message, TimeSpan messageDuration)
        {
            MessageDuration = messageDuration;
            TimedMessage = message;
            RaiseAnimationEvent();
        }

        #endregion

        private ITabUIManager _tabUIManager;
        public ITabUIManager TabUIManager => _tabUIManager;
        protected ViewModelMode ViewModelMode { get; set; }
        protected bool IfLastIndexSelectedGoToFirstTab = false;
        protected bool IsOnline { get; set; }

        protected ViewModelBase(ITabUIManager manager)
        {
            MetricQueue = new List<QueuedLogData>();
            CorrelationIDHelper = new CorrelationIDHelper();
            _tabUIManager = manager;
            _Init();
        }

        protected ViewModelBase()
        {
            MetricQueue = new List<QueuedLogData>();
            CorrelationIDHelper = new CorrelationIDHelper();
            _tabUIManager = new TabUIManager();
            _Init();
        }

        public override void Cleanup()
        {
            DynamicFormViewModel dfvm = this as DynamicFormViewModel;
            if (Deployment.Current.Dispatcher.CheckAccess()) //If on main thread, send message to patient context menu
            {
                //DynamicFormViewModel already did its own special menu cleanup
                if (dfvm == null)
                {
                    Messenger.Default.Send(new ContextSensitiveArgs { ViewModel = this },
                        "RemoveFromContextSensitiveMenu");
                }
            }

            // This is not the optimal place for this being a base class, but....
            // DynamicFormViewModel.HideFromNavigation 'mode' calls dynamic form in a popup from within the AdmissionMaintenance page (MSP and CMS forms) - not as its own DynamicForm page
            // For this case - Do not tell the AdmissionMaintenance page controls to cleanup (ie, those controls base on DetailUserControlBase and ChildControlBase)
            if ((dfvm == null) || ((dfvm != null) && (dfvm.HideFromNavigation == false)))
            {
                Messenger.Default.Send(this, "ViewModelClosing");
            }

            VirtuosoObjectCleanupHelper.CleanupAll(this);
            base.Cleanup();
            CommandManager?.CleanUp();
            CommandManager = null;
            TabUIManager?.Cleanup();
            _tabUIManager = null;
        }

        private void _Init()
        {
            IsOnline = EntityManager.IsOnlineCached;
            CommandManager = new CommandManager(this);
            ViewModelMode = ViewModelMode.EDIT;
            AnimationCompletedCommand = new RelayCommand(() =>
            {
                RaiseAnimationEvent(AnimationControlType.StopAnimation);
                if (ShowTimedMessageClosed != null)
                {
                    ShowTimedMessageClosed(this, EventArgs.Empty);
                }
                else
                {
                    ActivateNextTabInternal();
                }
            });
            SavedDataType = "";
        }

        private void ActivateNextTabInternal()
        {
            if (TabUIManager == null)
            {
                return;
            }

            if (ViewModelMode == ViewModelMode.ADD)
            {
                if (Deployment.Current.CheckAccess())
                {
                    var dispatcher = Deployment.Current.Dispatcher;
                    if (dispatcher.CheckAccess())
                    {
                        dispatcher.BeginInvoke(() =>
                        {
                            TabUIManager.ActivateNextTab(
                                IfLastIndexSelectedGoToFirstTab); //NOTE: when animation prematurely stopped, it's completed event is not raised
                        });
                    }
                }
                else
                {
                    TabUIManager.ActivateNextTab(
                        IfLastIndexSelectedGoToFirstTab); //NOTE: when animation prematurely stopped, it's completed event is not raised
                }
            }
        }

        protected CommandManager CommandManager { get; set; }

        public RelayCommand AddCommand { get; set; }

        public RelayCommand OKCommand { get; set; }

        public RelayCommand CancelCommand { get; set; }

        #region Methods

        private bool _CanEdit = true;

        public bool CanEdit
        {
            get { return _CanEdit; }
            set
            {
                _CanEdit = value;
                this.RaisePropertyChangedLambda(p => p.CanEdit);
            }
        }

        public virtual bool CanExit()
        {
            return true;
        }

        bool _PendingSubmit;

        public bool PendingSubmit
        {
            get { return _PendingSubmit; }
            set
            {
                if (_PendingSubmit != value)
                {
                    _PendingSubmit = value;
                    RaisePropertyChanged("PendingSubmit");
                }
            }
        }

        string _PendingMessage = "Unable to save at this time.  Additional edit(s) in process.";

        public string PendingMessage
        {
            get { return _PendingMessage; }
            set
            {
                if (_PendingMessage != value)
                {
                    _PendingMessage = value;
                    RaisePropertyChanged("PendingMessage");
                }
            }
        }

        public void OnSaveCompleted(object sender, ErrorEventArgs args)
        {
            if (args.Error != null)
            {
                ErrorWindow.CreateNew(args.Message, args);

                //INFO: got a server error, entities can be in invalid state - e.g. in error, but already committed locally.
                //      Only thing you can do is to display the errors and give the end user the chance to reject all changes.
                //MessageBox.Show("Unrecoverable errors - cancel and re-open form to resume editting", "Submit Failed", MessageBoxButton.OK);
                //MessageBox.Show(args.Message, "Submit Failed", MessageBoxButton.OK);
                //  1.) Hide Edit button (optionally add/show a Reject button, can just let end user 'x' out of the form though)
                CanEdit = false; //set property to hide EDIT button stack...
            }
        }

        protected String MakeLabelWithCountForApplicationView(Type view, String labelText)
        {
            Int32 count = 0;
            var nk = NavigateKey;
            if (nk != null)
            {
                count = nk.ActivePages.GetActiveCountForView(view);
            }

            if (count == 0)
            {
                return labelText;
            }

            return String.Format("{0}-({1})", labelText, count);
        }

        public virtual System.Threading.Tasks.Task OnNavigatedTo(Object param)
        {
            return AsyncUtility.TaskFromResult();
        }

        public virtual void OnNavigatingFrom(ref Boolean cancel)
        {
        }

        public bool MorphingNavigatingFromIntoNavigateBack =>
            ((NavigatingBack == false) && MorphNavigatingFromIntoNavigateBack());

        public virtual bool MorphNavigatingFromIntoNavigateBack()
        {
            return false;
        }

        protected List<NavigateKey> GetActiveDataItems(Type view)
        {
            var list = new List<NavigateKey>();
            if (NavigateKey != null)
            {
                foreach (var item in NavigateKey.ActivePages.Pages.Values)
                {
                    var nk = item.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                    if (nk != null && nk.ViewType == view)
                    {
                        list.Add(nk);
                    }
                }
            }

            return list;
        }

        protected List<NavigateKey> GetActiveDataItems(string application_suite)
        {
            var list = new List<NavigateKey>();
            if (NavigateKey != null)
            {
                foreach (var item in NavigateKey.ActivePages.Pages.Values)
                {
                    var nk = item.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                    if (nk != null && nk.ApplicationSuite == application_suite)
                    {
                        list.Add(nk);
                    }
                }
            }

            return list;
        }

        protected List<NavigateKey> GetActiveApplicationSuiteDataItems(bool HaveApplicationSuite = true)
        {
            var list = new List<NavigateKey>();
            if (NavigateKey != null)
            {
                foreach (var item in NavigateKey.ActivePages.Pages.Values)
                {
                    var nk = item.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                    if (nk != null && String.IsNullOrEmpty(nk.ApplicationSuite) == !HaveApplicationSuite)
                    {
                        list.Add(nk);
                    }
                }
            }

            return list;
        }

        public void RemoveFromCache()
        {
            try
            {
                var __uri = NavigateKey.UriString;

                //NOTE: page may not actually be removed from the cache.  Page will process Cleanup(), only if CanExit() returns TRUE
                NavigateKey.ActivePages.RemovePage(__uri);

                var LogWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
                var msg = string.Format("Removing from ActivePages: base.NavigateKey.UriString: {0}", __uri);
                LogWriter.Write(
                    msg,
                    new[] { GetType().ToString() }, //category
                    0, //priority
                    0, //eventid
                    TraceEventType.Information);
#if DEBUG
                System.Diagnostics.Debug.WriteLine(
                    "----------------------------------------------------------------------------------------------------------------------------");
                System.Diagnostics.Debug.WriteLine(msg);
                System.Diagnostics.Debug.WriteLine(
                    "----------------------------------------------------------------------------------------------------------------------------");
#endif
            }
            catch (KeyNotFoundException) //ignore these errors
            {
            }
        }

        private bool NavigatingBack;

        public void NavigateBack(bool sendNavRequest = true)
        {
            NavigatingBack = true;

            RemoveFromCache();

            String parentCurrentSource = NavigateKey.ActivePages.GetCurrentSource(NavigateKey.ParentUriOriginalString);
            if (String.IsNullOrWhiteSpace(parentCurrentSource))
            {
                parentCurrentSource = Constants.HOME_URI_STRING;
            }

            if (sendNavRequest)
            {
                Messenger.Default.Send(new Uri(parentCurrentSource, UriKind.Relative), "NavigationRequest");
            }
        }

        #endregion Methods
    }
}