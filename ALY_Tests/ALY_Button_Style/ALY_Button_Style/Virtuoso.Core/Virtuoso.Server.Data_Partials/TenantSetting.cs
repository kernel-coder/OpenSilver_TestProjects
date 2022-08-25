#region Usings

using System;
using System.Linq;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class TenantSetting
    {
        private bool _ExternalAuthenticationSecretChanged;


        public bool UseScheduling_SetAtServiceLine
        {
            get
            {
                var isSet = ServiceLineCache.GetActiveServiceLines().Any(sl => sl.UseScheduling);
                return isSet;
            }
        }

        public bool NonHospicePreEvalRequired_SetAtServiceLine
        {
            get
            {
                var isSet = ServiceLineCache.GetActiveServiceLines().Any(sl => sl.NonHospicePreEvalRequired);
                return isSet;
            }
        }

        public string StateCodeCode => CodeLookupCache.GetCodeFromKey(StateCode);

        public DateTime? PreviousDischargeWorklistStartDate { get; set; }
        public DateTime? PreviousTransferWorklistStartDate { get; set; }

        public bool ExternalAuthenticationSecretChanged
        {
            get { return _ExternalAuthenticationSecretChanged; }
            set
            {
                if (_ExternalAuthenticationSecretChanged != value)
                {
                    _ExternalAuthenticationSecretChanged = value;
                    RaisePropertyChanged("ExternalAuthenticationSecretChanged");
                }
            }
        }

        partial void OnUsingOrderEntryReviewersChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (UsingOrderEntryReviewers == false)
            {
                ServiceOrdersHeldUntilReviewed = false;
            }
        }

        partial void OnPurchasedHospiceChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (PurchasedHospice == false)
            {
                HospicePreEvalRequired = false;
                HospiceOnly = false;
                HISCoordinatorCanEdit = false;
                UsingHISCoordinator = false;
            }
        }

        partial void OnHospiceOnlyChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (HospiceOnly)
            {
                NonHospicePreEvalRequired = false;
            }
        }

        public void RaiseMyPropertyChanged()
        {
            RaisePropertyChanged("ICD10PresentDate");
            RaisePropertyChanged("ICD10RequiredDate");
            RaisePropertyChanged("ICD9CessationDate");
        }

        partial void OnICD10PresentDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ICD10PresentDate == null)
            {
                ICD10PresentDate = new DateTime(2013, 10, 01);
                RaisePropertyChanged("ICD10PresentDate");
            }
        }

        partial void OnICD10RequiredDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ICD10RequiredDate == null)
            {
                ICD10RequiredDate = new DateTime(2014, 09, 01);
                RaisePropertyChanged("ICD10RequiredDate");
            }
        }

        partial void OnICD9CessationDateChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (ICD9CessationDate == null)
            {
                ICD9CessationDate = new DateTime(2015, 10, 01);
                RaisePropertyChanged("ICD9CessationDate");
            }
        }


        partial void OnDisciplineRecertWindowChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (DisciplineRecertWindow == null)
            {
                DisciplineRecertWindow = 7;
            }
        }

        //NOTE: PrintInterimOrderForMailing functions as client side only state for whether to allow display and edit of EnvelopeWindow
        partial void OnPrintInterimOrderForMailingChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (PrintInterimOrderForMailing == false)
            {
                EnvelopeWindow = null; //clear EnvelopeWindow if clearing PrintInterimOrderForMailing
            }
        }

        partial void OnUsingDischargeWorklistChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (UsingDischargeWorklist == false)
            {
                DischargeWorklistStartDate = null;
            }
        }

        partial void OnDischargeWorklistStartDateChanged()
        {
            if (IsDeserializing && DischargeWorklistStartDate != null)
            {
                PreviousDischargeWorklistStartDate = DischargeWorklistStartDate;
            }
        }

        partial void OnUsingTransferWorklistChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            if (UsingTransferWorklist == false)
            {
                TransferWorklistStartDate = null;
            }
        }

        partial void OnTransferWorklistStartDateChanged()
        {
            if (IsDeserializing && TransferWorklistStartDate != null)
            {
                PreviousTransferWorklistStartDate = TransferWorklistStartDate;
            }
        }

        partial void OnExternalAuthenticationSecretChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            ExternalAuthenticationSecretChanged = string.IsNullOrEmpty(ExternalAuthenticationSecret) ? false : true;
        }
    }
}