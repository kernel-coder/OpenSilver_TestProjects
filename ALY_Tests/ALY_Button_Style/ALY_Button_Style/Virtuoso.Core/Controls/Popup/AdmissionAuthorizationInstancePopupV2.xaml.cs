using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class AdmissionAuthorizationInstancePopupV2 : ChildWindow, ICleanup
    {
        double VerticalApplicationPadding
        {
            get { return 100D; }
        } //Height

        double HorizontalApplicationPadding
        {
            get { return 200D; }
        } //Width
        //double MinControlWidth = 800D;
        //double MinControlHeight = 600D;        

        public AdmissionAuthorizationInstancePopupV2()
        {
            InitializeComponent();
        }

        public AdmissionAuthorizationInstancePopupV2(AdmissionAuthorization selectedItem,
            AdmissionAuthorizationInstance _admissionAuthorizationDetail, IPatientService model, AuthMode mode)
        {
            InitializeComponent();

            //NOTE: do not use Size of System.Windows.Application.Current.MainWindow - doesn't work when end user scales their resolution
            SizeWindow(new Size(
                (System.Windows.Application.Current.RootVisual as FrameworkElement).ActualWidth,
                (System.Windows.Application.Current.RootVisual as FrameworkElement).ActualHeight));

            Messenger.Default.Register<Size>(this, Constants.Application.Resize,
                (newSize) => { SizeWindow(newSize); });

            var vm = new AdmissionAuthorizationInstanceViewModel(selectedItem, _admissionAuthorizationDetail, model,
                mode);
            this.DataContext = vm;
            this.Loaded += AdmissionAuthorizationDetailPopupV2_Loaded;
        }

        private void SizeWindow(Size newSize)
        {
            //NOTE: System.Windows.Application.Current.MainWindow (Height/Width) doesn't work when end user adjusts their resolution
            //E.G. set 'Make text and other items larger or smaller' - 125%, 150%, 200%
            //var _w = System.Windows.Application.Current.MainWindow.Width;
            //var _h = System.Windows.Application.Current.MainWindow.Height;

            //int VerticalApplicationPadding { get { return 100; } }   //Height
            //int HorizontalApplicationPadding { get { return 200; } } //Width

            var newHeight = newSize.Height - VerticalApplicationPadding;
            var newWidth = newSize.Width - HorizontalApplicationPadding;

            //var h = Math.Max(newHeight, this.MinControlHeight);
            //var w = Math.Max(newWidth, this.MinControlWidth);

            this.Height =
                newHeight; //(Application.Current.RootVisual as FrameworkElement).ActualHeight - ApplicationPadding;
            this.Width =
                newWidth; //(Application.Current.RootVisual as FrameworkElement).ActualWidth - ApplicationPadding;
        }

        //private void AuthDetailsGrid_BindingValidationError(object sender, ValidationErrorEventArgs e)
        //{
        //    ValidationSummaryItem valsumremove = valSum.Errors.Where(v => v.Message.Equals(e.Error.ErrorContent.ToString())).FirstOrDefault();

        //    if (e.Action == ValidationErrorEventAction.Removed)
        //    {
        //        valSum.Errors.Remove(valsumremove);
        //    }
        //    else if (e.Action == ValidationErrorEventAction.Added)
        //    {
        //        if (valsumremove == null)
        //        {
        //            ValidationSummaryItem vsi = new ValidationSummaryItem() { Message = e.Error.ErrorContent.ToString(), Context = e.OriginalSource };
        //            vsi.Sources.Add(new ValidationSummaryItemSource(String.Empty, e.OriginalSource as Control));

        //            valSum.Errors.Add(vsi);

        //            e.Handled = true;
        //        }
        //    }
        //}  

        void AdmissionAuthorizationDetailPopupV2_Loaded(object sender, RoutedEventArgs e)
        {
            //SetFocusHelper.SelectFirstEditableWidget(this.AuthDetailsGrid); //not working - think need to do this from VM after all bindings are done...no way of really knowing that - try end of constructor...
            var dc = authPopupContentCtrl.DataContext as AuthInstanceViewModelBase;
            if (dc != null)
            {
                dc.SetViewDependencyObject(authPopupContentCtrl);
            }
        }

        // Offer ESC key support for closing the ChildWindow:
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                var VM = this.DataContext as AdmissionAuthorizationInstanceViewModel;
                if (VM != null)
                {
                    VM.Cleanup();
                }

                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        private void ChildWindow_Closing(object sender, CancelEventArgs e)
        {
            this.Cleanup();
        }

        private void ContentPresenter_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var dc = authPopupContentCtrl.DataContext as AuthInstanceViewModelBase;
            if (dc != null)
            {
                dc.SetViewDependencyObject(authPopupContentCtrl);
            }
        }

        public void Cleanup()
        {
            Messenger.Default.Unregister(this);

            this.Loaded -= AdmissionAuthorizationDetailPopupV2_Loaded;

            //var _popupContent = this.authPopupContentCtrl as ICleanup;
            //if (_popupContent != null)
            //{
            //    _popupContent.Cleanup();
            //}

            var VM = this.DataContext as AdmissionAuthorizationInstanceViewModel;
            if (VM != null)
            {
                VM.Cleanup();
                //this.DataContext = null;  //This is clearing DialogResult and thus causing the close event to be called a second time.
            }
        }
    }
}