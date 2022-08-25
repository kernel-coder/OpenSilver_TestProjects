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
    public partial class OrderEntryCoSignatureUserControl : UserControl, INotifyPropertyChanged
    {
        #region Signature dependency property

        public bool CanClearSignature
        {
            get
            {
                //if the signature's entitystat is new - then it can be cleared...
                if (OrderEntryCoSignature == null)
                    return false;
                if (OrderEntryCoSignature.Any() == false)
                    return true;
                if (OrderEntryCoSignature.First().EntityState == EntityState.New)
                    return true;
                else
                    return false;
            }
        }

        public byte[] Signature
        {
            get
            {
                if (OrderEntryCoSignature == null)
                    return null;
                if (OrderEntryCoSignature.Any() == false)
                    return null;
                return OrderEntryCoSignature.First().Signature;
            }
        }

        public bool HaveSignature
        {
            get
            {
                if (OrderEntryCoSignature == null)
                    return false;
                if (OrderEntryCoSignature.Any() == false)
                    return false;
                return (OrderEntryCoSignature.Any() == true);
            }
        }

        public bool CaptureSignature
        {
            get
            {
                if (IsPrint) return false;
                var have_signature = HaveSignature;
                if (have_signature && (OrderEntryCoSignature.First().EntityState == EntityState.New))
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
            DependencyProperty.Register("IsPrint", typeof(Boolean), typeof(OrderEntryCoSignatureUserControl), new PropertyMetadata(IsPrintPropertyChanged));

        private static void IsPrintPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            OrderEntryCoSignatureUserControl uc = sender as OrderEntryCoSignatureUserControl;
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
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.OrderEntryCoSignatureUserControl.IsPrintProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.OrderEntryCoSignatureUserControl.IsPrintProperty, value); }
        }

        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register(
            "Model",
            typeof(IPatientService),
            typeof(OrderEntryCoSignatureUserControl), null);

        public EntityCollection<OrderEntryCoSignature> OrderEntryCoSignature
        {
            get
            {
                return (EntityCollection<OrderEntryCoSignature>)GetValue(OrderEntryCoSignatureProperty);
            }
            set
            {
                SetValue(OrderEntryCoSignatureProperty, value);
            }
        }

        public static readonly DependencyProperty OrderEntryCoSignatureProperty =
            DependencyProperty.Register("OrderEntryCoSignature", typeof(EntityCollection<OrderEntryCoSignature>),
            typeof(OrderEntryCoSignatureUserControl), new PropertyMetadata(new PropertyChangedCallback(OrderEntryCoSignatureChanged)));

        private static void OrderEntryCoSignatureChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var ctrl = sender as OrderEntryCoSignatureUserControl;

                ctrl.RemoveOld(args.OldValue as OrderEntryCoSignature);
                ctrl.SetupNew(args.NewValue as OrderEntryCoSignature);
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        #endregion

        void RemoveOld(OrderEntryCoSignature oldValue)
        {
            if (oldValue != null)
                RemoveCollectionChangedHandler(oldValue);
        }

        void SetupNew(OrderEntryCoSignature newValue)
        {
            //clear out what might be showing from a previous OrderEntry
            inkPad.Strokes.Clear();

            if (newValue != null)  //can equal null when changing to an admission/patient without any orders
                AddCollectionChangedHandler();

            UpdateUI();
        }

        void RemoveCollectionChangedHandler(OrderEntryCoSignature old)
        {
            if (old != null)
                ((INotifyCollectionChanged)OrderEntryCoSignature).CollectionChanged -= OrderEntryCoSignatureCollectionChanged;
        }

        void AddCollectionChangedHandler()
        {
            ((INotifyCollectionChanged)OrderEntryCoSignature).CollectionChanged += new NotifyCollectionChangedEventHandler(OrderEntryCoSignatureCollectionChanged);
        }

        void OrderEntryCoSignatureCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateUI();
        }

        void UpdateUI()
        {
            //if there is no signature, clear the strokes collection...
            if (HaveSignature == false)
                inkPad.Strokes.Clear();

            this.RaisePropertyChanged("OrderEntryCoSignature");
            this.RaisePropertyChanged("CanClearSignature");
            this.RaisePropertyChanged("CaptureSignature");
            this.RaisePropertyChanged("HaveSignature");
            this.RaisePropertyChanged("Signature");
        }

        Stroke newStroke;

        public OrderEntryCoSignatureUserControl()
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

            var curr_signature = OrderEntryCoSignature.FirstOrDefault();
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
            var curr_signature = OrderEntryCoSignature.FirstOrDefault();
            if (curr_signature != null)
                curr_signature.Signature = signatureBytes;
            else
            {
                var new_signature = new OrderEntryCoSignature()
                {
                    Signature = signatureBytes
                };
                OrderEntryCoSignature.Add(new_signature);
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
