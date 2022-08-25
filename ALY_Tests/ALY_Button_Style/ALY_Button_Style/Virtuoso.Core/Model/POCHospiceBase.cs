#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class POCHospiceBase : QuestionUI
    {
        public EncounterPlanOfCare EncounterPlanOfCare { get; set; }
        public AdmissionPhysicianFacade AdmissionPhysician { get; set; }
        public EncounterAdmission EncounterAdmission { get; set; }

        public DateTime? VerbalSOCDate
        {
            get
            {
                if ((EncounterAdmission != null) && (EncounterAdmission.VerbalSOCDate != null))
                {
                    return EncounterAdmission.VerbalSOCDate;
                }

                if ((Admission != null) && (Admission.VerbalSOCDate != null))
                {
                    return Admission.VerbalSOCDate;
                }

                return null;
            }
            set
            {
                if (EncounterAdmission != null)
                {
                    EncounterAdmission.VerbalSOCDate = value;
                }

                if (Admission != null)
                {
                    Admission.VerbalSOCDate = value;
                }

                if (value.HasValue)
                {
                    ClearErrorFromProperty("VerbalSOCDate");
                }

                this.RaisePropertyChangedLambda(p => p.VerbalSOCDate);
            }
        }

        public POCHospiceBase(Admission admission, Encounter encounter, int? formSectionQuestionKey) : base(
            formSectionQuestionKey)
        {
            Messenger.Default.Register<int>(this,
                "AdmissionPhysician_FormUpdate",
                AdmissionKey => { AdmissionPhysician.RaiseEvents(); });

            Admission = admission;
            Encounter = encounter;
            bool useThisEncounter = Encounter.EncounterStatus == (int)EncounterStatusType.Completed;

            AdmissionPhysician = new AdmissionPhysicianFacade(useThisEncounter)
            {
                Admission = Admission,
                Encounter = Encounter
            };
            _AdmissionPhysicianList.Source = AdmissionPhysician.ActiveAdmissionPhysicians;
            PropertyChanged += POCBase_PropertyChanged;

            FilterPhysicians();
            SetupSelectedAdmissionPhysician();
        }

        public void GetOverrides()
        {
            if (EncounterAdmission == null
                && Question != null
                && Question.DataTemplate.StartsWith("POCHospicePhysicianV"))
            {
                EncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                if (EncounterAdmission == null)
                {
                    // new one up for PlanOfCareAddendum to interrogate
                    EncounterAdmission = new EncounterAdmission
                    {
                        EncounterKey = Encounter?.EncounterKey ?? -1
                    };
                    Encounter.EncounterAdmission.Add(EncounterAdmission);
                    EncounterAdmission.RefreshEncounterAdmissionFromAdmission(Admission, AdmissionPhysician,
                        AdmissionDiscipline); // Copy Admission data to Encounter
                    EncounterAdmission.AttendingPhysicianKey = OvrAttendingPhysicianKey;
                    SetEncounterAdmissionPhysicianAddressKey();
                }
            }

            if (EncounterAdmission != null && EncounterAdmission.AttendingPhysicianKey != OvrAttendingPhysicianKey)
            {
                OvrAttendingPhysicianKey = EncounterAdmission.AttendingPhysicianKey;
            }
        }

        public string GetPhysicianSignatureNarrative
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return null;
                }

                return EncounterPlanOfCare.PhysicianSignatureNarrative;
            }
        }

        public string EPOCPhysicianSignatureNarrative
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return null;
                }

                return EncounterPlanOfCare.PhysicianSignatureNarrative;
            }

            set
            {
                if (EncounterPlanOfCare == null)
                {
                    return;
                }

                EncounterPlanOfCare.PhysicianSignatureNarrative = value;
                RaisePropertyChanged("EPOCPhysicianSignatureNarrative");
            }
        }

        private void SetupSelectedAdmissionPhysician()
        {
            EncounterAdmission ea = Encounter.EncounterAdmission.FirstOrDefault();
            if ((ea != null)
                && (ea.AttendingPhysicianKey.HasValue)
               )
            {
                if (AdmissionPhysicianList != null)
                {
                    var myList = AdmissionPhysicianList.Cast<AdmissionPhysician>();
                    if (myList != null)
                    {
                        SelectedAdmissionPhysician = myList.FirstOrDefault(ap => ap.PhysicianKey == ea.AttendingPhysicianKey);
                    }
                }
            }

            if (SelectedAdmissionPhysician == null)
            {
                if (AdmissionPhysician == null)
                {
                    return;
                }

                SelectedAdmissionPhysician = AdmissionPhysician.AttendingAdmissionPhysician;
            }
        }

        private void RaiseSignedInformationPropertyChanged()
        {
            RaisePropertyChanged("PhyAddress1");
            RaisePropertyChanged("PhyAddress2");
            RaisePropertyChanged("PhyCityStateZip");
            RaisePropertyChanged("PhyPhoneNumber");
            RaisePropertyChanged("PhyFaxNumber");
            RaisePropertyChanged("OvrAttendingPhysicianKey");
        }

        public int? OvrAttendingPhysicianKey
        {
            get
            {
                return AdmissionPhysician.OverrideAttendingPhysicianKey == null
                    ? AdmissionPhysician.AttendingPhysicianKey
                    : AdmissionPhysician.OverrideAttendingPhysicianKey;
            }
            set
            {
                AdmissionPhysician.OverrideAttendingPhysicianKey = value;
                SetEncounterAdmissionPhysicianKey(value);
                SetupSigningMedicalDirector();
                Messenger.Default.Send(value, string.Format("OvrAttendingPhysicianKey{0}", Encounter.EncounterKey.ToString().Trim()));
            }
        }

        private AdmissionPhysician _selectedAdmissionPhysician;

        public AdmissionPhysician SelectedAdmissionPhysician
        {
            get { return _selectedAdmissionPhysician; }
            set
            {
                _selectedAdmissionPhysician = value;
                SetEncounterAdmissionPhysicianAddressKey();
                RaiseSignedInformationPropertyChanged();
            }
        }

        private void SetEncounterAdmissionPhysicianAddressKey()
        {
            if (Encounter == null)
            {
                return;
            }

            if (Encounter.EncounterAdmission == null)
            {
                return;
            }

            var ea = Encounter.EncounterAdmission.FirstOrDefault();
            if (ea == null)
            {
                return;
            }

            var addrKey = SelectedAdmissionPhysician?.PhysicianAddressKey;
            if (ea.AttendingPhysicianAddressKey != addrKey)
            {
                ea.AttendingPhysicianAddressKey = addrKey;
            }
        }

        private void SetEncounterAdmissionPhysicianKey(int? PhyKey)
        {
            if (Encounter == null)
            {
                return;
            }

            if (Encounter.EncounterAdmission == null)
            {
                return;
            }

            var ea = Encounter.EncounterAdmission.FirstOrDefault();
            if (ea == null)
            {
                return;
            }

            if (ea.AttendingPhysicianKey != PhyKey)
            {
                ea.AttendingPhysicianKey = PhyKey;
            }
        }

        public void SetEncounterAdmissionPreviousPhysicianKey()
        {
            if (Encounter == null)
            {
                return;
            }

            if (Encounter.EncounterAdmission == null)
            {
                return;
            }

            var ea = Encounter.EncounterAdmission.FirstOrDefault();
            if (ea == null)
            {
                return;
            }

            ea.PreviousAttendingPhysicianKey = ea.AttendingPhysicianKey;
        }

        public String PhyAddress1
        {
            get
            {
                if (AdmissionPhysician.AttendingPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (AdmissionPhysician.AttendingPhysician.MainAddress == null)
                    {
                        return null;
                    }

                    return AdmissionPhysician.AttendingPhysician.MainAddress.Address1;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.Address1;
            }
        }

        public String PhyAddress2
        {
            get
            {
                if (AdmissionPhysician.AttendingPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (AdmissionPhysician.AttendingPhysician.MainAddress == null)
                    {
                        return null;
                    }

                    return AdmissionPhysician.AttendingPhysician.MainAddress.Address2;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.Address2;
            }
        }

        public String PhyCityStateZip
        {
            get
            {
                if (AdmissionPhysician.AttendingPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (AdmissionPhysician.AttendingPhysician.MainAddress == null)
                    {
                        return null;
                    }

                    return AdmissionPhysician.AttendingPhysician.MainAddress.CityStateZip;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.CityStateZip;
            }
        }

        public String PhyPhoneNumber
        {
            get
            {
                if (AdmissionPhysician.AttendingPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    if (AdmissionPhysician.AttendingPhysician.MainPhone == null)
                    {
                        return null;
                    }

                    return AdmissionPhysician.AttendingPhysician.MainPhone.Number;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.PhoneNumber;
            }
        }

        public bool VerifiedSigned
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return false;
                }

                var ot = EncounterPlanOfCare.OrdersTracking.FirstOrDefault();
                int status = -1;
                if (ot != null)
                {
                    status = ot.Status;
                }

                return (status == (int)OrdersTrackingStatus.Signed || EncounterPlanOfCare.SignedDate.HasValue);
            }
            set
            {
                if (EncounterPlanOfCare == null)
                {
                    return;
                }

                if (value)
                {
                    OrdersTrackingManager.SetTrackingRowToSigned(EncounterPlanOfCare, (int)OrderTypesEnum.POC,
                        EncounterPlanOfCare.EncounterPlanOfCareKey);

                    RaisePropertyChanged("SignededByUserName");
                    RaisePropertyChanged("SignedDate");
                    RaisePropertyChanged("SentVerifiedGridEnabled");
                }
                else
                {
                    var ot = EncounterPlanOfCare.OrdersTracking.FirstOrDefault();

                    if (ot != null)
                    {
                        ot.Status = (int)OrdersTrackingStatus.Sent;
                        ot.StatusDate = DateTime.Now.Date;
                    }

                    EncounterPlanOfCare.SignedDate = null;
                    EncounterPlanOfCare.SignedBy = null;

                    RaisePropertyChanged("SignededByUserName");
                    RaisePropertyChanged("SignedDate");
                    RaisePropertyChanged("SentVerifiedGridEnabled");
                }
            }
        }

        private OrdersTrackingManager OrdersTrackingManager = new OrdersTrackingManager();

        public OrderEntry CurrentOrderEntry
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return null;
                }

                if (EncounterPlanOfCare.OrdersTracking.FirstOrDefault() == null)
                {
                    return null;
                }

                return EncounterPlanOfCare.OrdersTracking.FirstOrDefault().OrderEntry;
            }
        }

        public String PhyFaxNumber
        {
            get
            {
                if (AdmissionPhysician.AttendingPhysician == null)
                {
                    return null;
                }

                if (SelectedAdmissionPhysician == null)
                {
                    var FaxRow = AdmissionPhysician.AttendingPhysician.GetPhoneByType("FAX");
                    if (FaxRow == null)
                    {
                        return null;
                    }

                    return FaxRow.Number;
                }

                return SelectedAdmissionPhysician.PhysicianAddressOrMain.Fax;
            }
        }

        #region MedicalDirectorSignature

        public void SetupSigningMedicalDirector()
        {
            if ((CurrentEncounterPlanOfCare == null) || (Question == null) ||
                (Question.DataTemplate.StartsWith("POCHospicePhysicianV") == false))
            {
                return;
            }

            if ((Encounter == null) || (Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed))
            {
                return;
            }

            if (ShowMedicalDirector == false)
            {
                CurrentEncounterPlanOfCare.SigningMedicalDirectorKey = null;
            }
            else if ((CurrentEncounterPlanOfCare.SigningMedicalDirectorKey == null) &&
                     (AdmissionPhysician.ActiveAdmissionPhysicians != null))
            {
                AdmissionPhysician apMedicalDirector = AdmissionPhysician
                    .ActiveAdmissionPhysicians
                    .FirstOrDefault(p => ((p.PhysicianKey != OvrAttendingPhysicianKey) 
                                          && (p.PhysicianType == CodeLookupCache.GetKeyFromCode("PHTP", "MedDirect"))));
                if (apMedicalDirector != null)
                {
                    CurrentEncounterPlanOfCare.SigningMedicalDirectorKey = apMedicalDirector.AdmissionPhysicianKey;
                }
            }

            RaisePropertyChanged("ShowMedicalDirector");
            RaiseSignMedicalDirectorPropertiesChanged();
        }

        public bool ShowMedicalDirector
        {
            get
            {
                if ((Question == null) || (Question.DataTemplate.StartsWith("POCHospicePhysicianV") == false))
                {
                    return false;
                }

                if ((Encounter == null) || (CurrentEncounterPlanOfCare == null) || (Admission == null) ||
                    (AdmissionPhysician == null) || (Admission.AdmissionPhysician == null) ||
                    (Admission.HospiceAdmission == false))
                {
                    return false;
                }

                if ((Encounter.PreviousEncounterStatus == (int)EncounterStatusType.Completed) &&
                    (SigningMedicalDirectorKey != null))
                {
                    return true;
                }

                if ((OvrAttendingPhysicianKey == null) || (AdmissionPhysician.ActiveAdmissionPhysicians == null))
                {
                    return false;
                }

                if (AdmissionPhysician.ActiveAdmissionPhysicians.Any(p => ((p.PhysicianKey == OvrAttendingPhysicianKey) 
                                                                           && (p.PhysicianType == CodeLookupCache.GetKeyFromCode("PHTP", "MedDirect")))))
                {
                    return false;
                }

                return true;
            }
        }

        private EncounterPlanOfCare CurrentEncounterPlanOfCare
        {
            get
            {
                if (Encounter == null)
                {
                    return null;
                }

                EncounterPlanOfCare ep = (Encounter.EncounterPlanOfCare == null)
                    ? null
                    : Encounter.EncounterPlanOfCare.FirstOrDefault();
                return ep;
            }
        }

        public int? SigningMedicalDirectorKey
        {
            get
            {
                if (CurrentEncounterPlanOfCare == null)
                {
                    return null;
                }

                return CurrentEncounterPlanOfCare.SigningMedicalDirectorKey;
            }
        }

        public int? SigningMedicalDirectorPhysicianKey
        {
            get
            {
                if (SigningMedicalDirector == null)
                {
                    return null;
                }

                return SigningMedicalDirector.PhysicianKey;
            }
        }

        private AdmissionPhysician SigningMedicalDirector
        {
            get
            {
                if (SigningMedicalDirectorKey == null)
                {
                    return null;
                }

                if ((Admission == null) || (Admission.AdmissionPhysician == null))
                {
                    return null;
                }

                return Admission.AdmissionPhysician.FirstOrDefault(ap => ap.AdmissionPhysicianKey == (int)SigningMedicalDirectorKey);
            }
        }

        public String SignMDPhyAddress1
        {
            get
            {
                if ((SigningMedicalDirectorKey == null) || (SigningMedicalDirector == null))
                {
                    return null;
                }

                return SigningMedicalDirector.PhysicianAddressOrMain.Address1;
            }
        }

        public String SignMDPhyAddress2
        {
            get
            {
                if ((SigningMedicalDirectorKey == null) || (SigningMedicalDirector == null))
                {
                    return null;
                }

                return SigningMedicalDirector.PhysicianAddressOrMain.Address2;
            }
        }

        public String SignMDPhyCityStateZip
        {
            get
            {
                if ((SigningMedicalDirectorKey == null) || (SigningMedicalDirector == null))
                {
                    return null;
                }

                return SigningMedicalDirector.PhysicianAddressOrMain.CityStateZip;
            }
        }

        public String SignMDPhyPhoneNumber
        {
            get
            {
                if ((SigningMedicalDirectorKey == null) || (SigningMedicalDirector == null))
                {
                    return null;
                }

                return SigningMedicalDirector.PhysicianAddressOrMain.PhoneNumber;
            }
        }

        public String SignMDPhyFaxNumber
        {
            get
            {
                if ((SigningMedicalDirectorKey == null) || (SigningMedicalDirector == null))
                {
                    return null;
                }

                return SigningMedicalDirector.PhysicianAddressOrMain.Fax;
            }
        }

        public FaxingPhysician? GetFaxingPhysician()
        {
            // NOTE: Hospice uses AttendingPhysician (HomeHealth uses SigningPhysician)
            if (SelectedAdmissionPhysician != null)
            {
                PhysicianAddress physicianAddress =
                    PhysicianCache.Current.GetPhysicianAddressFromKey(SelectedAdmissionPhysician.PhysicianAddressKey);
                if (physicianAddress != null && string.IsNullOrWhiteSpace(physicianAddress.Fax) == false)
                {
                    return new FaxingPhysician
                        { PhysicianKey = SelectedAdmissionPhysician.PhysicianKey, FaxNumber = physicianAddress.Fax };
                }
            }

            return null;
        }

        private void RaiseSignMedicalDirectorPropertiesChanged()
        {
            RaisePropertyChanged("SigningMedicalDirectorKey");
            RaisePropertyChanged("SigningMedicalDirectorPhysicianKey");
            RaisePropertyChanged("SignMDPhyAddress1");
            RaisePropertyChanged("SignMDPhyAddress2");
            RaisePropertyChanged("SignMDPhyCityStateZip");
            RaisePropertyChanged("SignMDPhyPhoneNumber");
            RaisePropertyChanged("SignMDPhyFaxNumber");
        }

        #endregion MedicalDirectorSignature

        public bool ProtectedPhysician
        {
            get
            {
                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                // no need to override protection if its already not protected
                if (Protected == false)
                {
                    return false;
                }

                if (DynamicFormViewModel == null || DynamicFormViewModel.CurrentForm == null)
                {
                    return true;
                }

                return Encounter.CanEditPOCProtectedPhysician ? false : true;
            }
        }

        public bool ProtectedVerbalSOC
        {
            get
            {
                if (Encounter == null)
                {
                    return true;
                }

                // Everything is protected on an inactive form
                if (Encounter.Inactive)
                {
                    return true;
                }

                // fall into default protection processing
                // the clinician who 'owns' the form can edit it if its in one of the clinical edit states
                if (Encounter.EncounterBy == WebContext.Current.User.MemberID)
                {
                    if (Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEdit)
                    {
                        return false;
                    }

                    if (Encounter.EncounterStatus == (int)EncounterStatusType.OASISReviewEditRR)
                    {
                        return false;
                    }
                }

                if (Encounter.UserIsPOCOrderReviewerAndInPOCOrderReview)
                {
                    return false;
                }

                //Not saved to server, so allow edits regardless of status
                if (Encounter.EncounterKey <= 0)
                {
                    return false;
                }

                return Protected;
            }
        }

        private CollectionViewSource _AdmissionPhysicianList = new CollectionViewSource();
        public ICollectionView AdmissionPhysicianList => _AdmissionPhysicianList.View;

        private void FilterPhysicians()
        {
            List<AdmissionPhysician> apl = new List<AdmissionPhysician>();
            AdmissionPhysicianList.Filter = item =>
            {
                AdmissionPhysician adp = item as AdmissionPhysician;
                if (adp == null)
                {
                    return false;
                }

                // Filter out duplicates
                if (apl.Any(ph => ph.PhysicianKey == adp.PhysicianKey && ph.PhysicianAddressKey == adp.PhysicianAddressKey))
                {
                    return false;
                }

                apl.Add(adp);

                return true;
            };
        }

        void POCBase_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Admission")
            {
                AdmissionPhysician.Admission = Admission;
            }

            if (e.PropertyName == "Encounter")
            {
                AdmissionPhysician.Encounter = Encounter;
            }
        }

        public override void Cleanup()
        {
            Messenger.Default.Unregister<int>(this, "AdmissionPhysician_FormUpdate");
            Messenger.Default.Unregister(this);

            PropertyChanged -= POCBase_PropertyChanged;

            if (AdmissionPhysician != null)
            {
                if (AdmissionPhysician.Encounter != null)
                {
                    AdmissionPhysician.Encounter.Cleanup();
                }
            }

            base.Cleanup();
        }

        public override void ClearEntity()
        {
        }

        // always show for PlanOfCare - hide for anything else (e.g., TeamMeeting)
        public bool ShowPhysicianSignature => Encounter.EncounterIsPlanOfCare ? true : false;

        public bool ShowPrintDate
        {
            get
            {
                bool showPrintDate = false;

                if (Encounter != null)
                {
                    // always hide for anything other than PlanOfCare (e.g., TeamMeeting
                    if (Encounter.EncounterIsPlanOfCare)
                    {
                        showPrintDate = Encounter.EncounterStatus == (int)EncounterStatusType.Completed;
                    }
                }

                return showPrintDate;
            }
        }

        public bool SentVerifiedGridEnabled
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return false;
                }

                return EncounterPlanOfCare.SignedDate.HasValue == false &&
                       EncounterPlanOfCare.SignedBy.HasValue == false;
            }
        }

        public bool SentVerifiedGridVisibility
        {
            get
            {
                if (EncounterPlanOfCare == null)
                {
                    return false;
                }

                var a = EncounterPlanOfCare.MailedBy.HasValue;
                var b = EncounterPlanOfCare.MailedDate.HasValue;
                var c = CurrentOrderEntry != null && CurrentOrderEntry.OrderStatus >= 3;
                var d = CurrentOrderEntry != null && CurrentOrderEntry.OrdersTracking.FirstOrDefault() != null &&
                        CurrentOrderEntry.OrdersTracking.FirstOrDefault().Status >= 40;

                return a || b || c || d;
            }
        }

        public bool ShowMailedDate
        {
            get
            {
                if ((Encounter == null) || (Encounter.EncounterPlanOfCare == null))
                {
                    return false;
                }

                EncounterPlanOfCare epoc = Encounter.EncounterPlanOfCare.FirstOrDefault();
                if (epoc == null)
                {
                    return false;
                }

                if ((epoc.MailedDate != null) || (epoc.MailedBy != null))
                {
                    return true;
                }

                return false;
            }
        }

        public bool ShowSignedDate
        {
            get
            {
                if ((Encounter == null) || (Encounter.EncounterPlanOfCare == null))
                {
                    return false;
                }

                EncounterPlanOfCare epoc = Encounter.EncounterPlanOfCare.FirstOrDefault();
                if (epoc == null)
                {
                    return false;
                }

                if ((epoc.SignedDate != null) || (epoc.SignedBy != null))
                {
                    return true;
                }

                return false;
            }
        }

        public DateTime? PrintedDate
        {
            get
            {
                DateTime? printDate = null;

                if (Encounter != null)
                {
                    if ((Encounter.EncounterPlanOfCare != null) &&
                        (Encounter.EncounterPlanOfCare.FirstOrDefault() != null))
                    {
                        if (Encounter.EncounterPlanOfCare.FirstOrDefault().IsPrinted)
                        {
                            if (Encounter.EncounterPlanOfCare.FirstOrDefault().PrintedDate.HasValue)
                            {
                                DateTimeOffset sourceTime =
                                    Encounter.EncounterPlanOfCare.FirstOrDefault().PrintedDate.Value;
                                printDate = sourceTime.DateTime;
                            }
                        }
                    }
                }

                return printDate;
            }
        }

        public DateTime? MailedDate
        {
            get
            {
                DateTime? mailedDate = null;

                if (Encounter != null)
                {
                    if ((Encounter.EncounterPlanOfCare != null) &&
                        (Encounter.EncounterPlanOfCare.FirstOrDefault() != null))
                    {
                        if (Encounter.EncounterPlanOfCare.FirstOrDefault().MailedDate.HasValue)
                        {
                            DateTimeOffset sourceTime = Encounter.EncounterPlanOfCare.FirstOrDefault().MailedDate.Value;
                            mailedDate = sourceTime.DateTime;
                        }
                    }
                }

                return mailedDate;
            }
            set
            {
                if ((Encounter != null) && (Encounter.EncounterPlanOfCare != null))
                {
                    EncounterPlanOfCare current = Encounter.EncounterPlanOfCare.FirstOrDefault();
                    if (current == null)
                    {
                        return;
                    }

                    current.MailedDate = value == null ? (DateTimeOffset?)null : value.Value.Date;
                    current.MailedBy = (current.MailedDate.HasValue)
                        ? UserCache.Current.GetCurrentUserProfile().UserId
                        : (Guid?)null;
                    RaisePropertyChanged("MailedByUserName");
                }
            }
        }

        public DateTime? SignedDate
        {
            get
            {
                DateTime? signedDate = null;

                if (Encounter != null)
                {
                    if ((Encounter.EncounterPlanOfCare != null) &&
                        (Encounter.EncounterPlanOfCare.FirstOrDefault() != null))
                    {
                        if (Encounter.EncounterPlanOfCare.FirstOrDefault().SignedDate.HasValue)
                        {
                            DateTimeOffset sourceTime = Encounter.EncounterPlanOfCare.FirstOrDefault().SignedDate.Value;
                            signedDate = sourceTime.DateTime;
                        }
                    }
                }

                return signedDate;
            }
            set
            {
                if ((Encounter != null) && (Encounter.EncounterPlanOfCare != null))
                {
                    EncounterPlanOfCare current = Encounter.EncounterPlanOfCare.FirstOrDefault();
                    if (current == null)
                    {
                        return;
                    }

                    current.SignedDate = value == null ? (DateTimeOffset?)null : value.Value.Date;
                    current.SignedBy = (current.SignedDate.HasValue)
                        ? UserCache.Current.GetCurrentUserProfile().UserId
                        : (Guid?)null;
                    RaisePropertyChanged("SignededByUserName");
                }
            }
        }

        public string AddendumText { get; set; }

        public string PrintedByUserName
        {
            get
            {
                string name = null;

                if (Encounter != null)
                {
                    if ((Encounter.EncounterPlanOfCare != null) &&
                        (Encounter.EncounterPlanOfCare.FirstOrDefault() != null))
                    {
                        if (Encounter.EncounterPlanOfCare.FirstOrDefault().PrintedBy.HasValue)
                        {
                            name = UserCache.Current.GetFullNameFromUserId(Encounter.EncounterPlanOfCare
                                .FirstOrDefault().PrintedBy);
                        }
                    }
                }

                return name;
            }
        }

        public string MailedByUserName
        {
            get
            {
                string name = null;

                if (Encounter != null)
                {
                    if ((Encounter.EncounterPlanOfCare != null) &&
                        (Encounter.EncounterPlanOfCare.FirstOrDefault() != null))
                    {
                        if (Encounter.EncounterPlanOfCare.FirstOrDefault().MailedBy.HasValue)
                        {
                            name = UserCache.Current.GetFullNameFromUserId(Encounter.EncounterPlanOfCare
                                .FirstOrDefault().MailedBy);
                        }
                    }
                }

                return name;
            }
        }

        public string SignededByUserName
        {
            get
            {
                string name = null;

                if (Encounter != null)
                {
                    if ((Encounter.EncounterPlanOfCare != null) &&
                        (Encounter.EncounterPlanOfCare.FirstOrDefault() != null))
                    {
                        if (Encounter.EncounterPlanOfCare.FirstOrDefault().SignedBy.HasValue)
                        {
                            name = UserCache.Current.GetFullNameFromUserId(Encounter.EncounterPlanOfCare
                                .FirstOrDefault().SignedBy);
                        }
                    }
                }

                return name;
            }
        }

        public void RefreshPageBindings()
        {
            RaisePropertyChanged("ShowMailedDate");
            RaisePropertyChanged("ShowSignedDate");
            RaisePropertyChanged("SignededByUserName");
            RaisePropertyChanged("SignedDate");
            RaisePropertyChanged("MailedByUserName");
            RaisePropertyChanged("MailedDate");
            RaisePropertyChanged("PrintedByUserName");
            RaisePropertyChanged("PrintedDate");
            RaisePropertyChanged("ShowPrintDate");
            RaisePropertyChanged("SentVerifiedGridVisibility");
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            if (EncounterPlanOfCare == null)
            {
                return true;
            }

            if (Question.DataTemplate.Equals("POCMain"))
            {
                EncounterPlanOfCare.ValidationErrors.Clear();
                if (!EncounterPlanOfCare.Validate())
                {
                    return false;
                }
            }
            else if (Question.DataTemplate.StartsWith("POCHospicePhysicianV"))
            {
                // I don't like this much, but we need this structure to exist prior to returning to Dynammic form if the
                // Signing physician has been overridden.
                if (EncounterAdmission == null)
                {
                    EncounterAdmission = Encounter.EncounterAdmission.FirstOrDefault();
                    if (EncounterAdmission == null)
                    {
                        EncounterAdmission = new EncounterAdmission();
                        Encounter.EncounterAdmission.Add(EncounterAdmission);
                        EncounterAdmission.RefreshEncounterAdmissionFromAdmission(Admission, AdmissionPhysician,
                            AdmissionDiscipline); // Copy Admission data to Encounter
                    }
                }

                EncounterAdmission.AttendingPhysicianKey = OvrAttendingPhysicianKey;
                if (AdmissionPhysician.AttendingPhysicianKey == null)
                {
                    Encounter.ValidationErrors.Add(new ValidationResult("Attending Physician is required",
                        new[] { "OvrAttendingPhysicianKey", "AttendingPhysicianKey" }));
                    return false;
                }

                if (string.IsNullOrWhiteSpace(Encounter.POCPhysicianSignatureAddendumText) == false)
                {
                    EncounterPlanOfCare ep = (Encounter.EncounterPlanOfCare == null)
                        ? null
                        : Encounter.EncounterPlanOfCare.FirstOrDefault();
                    if (ep != null)
                    {
                        ep.UpdatedDate =
                            DateTime.UtcNow; // to trigger the re-interface of the POC signing physician change
                    }
                }

                if (Question.DataTemplate.Equals("POCPhysician"))
                {
                    EncounterPlanOfCare ep = (Encounter.EncounterPlanOfCare == null)
                        ? null
                        : Encounter.EncounterPlanOfCare.FirstOrDefault();
                    if ((ep != null) && (string.IsNullOrWhiteSpace(ep.PhysicianSignatureNarrative) == false) &&
                        ep.PhysicianSignatureNarrative.ToLower().Contains("(date)") && (ProtectedPhysician == false) &&
                        Encounter.Signed)
                    {
                        ep.ValidationErrors.Add(new ValidationResult(
                            "The certification statement cannot contain the string '(Date)'.  You must either replace it with the Face-to-Face date or change the statement to represent whatever is appropriate for this Plan of Care",
                            new[] { "PhysicianSignatureNarrative" }));
                        return false;
                    }
                }
            }
            else if (Question.DataTemplate.Equals("POCProvider") && (Encounter.FullValidation) &&
                     (IsHospiceAdmission == false))
            {
                // if somehow the VerbalSOCDate exists in EncounterAdmission but not in the admission??  Set the Admission VerbalSOCDate to the one in EncounterAdmission
                if (VerbalSOCDate != null && Admission != null && Admission.VerbalSOCDate == null)
                {
                    Admission.VerbalSOCDate = VerbalSOCDate;
                }

                if (VerbalSOCDate == null)
                {
                    Admission.ValidationErrors.Add(new ValidationResult("Verbal SOC Date is required",
                        new[] { "VerbalSOCDate" }));
                    if (EncounterAdmission != null)
                    {
                        EncounterAdmission.ValidationErrors.Add(new ValidationResult("Verbal SOC Date is required",
                            new[] { "VerbalSOCDate" }));
                    }

                    AddErrorForProperty("VerbalSOCDate", "Verbal SOC Date is required");
                    return false;
                }
            }

            return true;
        }
    }

    public class POCHospiceBaseFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterPlanOfCare epc = vm.CurrentEncounter.EncounterPlanOfCare.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);
            EncounterAdmission eadmit = vm.CurrentEncounter.EncounterAdmission.FirstOrDefault(x => x.EncounterKey == vm.CurrentEncounter.EncounterKey);

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            POCHospiceBase pm = new POCHospiceBase(vm.CurrentAdmission, vm.CurrentEncounter, __FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                OasisManager = vm.CurrentOasisManager,
                EncounterPlanOfCare = epc,
                EncounterAdmission = eadmit,
                DynamicFormViewModel = vm
            };
            pm.GetOverrides();
            pm.SetEncounterAdmissionPreviousPhysicianKey();

            pm.SetupSigningMedicalDirector();

            return pm;
        }
    }
}