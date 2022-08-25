using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Virtuoso.Core;
using Virtuoso.Core.ViewModel;

namespace Virtuoso.Controls
{
    public partial class SearchDialog : ChildWindow, ICleanup
    {
        int SearchDialogApplicationPadding { get { return 100; } }

        public string CurrentSearchOverride
        {
            get
            {
                return searchPanelUserControl.CurrentSearchOverride as string;
            }
            set
            {
                searchPanelUserControl.CurrentSearchOverride = value;
            }
        }

        public SearchPanelViewModel ParentViewModel
        {
            get
            {
                return searchPanelUserControl.ParentViewModel as SearchPanelViewModel;
            }
        }

        public SearchDialog()
        {
            InitializeComponent();

            //NOTE: do not use Size of System.Windows.Application.Current.MainWindow - doesn't work when end user scales their resolution
            SizeWindow(new Size(
                (Application.Current.RootVisual as FrameworkElement).ActualWidth, 
                (Application.Current.RootVisual as FrameworkElement).ActualHeight));
            
            searchPanelUserControl.ViewModelChanged += searchPanelUserControl_ViewModelChanged;
            this.Loaded += SearchDialog_Loaded;
            this.GotFocus += SearchDialog_GotFocus;

            Messenger.Default.Register<Size>(this, Constants.Application.Resize,
                (newSize) =>
                {
                    SizeWindow(newSize);
                });

            Messenger.Default.Register<Uri>(this, Constants.Application.SearchDialogNavigationRequest,
                (uri) =>
                {
                    SearchDialogNavigationRequest(uri);
                });
            
        }

        private void SizeWindow(Size newSize)
        {
            //NOTE: System.Windows.Application.Current.MainWindow (Height/Width) doesn't work when end user adjusts their resolution
            //E.G. set 'Make text and other items larger or smaller' - 125%, 150%, 200%
            //var _w = System.Windows.Application.Current.MainWindow.Width;
            //var _h = System.Windows.Application.Current.MainWindow.Height;

            this.Height = newSize.Height - SearchDialogApplicationPadding; //(Application.Current.RootVisual as FrameworkElement).ActualHeight - ApplicationPadding;
            this.Width = newSize.Width - SearchDialogApplicationPadding; //(Application.Current.RootVisual as FrameworkElement).ActualWidth - ApplicationPadding;
        }

        void SearchDialog_Loaded(object sender, RoutedEventArgs e)
        {
            this.ClosingDialog = false;
            this.Focus();
        }

        void searchPanelUserControl_ViewModelChanged(object sender, RoutedEventArgs e)
        {
            if (ParentViewModel != null)
            {
                ParentViewModel.ItemSelected -= ParentViewModel_ItemSelected;
                ParentViewModel.ItemSelected += new EventHandler<MenuEventArgs>(ParentViewModel_ItemSelected);
            }
        }

        void ParentViewModel_ItemSelected(object sender, EventArgs e)
        {
            CloseDialog(true);
        }

        void SearchDialog_GotFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(() => { searchPanelUserControl.Focus(); });
            this.GotFocus -= SearchDialog_GotFocus;
        }

        public void SaveState()
        {
            searchPanelUserControl.SaveState();
        }

        public void InitSearch(bool restoreSearchState = true, int? serviceLineKey = null)
        {
            //Search dialog will be created once, and opened/closed many times.
            //Each time it is (re)opened - call InitSearch(), so that VM can establish the 'current' page, 
            //E.G. figure out what the default search should be.
            searchPanelUserControl.InitSearch(restoreSearchState, serviceLineKey);
            this.searchPanelUserControl.Focus();
            RegisterCommands();
        }

        bool ClosingDialog = false;
        void CloseDialog(bool dialogResult)
        {
            this.ClosingDialog = true;
            this.DialogResult = dialogResult;
        }

        bool registered_close = false; //only register once
        private void RegisterCommands()
        {
            if (registered_close == false)
            {
                //DS 092514 - Bug 11528
                Messenger.Default.Register<bool>(this, Constants.Messaging.CloseSearchDialog,
                (DialogResult) =>
                {
                    CloseDialog(DialogResult);
                });
                registered_close = true;
            }
        }
              
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            //Prevent a "double" navigation request by checking if DialogResult has already been set
            if (this.ClosingDialog == true) return; // Cancel navigation

            var uri = ((SearchPanelViewModel)(sender)).SearchUri;
            Messenger.Default.Send<Uri>((Uri)uri, "NavigationRequest");
            CloseDialog(true);
        }

        private void SearchDialogNavigationRequest(Uri uri)
        {
            //Prevent a "double" navigation request by checking if DialogResult has already been set
            if (this.ClosingDialog == true) return; // Cancel navigation

            Messenger.Default.Send<Uri>(uri, "NavigationRequest");
            CloseDialog(true);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            CloseDialog(false);
        }

        // Offer ESC key support for closing the ChildWindow:
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                CloseDialog(false);
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        public void Cleanup()
        {
            //NOTE: Cleanup can be called multiple times

            searchPanelUserControl.ViewModelChanged -= searchPanelUserControl_ViewModelChanged;
            searchPanelUserControl.Cleanup();

            this.Loaded -= SearchDialog_Loaded;
            this.GotFocus -= SearchDialog_GotFocus;
            if (ParentViewModel != null) ParentViewModel.ItemSelected -= ParentViewModel_ItemSelected;

            Messenger.Default.Unregister(this);
            Messenger.Default.Unregister<Size>(this, Constants.Application.Resize);
        }
    }
}

