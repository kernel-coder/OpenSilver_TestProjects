#region Usings

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Core.Services
{
    public partial class CustomWindow : ChildWindow, ICleanup
    {
        int DialogApplicationPadding => 100;

        public CustomWindow(IDialogService viewModel)
        {
            InitializeComponent();

            viewModel.SetSelectFirstEditableWidgetAction(SelectFirstEditableWidget);

            DataContext = viewModel;

            Title = viewModel.Caption;

            if (viewModel.ResizeWindow)
            {
                //NOTE: do not use Size of System.Windows.Application.Current.MainWindow - doesn't work when end user scales their resolution
                SizeWindow(new Size(
                    (System.Windows.Application.Current.RootVisual as FrameworkElement).ActualWidth,
                    (System.Windows.Application.Current.RootVisual as FrameworkElement).ActualHeight));

                Messenger.Default.Register<Size>(this, Constants.Application.Resize,
                    newSize => { SizeWindow(newSize); });
            }
            else if (viewModel.SetMaxWidthAndHeight)
            {
                if (viewModel.Width.HasValue)
                {
                    mainContent.MaxWidth = viewModel.Width.Value;
                }

                if (viewModel.MinWidth.HasValue)
                {
                    mainContent.MinWidth = viewModel.MinWidth.Value;
                }

                if (viewModel.Height.HasValue)
                {
                    mainContent.MaxHeight = viewModel.Height.Value;
                }

                if (viewModel.MinHeight.HasValue)
                {
                    mainContent.MinHeight = viewModel.MinHeight.Value;
                }
            }
            else if
                (viewModel.DynamicSize ==
                 false) // (viewModel.DynamicSize could be true && viewModel.SetMaxWidthAndHeight could be false - in which case fall thru without setting Width/Height)
            {
                Width = (viewModel.Width.HasValue) ? viewModel.Width.Value : 500;
                Height = (viewModel.Height.HasValue) ? viewModel.Height.Value : 300;
            }
        }

        private void SizeWindow(Size newSize)
        {
            Height = newSize.Height - DialogApplicationPadding;
            Width = newSize.Width - DialogApplicationPadding;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }

            base.OnKeyDown(e);
        }

        public void SelectFirstEditableWidget()
        {
            SetFocusHelper.SelectFirstEditableWidget(mainContent);
        }

        protected override void OnOpened()
        {
            LayoutUpdated += OnChildWindowLayoutUpdated;

            base.OnOpened();
        }

        void OnChildWindowLayoutUpdated(object sender, EventArgs e)
        {
            // The following code will re-center the ChildWindow.
            // https://ruifigueiredopt.wordpress.com/2011/02/26/dynamically-re-position-a-childwindow/
            // When you have animations inside a childwindow or you resize the childwindow, It doesn’t re-position to the center of the screen.

            // J.E. - Would get a non-centered ChildWindow when setting MaxWidth and not MaxHeight 
            //        and having with a wrapping TextBlock in one of my UserControls, made the ChildWindow 'grow down'...
            // This code fixes that.

            var root = VisualTreeHelper.GetChild(this, 0) as FrameworkElement;
            if (root != null)
            {
                var contentRoot = root.FindName("ContentRoot") as FrameworkElement;

                if (contentRoot != null)
                {
                    var group = contentRoot.RenderTransform as TransformGroup;

                    if (group != null)
                    {
                        if (group.Children.Count == 6)
                        {
                            var tf1 = group.Children[3] as TranslateTransform;
                            var tf2 = group.Children[5] as TranslateTransform;

                            if (tf1 != null)
                            {
                                tf1.X = 0;
                                tf1.Y = 0;
                            }

                            if (tf2 != null)
                            {
                                tf2.X = 0;
                                tf2.Y = 0;
                            }
                        }
                    }
                }
            }
        }

        public void Cleanup()
        {
            // WriteLine("CustomWindow.Cleanup BEGIN: " + DateTime.Now.ToString());

            LayoutUpdated -= OnChildWindowLayoutUpdated;

            Messenger.Default.Unregister(this);

            // This CustomWindow helper class should not be responsible for cleaning up the Content - the user of this class must cleanup
            // via the view model and/or data template code behind

            // WriteLine("CustomWindow.Cleanup END: " + DateTime.Now.ToString());
        }

        private void ChildWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = (((IDialogService)DataContext).CanClose() == false);
        }

        private void ChildWindow_Closed(object sender, EventArgs e)
        {
            overLay.Visibility = Visibility.Visible; //closing dialog window, so hide UI
            ((IDialogService)DataContext)
                .CloseDialog(); // Make sure that if end user clicks the 'X' button in the ChildWindow that the ViewModel gets cleaned up
            System.Windows.Application.Current.RootVisual.SetValue(IsEnabledProperty, true);
        }
    }
}