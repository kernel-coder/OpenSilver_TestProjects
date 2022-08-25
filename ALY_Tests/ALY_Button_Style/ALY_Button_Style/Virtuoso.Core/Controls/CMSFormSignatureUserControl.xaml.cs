using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Converters;

namespace Virtuoso.Core.Controls
{
    public partial class CMSFormSignatureUserControl : UserControl, INotifyPropertyChanged, ICleanup
    {
        #region Signature dependency property

        public byte[] CMSFormSignature
        {
            get
            {
                return (byte[])GetValue(CMSFormSignatureProperty);
            }
            set
            {
                SetValue(CMSFormSignatureProperty, value);
            }
        }
        public static readonly DependencyProperty CMSFormSignatureProperty =
        DependencyProperty.Register("CMSFormSignature", typeof(byte[]), typeof(CMSFormSignatureUserControl), new PropertyMetadata(new PropertyChangedCallback(CMSFormSignatureChanged)));

        private static void CMSFormSignatureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var ctrl = sender as CMSFormSignatureUserControl;
                ctrl.SetSignature();
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }
        public byte[] Signature
        {
            get
            {
                if (CMSFormSignature == null) return null;
                if (CMSFormSignature.Length == 0) return null;
                return CMSFormSignature;
            }
        }

        #endregion

        public static readonly DependencyProperty IsPrintProperty =
            DependencyProperty.Register("IsPrint", typeof(Boolean), typeof(CMSFormSignatureUserControl), new PropertyMetadata(IsPrintPropertyChanged));

        private static void IsPrintPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CMSFormSignatureUserControl uc = sender as CMSFormSignatureUserControl;
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
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.CMSFormSignatureUserControl.IsPrintProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.CMSFormSignatureUserControl.IsPrintProperty, value); }
        }

        private bool BypassRaisePropertyChangedShowInkPresenter = false;
        private void SetSignature()
        {
            this.RaisePropertyChanged("Signature");
            if (BypassRaisePropertyChangedShowInkPresenter == false) this.RaisePropertyChanged("ShowInkPresenter");
            BypassRaisePropertyChangedShowInkPresenter = false;
        }

        Stroke newStroke;

        public CMSFormSignatureUserControl()
        {
            // Required to initialize variables
            InitializeComponent();
            this.Unloaded += this.CMSFormSignatureUserControl_Unloaded;
            IsPrint = false;
        }
        private void CMSFormSignatureUserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            SetSignature();
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
                CMSFormSignature = null;
                this.RaisePropertyChanged("Signature");
                borderImage.Visibility = Visibility.Collapsed;
                borderInkPresenter.Visibility = Visibility.Visible;
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

            BypassRaisePropertyChangedShowInkPresenter = true;
            var signatureBytes = br.ReadBytes((int)s.Length);
            CMSFormSignature = signatureBytes;
            this.RaisePropertyChanged("Signature");
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
            else if (e.Action == ValidationErrorEventAction.Removed) return;
            else if (e.Error == null) return;
            else if (e.Error.ErrorContent == null) return;
            else if (string.IsNullOrWhiteSpace(e.Error.ErrorContent.ToString())) return;
            textBlockErrorText.Text = e.Error.ErrorContent.ToString();
            textBlockErrorText.Visibility = Visibility.Visible;
        }
        public void Cleanup()
        {
            this.Unloaded -= this.CMSFormSignatureUserControl_Unloaded;
        }
    }
}