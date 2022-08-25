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
    public partial class SignatureDataUserControl : UserControl, INotifyPropertyChanged
    {
        #region Signature dependency property

        public EncounterData EncounterData
        {
            get
            {
                return (EncounterData)GetValue(EncounterDataProperty);
            }
            set
            {
                SetValue(EncounterDataProperty, value);
            }
        }

        public byte[] Signature
        {
            get
            {
                if (EncounterData == null)
                    return null;
                return EncounterData.SignatureData;
            }
        }

        public static readonly DependencyProperty EncounterDataProperty =
            DependencyProperty.Register("EncounterData", typeof(EncounterData),
            typeof(SignatureDataUserControl), new PropertyMetadata(new PropertyChangedCallback(EncounterDataChanged)));

        private static void EncounterDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var ctrl = sender as SignatureDataUserControl;
                ctrl.SetSignature();
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        public int ForceSignatureClear
        {
            get
            {
                return (int)GetValue(ForceSignatureClearProperty);
            }
            set
            {
                SetValue(ForceSignatureClearProperty, value);
            }
        }

        public static readonly DependencyProperty ForceSignatureClearProperty =
            DependencyProperty.Register("ForceSignatureClear", typeof(int),
            typeof(SignatureDataUserControl), new PropertyMetadata(new PropertyChangedCallback(ForceSignatureClearChanged)));

        private static void ForceSignatureClearChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var ctrl = sender as SignatureDataUserControl;
                ctrl.Button_Click(ctrl, new RoutedEventArgs());
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        #endregion

        public static readonly DependencyProperty IsPrintProperty =
            DependencyProperty.Register("IsPrint", typeof(Boolean), typeof(SignatureDataUserControl), new PropertyMetadata(IsPrintPropertyChanged));

        private static void IsPrintPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            SignatureDataUserControl uc = sender as SignatureDataUserControl;
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
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.SignatureDataUserControl.IsPrintProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.SignatureDataUserControl.IsPrintProperty, value); }
        }

        private void SetSignature()
        {
            this.RaisePropertyChanged("Signature");
            this.RaisePropertyChanged("ShowInkPresenter");
        }

        Stroke newStroke;

        public SignatureDataUserControl()
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
                if (EncounterData != null)
                {
                    EncounterData.SignatureData = null;
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

            if (EncounterData != null)
            {
                var signatureBytes = br.ReadBytes((int)s.Length);
                EncounterData.SignatureData = signatureBytes;
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