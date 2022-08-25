#region Usings

using System.ComponentModel;
using System.Windows.Data;
using GalaSoft.MvvmLight.Command;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Services;
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class Wound : PatientCollectionBase
    {
        public RelayCommand<UserControlBaseEventArgs<AdmissionWoundSite>> AddCommand { get; protected set; }

        public Wound(int? formSectionQuestionKey) : base(formSectionQuestionKey)
        {
        }

        public override void BackupEntity(bool restore)
        {
            if (restore)
            {
            }
        }
    }

    public class WoundFactory
    {
        public static QuestionUI Create(IDynamicFormService m, DynamicFormViewModel vm, FormSection formsection,
            int pqgkey, int qgkey, int sequence, bool copyforward, Question q)
        {
            var __FormSectionQuestionKey = DynamicFormService.FindQuestionID(formsection, pqgkey, qgkey, sequence, q);

            Wound w = new Wound(__FormSectionQuestionKey)
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
            };
            foreach (AdmissionWoundSite aws in w.Admission.AdmissionWoundSite) aws.DoCalculates();
            CollectionViewSource cvs = new CollectionViewSource();
            cvs.Source = w.Admission.AdmissionWoundSite;
            cvs.SortDescriptions.Clear();
            cvs.SortDescriptions.Add(new SortDescription("Number", ListSortDirection.Ascending));
            w.PrintCollection = cvs.View;

            w.SortOrder = "Number";
            return w;
        }
    }
}