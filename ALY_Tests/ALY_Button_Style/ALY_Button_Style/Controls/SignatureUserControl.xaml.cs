using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Media.Imaging;
using Virtuoso.Core.Converters;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Server.Data;
using System.ComponentModel;
using GalaSoft.MvvmLight.Messaging;

namespace Virtuoso.Core.Controls
{
    public partial class SignatureUserControl : UserControl, INotifyPropertyChanged
    {
        #region Signature dependency property

        //public static readonly DependencyProperty ModelProperty =
        //    DependencyProperty.Register(
        //    "Model",
        //    typeof(IDynamicFormService),
        //    typeof(SignatureUserControl), null);

        public static readonly DependencyProperty IsPrintProperty =
            DependencyProperty.Register("IsPrint", typeof(Boolean), typeof(SignatureUserControl), new PropertyMetadata(IsPrintPropertyChanged));

        private static void IsPrintPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            SignatureUserControl uc = sender as SignatureUserControl;
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
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.SignatureUserControl.IsPrintProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.SignatureUserControl.IsPrintProperty, value); }
        }

        //public IDynamicFormService Model
        //{
        //    get
        //    {
        //        return (IDynamicFormService)GetValue(ModelProperty);
        //    }
        //    set
        //    {
        //        SetValue(ModelProperty, value);
        //    }
        //}

        //public EntityCollection<EncounterSignature> EncounterSignature
        //{
        //    get
        //    {
        //        //this.RaisePropertyChanged("Signature");
        //        return (EntityCollection<EncounterSignature>)GetValue(EncounterSignatureProperty);
        //    }
        //    set
        //    {
        //        SetValue(EncounterSignatureProperty, value);
        //    }
        //}
        public static readonly DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool),
            typeof(SignatureUserControl), new PropertyMetadata(null));

        public bool IsReadOnly
        {
            get { return (bool)GetValue(IsReadOnlyProperty); }
            set { SetValue(IsReadOnlyProperty, value); }
        }

        public static readonly DependencyProperty SlaveProperty =
            DependencyProperty.Register("Slave", typeof(int),
            typeof(SignatureUserControl), new PropertyMetadata(new PropertyChangedCallback(SlaveChanged)));

        public int Slave
        {
            get { return (int)GetValue(SlaveProperty); }
            set { SetValue(SlaveProperty, value); }
        }
        private static void SlaveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                var ctrl = d as SignatureUserControl;
                ctrl.RefreshSignature();
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
            }
        }

        private void RefreshSignature()
        {
            SetSignature();
        }

        //public Encounter Encounter
        //{
        //    get
        //    {
        //        return (Encounter)GetValue(EncounterProperty);
        //    }
        //    set
        //    {
        //        SetValue(EncounterProperty, value);
        //    }
        //}

        //public byte[] Signature
        //{
        //    get
        //    {
        //        if (EncounterSignature == null)
        //            return null;
        //        if (EncounterSignature.Any() == false)
        //            return null;
        //        return EncounterSignature.First().Signature;
        //    }
        //}

        //public static readonly DependencyProperty EncounterSignatureProperty =
        //    DependencyProperty.Register("EncounterSignature", typeof(EntityCollection<EncounterSignature>),
        //    typeof(SignatureUserControl), new PropertyMetadata(new PropertyChangedCallback(EncounterSignatureChanged)));

        //private static void EncounterSignatureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        //{
        //    try
        //    {
        //        var ctrl = sender as SignatureUserControl;
        //        ctrl.SetSignature();
        //    }
        //    catch (Exception oe)
        //    {
        //        MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
        //        throw;
        //    }
        //}
        //public static readonly DependencyProperty EncounterProperty =
        //    DependencyProperty.Register("Encounter", typeof(Encounter),
        //    typeof(SignatureUserControl), new PropertyMetadata(new PropertyChangedCallback(EncounterChanged)));

        //private static void EncounterChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        //{
        //}
        //private DateTime? _signatureDate;
        //public DateTime? SignatureDate
        //{
        //    get
        //    {
        //        if (EncounterSignature == null)
        //            return null;
        //        if (EncounterSignature.Any() == false)
        //            return null;
        //        return EncounterSignature.First().SignatureDate;
        //    }
        //    set
        //    {
        //        _signatureDate = value; // need for cases when the signature box is signed second.
        //        if (EncounterSignature != null && EncounterSignature.Any() == true)
        //            EncounterSignature.First().SignatureDate = value;
        //    }
        //}
        #endregion

        private void SetSignature()
        {
            this.RaisePropertyChanged("Signature");
            this.RaisePropertyChanged("SignatureDate");
            this.RaisePropertyChanged("ShowInkPresenter");
            this.RaisePropertyChanged("Encounter");
            this.RaisePropertyChanged("EncounterSignature");
            this.RaisePropertyChanged("IsEnabled");
            this.RaisePropertyChanged("IsReadOnly");
            this.RaisePropertyChanged("Visibility");
        }

        Stroke newStroke;

        public SignatureUserControl()
        {
            // Required to initialize variables
            InitializeComponent();
            IsPrint = false;
        }
        public bool ShowInkPresenter
        {
            get 
            {
                return true;
                if (IsReadOnly) return false; // never display InkPresenter in ReadOnly mode, we want to see the Image
                
                //return (!IsPrint && Signature == null); 
            }
        }
        public bool ShowInkPresenterIfNotOrderEntry
        {
            get 
            {
                //if ((Encounter != null) && (Encounter.EncounterIsOrderEntry)) return false;
                return ShowInkPresenter; 
            }
        }
        private void InkPresenter_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.inkPad.CaptureMouse();
            newStroke = new Stroke();
#if OPENSILVER
            var point = e.GetPosition(inkPad);
           // newStroke.StylusPoints.Add(new StylusPoint() { X = point.X, Y = point.Y });
#else
                newStroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(inkPad));
#endif

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
#if OPENSILVER
                var point = e.GetPosition(inkPad);
                //newStroke.StylusPoints.Add(new StylusPoint() { X = point.X, Y = point.Y });
#else
                newStroke.StylusPoints.Add
                    (e.StylusDevice.GetStylusPoints(inkPad));
#endif


            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            inkPad.Strokes.Clear();

            try
            {
                //var curr_encounter_signature = EncounterSignature.FirstOrDefault();
                //if (curr_encounter_signature != null)
                //{
                //    curr_encounter_signature.SignatureDate = null;
                //    Model.RemoveSignature(curr_encounter_signature);
                //    this.RaisePropertyChanged("Signature");
                //    borderImage.Visibility = Visibility.Collapsed;
                //    borderInkPresenter.Visibility = Visibility.Visible;
                //    if (Encounter !=  null ) Messenger.Default.Send(Encounter, "EncounterSignatureChanged");
                //}
                //this.RaisePropertyChanged("SignatureDate");
            }
            catch
            {
            }
        }

        private async void ConvertToImage()
        {
            WriteableBitmap wbBitmap = new WriteableBitmap(inkPad, new TranslateTransform());
#if OPENSILVER
            await wbBitmap.WaitToInitialize();
#endif
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

            //if (EncounterSignature != null)
            //{
            //    var signatureBytes = br.ReadBytes((int)s.Length);
            //    var curr_encounter_signature = EncounterSignature.FirstOrDefault();
            //    if (curr_encounter_signature != null)
            //        curr_encounter_signature.Signature = signatureBytes;
            //    else
            //    {
            //        var new_signature = new EncounterSignature()
            //        {
            //            Signature = signatureBytes,
            //            SignatureDate = (SignatureDate == null ? _signatureDate : SignatureDate)
            //        };
            //        EncounterSignature.Add(new_signature);
            //    }
            //    if ((SignatureDate == null) && (Encounter != null)  && (Encounter.EncounterIsCOTI == true))
            //    {
            //        SignatureDate = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Unspecified).Date; // timestamp COIT signatures
            //    }

            //    this.RaisePropertyChanged("Signature");
            //    this.RaisePropertyChanged("SignatureDate");
            //    if (Encounter != null) Messenger.Default.Send(Encounter, "EncounterSignatureChanged");
            //}
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}