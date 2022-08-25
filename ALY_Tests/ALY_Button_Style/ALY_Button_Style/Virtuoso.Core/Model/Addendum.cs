#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Addendum : QuestionUI
    {
        public Addendum(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private const string _none = "'none'";
        public EncounterAddendum EncounterAddendum { get; set; }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            EncounterAddendum.ValidationErrors.Clear();
            if (String.IsNullOrWhiteSpace(AddendumText))
            {
                AddendumText = null;
            }

            if ((AddendumText == null) && String.IsNullOrWhiteSpace(OasisAddendum) &&
                String.IsNullOrWhiteSpace(NonOASISAddendum))
            {
                EncounterAddendum.ValidationErrors.Add(new ValidationResult("The Addendum field is required.",
                    new[] { "AddendumText" }));
                return false;
            }

            EncounterAddendum.AddendumText = AddendumText;
            if (EncounterAddendum.IsNew)
            {
                Encounter.EncounterAddendum.Add(EncounterAddendum);
            }

            if (string.IsNullOrWhiteSpace(OasisAddendum) == false)
            {
                EncounterAddendum.AddendumText = "(in " + Encounter.SYS_CDDescription + " Edit)   " +
                                                 EncounterAddendum.AddendumText + "\r" +
                                                 OasisAddendum.Replace("<LineBreak />", "\r");
            }

            if (string.IsNullOrWhiteSpace(NonOASISAddendum) == false)
            {
                EncounterAddendum.AddendumText =
                    ((AddendumText == "") ? "" : " " + EncounterAddendum.AddendumText + "\r") +
                    NonOASISAddendum.Replace("<LineBreak />", "\r");
            }

            return true;
        }

        private string _AddendumText;

        public string AddendumText
        {
            get { return _AddendumText; }
            set
            {
                _AddendumText = value;
                RaisePropertyChanged("AddendumText");
            }
        }

        public string OasisChangesLabel
        {
            get
            {
                if (Encounter == null)
                {
                    return "OASIS Changes";
                }

                return Encounter.SYS_CDDescription + " Changes";
            }
        }

        public bool ShowNewAddendum
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return false;
                }

                if (DynamicFormViewModel.IsReadOnlyEncounter)
                {
                    return false;
                }

                return (Protected) ? false : true;
            }
        }

        public string OasisAddendum
        {
            get
            {
                if (OasisManager == null)
                {
                    return null;
                }

                if (DynamicFormViewModel == null)
                {
                    return null;
                }

                if (DynamicFormViewModel.IsReadOnlyEncounter)
                {
                    return null;
                }

                return OasisManager.OasisAddendum(false);
            }
        }

        private string CR = char.ToString('\r');
        private string nonOASISAddendum;

        public string NonOASISAddendum
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return null;
                }

                if (DynamicFormViewModel.IsReadOnlyEncounter)
                {
                    return null;
                }

                string text = nonOASISAddendum;

                if ((OrderEntryManager != null) && !string.IsNullOrWhiteSpace(OrderEntryManager.AddendumText))
                {
                    if (!string.IsNullOrWhiteSpace(nonOASISAddendum))
                    {
                        text += CR;
                    }

                    text = text + OrderEntryManager.AddendumText.Replace("<LineBreak />", CR);
                }

                if (!string.IsNullOrWhiteSpace(Encounter.POCPhysicianSignatureAddendumText))
                {
                    if (!string.IsNullOrWhiteSpace(nonOASISAddendum))
                    {
                        text += CR;
                    }

                    text = text + Encounter.POCPhysicianSignatureAddendumText;
                }

                if ((DynamicFormViewModel != null) &&
                    (string.IsNullOrWhiteSpace(DynamicFormViewModel.POCSendAddendum) == false))
                {
                    if (!string.IsNullOrWhiteSpace(nonOASISAddendum))
                    {
                        text += CR;
                    }

                    text = text + DynamicFormViewModel.POCSendAddendum;
                }

                if ((DynamicFormViewModel != null) &&
                    (string.IsNullOrWhiteSpace(DynamicFormViewModel.POCSignedAddendum) == false))
                {
                    if (!string.IsNullOrWhiteSpace(nonOASISAddendum))
                    {
                        text += CR;
                    }

                    text = text + DynamicFormViewModel.POCSignedAddendum;
                }

                return text;
            }
            set { nonOASISAddendum = value; }
        }

        public bool IsEditablePOC
        {
            get
            {
                bool isEditablePOC = false;
                if (DynamicFormViewModel != null && DynamicFormViewModel.CurrentForm != null &&
                    DynamicFormViewModel.CurrentForm.IsPlanOfCare
                    && DynamicFormViewModel.CurrentForm.Description.Contains("Edit"))
                {
                    isEditablePOC = true;
                }

                return isEditablePOC;
            }
        }

        public bool AddendumHidden => Hidden || IsEditablePOC;

        public bool ProtectedAddendum
        {
            get
            {
                if (Encounter == null)
                {
                    return true;
                }

                if (Encounter.Inactive)
                {
                    return true;
                }

                return false;
            }
        }

        public string LogPropertyChanges(UserProfile userProfile, TenantSettingsCache tenantSettingCache,
            IEnumerable<PropertyChange<object>> propertyChanges)
        {
            var sb = new StringBuilder();
            foreach (var propertyChange in propertyChanges)
            {
                string logMsg = string.Empty;
                switch (propertyChange.PropertyDetails.Name)
                {
                    case "DischargeDateTime":
                    {
                        string origDischargeDate = propertyChange.OrigValue == null
                            ? _none
                            : ((DateTime)propertyChange.OrigValue).ToShortDateString();
                        string newDischargeDate = propertyChange.NewValue == null
                            ? _none
                            : ((DateTime)propertyChange.NewValue).ToShortDateString();
                        logMsg = string.Format("Discharge Date changed from {0} to {1}", origDischargeDate,
                            newDischargeDate);
                    }
                        break;
                    case "DischargeReasonKey":
                    {
                        //TODO: JS Get Description from CodeLookupCache
                        string origReason = propertyChange.OrigValue == null
                            ? _none
                            : CodeLookupCache.GetDescriptionFromCode("PATDISCHARGEREASON",
                                CodeLookupCache.GetCodeFromKey((int?)propertyChange.OrigValue));
                        string newReason = propertyChange.NewValue == null
                            ? _none
                            : CodeLookupCache.GetDescriptionFromCode("PATDISCHARGEREASON",
                                CodeLookupCache.GetCodeFromKey((int?)propertyChange.NewValue));

                        logMsg = string.Format("Discharge Reason changed from '{0}' to '{1}'", origReason, newReason);
                    }
                        break;
                }

                if (logMsg == string.Empty)
                {
                    continue;
                }

                //sb.AppendLine(string.Format("{0} by {1} {2} on {3} ", logMsg, userProfile.FirstName, 
                //        userProfile.LastName, DateTime.Now.ToShortDateString()));
                if (propertyChange.Location != null)
                {
                    logMsg += " at " + propertyChange.Location;
                }

                sb.AppendLine(logMsg);
            }

            //Instantiate the addendum with an empty string if needed
            nonOASISAddendum = (nonOASISAddendum ?? string.Empty) + sb;
            //return only the log lines created by this call, not the whole addendum (which could be the same, circumstantially)
            return sb.ToString();
        }

        public OrderEntryManager OrderEntryManager { get; set; }
        public RelayCommand OrderTrackingHistoryCommand { set; get; }

        public void AddendumSetup()
        {
            OrderTrackingHistoryCommand = new RelayCommand(() => { OrderTrackingHistory(); });
            ChangeHistoryListSetup();
        }

        public void ChangeHistoryListSetup()
        {
            if ((Admission == null) || (Admission.OrdersTracking == null) || (Encounter == null))
            {
                return;
            }

            OrdersTracking ot = null;
            // Get OrdersTracking from current Order, POC or COTI
            if (Encounter.CurrentOrderEntry != null)
            {
                ot = Admission.OrdersTracking.Where(o => o.OrderEntryKey == Encounter.CurrentOrderEntry.OrderEntryKey)
                    .FirstOrDefault();
            }

            if ((ot == null) && (Encounter.MyEncounterPlanOfCare != null))
            {
                ot = Admission.OrdersTracking
                    .Where(o => o.EncounterPlanOfCareKey == Encounter.MyEncounterPlanOfCare.EncounterPlanOfCareKey)
                    .FirstOrDefault();
            }

            AdmissionCOTI ac = (Encounter.AdmissionCOTI == null) ? null : Encounter.AdmissionCOTI.FirstOrDefault();
            if ((ot == null) && Encounter.EncounterIsCOTI && (ac != null))
            {
                ot = Admission.OrdersTracking.Where(o => o.AdmissionCOTIKey == ac.AdmissionCOTIKey).FirstOrDefault();
            }

            ChangeHistoryList = (ot == null)
                ? null
                : ot.ChangeHistory.Where(c => c.HasHistory).OrderByDescending(c => c.UpdatedDate).ToList();
            if ((ChangeHistoryList == null) || (ChangeHistoryList.Any() == false))
            {
                ChangeHistoryList = null;
            }
        }

        public bool ShowOrderTrackingHistoryCommand => (ChangeHistoryList == null) ? false : true;

        public void OrderTrackingHistory()
        {
            ShowOrderTrackingHistory = (ShowOrderTrackingHistory) ? false : true;
        }

        private bool _ShowOrderTrackingHistory;

        public bool ShowOrderTrackingHistory
        {
            get { return _ShowOrderTrackingHistory; }
            set
            {
                _ShowOrderTrackingHistory = value;
                this.RaisePropertyChangedLambda(p => p.ShowOrderTrackingHistory);
            }
        }

        private IEnumerable<ChangeHistory> _ChangeHistoryList;

        public IEnumerable<ChangeHistory> ChangeHistoryList
        {
            get { return _ChangeHistoryList; }
            set
            {
                _ChangeHistoryList = value;
                this.RaisePropertyChangedLambda(p => p.ChangeHistoryList);
            }
        }
    }

    public class AddendumFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            EncounterAddendum addendum = new EncounterAddendum();

            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Addendum ad = new Addendum(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                EncounterAddendum = addendum,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
                DynamicFormViewModel = vm
            };
            ad.AddendumSetup();
            if (ad.IsEditablePOC)
            {
                ad.ConditionalHidden = true;
            }

            return ad;
        }
    }
}