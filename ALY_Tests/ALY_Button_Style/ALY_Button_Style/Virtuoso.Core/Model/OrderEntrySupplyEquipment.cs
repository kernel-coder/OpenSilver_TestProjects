#region Usings

using System.Linq;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class OrderEntrySupplyEquipment : QuestionUI
    {
        public OrderEntrySupplyEquipment(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public IOrderedEnumerable<AdmissionEquipment> AdmissionEquipmentList
        {
            get
            {
                IOrderedEnumerable<AdmissionEquipment> admissionEquipmentList = null;

                if ((Admission != null) && (Admission.AdmissionEquipment != null))
                {
                    admissionEquipmentList = Admission.AdmissionEquipment.Where(ae => (!ae.Inactive)
                        && (Encounter != null)
                        && (Encounter.EncounterEquipment != null)
                        && Encounter.EncounterEquipment.Any(ee => ee.AdmissionEquipmentKey == ae.AdmissionEquipmentKey)
                    ).OrderBy(ae => ae.EquipmentDescription);
                }

                return admissionEquipmentList;
            }
        }

        public override void PreProcessing()
        {
            if (GoalManager == null)
            {
                return;
            }

            if (Admission == null)
            {
                return;
            }

            if (Admission.AdmissionGoal == null)
            {
            }
        }

        public override QuestionUI Clone()
        {
            Rehab r = new Rehab(__FormSectionQuestionKey)
            {
                Question = Question,
                IndentLevel = IndentLevel,
                Patient = Patient,
                Encounter = Encounter,
                Admission = Admission,
                GoalManager = GoalManager
            };
            r.SetupOrderEntryProtectedOverrideRunTime();
            r.RehabSetup();
            r.PreProcessing();
            return r;
        }


        public IDynamicFormService FormModel { get; set; }

        public string EquipmentDataTemplate => "EquipmentOrders";

        public string SuppliesDataTemplate => "Supplies";

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            ValidationError = string.Empty;

            if (Protected)
            {
                return true; // Only validate if they can actuall do something about it.
            }

            return true;
        }

        public RelayCommand DataTemplateLoaded { get; set; }
        public RelayCommand DataTemplateUnLoaded { get; set; }

        public void RehabSetup()
        {
            DataTemplateLoaded = new RelayCommand(() =>
            {
                ProtectedOverrideRunTime = SetupOrderEntryProtectedOverrideRunTime();
                if (OrderEntryManager != null)
                {
                    this.RaisePropertyChangedLambda(p => p.Protected);
                }
            });
            DataTemplateUnLoaded = new RelayCommand(() =>
            {

            });
        }

        public OrderEntryManager OrderEntryManager;

        public bool? SetupOrderEntryProtectedOverrideRunTime()
        {
            // If not an order - do not override Protection (VO orders don't count)
            if (OrderEntryManager == null)
            {
                return null;
            }

            if (OrderEntryManager.IsVO)
            {
                return null;
            }

            if (Encounter == null)
            {
                return null;
            }

            if (Encounter.EncounterIsOrderEntry == false)
            {
                return null;
            }

            // Everything is protected on inactive forms
            if (Encounter.Inactive)
            {
                return true;
            }

            if (OrderEntryManager.CurrentOrderEntry == null)
            {
                return true;
            }

            // the clinician who 'owns' the order can edit it if its in an edit state (and not voided)
            if ((Encounter.EncounterBy == WebContext.Current.User.MemberID) &&
                (Encounter.EncounterStatus == (int)EncounterStatusType.Edit))
            {
                return (OrderEntryManager.CurrentOrderEntry.OrderStatus == (int)OrderStatusType.Voided) ? true : false;
            }

            // anyone with OrderEntry role when the form is in orderentry review
            return (OrderEntryManager.CurrentOrderEntry.CanEditOrderReviewed) ? false : true;
        }
    }

    public class OrderEntrySupplyEquipmentFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            OrderEntrySupplyEquipment r = new OrderEntrySupplyEquipment(__FormSectionQuestionKey)
            {
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                FormModel = m,
                DynamicFormViewModel = vm,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                GoalManager = vm.CurrentGoalManager,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
            };
            r.SetupOrderEntryProtectedOverrideRunTime();
            r.RehabSetup();

            return r;
        }
    }
}