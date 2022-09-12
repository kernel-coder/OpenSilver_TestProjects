using GalaSoft.MvvmLight.Command;
using Newtonsoft.Json;
using OpenRiaServices.DomainServices.Client;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System;
using System.Windows;
using Virtuoso.Server.Data;
using Virtuoso.Core.ViewModel;
using System.ServiceModel.Channels;

namespace Virtuoso.Maintenance.Controls
{
    public partial class PatientPainScaleUserControl
    {
        public PatientPainScaleUserControl()
        {
            InitializeComponent();

#if OPENSILVER
            GridRoot.CustomLayout = true;
            GridRoot.ClipToBounds = true;
#endif

            DataContext = new PainScale();
        }

        public object EncounterPain
        {
            get { return (object)GetValue(EncounterPainProperty); }
            set { SetValue(EncounterPainProperty, value); }
        }

        public static readonly DependencyProperty EncounterPainProperty =
            DependencyProperty.Register("EncounterPain", typeof(object), typeof(PatientPainScaleUserControl),
                null);
    }

    public class PainScale : GenericBase
    {

        private int previousscore = -1;
        private int previoustarget = -1;

        public RelayCommand ProcessGoals { get; protected set; }
        public RelayCommand PainFaces_Command { get; protected set; }
        public RelayCommand PainFacesOKButton_Click { get; protected set; }
        public RelayCommand PainFacesCancelButton_Click { get; protected set; }

        public RelayCommand PainPAINAD_Command { get; protected set; }
        public RelayCommand PainPAINADOKButton_Click { get; protected set; }
        public RelayCommand PainPAINADCancelButton_Click { get; protected set; }

        public RelayCommand PainFLACC_Command { get; protected set; }
        public RelayCommand PainFLACCOKButton_Click { get; protected set; }
        public RelayCommand PainFLACCCancelButton_Click { get; protected set; }

        public PainScale() : base()
        {
            ProcessGoals = new RelayCommand(() =>
            {
                string response = string.Empty;
                string subresponse = string.Empty;
                string reason = string.Empty;
                bool remove = true;
                int? keytouse = null;

                //we now collect the target value so pass it allow to the goal
            });

            SetupCommands();
        }

        private void this_EncounterPainPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName.Equals("PainScale")) ||
                (e.PropertyName.Equals("PainScore10")) ||
                (e.PropertyName.Equals("PainScoreFLACC")) ||
                (e.PropertyName.Equals("PainScorePAINAD")) ||
                (e.PropertyName.Equals("PainScoreFACES")))
            {
            }
        }

        private bool _IsFACES;

        public bool IsFACES
        {
            get { return _IsFACES; }
            set
            {
                if (_IsFACES != value)
                {
                    _IsFACES = value;
                    this.RaisePropertyChangedLambda(p => p.IsFACES);
                }
            }
        }

        private bool _IsFLACC;

        public bool IsFLACC
        {
            get { return _IsFLACC; }
            set
            {
                if (_IsFLACC != value)
                {
                    _IsFLACC = value;
                    this.RaisePropertyChangedLambda(p => p.IsFLACC);
                }
            }
        }

        private bool _IsPAINAD;

        public bool IsPAINAD
        {
            get { return _IsPAINAD; }
            set
            {
                if (_IsPAINAD != value)
                {
                    _IsPAINAD = value;
                    this.RaisePropertyChangedLambda(p => p.IsPAINAD);
                }
            }
        }

        private int? _prevPainScoreFACES;
        private int? _prevPainScoreFLACC;
        private int? _prevPainScoreFLACCActivity;
        private int? _prevPainScoreFLACCConsole;
        private int? _prevPainScoreFLACCCry;
        private int? _prevPainScoreFLACCFace;
        private int? _prevPainScoreFLACCLegs;

        private int? _prevPainScorePAINAD;
        private int? _prevPainScorePAINADBreathing;
        private int? _prevPainScorePAINADCry;
        private int? _prevPainScorePAINADFace;
        private int? _prevPainScorePAINADActivity;
        private int? _prevPainScorePAINADConsole;

        private void SetupCommands()
        {
            PainFaces_Command = new RelayCommand(
                () => { PainFacesPopup(); });
            PainFacesOKButton_Click = new RelayCommand(
                () =>
                {
                    IsFACES = false;
                    ProcessGoals.Execute(null);
                });
            PainFacesCancelButton_Click = new RelayCommand(
                () =>
                {
                    IsFACES = false;
                });
            PainFLACC_Command = new RelayCommand(
                () => { PainFLACCPopup(); });
            PainFLACCOKButton_Click = new RelayCommand(
                () =>
                {
                    IsFLACC = false;
                    ProcessGoals.Execute(null);
                });
            PainFLACCCancelButton_Click = new RelayCommand(
                () =>
                {
                    IsFLACC = false;
                });
            PainPAINAD_Command = new RelayCommand(
                () => { PainPAINADPopup(); });

            PainPAINADOKButton_Click = new RelayCommand(
                () =>
                {

                    IsPAINAD = false;
                    ProcessGoals.Execute(null);
                });
            PainPAINADCancelButton_Click = new RelayCommand(
                () =>
                {

                    IsPAINAD = false;
                });
        }

        public void PainFLACCPopup()
        {
            IsFLACC = true;
        }

        public void PainPAINADPopup()
        {
            IsPAINAD = true;
        }

        public void PainFacesPopup()
        {
            IsFACES = true;
        }


    }
}