using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Virtuoso.Core.Navigation;
using Virtuoso.Core.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Server.Data;
using Virtuoso.Core.Utility;
using System.Linq;

namespace Virtuoso.Core.View
{
    public interface IPatientMaintenanceMenuSupport
    {
        Patient SelectedPatient { get; set; }
        NavigateKey NavigateKey { get; set; }
    }

    public interface IPatientAdmissionMaintenanceMenuSupport
    {
        Patient SelectedPatient { get; set; }
        NavigateKey NavigateKey { get; set; }
    }

    public class PageBase : Page
    {
        public PageBase()
            : base()
        {
        }

        #region Properties

        protected NavigateKey NavigateKey
        {
            get { return this.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey; }
        }

        #endregion //Properties

        #region Methods

        //Called when page removed from Page 'cache'
        public virtual bool Cleanup()
        {
            var vm = this.DataContext as GalaSoft.MvvmLight.ICleanup;
            if (vm != null)
            {
                bool _canExit = CanExit();
                if (_canExit)
                {
                    // disconnect the bindings before we call cleanup
                    //this.DataContext = null; Doing this prevents the control from knowing it should clean up ...

                    // J.E. I cannot find any page that finds PopupRoot controls.  When called from DynamicForm.xaml on the Medications section, this code
                    //      can loop through +100K controls adding between 3-6 seconds to the time it takes to exit the form - and not finding any PopupRoot 
                    //      controls, so for now - skippping over DynamicForm.
                    if (this.GetType().FullName.EndsWith("Virtuoso.Home.V2.Views.DynamicForm") == false)
                    {
                        var fn4 = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(this)
                            .Where(c => c.Name.StartsWith("PopupRoot")).ToList();
                        foreach (var rc in fn4)
                        {
                            VirtuosoObjectCleanupHelper.CleanupAll(rc);
                            rc.DataContext = null;
                        }
                    }

                    vm.Cleanup();
                }

                return _canExit;
            }

            return true;
        }

        //Called when page removed from Page 'cache'
        public virtual bool CanExit()
        {
            var vm = this.DataContext as Virtuoso.Core.ViewModel.IScreen;
            if (vm != null)
                return vm.CanExit();
            else
                return true;
        }

        private bool NavigationKeySet { get; set; }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try
            {
                ViewModelBase vm = this.DataContext as ViewModelBase;
                var __MorphingNavigatingFromIntoNavigateBack = vm?.MorphingNavigatingFromIntoNavigateBack;

                if (e?.Uri?.OriginalString.Equals("/Work") ==
                    false // NOTE: do not auto-remove current page from cache if we're navigating to Open Work
                    && (vm != null && __MorphingNavigatingFromIntoNavigateBack.GetValueOrDefault()))
                {
                    vm.RemoveFromCache();
                }

                base.OnNavigatingFrom(e);

                if (this.NavigateKey != null)
                {
                    this.NavigateKey.CurrentSource = this.NavigationService.CurrentSource.OriginalString;
                    if (!NavigationKeySet)
                    {
                        Messenger.Default.Send(this.NavigateKey, "NavigationKeyChanged");
                        NavigationKeySet = true;
                    }
                }

                if (vm != null)
                {
                    Boolean cancel = false;
                    vm.OnNavigatingFrom(ref cancel);
                    e.Cancel = cancel;
                }
            }
            finally
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine("(OnNavigatingFrom) PageOpened: {0}", e.Uri);
                    Messenger.Default.Send<bool>(true, "PageOpened");
                });
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                base.OnNavigatedTo(e);

                Messenger.Default.Send<ContextSensitiveArgs>(
                    new ContextSensitiveArgs() { ViewModel = this.DataContext }, "SetContextSensitiveMenu");
                var vm = this.DataContext as ViewModelBase;
                if (vm != null)
                {
                    vm.NavigateKey = this.NavigateKey;
                    if (vm.NavigateKey != null && !NavigationKeySet)
                    {
                        Messenger.Default.Send(this.NavigateKey, "NavigationKeyChanged");
                        NavigationKeySet = true;
                    }

                    object id = -1;
                    try
                    {
                        if (NavigationContext.QueryString.ContainsKey(Constants.ACTION))
                        {
                            if (NavigationContext.QueryString[Constants.ACTION].Equals(Constants.ADDNEW))
                            {
                                id = 0;
                            }
                            else
                            {
                                throw new Exception(Constants.UNKNOWN_ACTION);
                            }
                        }
                        else if (NavigationContext.QueryString.ContainsKey(Constants.TRACKING_KEY))
                        {
                            id = 0;
                        }
                        else if (NavigationContext.QueryString.ContainsKey(Constants.ID))
                        {
                            //FYI = won't always be an int - E.G. UserProfile will be a GUID...
                            //Shouldn't the receiving page decide what to do with this ID?
                            //id = Int32.Parse(NavigationContext.QueryString[Constants.ID]);
                            id = NavigationContext.QueryString[Constants.ID];
                        }

                        if (id.ToString().Equals("-1"))
                        {
                            await vm.OnNavigatedTo(NavigationContext.QueryString);
                        }
                        else
                        {
                            await vm.OnNavigatedTo(id);
                        }
                    }
                    catch (Exception eX)
                    {
                        Virtuoso.Core.Controls.ErrorWindow.CreateNew("Error PageBase.OnNavigatedTo",
                            new Events.ErrorEventArgs(eX));
                        this.NavigationService.Navigate(new Uri(Constants.HOME_URI_STRING, UriKind.Relative));
                    }
                }
            }
            finally
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    System.Diagnostics.Debug.WriteLine("(OnNavigatedTo) PageOpened: {0}", e.Uri);
                    Messenger.Default.Send<bool>(false, "PageOpened");
                });
            }
        }

        #endregion //Methods
    }

    public class PageBaseTab : PageBase
    {
        private TabControl pageTabControl = null;
        private HyperlinkButton addHyperlinkButton = null;

        public PageBaseTab()
            : base()
        {
            this.Loaded += new RoutedEventHandler(PageBaseTab_Loaded);
        }

        #region Methods

        private void PageBaseTab_Loaded(Object sender, RoutedEventArgs e)
        {
            addHyperlinkButton = FindVisualChildByName<HyperlinkButton>(this, "addHyperlinkButton");
            if (addHyperlinkButton != null)
            {
                pageTabControl = FindVisualChildByName<TabControl>(this, "pageTabControl");
                if (pageTabControl != null)
                    this.addHyperlinkButton.Click += new RoutedEventHandler(addHyperlinkButton_Click);
            }
        }

        private void addHyperlinkButton_Click(Object sender, RoutedEventArgs e)
        {
            if (pageTabControl != null) this.pageTabControl.SelectedIndex = 0;
        }

        private static T FindVisualChildByName<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                string controlName = child.GetValue(Control.NameProperty) as string;
                if (controlName == name)
                {
                    return child as T;
                }
                else
                {
                    T result = FindVisualChildByName<T>(child, name);
                    if (result != null) return result;
                }
            }

            return null;
        }

        public override bool Cleanup()
        {
            this.Loaded -= PageBaseTab_Loaded;
            this.pageTabControl = null;
            return base.Cleanup();
        }

        #endregion //Methods
    }
}