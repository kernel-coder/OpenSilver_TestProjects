using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ComponentModel;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Server.Data;
using System.Windows.Data;
using Virtuoso.Core.Cache;
using System.Windows.Resources;
using System.Windows.Media.Imaging;
using System.Collections.Specialized;
using System.Collections;
using Virtuoso.Client.Infrastructure;
using Virtuoso.Client.Core;

namespace Virtuoso.Core.Controls
{
    public interface IPatientChanged
    {
        void RemoveOld(Patient oldValue);
        void SetupNew(Patient newValue);
    }

    public partial class PatientPhotoUserControl : UserControl, IPatientChanged, INotifyPropertyChanged
    {
        CollectionViewSource _photosViewSource;
        BitmapImage defaultMaleImage { get; set; }
        BitmapImage defaultFemaleImage { get; set; }
        BitmapImage defaultOtherImage { get; set; }

        public Patient Patient
        {
            get
            {
                return (Patient)GetValue(PatientProperty);
            }
            set
            {
                SetValue(PatientProperty, value);
            }
        }

        public ICollectionView FilteredPhotos
        {
            get { return _photosViewSource.View; }
        }

        public byte[] Photo
        {
            get
            {
                if (Patient == null)
                    return null;
                if (Patient.PatientPhoto == null)
                    return null;
                if (Patient.PatientPhoto.Any() == false)
                    return null;
                return Patient.PatientPhoto.Where(p => p.HistoryKey == null).First().Photo;
            }
        }

        public bool HavePhotos
        {
            get
            {
                if (Patient == null)
                    return false;
                if (Patient.PatientPhoto == null)
                    return false;
                if (Patient.PatientPhoto.Any() == false)
                    return false;
                return (Patient.PatientPhoto.Where(p => p.HistoryKey == null).Any() == true);
            }
        }

        public BitmapImage DefaultPhoto
        {
            get
            {
                if (HavePhotos)
                    return null;
                if (Patient == null)
                    return null;
                if (Patient.Gender.HasValue)
                {
                    var gender = CodeLookupCache.GetCodeFromKey(Patient.Gender.Value);

                    switch (gender)
                    {
                        case "M": return defaultMaleImage;
                        case "F": return defaultFemaleImage;
                        default: return defaultOtherImage;
                    }
                }
                else
                    return defaultOtherImage;
            }
        }

        public static readonly DependencyProperty PatientProperty =
            DependencyProperty.Register("Patient", typeof(Patient),
            typeof(PatientPhotoUserControl), new PropertyMetadata(PatientChanged));

        private static void PatientChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var s = sender as IPatientChanged;
                if (s != null)
                {
                    s.RemoveOld(args.OldValue as Patient);
                    s.SetupNew(args.NewValue as Patient);
                }
            }
            catch (Exception oe)
            {
                //TODO: change MessageBox.Show to write to Log
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        public void RemoveOld(Patient oldValue)
        {
            RemoveCollectionChangedHandler(oldValue);
            _photosViewSource.Source = null;
        }

        public void SetupNew(Patient newValue)
        {
            if (Patient != null)
            {
                _photosViewSource.Source = Patient.PatientPhoto;
                AddCollectionChangedHandler();
            }
            UpdateUI();
        }

        void RemoveCollectionChangedHandler(Patient old)
        {
            if (old != null)
            {
                //var removeMessage = String.Format("Removing delegate for patient key: {0}", old.PatientKey);

                //var old_ref = (_photosViewSource.Source as EntityCollection<PatientPhoto>);
                //if (old_ref != null)
                //    ((INotifyCollectionChanged)old_ref).CollectionChanged -= PatientPhotoCollectionChanged;
                //((INotifyCollectionChanged)Patient.PatientPhoto).CollectionChanged -= PatientPhotoCollectionChanged;

                ((INotifyCollectionChanged)old.PatientPhoto).CollectionChanged -= PatientPhotoCollectionChanged;
            }
        }

        void AddCollectionChangedHandler()
        {
            //var addMessage = String.Format("Adding delegate for patient key: {0}", Patient.PatientKey);
            //var new_ref = (_photosViewSource.Source as EntityCollection<PatientPhoto>);
            //if (new_ref != null)
            //    ((INotifyCollectionChanged)new_ref).CollectionChanged -= PatientPhotoCollectionChanged;
            ((INotifyCollectionChanged)Patient.PatientPhoto).CollectionChanged += new NotifyCollectionChangedEventHandler(PatientPhotoCollectionChanged);
        }

        void PatientPhotoCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateUI();
        }

        void _photosViewSource_Filter(object sender, FilterEventArgs e)
        {
            var photo = e.Item as PatientPhoto;
            e.Accepted = (photo.HistoryKey == null);
        }

        void UpdateUI()
        {
            this.RaisePropertyChanged("HavePhotos");
            this.RaisePropertyChanged("DefaultPhoto");
            this.RaisePropertyChanged("Patient");

            if (_photosViewSource.View != null)  //View will be NULL when newly selected patient is NULL, so do not call Refresh()
                _photosViewSource.View.Refresh();

            this.RaisePropertyChanged("FilteredPhotos");
            this.RaisePropertyChanged("Photo");
        }

        void InitializeDefaultImages()
        {
            var appFeatures = VirtuosoContainer.Current.GetInstance<IAppFeatures>();
            defaultMaleImage = appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/patient-male-generic.jpg");
            defaultFemaleImage = appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/patient-female-generic.jpg");
            defaultOtherImage = appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/logo.png");            
        }

        public PatientPhotoUserControl()
        {
            InitializeComponent();

            _photosViewSource = new CollectionViewSource();
            _photosViewSource.Filter += new FilterEventHandler(_photosViewSource_Filter);

            InitializeDefaultImages();
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
