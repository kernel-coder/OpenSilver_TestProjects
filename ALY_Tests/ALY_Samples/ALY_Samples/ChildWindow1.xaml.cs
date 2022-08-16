using System.Windows;
using System.Windows.Controls;

namespace ALY_Button_Style
{
    public partial class ChildWindow1 : ChildWindow
    {
        public ChildWindow1()
        {
            InitializeComponent();

            SizeWindow(new Size(
               (Application.Current.RootVisual as FrameworkElement).ActualWidth,
               (Application.Current.RootVisual as FrameworkElement).ActualHeight));

            ((FrameworkElement)Application.Current.RootVisual).SizeChanged += new SizeChangedEventHandler(App_SizeChanged);
        }

        void App_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //NOTE: System.Windows.Application.Current.MainWindow (Height/Width) doesn't work when end user adjusts their resolution
            //E.G. set 'Make text and other items larger or smaller' - 125%, 150%, 200%
            //var _w = System.Windows.Application.Current.MainWindow.Width;
            //var _h = System.Windows.Application.Current.MainWindow.Height;

            var __Height = (Application.Current.RootVisual as FrameworkElement).ActualHeight;
            var __Width = (Application.Current.RootVisual as FrameworkElement).ActualWidth;

            //Messenger.Default.Send<Size>(e.NewSize, Constants.Application.Resize);
            SizeWindow(new Size(__Width, __Height));
        }


        double SearchDialogApplicationPadding = 100;
        private void SizeWindow(Size newSize)
        {
            //NOTE: System.Windows.Application.Current.MainWindow (Height/Width) doesn't work when end user adjusts their resolution
            //E.G. set 'Make text and other items larger or smaller' - 125%, 150%, 200%
            //var _w = System.Windows.Application.Current.MainWindow.Width;
            //var _h = System.Windows.Application.Current.MainWindow.Height;

            this.Height = newSize.Height - SearchDialogApplicationPadding; //(Application.Current.RootVisual as FrameworkElement).ActualHeight - ApplicationPadding;
            this.Width = newSize.Width - SearchDialogApplicationPadding; //(Application.Current.RootVisual as FrameworkElement).ActualWidth - ApplicationPadding;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Width = 340;
            this.Height = 350;
        }
    }
}

