#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Client.Utils;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Cache.Allergy.Extensions;
using Virtuoso.Core.Cache.Extensions;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Services;
using Virtuoso.Core.Services.MAR;
using Virtuoso.Core.Utility;
using Virtuoso.Core.ViewModel;
using Virtuoso.Portable.Model;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.Model
{
    public class PatientCollectionBase : QuestionUI
    {
        public bool IsOnline => EntityManager.IsOnline;
        public ICollectionView PrintCollectionForDisplay { get; set; }
        public ICollectionView PrintCollectionMeds { get; set; }
        public List<PrintLibraryDisplayStruct> PrintListOfStrings = new List<PrintLibraryDisplayStruct>();

        public IDynamicFormService FormModel { get; set; }
        public OrderEntryManager OrderEntryManager { get; set; }
        public RelayCommand AllergySearchClosedCommand { get; protected set; }
        public RelayCommand DataTemplateLoaded { get; set; }

        public RelayCommand<string> AllergySearchCommand { get; protected set; }
        public RelayCommand<CachedAllergyCode> AllergyAddCommand { get; protected set; }
        public RelayCommand AllergyAddTextCommand { get; protected set; }
        public RelayCommand AllergyClearCommand { get; protected set; }
        public RelayCommand<string> LabAddCommand { get; protected set; }
        public RelayCommand AddTranslatorCommand { get; protected set; }
        public RelayCommand<PatientTranslator> RemoveTranslatorCommand { get; protected set; }

        private CollectionViewSource _FilteredAllergyCodes = new CollectionViewSource();
        public ICollectionView FilteredAllergyCodes => _FilteredAllergyCodes.View;

        private string _AllergyCodeSearch;

        public string AllergyCodeSearch
        {
            get { return _AllergyCodeSearch; }
            set
            {
                _AllergyCodeSearch = value;
                this.RaisePropertyChangedLambda(e => e.AllergyCodeSearch);
            }
        }

        private bool _AllergySearchBusy;

        public bool AllergySearchBusy
        {
            get { return _AllergySearchBusy; }
            private set
            {
                _AllergySearchBusy = value;
                this.RaisePropertyChangedLambda(e => e.AllergySearchBusy);
            }
        }

        private PatientAllergy _NewPatientAllergy;

        public PatientAllergy NewPatientAllergy
        {
            get { return _NewPatientAllergy; }
            set
            {
                _NewPatientAllergy = value;
                this.RaisePropertyChangedLambda(e => e.NewPatientAllergy);
            }
        }

        public PatientCollectionBase(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
            _FilteredAllergyCodes.SortDescriptions.Add(
                new SortDescription("SubstanceName", ListSortDirection.Ascending));

            DataTemplateLoaded = new RelayCommand(() =>
            {
                ProtectedOverrideRunTime = SetupOrderEntryProtectedOverrideRunTime();
                if (OrderEntryManager != null)
                {
                    this.RaisePropertyChangedLambda(p => p.Protected);
                }
            });

            AllergySearchClosedCommand = new RelayCommand(() => { AllergyClearCommand.Execute(null); });

            AllergySearchCommand = new RelayCommand<string>(s =>
            {
                _allergyAddText = (string.IsNullOrWhiteSpace(s)) ? null : s;
                AllergyCodeSearch = s;
                AllergySearchBusy = true;

                _FilteredAllergyCodes.Source = null;
                RaisePropertyChanged("FilteredAllergyCodes");

                AsyncUtility.RunAsync(async () => {
                    var results = await AllergyCache.Current.Search(AllergyCodeSearch, false);
                    AsyncUtility.RunOnMainThread(() => SetAllergySearchResults(results));
                });
            }, s => !AllergySearchBusy);

            AllergyAddCommand = new RelayCommand<CachedAllergyCode>(allergy =>
            {
                if (Patient.PatientAllergy.Where(p =>
                        ((p.AllergyCodeKey == allergy.AllergyCodeKey) && (!p.AllergyEndDate.HasValue) &&
                         (p.Inactive == false))).Any() == false)
                {
                    PatientAllergy pa = new PatientAllergy
                    {
                        AllergyCodeKey = allergy.AllergyCodeKey,
                        Code = allergy.UNII,
                        Description = string.IsNullOrWhiteSpace(allergy.DisplayName)
                            ? allergy.SubstanceName
                            : allergy.DisplayName,
                        AllergyStartDate = ((Encounter != null) && (Encounter.EncounterStartDate != null))
                            ? Encounter.EncounterStartDate.GetValueOrDefault().Date
                            : DateTime.Now.Date,
                        IsEditting = true
                    };
                    if ((Encounter != null && (Encounter.EncounterKey > 0)))
                    {
                        pa.AddedFromEncounterKey = Encounter.EncounterKey;
                    }

                    Patient.PatientAllergy.Add(pa);
                    AllergySearchClosedCommand.Execute(null);
                    Deployment.Current.Dispatcher.BeginInvoke(() => { NewPatientAllergy = pa; });
                }
                else
                {
                    NavigateCloseDialog d = new NavigateCloseDialog();
                    if (d != null)
                    {
                        d.Closed += (s, err) => { };
                        d.NoVisible = false;
                        d.OKLabel = "OK";
                        d.Title = "Cannot add duplicate allergies";
                        d.Width = double.NaN;
                        d.Height = double.NaN;
                        d.ErrorMessage = String.Format("Cannot add a duplicate allergy of name: {0}.",
                            (string.IsNullOrWhiteSpace(allergy.DisplayName)
                                ? allergy.SubstanceName
                                : allergy.DisplayName));
                        d.Show();
                    }
                }
            });
            AllergyAddTextCommand = new RelayCommand(() =>
            {
                if (string.IsNullOrWhiteSpace(_allergyAddText))
                {
                    return;
                }

                if (Patient.PatientAllergy.Where(p =>
                            ((p.Description == _allergyAddText) && (!p.AllergyEndDate.HasValue) &&
                             (p.Inactive == false)))
                        .Any() == false)
                {
                    PatientAllergy pa = new PatientAllergy
                    {
                        Description = _allergyAddText,
                        AllergyStartDate = ((Encounter != null) && (Encounter.EncounterStartDate != null))
                            ? Encounter.EncounterStartDate.GetValueOrDefault().Date
                            : DateTime.Now.Date,
                        IsEditting = true
                    };
                    if ((Encounter != null && (Encounter.EncounterKey > 0)))
                    {
                        pa.AddedFromEncounterKey = Encounter.EncounterKey;
                    }

                    Patient.PatientAllergy.Add(pa);
                    AllergySearchClosedCommand.Execute(null);
                    Deployment.Current.Dispatcher.BeginInvoke(() => { NewPatientAllergy = pa; });
                }
                else
                {
                    NavigateCloseDialog d = new NavigateCloseDialog();
                    if (d != null)
                    {
                        d.Closed += (s, err) => { };
                        d.NoVisible = false;
                        d.OKLabel = "OK";
                        d.Title = "Cannot add duplicate allergies";
                        d.Width = double.NaN;
                        d.Height = double.NaN;
                        d.ErrorMessage = String.Format("Cannot add a duplicate allergy of name: {0}.", _allergyAddText);
                        d.Show();
                    }
                }
            });

            AllergyClearCommand = new RelayCommand(() =>
            {
                AllergyCodeSearch = "";
                _allergyAddText = "";
                _FilteredAllergyCodes.Source = null;
                this.RaisePropertyChangedLambda(p => p.FilteredAllergyCodes);
                this.RaisePropertyChangedLambda(p => p.AllergyAddTextLabel);
            }, () => ((FilteredAllergyCodes != null) && (FilteredAllergyCodes.IsEmpty == false)));

            LabAddCommand = new RelayCommand<string>(category =>
            {
                PatientLab pl = new PatientLab
                {
                    Category = category,
                    IsEditting = true
                };
                if ((Encounter != null && (Encounter.EncounterKey > 0)))
                {
                    pl.AddedFromEncounterKey = Encounter.EncounterKey;
                }

                Patient.PatientLab.Add(pl);
            });

            AddTranslatorCommand = new RelayCommand(() => { Patient.PatientTranslator.Add(new PatientTranslator()); });

            RemoveTranslatorCommand = new RelayCommand<PatientTranslator>(pt =>
            {
                pt.ValidationErrors.Clear();
                Patient.PatientTranslator.Remove(pt);
                FormModel.RemovePatientTranslator(pt);
                if (Patient.PatientTranslator.Any() == false)
                {
                    Patient.Translator = false;
                }
            });
        }

        private string _allergyAddText;

        public string AllergyAddTextLabel => (string.IsNullOrWhiteSpace(_allergyAddText))
            ? null
            : "  Add free text allergen '" + _allergyAddText + "'  "; // readonly

        private void SetAllergySearchResults(IEnumerable<CachedAllergyCode> list)
        {
            _FilteredAllergyCodes.Source = list;

            if (string.IsNullOrWhiteSpace(_allergyAddText) == false)
            {
                if (list != null)
                {
                    CachedAllergyCode cac = list.Where(a =>
                        (((string.IsNullOrWhiteSpace(a.DisplayName) == false) &&
                          (a.DisplayName.ToLower() == _allergyAddText.ToLower())) ||
                         (string.IsNullOrWhiteSpace(a.DisplayName) &&
                          (a.SubstanceName.ToLower() == _allergyAddText.ToLower())))).FirstOrDefault();
                    if (cac != null)
                    {
                        _allergyAddText = null;
                    }
                }
            }

            this.RaisePropertyChangedLambda(p => p.AllergyAddTextLabel);

            AllergySearchBusy = false;
            RaisePropertyChanged("FilteredAllergyCodes");
            AllergyAddCommand.RaiseCanExecuteChanged();
            AllergyClearCommand.RaiseCanExecuteChanged();
        }

        public override void RefreshPrintCollection()
        {
            base.RefreshPrintCollection();
            if (Question.DataTemplate.Equals("OrdersForDisciplineAndTreatment"))
            {
                if (DynamicFormViewModel == null)
                {
                    return;
                }

                if (DynamicFormViewModel.CurrentGoalManager == null)
                {
                    return;
                }

                DynamicFormViewModel.CurrentGoalManager.CreatePOCOrdersForDiscPrintList();
                BuildStringPrintList(PrintListOfStrings);
                if (PrintCollection != null)
                {
                    PrintCollection.Refresh();
                }

                if (PrintCollectionForDisplay != null)
                {
                    PrintCollectionForDisplay.Refresh();
                }

                this.RaisePropertyChangedLambda(p => p.PrintCollection);
            }

            if (Question.DataTemplate.Equals("Medication"))
            {
                BuildStringPrintListForMeds(PrintListOfStrings);
                if (PrintCollection != null)
                {
                    PrintCollection.Refresh();
                }

                this.RaisePropertyChangedLambda(p => p.PrintCollection);
            }
        }

        public void BuildStringPrintList(List<PrintLibraryDisplayStruct> ListParm)
        {
            if (ListParm == null)
            {
                return;
            }

            ListParm.Clear();
            PrintLibraryDisplayStruct tmpRow;
            foreach (var ord in DynamicFormViewModel.CurrentGoalManager.POCOrdersForDiscPrintList
                         .OrderBy(ord => ord.DisciplineCode).ThenBy(ord2 => ord2.StartDate))
            {
                foreach (var fcd in ord.DisciplineFrequencies)
                {
                    tmpRow = new PrintLibraryDisplayStruct();
                    tmpRow.IsHeader = true;
                    tmpRow.InString2 = false;
                    tmpRow.DisplayString1 = fcd.DisplayDisciplineFrequencyText;
                    ListParm.Add(tmpRow);
                    if (fcd.IsPRN_Client)
                    {
                        tmpRow = new PrintLibraryDisplayStruct();
                        tmpRow.InString2 = true;
                        tmpRow.IsHeader = false;
                        tmpRow.DisplayString1 = "Purpose: ";
                        tmpRow.DisplayString2 = fcd.Purpose;
                        ListParm.Add(tmpRow);
                    }
                }

                if (Admission.HospiceAdmission == false)
                {
                    foreach (var treat in ord.AdmissionGoalElements)
                    {
                        tmpRow = new PrintLibraryDisplayStruct();
                        tmpRow.IsHeader = false;
                        tmpRow.InString2 = false;
                        tmpRow.DisplayString2 = treat.POCOverrideCode;
                        tmpRow.DisplayString3 = treat.GoalElementText;
                        ListParm.Add(tmpRow);
                    }
                }
            }
        }

        #region Medication Print

        public void BuildStringPrintListForMeds(List<PrintLibraryDisplayStruct> ListParm)
        {
            bool containsHospiceMeds = false;
            if (ListParm == null)
            {
                return;
            }

            ListParm.Clear();
            PrintLibraryDisplayStruct tmpRow;

            AddDisplayStringsToList(ListParm, "Medication", "Start Date", "End Date", true, false);

            foreach (PatientMedication med in PrintCollectionMeds)
            {
                tmpRow = new PrintLibraryDisplayStruct();
                tmpRow.IsHeader = false;
                tmpRow.IsBody = true;

                containsHospiceMeds = containsHospiceMeds || med.MedicationCoveredByHospice;
                tmpRow.DisplayString1 = med.DescriptionPlusHospiceIndicator;
                if (DynamicFormViewModel != null && DynamicFormViewModel.CurrentEncounter != null &&
                    DynamicFormViewModel.CurrentEncounter.EncounterIsPlanOfCare)
                {
                    tmpRow.DisplayString1 = tmpRow.DisplayString1 + "     " + med.MedicationNewChangedFlag;
                }

                tmpRow.DisplayString2 = med.MedicationStartDateTimeDisplay;
                tmpRow.DisplayString3 = med.MedicationEndDateTimeDisplay;

                if (med.IsIV)
                {
                    ListParm.Add(GetPrintStringIVField("IV Concentration                ", med.IVConcentration));
                    ListParm.Add(GetPrintStringIVField("IV Rate(ml/hr)                  ",
                        (med.IVRate.HasValue) ? med.IVRate.ToString() : string.Empty));
                    ListParm.Add(GetPrintStringIVField("First Dose in Controlled Setting",
                        med.IVFirstInControlledSetting.ToString()));
                    ListParm.Add(GetPrintStringIVField("Continuous or Intermittent      ", med.IVContinuousString));
                    ListParm.Add(GetPrintStringIVField("IV Type                         ", med.IVTypeString));
                }

                if (!string.IsNullOrEmpty(med.MedicationManagedBy))
                {
                    AddPrintStringIndentComment(ListParm, "Managed By", med.MedicationManagedBy);
                    tmpRow = new PrintLibraryDisplayStruct();
                }

                if (!String.IsNullOrEmpty(med.Comment))
                {
                    AddPrintStringIndentComment(ListParm, "Comment", med.Comment);
                }
            }

            if (containsHospiceMeds)
            {
                AddDisplayStringsToList(ListParm,
                    "(H) indicates the associated medication is covered by the Hospice Benefit.", null, null, false,
                    true);
            }

            BuildStringListMedsReconcile(ListParm);
            BuildStringListMedsTeaching(ListParm);
            BuildStringListMedsManagement(ListParm);
            BuildStringListMedsAdmin(ListParm);
        }

        private void BuildStringListMedsReconcile(List<PrintLibraryDisplayStruct> ListParm)
        {
            if (!ShowMedReconcileTeachingManagesOnPrint)
            {
                return;
            }

            AddDisplayStringsToList(ListParm, "Reconciled Medications", "Date/Time", "Reconciled By", true, false);

            if (PatientMedReconcilePrintList.Any() == false)
            {
                AddPrintStringIndentComment(ListParm, "None", "");
            }

            foreach (var pr in PatientMedReconcilePrintList)
            {
                AddDisplayStringsToList(ListParm, pr.ReconcileMedications, pr.ReconcileDateTimeFormatted,
                    pr.FommattedReconcileBy, false, true);

                if (!String.IsNullOrEmpty(pr.ReconcileComment))
                {
                    AddPrintStringIndentComment(ListParm, "Comment", pr.ReconcileComment);
                }
            }
        }

        private void BuildStringListMedsTeaching(List<PrintLibraryDisplayStruct> ListParm)
        {
            if (!ShowMedReconcileTeachingManagesOnPrint)
            {
                return;
            }

            AddDisplayStringsToList(ListParm, "Taught Medications", "Date/Time", "Taught By", true, false);

            if (PatientMedTeachingPrintList.Any() == false)
            {
                AddPrintStringIndentComment(ListParm, "None", "");
            }

            foreach (var pr in PatientMedTeachingPrintList)
            {
                AddDisplayStringsToList(ListParm, pr.TeachingMedications, pr.TeachingDateTimeFormatted,
                    pr.FommattedTeachingBy, false, true);

                if (!String.IsNullOrEmpty(pr.TeachingComment))
                {
                    AddPrintStringIndentComment(ListParm, "Comment", pr.TeachingComment);
                }
            }
        }

        private void BuildStringListMedsManagement(List<PrintLibraryDisplayStruct> ListParm)
        {
            if (!ShowMedReconcileTeachingManagesOnPrint)
            {
                return;
            }

            AddDisplayStringsToList(ListParm, "Managed Medications", "Date/Time", "Managed By", true, false);

            if (PatientMedManagementPrintList.Any() == false)
            {
                AddPrintStringIndentComment(ListParm, "None", "");
            }

            foreach (var pr in PatientMedManagementPrintList)
            {
                AddDisplayStringsToList(ListParm, pr.ManagementMedications, pr.ManagementDateTimeFormatted,
                    pr.FormattedManagementBy, false, true);

                AddPrintStringIndentComment(ListParm, "Pharmacy delivers to patient",
                    (pr.PharmacyDelivers == true ? "Yes" : "No"));
                AddPrintStringIndentComment(ListParm, "Medication kept in lockable container",
                    (pr.LockableContainer == true ? "Yes" : "No"));
                AddPrintStringIndentComment(ListParm, "Instructed regarding medication schedule",
                    (pr.ScheduleInstructed == true ? "Yes" : "No"));
                AddPrintStringIndentComment(ListParm, "Who was instructed", pr.ScheduleInstructedTo);
                AddPrintStringIndentComment(ListParm, "Assessed", pr.Assessed);
                if (!String.IsNullOrEmpty(pr.AssessedComment))
                {
                    AddPrintStringIndentComment(ListParm, "Assessed Comment", pr.AssessedComment);
                }

                AddPrintStringIndentComment(ListParm, "Patient continues to require medication management",
                    (pr.ContinueMedManagement == true ? "Yes" : "No"));
                if (!String.IsNullOrEmpty(pr.ContinueMedManagementComment))
                {
                    AddPrintStringIndentComment(ListParm, "Patient continues to require medication management due to",
                        pr.ContinueMedManagementComment);
                }
            }
        }

        private void BuildStringListMedsAdmin(List<PrintLibraryDisplayStruct> ListParm)
        {
            if (!ShowMedReconcileTeachingManagesOnPrint)
            {
                return;
            }

            AddDisplayStringsToList(ListParm, "Administered Medications", "Date/Time", "Administered By", true, false);

            if (PatientMedicationAdministrationList.Any() == false)
            {
                AddPrintStringIndentComment(ListParm, "None", "");
            }

            foreach (var pr in PatientMedicationAdministrationList)
            {
                AddDisplayStringsToList(ListParm, pr.AdministrationMedications, pr.AdministrationDateTimeFormatted,
                    pr.FommattedAdministrationBy, false, true);

                AddPrintStringIndentComment(ListParm, "Time Given", pr.TimesGiven);
                AddPrintStringIndentComment(ListParm, "Clinician wittnessed the medication administration",
                    (pr.ClinicianWitnessed ? "Yes" : "No"));
                AddPrintStringIndentComment(ListParm, "Who Administered Medication?", pr.WhoAdministeredValue);
                AddPrintStringIndentComment(ListParm, "Administration Site", pr.AdministrationSiteDescription);

                var adminType = pr.AdministeredTypeDescription;
                var cl = CodeLookupCache.GetCodeFromKey(pr.AdministeredType);
                if (cl != null && cl.ToLower() == "prepour")
                {
                    adminType = string.Format("{0} ({1} - {2})", adminType,
                        (pr.PrePourFromDate.HasValue ? pr.PrePourFromDate.Value.ToShortDateString() : ""),
                        (pr.PrePourThruDate.HasValue ? pr.PrePourThruDate.Value.ToShortDateString() : ""));
                }

                AddPrintStringIndentComment(ListParm, "Administration Type", adminType);
                AddPrintStringIndentComment(ListParm, "Patient Refused", (pr.PatientRefused ? "Yes" : "No"));
                AddPrintStringIndentComment(ListParm, "No Refill/Not Available",
                    (pr.NoRefillOrNotAvailable ? "Yes" : "No"));
            }
        }

        private PrintLibraryDisplayStruct GetPrintStringIVField(string fieldLabel, string fieldValue)
        {
            PrintLibraryDisplayStruct tmpRow = new PrintLibraryDisplayStruct();
            tmpRow.IsBody = false;
            tmpRow.IsHeader = false;
            tmpRow.InString2 = false;
            tmpRow.InString3 = true;
            tmpRow.DisplayString1 = fieldLabel;
            tmpRow.DisplayString2 = fieldValue;
            return tmpRow;
        }

        private void AddPrintStringIndentComment(List<PrintLibraryDisplayStruct> ListParm, string label, string comment)
        {
            PrintLibraryDisplayStruct tmpRow = new PrintLibraryDisplayStruct();
            tmpRow.InString2 = true;
            tmpRow.DisplayString1 = label;
            tmpRow.DisplayString2 = comment;
            ListParm.Add(tmpRow);
        }

        private void AddDisplayStringsToList(List<PrintLibraryDisplayStruct> ListParm, string displayString1,
            string displayString2, string displayString3, bool isHeader, bool isBody)
        {
            PrintLibraryDisplayStruct tmpRow = new PrintLibraryDisplayStruct();
            tmpRow.IsBody = isBody;
            tmpRow.IsHeader = isHeader;

            tmpRow.DisplayString1 = displayString1;
            tmpRow.DisplayString2 = displayString2;
            tmpRow.DisplayString3 = displayString3;
            ListParm.Add(tmpRow);
        }

        #endregion

        public override QuestionUI Clone()
        {
            PatientCollectionBase pcb = new PatientCollectionBase(__FormSectionQuestionKey)
            {
                Question = Question,
                IndentLevel = IndentLevel,
                Patient = Patient,
                Encounter = Encounter,
                Admission = Admission,
                DynamicFormViewModel = DynamicFormViewModel,
            };
            pcb.IsClonedQuestion = true;
            CollectionViewSource cvs = new CollectionViewSource();
            CollectionViewSource cvs2 = new CollectionViewSource();

            if ((Question.DataTemplate.Equals("Diagnosis")) || (Question.DataTemplate.Equals("DiagnosisCM")) ||
                (Question.DataTemplate.Equals("DiagnosisPCS")))
            {
                cvs.Source = pcb.Admission.AdmissionDiagnosis;
            }
            else if (Question.DataTemplate.Equals("Allergy") || Question.DataTemplate.Equals("DischargeAllergies") ||
                     Question.DataTemplate.Equals("AllergyList"))
            {
                cvs.Source = pcb.Patient.PatientAllergy;
            }
            else if (Question.DataTemplate.Equals("Medication") || Question.DataTemplate.Equals("MedicationList"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientMedication.OrderBy(med => med.MedicationStatus)
                    .ThenBy(med2 => med2.MedicationName);
                pcb.PrintCollectionMeds = cvs.View;

                pcb.SortOrder = "MedicationStatus|MedicationName";

                pcb.PrintCollectionMeds.Filter = item =>
                {
                    PatientMedication pm = item as PatientMedication;
                    var accept =
                        (pm.IsNew || pm.EncounterMedication.Where(p => p.EncounterKey == pcb.Encounter.EncounterKey)
                            .Any()) && !pm.AddedInError;
                    //needed to print old POC's that included this stuff.  New ones exclude these rows in DynamicFormViewModel.
                    if (accept && (pcb.DynamicFormViewModel.CurrentForm.IsPlanOfCare ||
                                   pcb.DynamicFormViewModel.CurrentForm.IsOrderEntry) &&
                        pcb.DynamicFormViewModel != null
                        && pcb.DynamicFormViewModel.CurrentForm != null
                        && pcb.Encounter != null && pcb.Encounter.EncounterPlanOfCare != null)
                    {
                        var epc = pcb.Encounter.EncounterPlanOfCare.FirstOrDefault();
                        if (epc != null)
                        {
                            accept = pm.IsPOCMedication(epc.CertificationFromDate, epc.CertificationThruDate,
                                pcb.Encounter);
                        }
                    }

                    return accept;
                };

                pcb.BuildStringPrintListForMeds(pcb.PrintListOfStrings);
                cvs.Source = pcb.PrintListOfStrings;
                pcb.PrintCollection = cvs.View;
            }
            else if (Question.DataTemplate.Equals("DischargeMeds"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientMedication.OrderBy(med => med.MedicationStatus)
                    .ThenBy(med2 => med2.MedicationName);
                pcb.PrintCollection = cvs.View;

                if (PrintCollectionMeds != null)
                {
                    foreach (PatientMedication med in PrintCollectionMeds)
                        if (med.MedicationCoveredByHospice)
                        {
                            break;
                        }
                }

                pcb.SortOrder = "MedicationStatus|MedicationName";
                pcb.PrintCollection.Filter = item =>
                {
                    PatientMedication pm = item as PatientMedication;
                    var accept =
                        (pm.IsNew || pm.EncounterMedication.Where(p => p.EncounterKey == pcb.Encounter.EncounterKey)
                            .Any()) && !pm.AddedInError;
                    //needed to print old POC's that included this stuff.  New ones exclude these rows in DynamicFormViewModel.
                    if (accept && (pcb.DynamicFormViewModel.CurrentForm.IsPlanOfCare ||
                                   pcb.DynamicFormViewModel.CurrentForm.IsOrderEntry) &&
                        pcb.DynamicFormViewModel != null
                        && pcb.DynamicFormViewModel.CurrentForm != null
                        && pcb.Encounter != null && pcb.Encounter.EncounterPlanOfCare != null)
                    {
                        var epc = pcb.Encounter.EncounterPlanOfCare.FirstOrDefault();
                        if (epc != null)
                        {
                            accept = pm.IsPOCMedication(epc.CertificationFromDate, epc.CertificationThruDate,
                                pcb.Encounter);
                        }
                    }

                    return accept;
                };
            }
            else if (Question.DataTemplate.Equals("PainLocation"))
            {
                cvs.Source = pcb.Admission.AdmissionPainLocation;
            }
            else if (Question.DataTemplate.Equals("IVTherapy"))
            {
                cvs.Source = pcb.Admission.AdmissionIVSite;
                pcb.PrintCollection = cvs.View;
                pcb.SortOrder = "Number";
                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionIVSite ais = item as AdmissionIVSite;
                    if (ais.DeletedDate != null)
                    {
                        return false;
                    }

                    return ais.EncounterIVSite.Where(p => p.EncounterKey == pcb.Encounter.EncounterKey).Any();
                };
            }
            else if ((Question.DataTemplate.Equals("LevelOfCare")) || (Question.DataTemplate.Equals("POCLevelOfCare")))
            {
                cvs.Source = pcb.Admission.AdmissionLevelOfCare;
            }
            else if (Question.DataTemplate.Equals("AdmissionDisciplineFrequency"))
            {
                cvs.Source = pcb.Admission.AdmissionDisciplineFrequency;
            }
            else if (Question.DataTemplate.Equals("OrdersForDisciplineAndTreatment"))
            {
                pcb.CanTrimPrintCollection = true;

                pcb.BuildStringPrintList(pcb.PrintListOfStrings);
                cvs.Source = pcb.PrintListOfStrings;
                pcb.PrintCollection = cvs.View;

                cvs2.Source = pcb.DynamicFormViewModel.CurrentGoalManager.POCOrdersForDiscPrintList
                    .OrderBy(ord => ord.DisciplineCode).ThenBy(ord2 => ord2.StartDate);
                pcb.PrintCollectionForDisplay = cvs2.View;

                pcb.PrintCollection.Filter = item =>
                {
                    return true;
                };
                pcb.PrintCollectionForDisplay.Filter = item =>
                {
                    return true;
                };
                Messenger.Default.Register<bool>(pcb, "RefreshGoalElements",
                    item => { pcb.RefreshPrintCollection(); });
            }

            else if (Question.DataTemplate.Equals("Infections"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientInfection;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "ConfirmationDate|ResolvedDate";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    PatientInfection ai = item as PatientInfection;
                    return (ai.IsNew
                            || pcb.Encounter.EncounterPatientInfection.Any(ei =>
                                ei.PatientInfectionKey == ai.PatientInfectionKey));
                };
            }
            else if (Question.DataTemplate.Equals("AdverseEvents"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientAdverseEvent;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "EventDate|DocumentedDateTime";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    PatientAdverseEvent pae = item as PatientAdverseEvent;
                    return (pae.IsNew || pcb.Encounter.EncounterPatientAdverseEvent.Any(e =>
                        e.PatientAdverseEventKey == pae.PatientAdverseEventKey));
                };
            }
            else if (Question.DataTemplate.Equals("zOBSOLETEInfections"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Admission.AdmissionInfection;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "InfectionType|CultureDate";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionInfection ai = item as AdmissionInfection;
                    return (ai.IsNew
                            || pcb.Encounter.EncounterInfection.Any(ei =>
                                ei.AdmissionInfectionKey == ai.AdmissionInfectionKey));
                };
            }
            else if (Question.DataTemplate.Equals("Labs"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientLab;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "-OrderDate|Test";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    PatientLab pl = item as PatientLab;
                    return (pl.IsNew
                            || pcb.Encounter.EncounterLab.Any(ep => ep.PatientLabKey == pl.PatientLabKey));
                };
            }


            if (!Question.DataTemplate.Equals("OrdersForDisciplineAndTreatment") && !pcb.CanTrimPrintCollection)
            {
                pcb.PrintCollection = cvs.View;
                if (pcb.PrintCollection != null)
                {
                    pcb.PrintCollection.Refresh();
                }
            }

            return pcb;
        }

        public override bool Validate(out string SubSections)
        {
            SubSections = string.Empty;

            ValidationError = string.Empty;

            if ((Question == null) || (Question.DataTemplate == null) || (Admission == null) || (Encounter == null))
            {
                return true;
            }

            if ((Admission.AdmissionDiagnosis != null) && ((Question.DataTemplate.Equals("Diagnosis")) ||
                                                           (Question.DataTemplate.Equals("DiagnosisCM")) ||
                                                           (Question.DataTemplate.Equals("DiagnosisPCS"))))
            {
                // validate each diagnosis start /end date - ValidEnoughToSave = false;
                bool allValid = true;
                foreach (AdmissionDiagnosis ad in Admission.AdmissionDiagnosis)
                {
                    if (ad.IsNew || ad.HasResequenceChanges) // HasResequenceChanges gets us closer than just HasChanges
                    {
                        if ((ad.DiagnosisStartDate.HasValue) &&
                            (((DateTime)ad.DiagnosisStartDate).Date > DateTime.Today.Date))
                        {
                            ad.ValidationErrors.Add(new ValidationResult(
                                "The diagnosis Start Date cannot be greater than today.",
                                new[] { "DiagnosisStartDate" }));
                            allValid = false;
                            DynamicFormViewModel.ValidEnoughToSave = false;
                        }

                        if ((ad.DiagnosisEndDate.HasValue) &&
                            (((DateTime)ad.DiagnosisEndDate).Date > DateTime.Today.Date))
                        {
                            ad.ValidationErrors.Add(new ValidationResult(
                                "The diagnosis End Date cannot be greater than today.", new[] { "DiagnosisEndDate" }));
                            allValid = false;
                            DynamicFormViewModel.ValidEnoughToSave = false;
                        }
                    }
                }

                if (allValid == false)
                {
                    return false;
                }

                if ((Question.DataTemplate.Equals("Diagnosis")) || (Question.DataTemplate.Equals("DiagnosisCM")))
                {
                    DateTime encounterStartDate =
                        (Encounter.EncounterStartDate.HasValue && (Encounter.EncounterStartDate != DateTime.MinValue))
                            ? Encounter.EncounterStartDate.Value.Date
                            : DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified);

                    // validate group
                    List<AdmissionDiagnosis> pdList = Admission.AdmissionDiagnosis.Where(p => (!p.Superceded) &&
                            p.Diagnosis && (p.Version == TenantSettingsCache.Current.TenantSettingRequiredICDVersion) &&
                            ((p.RemovedDate == null) && ((p.DiagnosisStartDate.Value.Date <= encounterStartDate) &&
                                                         ((p.DiagnosisEndDate == null) ||
                                                          ((p.DiagnosisEndDate != null) &&
                                                           (p.DiagnosisEndDate.Value.Date >= encounterStartDate))))))
                        .ToList();

                    if ((pdList != null) && (Encounter.FullValidation))
                    {
                        if (pdList.Any() == false)
                        {
                            ValidationError = "At least one active ICD " +
                                              TenantSettingsCache.Current.TenantSettingRequiredICDVersion.ToString()
                                                  .Trim() + " diagnosis must be entered";
                            return false;
                        }
                    }

                    if ((pdList != null) && (pdList.Where(p => p.Code == "000.00").Any()))
                    {
                        if ((Encounter.FullValidation &&
                             (Encounter.EncounterStatus != (int)EncounterStatusType.CoderReview)) ||
                            (Encounter.EncounterStatus == (int)EncounterStatusType.CoderReviewEdit))
                        {
                            ValidationError = "The dummy diagnosis (000.00) is not allowed";
                            return false;
                        }
                    }
                }
            }
            else if ((Admission.AdmissionPainLocation != null) && ConditionalRequired &&
                     Question.DataTemplate.Equals("PainLocation"))
            {
                if (Admission.AdmissionPainLocation.Any() == false)
                {
                    ValidationError = "At least one pain location must be entered";
                    return false;
                }
            }

            bool valid = true;
            if ((Question.DataTemplate.Equals("Allergy")) && (Patient.PatientAllergy != null))
            {
                foreach (PatientAllergy pa in Patient.PatientAllergy.Where(p => (!p.Inactive)))
                    if (pa.IsNew || pa.IsModified)
                    {
                        pa.ValidationErrors.Clear();
                        if (pa.Validate() == false)
                        {
                            valid = false;
                        }
                    }
            }

            if ((Question.DataTemplate.Equals("Translator")) && (Patient.PatientTranslator != null))
            {
                foreach (PatientTranslator pt in Patient.PatientTranslator)
                    if (pt.IsNew || pt.IsModified)
                    {
                        pt.ValidationErrors.Clear();
                        if (pt.Validate() == false)
                        {
                            valid = false;
                        }
                    }
            }

            if ((DynamicFormViewModel != null) && (DynamicFormViewModel.CurrentForm != null) &&
                (Admission.AdmissionDisciplineFrequency != null) &&
                (Question.DataTemplate.Equals("AdmissionDisciplineFrequency") && Encounter.FullValidation) &&
                Encounter != null && Encounter.ServiceTypeKey != null)
            {
                if ((DynamicFormViewModel.CurrentForm.IsEval || DynamicFormViewModel.CurrentForm.IsResumption) &&
                    (!Admission.AdmissionDisciplineFrequency
                         .Where(fcd => !fcd.Inactive && fcd.DisciplineKey ==
                             ServiceTypeCache.GetDisciplineKey((int)Encounter.ServiceTypeKey)).Any() ||
                     Admission.AdmissionDisciplineFrequency.Any() == false))
                {
                    if (Encounter.FullValidation)
                    {
                        ValidationError = "At least one visit frequency is required";
                        Admission.ValidationErrors.Add(new ValidationResult("At least one visit frequency is required",
                            new[] { "AdmissionKey" }));
                        valid = false;
                    }
                }
            }

            if ((Question.DataTemplate.Equals("EmploymentRelated") && Encounter.FullValidation) && Admission != null &&
                Encounter != null)
            {
                if ((Admission.ValidationErrors != null) && (Admission.ValidationErrors.Any() == false))
                {
                    Admission.ValidateEmploymentRelated();
                }

                if ((Admission.ValidationErrors != null) &&
                    (Encounter.FullValidation && Admission.ValidationErrors.Any()))
                {
                    valid = false;
                }
            }

            if (Question.DataTemplate.Equals("Medication") && Admission != null && Encounter != null)
            {
                if (Protected)
                {
                    valid = true;
                }
                else if (Encounter.FullValidation)
                {
                    IMARDataSet marDS = MARUtils.CreateMarDataSet((IPatientService)FormModel, Patient, Admission, Encounter, isAdmissionMode: false, startDate: null);
                    marDS.Refresh(); // This will create and 'cleanup' MAR entities.  E.G. delete untouched MAR on discontinued meds.

                    // TODO MAR Remove/Update hard MAR errors for encounters
                    var utils = MARUtils.CreateDataSetValidator(Patient, Admission, Encounter, isAdmissionMode: false, startDate: null);

                    var timesRet = utils.AnyCurrentMedicationsRequireMARMedicationTimes();
                    if (timesRet.Result)
                    {
                        ValidationError = timesRet.ValidationError;
                        valid = false;
                    }

                    var documentationRet = utils.AnyCurrentMedicationsRequireMARDocumentation();
                    if (documentationRet.Result)
                    {
                        ValidationError = documentationRet.ValidationError;
                        valid = false;
                    }
                }
            }

            return valid;
        }


        public string SortOrder { get; set; }

        public PatientDiagnosisComment PatientDiagnosisComment
        {
            get
            {
                if (Patient == null)
                {
                    return null;
                }

                if (Patient.PatientDiagnosisComment == null)
                {
                    return null;
                }

                PatientDiagnosisComment pdc = null;
                if (Encounter == null)
                {
                    pdc = Patient.PatientDiagnosisComment.FirstOrDefault(p => (p.IsNew));
                    if (pdc == null)
                    {
                        pdc = new PatientDiagnosisComment();
                        Patient.PatientDiagnosisComment.Add(pdc);
                    }
                }
                else
                {
                    pdc = Patient.PatientDiagnosisComment
                        .FirstOrDefault(p => ((p.IsNew) || (p.AddedFromEncounterKey == Encounter.EncounterKey)));
                    if (pdc == null)
                    {
                        pdc = new PatientDiagnosisComment();
                        if ((Encounter != null && (Encounter.EncounterKey > 0)))
                        {
                            pdc.AddedFromEncounterKey = Encounter.EncounterKey;
                        }

                        Patient.PatientDiagnosisComment.Add(pdc);
                    }
                }

                return pdc;
            }
        }

        public string PatientDiagnosisCommentHistory
        {
            get
            {
                if (Patient == null)
                {
                    return "No comment history.";
                }

                if (Patient.PatientDiagnosisComment == null)
                {
                    return "No comment history.";
                }

                List<PatientDiagnosisComment> pdcList = null;
                if (Encounter == null)
                {
                    pdcList = Patient.PatientDiagnosisComment
                        .Where(p => ((string.IsNullOrWhiteSpace(p.Comment) == false) && (p.IsNew == false)))
                        .OrderByDescending(p => p.UpdatedDate).ToList();
                }
                else
                {
                    // Go thru EncounterDiagnosisComment
                    List<EncounterDiagnosisComment> edcList = Encounter.EncounterDiagnosisComment.Where(p =>
                            ((string.IsNullOrWhiteSpace(p.PatientDiagnosisComment.Comment) == false) &&
                             (p.PatientDiagnosisComment.IsNew == false) &&
                             (p.PatientDiagnosisComment.AddedFromEncounterKey != Encounter.EncounterKey)))
                        .OrderByDescending(p => p.PatientDiagnosisComment.UpdatedDate).ToList();
                    foreach (EncounterDiagnosisComment edc in edcList)
                        if (edc != null)
                        {
                            if (pdcList == null)
                            {
                                pdcList = new List<PatientDiagnosisComment>();
                            }

                            pdcList.Add(edc.PatientDiagnosisComment);
                        }
                }

                if ((pdcList == null) || (pdcList.Any() == false))
                {
                    return "No comment history.";
                }

                string history = null;
                bool useMilitaryTime = TenantSettingsCache.Current.TenantSetting.UseMilitaryTime;
                foreach (PatientDiagnosisComment pdc in pdcList)
                {
                    var userData = Client.Core.XamlHelper.EncodeAsXaml(pdc.Comment);
                    history = history +
                              string.Format("<Bold>On {0} {1} By {2}</Bold><LineBreak />{3}<LineBreak /><LineBreak />",
                                  pdc.UpdatedDate.ToLocalTime().ToShortDateString(),
                                  ((useMilitaryTime)
                                      ? pdc.UpdatedDate.ToLocalTime().ToString("HHmm")
                                      : pdc.UpdatedDate.ToLocalTime().ToShortTimeString()),
                                  ((pdc.UpdatedBy == null)
                                      ? "Unknown"
                                      : UserCache.Current.GetFormalNameFromUserId(pdc.UpdatedBy)),
                                  userData); //pdc.Comment.Replace("\r", "<LineBreak />"));
                }

                return history;
            }
        }

        public void AddRowsFromIncompleteEncounters()
        {
            // Add back in rows that were exluded for the old dynamic form control .
            // Note -  CurrentEncounter.IsNew IMPLICATIONS
            //       We were only adding these FCDs from unsigned forms if the user displayed the FCD section on the form BEFORE they press the first OK to save the form
            //       Needed to move this code out of the control (VisitFrequencyUserControlBase) and into the backing factory

            // refresh data on POC's even after save.  The view model has the logic to make the decision.
            if (Admission != null && Encounter != null && (Encounter.IsNew ||
                                                           (DynamicFormViewModel != null &&
                                                            DynamicFormViewModel.RefreshCopyForwardData)))
            {
                var existingadf = Admission.AdmissionDisciplineFrequency.Where(p => !p.Superceded && !p.Inactive
                    && !(Encounter.EncounterDisciplineFrequency.Any(edf =>
                        edf.DispFreqKey == p.DisciplineFrequencyKey)));
                foreach (var adf in existingadf)
                {
                    EncounterDisciplineFrequency edf = new EncounterDisciplineFrequency();
                    edf.AdmissionKey = Admission.AdmissionKey;
                    Encounter.EncounterDisciplineFrequency.Add(edf);
                    edf.PatientKey = Admission.PatientKey;
                    adf.EncounterDisciplineFrequency.Add(edf);
                    if ((DynamicFormViewModel != null) && (DynamicFormViewModel.CurrentOrderEntryManager != null))
                    {
                        EncounterStartDisciplineFrequency esdf = new EncounterStartDisciplineFrequency
                            { DisplayDisciplineFrequencyText = adf.DisplayDisciplineFrequencyText };
                        esdf.AdmissionKey = Admission.AdmissionKey;
                        Encounter.EncounterStartDisciplineFrequency.Add(esdf);
                        esdf.PatientKey = Admission.PatientKey;
                        adf.EncounterStartDisciplineFrequency.Add(esdf);
                    }
                }
            }
        }

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
                MessageBox.Show(
                    "PatientCollectionBase.SetupOrderEntryProtectedOverrideRunTime Error:  Encounter is null, contact AlayaCare support.");
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

        public bool ShowMedReconcileTeachingManagesOnPrint
        {
            get
            {
                if (DynamicFormViewModel == null)
                {
                    return true;
                }

                if (DynamicFormViewModel.CurrentForm == null)
                {
                    return true;
                }

                if (DynamicFormViewModel.CurrentForm.IsPlanOfCare)
                {
                    return false;
                }

                return true;
            }
        }

        public List<PatientMedicationAdministration> PatientMedicationAdministrationList
        {
            get
            {
                if (Patient == null || Patient.PatientMedicationAdministration == null)
                {
                    return null;
                }

                return Patient.PatientMedicationAdministration.Where(pm => pm.IsForEncounter(Encounter)).ToList();
            }
        }

        public List<PatientMedicationReconcile> PatientMedReconcilePrintList
        {
            get
            {
                if (Patient == null || Patient.PatientMedicationReconcile == null)
                {
                    return null;
                }

                return Patient.PatientMedicationReconcile.Where(pm => pm.IsForEncounter(Encounter)).ToList();
            }
        }

        public List<PatientMedicationTeaching> PatientMedTeachingPrintList
        {
            get
            {
                if (Patient == null || Patient.PatientMedicationTeaching == null)
                {
                    return null;
                }

                return Patient.PatientMedicationTeaching.Where(pm => pm.IsForEncounter(Encounter)).ToList();
            }
        }

        public List<PatientMedicationManagement> PatientMedManagementPrintList
        {
            get
            {
                if (Patient == null || Patient.PatientMedicationManagement == null)
                {
                    return null;
                }

                return Patient.PatientMedicationManagement.Where(pm => pm.IsForEncounter(Encounter)).ToList();
            }
        }

        public bool ContainsHospiceMedication
        {
            get
            {
                bool containsHospice = false;
                if ((Patient != null)
                    && (Patient.PatientMedication != null)
                   )
                {
                    containsHospice = Patient.PatientMedication.Any(pm => pm.MedicationCoveredByHospice);
                }

                return containsHospice;
            }
        }

        #region Admitting Diagnosis Region

        public string AssociatedDiagnosis9 => GetFirstDiagnosisByVersion(9);

        public string AssociatedDiagnosis10 => GetFirstDiagnosisByVersion(10);

        private string GetFirstDiagnosisByVersion(int versionParm)
        {
            if (Admission == null || Admission.AdmissionDiagnosis == null)
            {
                return null;
            }

            if (Encounter == null || Encounter.EncounterDiagnosis == null)
            {
                return null;
            }

            var diag = Admission.AdmissionDiagnosis.Where(ad =>
                    ad.Version == versionParm &&
                    Encounter.EncounterDiagnosis.Any(e => e.DiagnosisKey == ad.AdmissionDiagnosisKey))
                .OrderBy(add => add.Sequence).FirstOrDefault();

            return diag == null ? null : diag.Code + " - " + diag.Description;
        }

        public string AssociateDiagnosis9AndOr10
        {
            get
            {
                int version =
                    TenantSettingsCache.Current.TenantSettingRequiredICDVersionDateTimeOffset(Encounter
                        .EncounterOrTaskStartDateAndTime);
                string associatedDiagnosis = null;
                // if ICD9 mode: try to use 9 first, but use 10 as a backup  -  if ICD10 mode: try to use 10 first, but use 9 as a backup
                if (version == 9)
                {
                    associatedDiagnosis = (string.IsNullOrWhiteSpace(AssociatedDiagnosis9))
                        ? AssociatedDiagnosis10
                        : AssociatedDiagnosis9;
                }
                else
                {
                    associatedDiagnosis = (string.IsNullOrWhiteSpace(AssociatedDiagnosis10))
                        ? AssociatedDiagnosis9
                        : AssociatedDiagnosis10;
                }

                return (string.IsNullOrWhiteSpace(associatedDiagnosis)) ? null : associatedDiagnosis;
            }
        }

        #endregion

        public override void Cleanup()
        {
            Messenger.Default.Unregister(this);

            base.Cleanup();
        }
    }

    public class PatientCollectionBaseFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            PatientCollectionBase pcb = new PatientCollectionBase(__FormSectionQuestionKey)
            {
                DynamicFormViewModel = vm,
                FormModel = m,
                Section = formsection.Section,
                QuestionGroupKey = qgkey,
                Question = q,
                Patient = vm.CurrentPatient,
                Admission = vm.CurrentAdmission,
                Encounter = vm.CurrentEncounter,
                OasisManager = vm.CurrentOasisManager,
                OrderEntryManager = vm.CurrentOrderEntryManager,
            };
            pcb.ProtectedOverrideRunTime = pcb.SetupOrderEntryProtectedOverrideRunTime();
            CollectionViewSource cvs = new CollectionViewSource();
            CollectionViewSource cvs2 = new CollectionViewSource();

            if (q.DataTemplate.Trim().ToLower().StartsWith("admissiondisciplinefrequency"))
            {
                pcb.AddRowsFromIncompleteEncounters();
            }

            // Override default protection on ICD fields - allowing users with the ICDCoder role to edit them if the encounter is in CodeReview state
            if (((q.DataTemplate == "Diagnosis") || (q.DataTemplate == "DiagnosisCM") ||
                 (q.DataTemplate == "DiagnosisPCS")) && (vm.CurrentEncounter != null) &&
                (vm.CurrentEncounter.Inactive == false))
            {
                if ((vm.CurrentEncounter.EncounterStatus == (int)EncounterStatusType.CoderReview) &&
                    RoleAccessHelper.CheckPermission(RoleAccess.ICDCoder, false))
                {
                    pcb.ProtectedOverrideRunTime = false;
                }

                if (vm.CurrentEncounter.UserIsPOCOrderReviewerAndInPOCOrderReview)
                {
                    pcb.ProtectedOverrideRunTime = false;
                }
            }

            if (q.DataTemplate.Equals("Diagnosis") || (q.DataTemplate == "DiagnosisCM") ||
                (q.DataTemplate == "DiagnosisPCS") ||
                q.DataTemplate.Equals("POCDiagnosis") || (q.DataTemplate == "POCDiagnosisCM") ||
                (q.DataTemplate == "POCDiagnosisPCS") ||
                q.DataTemplate.Equals("POCDiagnosisEdit") || (q.DataTemplate == "POCDiagnosisEditCM") ||
                (q.DataTemplate == "POCDiagnosisEditPCS"))
            {
                pcb.CanTrimPrintCollection = false;
                cvs.Source = pcb.Admission.AdmissionDiagnosis;
                pcb.PrintCollection = cvs.View;
                string[] sorts = null;
                if (q.DataTemplate.Equals("Diagnosis") || (q.DataTemplate == "DiagnosisCM") ||
                    q.DataTemplate.Equals("POCDiagnosis") || (q.DataTemplate == "POCDiagnosisCM") ||
                    q.DataTemplate.Equals("POCDiagnosisEdit") || (q.DataTemplate == "POCDiagnosisEditCM"))
                {
                    pcb.SortOrder = "Version|DiagnosisStatus|Sequence";
                }
                else
                {
                    pcb.SortOrder = "Version|DiagnosisStatus|DiagnosisStartDate";
                }

                sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionDiagnosis pd = item as AdmissionDiagnosis;
                    if ((q.DataTemplate == "DiagnosisCM") || (q.DataTemplate == "POCDiagnosisCM") ||
                        (q.DataTemplate == "POCDiagnosisEditCM"))
                    {
                        if (pd.Diagnosis == false)
                        {
                            return false;
                        } // Medical Only
                    }
                    else if ((q.DataTemplate == "DiagnosisPCS") || (q.DataTemplate == "POCDiagnosisPCS") ||
                             (q.DataTemplate == "POCDiagnosisEditPCS"))
                    {
                        if (pd.Diagnosis)
                        {
                            return false;
                        } // Surgical Only
                    }

                    int version = 9;
                    if ((q.DataTemplate == "Diagnosis") || (q.DataTemplate == "DiagnosisCM") ||
                        (q.DataTemplate == "DiagnosisPCS"))
                    {
                        version = TenantSettingsCache.Current.TenantSettingRequiredICDVersionPrint(pcb.Encounter);
                    }
                    else
                    {
                        version = TenantSettingsCache.Current.TenantSettingRequiredICDVersionPrintPOC(pcb.Encounter);
                    }

                    if (pd.Version != version)
                    {
                        return false;
                    }

                    bool ret = pd.IsNew || pd.EncounterDiagnosis.Any(p => p.EncounterKey == pcb.Encounter.EncounterKey);
                    return ret;
                };
            }
            else if (q.DataTemplate.Equals("Allergy") || q.DataTemplate.Equals("DischargeAllergies") ||
                     q.DataTemplate.Equals("AllergyList"))
            {
                cvs.Source = pcb.Patient.PatientAllergy;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "AllergyStatus|Description";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    PatientAllergy pa = item as PatientAllergy;
                    return pa.IsNew || (!pa.Inactive && pa.EncounterAllergy.Any(p => (p.EncounterKey == pcb.Encounter.EncounterKey)));
                };
            }
            else if (q.DataTemplate.Equals("Medication") || q.DataTemplate.Equals("MedicationList"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientMedication.OrderBy(med => med.MedicationStatus)
                    .ThenBy(med1 => med1.MedicationName);
                pcb.PrintCollectionMeds = cvs.View;

                pcb.SortOrder = "MedicationStatus|MedicationName";

                pcb.PrintCollectionMeds.Filter = item =>
                {
                    PatientMedication pm = item as PatientMedication;
                    var accept = (pm.IsNew || pm.EncounterMedication.Any(p => p.EncounterKey == pcb.Encounter.EncounterKey)) && !pm.AddedInError;

                    // needed to print old POC's that included this stuff.  New ones exclude these rows in DynamicFormViewModel.
                    if (accept && (pcb.DynamicFormViewModel.CurrentForm.IsPlanOfCare ||
                                   pcb.DynamicFormViewModel.CurrentForm.IsOrderEntry) &&
                        pcb.DynamicFormViewModel != null
                        && pcb.DynamicFormViewModel.CurrentForm != null
                        && pcb.Encounter != null && pcb.Encounter.EncounterPlanOfCare != null)
                    {
                        var epc = pcb.Encounter.EncounterPlanOfCare.FirstOrDefault();
                        if (epc != null)
                        {
                            accept = pm.IsPOCMedication(epc.CertificationFromDate, epc.CertificationThruDate,
                                pcb.Encounter);
                        }
                    }

                    return accept;
                };

                pcb.BuildStringPrintListForMeds(pcb.PrintListOfStrings);
                cvs.Source = pcb.PrintListOfStrings;
                pcb.PrintCollection = cvs.View;
            }
            else if (q.DataTemplate.Equals("DischargeMeds"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientMedication.OrderBy(med => med.MedicationStatus)
                    .ThenBy(med1 => med1.MedicationName);
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "MedicationStatus|MedicationName";

                pcb.PrintCollection.Filter = item =>
                {
                    PatientMedication pm = item as PatientMedication;
                    var accept = (pm.IsNew || pm.EncounterMedication.Any(p => p.EncounterKey == pcb.Encounter.EncounterKey)) && !pm.AddedInError;
                    // needed to print old POC's that included this stuff.  New ones exclude these rows in DynamicFormViewModel.
                    if (accept && (pcb.DynamicFormViewModel.CurrentForm.IsPlanOfCare ||
                                   pcb.DynamicFormViewModel.CurrentForm.IsOrderEntry) &&
                        pcb.DynamicFormViewModel != null
                        && pcb.DynamicFormViewModel.CurrentForm != null
                        && pcb.Encounter != null && pcb.Encounter.EncounterPlanOfCare != null)
                    {
                        var epc = pcb.Encounter.EncounterPlanOfCare.FirstOrDefault();
                        if (epc != null)
                        {
                            accept = pm.IsPOCMedication(epc.CertificationFromDate, epc.CertificationThruDate,
                                pcb.Encounter);
                        }
                    }

                    return accept;
                };
            }
            else if ((q.DataTemplate.Equals("LevelOfCare")) || (q.DataTemplate.Equals("POCLevelOfCare")))
            {
                cvs.Source = pcb.Admission.AdmissionLevelOfCare;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "-LevelOfCareFromDate";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionLevelOfCare aloc = item as AdmissionLevelOfCare;
                    return aloc.EncounterLevelOfCare.Any(p => p.EncounterKey == pcb.Encounter.EncounterKey);
                };
                if ((q.DataTemplate.Equals("LevelOfCare")) && (vm != null) && (vm.CurrentForm != null) &&
                    (vm.CurrentForm.IsHIS))
                {
                    if ((vm.CurrentOasisManager != null) && (vm.CurrentOasisManager.IsHISVersion2orHigher == false))
                    {
                        pcb.Hidden = true;
                    }
                }
            }
            else if (q.DataTemplate.Equals("IVTherapy"))
            {
                cvs.Source = pcb.Admission.AdmissionIVSite;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "Number";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionIVSite ais = item as AdmissionIVSite;
                    if (ais.DeletedDate != null)
                    {
                        return false;
                    }

                    if (ais.IsNew)
                    {
                        return true;
                    }

                    bool accept = ais.EncounterIVSite.Any(p => p.EncounterKey == pcb.Encounter.EncounterKey);
                    return accept;
                };
            }
            else if (q.DataTemplate.Equals("PainLocation"))
            {
                cvs.Source = pcb.Admission.AdmissionPainLocation;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "PainSite";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionPainLocation apl = item as AdmissionPainLocation;
                    bool accept = apl.EncounterPainLocation.Any(p => p.EncounterKey == pcb.Encounter.EncounterKey);
                    return accept;
                };
            }
            else if (q.DataTemplate.Equals("AdmissionDisciplineFrequency"))
            {
                cvs.Source = pcb.Admission.AdmissionDisciplineFrequency;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "StartDate";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                {
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }
                }

                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionDisciplineFrequency adf = item as AdmissionDisciplineFrequency;
                    if (adf.IsNew)
                    {
                        return true;
                    }

                    if (pcb.Encounter.EncounterDisciplineFrequency.Any(e =>
                            e.DispFreqKey == adf.DisciplineFrequencyKey) == false)
                    {
                        return false;
                    }

                    return (!pcb.Admission.HospiceAdmission) || pcb.Admission.IsAdmisionDisciplineFrequencyInCurrentCert(adf);
                };
            }
            else if (q.DataTemplate.Equals("OrdersForDisciplineAndTreatment"))
            {
                pcb.BuildStringPrintList(pcb.PrintListOfStrings);
                cvs.Source = pcb.PrintListOfStrings;
                pcb.PrintCollection = cvs.View;

                cvs2.Source = pcb.DynamicFormViewModel.CurrentGoalManager.POCOrdersForDiscPrintList
                    .OrderBy(ord => ord.DisciplineCode).ThenBy(ord2 => ord2.StartDate);
                pcb.PrintCollectionForDisplay = cvs2.View;

                pcb.PrintCollection.Filter = item =>
                {
                    return true;
                };
                pcb.PrintCollectionForDisplay.Filter = item =>
                {
                    return true;
                };

                Messenger.Default.Register<bool>(pcb, "RefreshGoalElements",
                    item => { pcb.RefreshPrintCollection(); });
                pcb.CanTrimPrintCollection = true;
            }
            else if (q.DataTemplate.Equals("Infections"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientInfection;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "ConfirmationDate|ResolvedDate";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    if (pcb.Encounter != null)
                    {
                        PatientInfection pl = item as PatientInfection;
                        return (pl.IsNew || (pcb.Encounter != null &&
                                             pcb.Encounter.EncounterPatientInfection.Any(ei =>
                                                 ei.PatientInfectionKey == pl.PatientInfectionKey)));
                    }
                    else
                    {
                        PatientInfection pl = item as PatientInfection;
                        return (!pl.Superceded);
                    }
                };
            }
            else if (q.DataTemplate.Equals("AdverseEvents"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientAdverseEvent;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "EventDate|DocumentedDateTime";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    if (pcb.Encounter != null)
                    {
                        PatientAdverseEvent pae = item as PatientAdverseEvent;
                        return (pae.IsNew || (pcb.Encounter != null &&
                                              pcb.Encounter.EncounterPatientAdverseEvent.Any(ei =>
                                                  ei.PatientAdverseEventKey == pae.PatientAdverseEventKey)));
                    }
                    else
                    {
                        PatientAdverseEvent pae = item as PatientAdverseEvent;
                        return (!pae.Superceded);
                    }
                };
            }
            else if (q.DataTemplate.Equals("zOBSOLETEInfections"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Admission.AdmissionInfection;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "InfectionType|CultureDate";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    AdmissionInfection pl = item as AdmissionInfection;
                    return (pl.IsNew || pcb.Encounter.EncounterInfection.Any(ei => ei.AdmissionInfectionKey == pl.AdmissionInfectionKey));
                };
            }
            else if (q.DataTemplate.Equals("Labs"))
            {
                pcb.CanTrimPrintCollection = true;
                cvs.Source = pcb.Patient.PatientLab;
                pcb.PrintCollection = cvs.View;

                pcb.SortOrder = "-OrderDate|Test";
                string[] sorts = pcb.SortOrder.Split('|');
                for (int i = 0; i < sorts.Length; i++)
                    if (sorts[i].StartsWith("-"))
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1),
                            ListSortDirection.Descending));
                    }
                    else
                    {
                        pcb.PrintCollection.SortDescriptions.Add(new SortDescription(sorts[i],
                            ListSortDirection.Ascending));
                    }

                pcb.PrintCollection.Filter = item =>
                {
                    PatientLab pl = item as PatientLab;
                    return (pl.IsNew || pcb.Encounter.EncounterLab.Any(ei => ei.PatientLabKey == pl.PatientLabKey));
                };
            }

            return pcb;
        }
    }
}