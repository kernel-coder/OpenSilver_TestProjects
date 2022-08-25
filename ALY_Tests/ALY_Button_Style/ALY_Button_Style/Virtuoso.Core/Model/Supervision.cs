#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Helpers;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Supervision : QuestionBase
    {
        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);
            base.Cleanup();
        }

        public CollectionViewSource admittedDisciplineList = new CollectionViewSource();
        public ICollectionView AdmittedDisciplineList => admittedDisciplineList.View;
        public RelayCommand AddDisciplineSupervision { get; set; }
        public RelayCommand<EncounterSupervision> RemoveDisciplineSupervision { get; set; }

        private List<AdmissionDiscipline> AdmissionDisciplinesToUse
        {
            get
            {
                var userProfile = UserCache.Current.GetCurrentUserProfile();
                return Admission.AdmissionDiscipline.Where(ad =>
                    (ad.AdmissionStatusCode == "A" || ad.AdmissionStatusCode == "M")
                    && TaskSchedulingHelper.UserCanSupervizeServiceType(ad.DisciplineKey, userProfile)
                    && !Encounter.EncounterSupervision.Any(es => es.DisciplineKey == ad.DisciplineKey)).ToList();
            }
        }

        public Supervision(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            AddDisciplineSupervision = new RelayCommand(() =>
            {
                if (Encounter == null)
                {
                    return;
                }

                if (Encounter.EncounterSupervision == null)
                {
                    return;
                }

                String subSections = "";
                if (Encounter.EncounterSupervision.Any() && !Validate(out subSections))
                {
                    return;
                }

                AdmissionDiscipline discRow = null;
                if (Admission != null && Admission.AdmissionDiscipline != null && Encounter != null &&
                    Encounter.EncounterSupervision != null)
                {
                    discRow = AdmissionDisciplinesToUse.FirstOrDefault();
                }

                Encounter.EncounterSupervision.Add(new EncounterSupervision
                {
                    CanChangeDiscipline = NumberOfDisciplinesLeftToSupervise > 1 ? true : false,
                    DisciplineKey = discRow?.DisciplineKey ?? 0,
                    Version = 2,
                    EncounterKey = Encounter.EncounterKey
                });
                if (NumberOfDisciplinesLeftToSupervise == 0 && Encounter != null &&
                    Encounter.EncounterSupervision != null)
                {
                    Encounter.EncounterSupervision.ForEach(es => { es.CanChangeDiscipline = false; });
                }

                RaisePropertyChanged("EncounterSupervision");
                this.RaisePropertyChangedLambda(p => p.CanAddNewRows);
                this.RaisePropertyChangedLambda(p => p.AddButtonLabel);
            });
            RemoveDisciplineSupervision = new RelayCommand<EncounterSupervision>(item =>
            {
                if (Encounter == null)
                {
                    return;
                }

                if (Encounter.EncounterSupervision == null)
                {
                    return;
                }

                if (item != null)
                {
                    Encounter.EncounterSupervision.Remove(item);
                    Model.RemoveEncounterSupervision(item);
                    item.EncounterKey = 0;
                }

                if (NumberOfDisciplinesLeftToSupervise > 0 && Encounter != null &&
                    Encounter.EncounterSupervision != null)
                {
                    Encounter.EncounterSupervision.ForEach(es => { es.CanChangeDiscipline = true; });
                }

                this.RaisePropertyChangedLambda(p => p.CanAddNewRows);
                this.RaisePropertyChangedLambda(p => p.AddButtonLabel);
            });
        }

        private bool IsRequired;

        public void DoSupervisionWithVisitChanged(bool DoSupFlag)
        {
            if (Encounter == null)
            {
                return;
            }

            if (Encounter.EncounterSupervision == null)
            {
                return;
            }

            IsRequired = DoSupFlag;
            if (!DoSupFlag)
            {
                foreach (var es in Encounter.EncounterSupervision) Model.RemoveEncounterSupervision(es);
                RaisePropertyChanged("CanAddNewRows");
            }
        }

        public bool CanAddNewRows => (NumberOfDisciplinesLeftToSupervise > 0 && !Protected) ? true : false;

        public String AddButtonLabel
        {
            get
            {
                String Label = "Supervise Another Discipline";
                if (Encounter == null)
                {
                    return Label;
                }

                if (Encounter.EncounterSupervision == null)
                {
                    return Label;
                }

                if (Encounter.EncounterSupervision.Any() == false)
                {
                    Label = "Supervise a Discipline";
                }

                return Label;
            }
        }

        public int NumberOfDisciplinesLeftToSupervise
        {
            get
            {
                if (Encounter == null)
                {
                    return 0;
                }

                if (Encounter.EncounterSupervision == null)
                {
                    return 0;
                }

                if (Admission == null)
                {
                    return 0;
                }

                if (Admission.AdmissionDiscipline == null)
                {
                    return 0;
                }

                return AdmissionDisciplinesToUse.Count();
            }
        }

        private string _SupervisionValidationError;

        public string SupervisionValidationError
        {
            get { return _SupervisionValidationError; }
            set
            {
                _SupervisionValidationError = value;
                RaisePropertyChanged("SupervisionValidationError");
            }
        }

        public override bool Validate(out string SubSections)
        {
            bool AllValid = true;
            SubSections = string.Empty;

            if (Encounter == null)
            {
                return AllValid;
            }

            if (Encounter.EncounterSupervision == null)
            {
                return AllValid;
            }

            SupervisionValidationError = null;
            if ((IsRequired || Required) && CanAddNewRows && (Protected == false) &&
                (Encounter.EncounterSupervision.Any() == false))
            {
                SupervisionValidationError = "At least one Supervision is required.";
                AllValid = false;
            }

            if ((IsRequired || Required) && (CanAddNewRows == false) && (Protected == false) &&
                (Encounter.EncounterSupervision.Any() == false))
            {
                SupervisionValidationError =
                    "At least one Supervision is required and you are not authorized to supervise any admitted disciplines.";
                AllValid = false;
            }

            foreach (var item in Encounter.EncounterSupervision)
            {
                item.ValidationErrors.Clear();

                if (item.Validate())
                {
                    if (item.EmployeePresent == null)
                    {
                        item.ValidationErrors.Add(new ValidationResult("Employee Present must be answered yes or no.",
                            new[] { "EmployeePresent" }));
                        AllValid = false;
                    }

                    if (item.EmployeePresent == true && String.IsNullOrEmpty(item.EmployeeName))
                    {
                        item.ValidationErrors.Add(new ValidationResult(
                            "Employee Present is Answered yes, Employee Name must be populated.",
                            new[] { "EmployeeName" }));
                        AllValid = false;
                    }

                    if (item.ShowNewAideFields)
                    {
                        item.FollowingCarePlan = null;
                    }

                    if ((item.FollowingCarePlan == null) && (item.ShowNewAideFields == false))
                    {
                        item.ValidationErrors.Add(new ValidationResult(
                            "Following Care Plan must be answered yes or no.", new[] { "FollowingCarePlan" }));
                        AllValid = false;
                    }

                    if (Encounter.EncounterSupervision.Any(es =>
                            es.DisciplineKey == item.DisciplineKey &&
                            es.EncounterSupervisionKey != item.EncounterSupervisionKey))
                    {
                        item.ValidationErrors.Add(new ValidationResult(
                            "Only one supervision per discipline is permitted.", new[] { "DisciplineKey" }));
                        AllValid = false;
                    }

                    AllValid = (item.ValidateNewAideFields() != false) && AllValid;
                }
                else
                {
                    AllValid = false;
                }
            }

            return AllValid;
        }
    }

    public class SupervisionFactory
    {
        public static QuestionBase Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Supervision qb = new Supervision(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                Section = formsection.Section,
                ParentGroupKey = pqgkey,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Encounter = vm.CurrentEncounter,
                Admission = vm.CurrentAdmission,
                AdmissionDiscipline = vm.CurrentAdmissionDiscipline,
                OasisManager = vm.CurrentOasisManager,
                admittedDisciplineList =
                {
                    Source = DisciplineCache.GetDisciplines().Where(d => d.SupportsAssistants).ToList()
                }
            };

            var userProfile = UserCache.Current.GetCurrentUserProfile();
            qb.AdmittedDisciplineList.Filter = item =>
            {
                Discipline disc = item as Discipline;
                // skim off and include any disciplines that were already supervised this visit
                // note - we may be in a legacy visit where the supervised visit is no longer admiteed
                if ((qb.Encounter != null) && (qb.Encounter.EncounterSupervision != null) &&
                    qb.Encounter.EncounterSupervision.Any(es => es.DisciplineKey == disc.DisciplineKey))
                {
                    return true;
                }

                // add any currently 'admitted' (A and M) disciplines that this user can supervise
                return qb.Admission.AdmissionDiscipline.Any(ad =>
                    ((ad.DisciplineKey == disc.DisciplineKey) &&
                     (ad.AdmissionStatusCode == "A" || ad.AdmissionStatusCode == "M") &&
                     (TaskSchedulingHelper.UserCanSupervizeServiceType(ad.DisciplineKey, userProfile))));
            };
            if (qb.Encounter != null && qb.Encounter.EncounterSupervision != null)
            {
                bool canEdit = qb.NumberOfDisciplinesLeftToSupervise > 1 ? true : false;
                qb.Encounter.EncounterSupervision.ForEach(es => { es.CanChangeDiscipline = canEdit; });
            }

            Messenger.Default.Register<bool>(qb,
                string.Format("DoSupervisionWithVisitChanged{0}", qb.Encounter.EncounterID.ToString().Trim()),
                b => qb.DoSupervisionWithVisitChanged(b));
            return qb;
        }
    }
}