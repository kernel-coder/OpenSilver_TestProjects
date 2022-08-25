using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Virtuoso.Core.Converters;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class EncounterCoSignatureUserControl : UserControl, INotifyPropertyChanged
    {
        #region Signature dependency property

        public bool CanClearSignature
        {
            get
            {
                //if the signature's entitystat is new - then it can be cleared...
                if (EncounterCoSignature == null)
                    return false;
                if (EncounterCoSignature.Any() == false)
                    return true;
                if (EncounterCoSignature.First().EntityState == EntityState.New)
                    return true;
                else
                    return false;
            }
        }

        public byte[] Signature
        {
            get
            {
                if (EncounterCoSignature == null)
                    return null;
                if (EncounterCoSignature.Any() == false)
                    return null;
                return EncounterCoSignature.First().Signature;
            }
        }

        public bool HaveSignature
        {
            get
            {
                if (EncounterCoSignature == null)
                    return false;
                if (EncounterCoSignature.Any() == false)
                    return false;
                return (EncounterCoSignature.Any() == true);
            }
        }

        public bool CaptureSignature
        {
            get
            {
                if (IsPrint) return false;
                var have_signature = HaveSignature;
                if (have_signature && (EncounterCoSignature.First().EntityState == EntityState.New))
                    return true;
                else
                {
                    if (have_signature)
                        return false;
                    else
                        return true;
                }
            }
        }

        public IPatientService Model
        {
            get
            {
                return (IPatientService)GetValue(ModelProperty);
            }
            set
            {
                SetValue(ModelProperty, value);
            }
        }
        public static readonly DependencyProperty IsPrintProperty =
            DependencyProperty.Register("IsPrint", typeof(Boolean), typeof(EncounterCoSignatureUserControl), new PropertyMetadata(IsPrintPropertyChanged));

        private static void IsPrintPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            EncounterCoSignatureUserControl uc = sender as EncounterCoSignatureUserControl;
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
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.EncounterCoSignatureUserControl.IsPrintProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.EncounterCoSignatureUserControl.IsPrintProperty, value); }
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
            "Model",
            typeof(IPatientService),
            typeof(EncounterCoSignatureUserControl), null);

        public EntityCollection<EncounterCoSignature> EncounterCoSignature
        {
            get
            {
                return (EntityCollection<EncounterCoSignature>)GetValue(EncounterCoSignatureProperty);
            }
            set
            {
                SetValue(EncounterCoSignatureProperty, value);
            }
        }

        public static readonly DependencyProperty EncounterCoSignatureProperty =
            DependencyProperty.Register("EncounterCoSignature", typeof(EntityCollection<EncounterCoSignature>),
            typeof(EncounterCoSignatureUserControl), new PropertyMetadata(new PropertyChangedCallback(EncounterCoSignatureChanged)));

        private static void EncounterCoSignatureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var ctrl = sender as EncounterCoSignatureUserControl;

                ctrl.RemoveOld(args.OldValue as EncounterCoSignature);
                ctrl.SetupNew(args.NewValue as EncounterCoSignature);
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        #endregion

        void RemoveOld(EncounterCoSignature oldValue)
        {
            if (oldValue != null)
                RemoveCollectionChangedHandler(oldValue);
        }

        void SetupNew(EncounterCoSignature newValue)
        {
            //clear out what might be showing from a previous Encounter
            inkPad.Strokes.Clear();

            if (newValue != null)  //can equal null when changing to an admission/patient without any Encounters
                AddCollectionChangedHandler();

            UpdateUI();
        }

        void RemoveCollectionChangedHandler(EncounterCoSignature old)
        {
            if (old != null)
                ((INotifyCollectionChanged)EncounterCoSignature).CollectionChanged -= EncounterCoSignatureCollectionChanged;
        }

        void AddCollectionChangedHandler()
        {
            ((INotifyCollectionChanged)EncounterCoSignature).CollectionChanged += new NotifyCollectionChangedEventHandler(EncounterCoSignatureCollectionChanged);
        }

        void EncounterCoSignatureCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateUI();
        }

        void UpdateUI()
        {
            //if there is no signature, clear the strokes collection...
            if (HaveSignature == false)
                inkPad.Strokes.Clear();

            this.RaisePropertyChanged("EncounterCoSignature");
            this.RaisePropertyChanged("CanClearSignature");
            this.RaisePropertyChanged("CaptureSignature");
            this.RaisePropertyChanged("HaveSignature");
            this.RaisePropertyChanged("Signature");
        }

        Stroke newStroke;

        public EncounterCoSignatureUserControl()
        {
            // Required to initialize variables
            InitializeComponent();
            IsPrint = false;
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

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            inkPad.Strokes.Clear();

            var curr_signature = EncounterCoSignature.FirstOrDefault();
            if (curr_signature != null)
                Model.Remove(curr_signature);

            this.RaisePropertyChanged("Signature");
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

            var signatureBytes = br.ReadBytes((int)s.Length);
            var curr_signature = EncounterCoSignature.FirstOrDefault();
            if (curr_signature != null)
                curr_signature.Signature = signatureBytes;
            else
            {
                var new_signature = new EncounterCoSignature()
                {
                    Signature = signatureBytes
                };
                EncounterCoSignature.Add(new_signature);
            }

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
    }
}
