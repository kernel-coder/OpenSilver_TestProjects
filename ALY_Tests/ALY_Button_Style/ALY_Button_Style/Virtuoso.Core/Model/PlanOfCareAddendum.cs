#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class PlanOfCareAddendum : QuestionBase
    {
        public PlanOfCareAddendum(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        private List<OrderEntry> _OrdersFromOtherPhysiciansList;

        public List<OrderEntry> OrdersFromOtherPhysiciansList
        {
            get { return _OrdersFromOtherPhysiciansList; }
            set
            {
                _OrdersFromOtherPhysiciansList = value;
                this.RaisePropertyChangedLambda(p => p.OrdersFromOtherPhysiciansList);
                this.RaisePropertyChangedLambda(p => p.AreOrdersFromOtherPhysicians);
            }
        }

        public bool AreOrdersFromOtherPhysicians
        {
            get
            {
                if (OrdersFromOtherPhysiciansList == null)
                {
                    return false;
                }

                return (OrdersFromOtherPhysiciansList.Any() != false);
            }
        }

        private List<OrderEntry> _OrdersFromSigningPhysicianList;

        public List<OrderEntry> OrdersFromSigningPhysicianList
        {
            get { return _OrdersFromSigningPhysicianList; }
            set
            {
                _OrdersFromSigningPhysicianList = value;
                this.RaisePropertyChangedLambda(p => p.OrdersFromSigningPhysicianList);
                this.RaisePropertyChangedLambda(p => p.AreOrdersFromSigningPhysician);
            }
        }

        public bool AreOrdersFromSigningPhysician
        {
            get
            {
                if (OrdersFromSigningPhysicianList == null)
                {
                    return false;
                }

                return (OrdersFromSigningPhysicianList.Any() != false);
            }
        }

        public string PlanOfCareAddendumLabel
        {
            get
            {
                AdmissionCertification ac = DynamicFormViewModel.GetAdmissionCertForEncounter();
                DateTime periodStart = ((ac != null) && (ac.PeriodStartDate.HasValue))
                    ? (DateTime)ac.PeriodStartDate
                    : DateTime.MinValue;
                DateTime periodEnd = ((ac != null) && (ac.PeriodEndDate.HasValue))
                    ? (DateTime)ac.PeriodEndDate
                    : DateTime.MinValue;
                return string.Format("Plan of Care Addendum for {0} thru {1}",
                    ((periodStart == DateTime.MinValue) ? "?" : periodStart.ToShortDateString()),
                    ((periodEnd == DateTime.MinValue) ? "?" : periodEnd.ToShortDateString()));
            }
        }

        public string SigningPhysicianFullNameInformalWithSuffix
        {
            get
            {
                string physician =
                    PhysicianCache.Current.GetPhysicianFullNameInformalWithSuffixFromKey(SigningPhysicianKey);
                return (string.IsNullOrWhiteSpace(physician)) ? "Physician ?" : physician;
            }
        }

        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public void PlanOfCareAddendumSetup()
        {
            bool usingPOCAddendum = UsingPOCAddendum;
            Hidden = (!usingPOCAddendum);
            EncounterPlanOfCare ep = Encounter.EncounterPlanOfCare.FirstOrDefault();
            if (ep == null)
            {
                return;
            }

            ep.POCAddendum = usingPOCAddendum;
            if (usingPOCAddendum == false)
            {
                return;
            }

            Messenger.Default.Register<int?>(this, string.Format("OverrideSigningPhysicianKeyChanged{0}", Encounter.EncounterKey.ToString().Trim()), OverrideSigningPhysicianKeyChanged);
            if (Encounter.EncounterStatus == (int)EncounterStatusType.Edit)
            {
                BuildEncounterPlanOfCareOrder();
            }

            BuildPhysicianLists();
        }

        private bool UsingPOCAddendum
        {
            get
            {
                if ((Encounter == null) || (Encounter.EncounterPlanOfCare == null) ||
                    (Encounter.EncounterPlanOfCareOrder == null))
                {
                    return false;
                }

                EncounterPlanOfCare ep = Encounter.EncounterPlanOfCare.FirstOrDefault();
                if (ep == null)
                {
                    return false;
                }

                // override checks if we are already using POCAddendums (for legacy POCs)
                if (ep.POCAddendum)
                {
                    return true;
                }

                if (Encounter.EncounterPlanOfCareOrder.Any())
                {
                    return true;
                }

                // Fell thru - check setup
                if (Encounter.EncounterStatus != (int)EncounterStatusType.Edit)
                {
                    return false;
                }

                if ((Admission == null) || Admission.HospiceAdmission)
                {
                    return false;
                }

                if ((DynamicFormViewModel == null) || (DynamicFormViewModel.CurrentForm == null) ||
                    (DynamicFormViewModel.CurrentForm.IsPlanOfCare == false))
                {
                    return false;
                }

                if (TenantSettingsCache.Current.TenantSettingCreatePOCAddendum == false)
                {
                    return false;
                }

                return true;
            }
        }

        private void BuildEncounterPlanOfCareOrder()
        {
            if ((Admission == null) || (Admission.OrderEntry == null) || (Encounter == null) ||
                (Encounter.EncounterPlanOfCare == null) || (Encounter.EncounterPlanOfCareOrder == null))
            {
                return;
            }

            List<OrderEntry> oeEligibleList = new List<OrderEntry>();
            EncounterPlanOfCare ep = Encounter.EncounterPlanOfCare.FirstOrDefault();
            if (ep != null)
            {
                // Get Cert Cycle Start and End Dates to extact orders
                DateTime startDate = (ep.CertificationFromDate.HasValue)
                    ? ((DateTime)ep.CertificationFromDate).Date
                    : DateTime.MinValue;
                DateTime endDate = (ep.CertificationThruDate.HasValue)
                    ? ((DateTime)ep.CertificationThruDate).Date
                    : DateTime.MinValue;
                if ((startDate != DateTime.MinValue) && (endDate != DateTime.MinValue))
                {
                    oeEligibleList = Admission.OrderEntry.Where(oe =>
                        ((oe.HistoryKey == null) && oe.OrderStatusOKForPOCAddendum &&
                         (oe.CompletedDateBetweenDates(startDate, endDate)))).ToList();
                }
            }

            // Inactivate old (voided) ones
            foreach (EncounterPlanOfCareOrder ePOCo in Encounter.EncounterPlanOfCareOrder)
                if (oeEligibleList.Where(oe => oe.OrderEntryKey == ePOCo.OrderEntryKey).Any() == false)
                {
                    ePOCo.Inactive = true;
                }

            // Then add new ones
            foreach (OrderEntry oe in oeEligibleList)
            {
                EncounterPlanOfCareOrder ePOCo = Encounter.EncounterPlanOfCareOrder
                    .Where(e => e.OrderEntryKey == oe.OrderEntryKey).FirstOrDefault();
                if (ePOCo == null)
                {
                    ePOCo = new EncounterPlanOfCareOrder
                        { EncounterKey = Encounter.EncounterKey, OrderEntryKey = oe.OrderEntryKey };
                    Encounter.EncounterPlanOfCareOrder.Add(ePOCo);
                }

                ePOCo.Inactive = false;
            }
        }

        private void BuildPhysicianLists()
        {
            // Note - if SigningPhysicianKey is null all orders goto OrdersFromOtherPhysiciansList
            OrdersFromOtherPhysiciansList = new List<OrderEntry>();
            OrdersFromSigningPhysicianList = new List<OrderEntry>();
            if ((Admission == null) || (Admission.OrderEntry == null) || (Encounter == null) ||
                (Encounter.EncounterPlanOfCareOrder == null))
            {
                return;
            }

            List<EncounterPlanOfCareOrder> ePOCoList =
                Encounter.EncounterPlanOfCareOrder.Where(e => e.Inactive == false).ToList();
            if ((ePOCoList == null) || (ePOCoList.Any() == false))
            {
                return;
            }

            int? signingPhysicianKey = SigningPhysicianKey;
            List<OrderEntry> ordersFromOtherPhysiciansList = new List<OrderEntry>();
            List<OrderEntry> ordersFromSigningPhysicianList = new List<OrderEntry>();
            foreach (EncounterPlanOfCareOrder ePOCo in ePOCoList)
            {
                OrderEntry oe = Admission.OrderEntry.FirstOrDefault(o => o.OrderEntryKey == ePOCo.OrderEntryKey);
                if (oe == null)
                {
                    continue;
                }

                if (oe.SigningPhysicianKey == signingPhysicianKey)
                {
                    ordersFromSigningPhysicianList.Add(oe);
                }
                else
                {
                    ordersFromOtherPhysiciansList.Add(oe);
                }
            }

            OrdersFromOtherPhysiciansList = ordersFromOtherPhysiciansList.OrderBy(oe => oe.SigningPhysicianKey)
                .ThenBy(oe => oe.CompletedDate).ToList();
            OrdersFromSigningPhysicianList = ordersFromSigningPhysicianList.OrderBy(oe => oe.CompletedDate).ToList();
        }

        private int? _overrideSigningPhysicianKey;

        private void OverrideSigningPhysicianKeyChanged(int? key)
        {
            _overrideSigningPhysicianKey = key;
            BuildPhysicianLists();
            this.RaisePropertyChangedLambda(p => p.SigningPhysicianFullNameInformalWithSuffix);
        }

        private int? SigningPhysicianKey
        {
            get
            {
                if ((Encounter != null) && (Encounter.EncounterAdmission != null))
                {
                    EncounterAdmission ea = Encounter.EncounterAdmission.FirstOrDefault();
                    if ((ea != null) && (ea.SigningPhysicianKey != null) && (ea.SigningPhysicianKey != 0))
                    {
                        return ea.SigningPhysicianKey;
                    }
                }

                return _overrideSigningPhysicianKey;
            }
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;
            return true;
        }
    }

    public class PlanOfCareAddendumFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PlanOfCareAddendum pocA = new PlanOfCareAddendum(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
            };
            pocA.PlanOfCareAddendumSetup();
            return pocA;
        }
    }
}