using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Virtuoso.Core.Converters;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class SignatureVOUserControl : UserControl, INotifyPropertyChanged
    {
        #region Signature dependency property

        public OrderEntryVO OrderEntryVO
        {
            get
            {
                return (OrderEntryVO)GetValue(OrderEntryVOProperty);
            }
            set
            {
                SetValue(OrderEntryVOProperty, value);
            }
        }

        public byte[] Signature
        {
            get
            {
                if (OrderEntryVO == null) return null;
                return OrderEntryVO.Signature;
            }
        }

        public static readonly DependencyProperty OrderEntryVOProperty =
            DependencyProperty.Register("OrderEntryVO", typeof(OrderEntryVO),
            typeof(SignatureVOUserControl), new PropertyMetadata(new PropertyChangedCallback(OrderEntryVOChanged)));

        private static void OrderEntryVOChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var ctrl = sender as SignatureVOUserControl;
                ctrl.SetSignature();
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        #endregion

        public static readonly DependencyProperty IsPrintProperty =
            DependencyProperty.Register("IsPrint", typeof(Boolean), typeof(SignatureVOUserControl), new PropertyMetadata(IsPrintPropertyChanged));

        private static void IsPrintPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            SignatureVOUserControl uc = sender as SignatureVOUserControl;
            if (uc != null)
            {
                bool? isPrint = args.NewValue as bool?;
                uc.stackPanelSignature.Visibility = (isPrint == true) ? Visibility.Collapsed : Visibility.Visible;
                var par = uc.inkPad.Parent;
                if (par != null)
                {
                    if (par is Border)
                    {
                        var border = (Border)par;
                        border.Child = null;
                    }
                    else if (par is Panel)
                    {
                        Panel panel = (Panel)par;
                        panel.Children.Remove(uc.inkPad);
                    }
                }
            }
        }
        public bool IsPrint
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.SignatureVOUserControl.IsPrintProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.SignatureVOUserControl.IsPrintProperty, value); }
        }

        private void SetSignature()
        {
            this.RaisePropertyChanged("Signature");
            this.RaisePropertyChanged("ShowInkPresenter");
        }

        Stroke newStroke;

        public SignatureVOUserControl()
        {
            // Required to initialize variables
            InitializeComponent();
            IsPrint = false;
        }
        public bool ShowInkPresenter
        {
            get { return !IsPrint && Signature == null; }
        }

        private void InkPresenter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.inkPad.CaptureMouse();
            newStroke = new Stroke();
            newStroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(inkPad));
            inkPad.Strokes.Add(newStroke);
        }

        private void InkPresenter_LostMouseCapture(object sender, MouseEventArgs e)
        {
            newStroke = null;
            inkPad.ReleaseMouseCapture();

            ConvertToImage();
        }

        private void InkPresenter_MouseMove(object sender, MouseEventArgs e)
        {
            if (newStroke != null)
            {
                newStroke.StylusPoints.Add
                    (e.StylusDevice.GetStylusPoints(inkPad));
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            inkPad.Strokes.Clear();

            try
            {
                if (OrderEntryVO != null)
                {
                    OrderEntryVO.Signature = null;
                    this.RaisePropertyChanged("Signature");
                    borderImage.Visibility = Visibility.Collapsed;
                    borderInkPresenter.Visibility = Visibility.Visible;
                }
            }
            catch
            {
            }
        }

        private void ConvertToImage()
        {
            WriteableBitmap wbBitmap = new WriteableBitmap(inkPad, new TranslateTransform());
            EditableImage eiImage = new EditableImage(wbBitmap.PixelWidth, wbBitmap.PixelHeight);

            try
            {
                for (int y = 0; y < wbBitmap.PixelHeight; ++y)
                {
                    for (int x = 0; x < wbBitmap.PixelWidth; ++x)
                    {
                        int pixel = wbBitmap.Pixels[wbBitmap.PixelWidth * y + x];
                        eiImage.SetPixel(x, y,
                        (byte)((pixel >> 16) & 0xFF),
                        (byte)((pixel >> 8) & 0xFF),
                        (byte)(pixel & 0xFF), (byte)((pixel >> 24) & 0xFF)
                        );
                    }
                }
            }
            catch (System.Security.SecurityException)
            {
                throw new Exception("Cannot print images from other domains");
            }

            Stream s = eiImage.GetStream();
            BinaryReader br = new BinaryReader(s);

            if (OrderEntryVO != null)
            {
                var signatureBytes = br.ReadBytes((int)s.Length);
                OrderEntryVO.Signature = signatureBytes;
                this.RaisePropertyChanged("Signature");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void signature_ctrl_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            textBlockErrorText.Text = ""; 
            textBlockErrorText.Visibility = Visibility.Collapsed; 
            if (e == null) return;
            else if (e.Action ==  ValidationErrorEventAction.Removed) return;
            else if (e.Error == null) return;
            else if (e.Error.ErrorContent == null) return;
            else if (string.IsNullOrWhiteSpace(e.Error.ErrorContent.ToString()))return;
            textBlockErrorText.Text = e.Error.ErrorContent.ToString();
            textBlockErrorText.Visibility = Visibility.Visible;
        }
    }
}