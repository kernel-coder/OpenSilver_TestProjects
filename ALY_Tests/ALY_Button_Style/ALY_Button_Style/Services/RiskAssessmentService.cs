#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
//for ObservableCollection.ForEach

#endregion

namespace Virtuoso.Core.Services
{
    public interface IRiskAssessmentService : IMultiModelDataService, ICleanup
    {
        EntitySet<RiskAssessment> RiskAssessments { get; }
        EntitySet<RiskGroup> RiskGroups { get; }
        EntitySet<RiskQuestion> RiskQuestions { get; }
        EntitySet<Question> URLs { get; }
        EntitySet<Form> Forms { get; }
        EntitySet<FormSection> FormSections { get; }
        void GetAsync();
        void Remove(RiskAssessmentLayout ral);
        void Remove(RiskGroup rg);
        void Remove(RiskGroupQuestion rgq);
        void Remove(RiskRange rr);
        void Remove(FormSection fs);
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IRiskAssessmentService))]
    public class RiskAssessmentService : PagedModelBase, IRiskAssessmentService
    {
        public VirtuosoDomainContext FormContext { get; set; }

        public bool IsSubmitting => FormContext.IsSubmitting;

        public RiskAssessmentService()
        {
            FormContext = new VirtuosoDomainContext();
            FormContext.PropertyChanged += Context_PropertyChanged;
        }

        public void Remove(RiskAssessmentLayout ral)
        {
            FormContext.RiskAssessmentLayouts.Remove(ral);
        }

        public void Remove(RiskGroup rg)
        {
            rg.RiskGroupQuestion.ForEach(Remove);
            FormContext.RiskGroups.Remove(rg);
        }

        public void Remove(RiskGroupQuestion rgq)
        {
            FormContext.RiskGroupQuestions.Remove(rgq);
        }

        public void Remove(RiskRange rr)
        {
            FormContext.RiskRanges.Remove(rr);
        }

        public void Remove(FormSection fs)
        {
            fs.FormSectionQuestion.ForEach(Remove);
            FormContext.FormSections.Remove(fs);
        }

        public void Remove(FormSectionQuestion fsq)
        {
            FormContext.FormSectionQuestions.Remove(fsq);
        }

        public override void LoadData()
        {
            if (IsLoading || FormContext == null)
            {
                return;
            }

            IsLoading = true;

            GetAsync();
        }

        public void GetAsync()
        {
            Dispatcher.BeginInvoke(() =>
            {
                IsLoading = true;

                FormContext.RejectChanges();
                FormContext.EntityContainer.Clear();

                var riskquery = FormContext.GetRiskAssessmentForMaintenanceQuery();

                var riskgroupquery = FormContext.GetRiskGroupQuery();

                var riskquestionquery = FormContext.GetRiskQuestionQuery();

                var formquery = FormContext.GetFormsForRiskAssessmentMaintenanceQuery();

                var questionquery = FormContext.GetQuestionQuery().Where(p => p.DataTemplate.Equals("URL"));

                DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

                batch.Add(FormContext.Load(riskquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(riskgroupquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(riskquestionquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(formquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(questionquery, LoadBehavior.RefreshCurrent, false));
            });
        }

        private void DataLoadComplete(DomainContextLoadBatch batch)
        {
            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        LoadErrors.Add(fop.Error);
                    }
            }

            IsLoading = true;

            if (OnMultiLoaded != null)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    RiskAssessments = FormContext.RiskAssessments;
                    RiskGroups = FormContext.RiskGroups;
                    RiskQuestions = FormContext.RiskQuestions;
                    URLs = FormContext.Questions;
                    Forms = FormContext.Forms;
                    FormSections = FormContext.FormSections;

                    IsLoading = false;

                    OnMultiLoaded(this, new MultiErrorEventArgs(LoadErrors));
                });
            }
        }

        public event EventHandler<MultiErrorEventArgs> OnMultiLoaded;

        public event EventHandler<MultiErrorEventArgs> OnMultiSaved;

        public void SaveMultiAsync()
        {
            SaveMultiAsync(null);
        }

        public void SaveMultiAsync(Action preSubmitAction)
        {
            IsLoading = true; //probably should rename to IsBusy...

            DomainContextSubmitBatch batch = new DomainContextSubmitBatch(DataSubmitComplete);

            if (FormContext.HasChanges)
            {
                preSubmitAction?.Invoke();

                batch.Add(
                    FormContext.SubmitChanges(results =>
                    {
                        if (results.HasError)
                        {
                            results.MarkErrorAsHandled();
                        }
                    }, null)
                );
            }

            if (batch.PendingOperationCount == 0)
            {
                if (OnMultiSaved != null)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        List<Exception> Errors = new List<Exception>();
                        List<string> EntityErrors = new List<string>();

                        OnMultiSaved(this, new MultiErrorEventArgs(Errors, EntityErrors));
                    });
                }
            }
        }

        private void DataSubmitComplete(DomainContextSubmitBatch batch)
        {
            List<Exception> Errors = new List<Exception>();
            List<string> EntityErrors = new List<string>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                {
                    if (fop.HasError)
                    {
                        Errors.Add(fop.Error);
                    }

                    foreach (var entity in fop.EntitiesInError)
                    {
                        string name = entity.GetType().Name;
                        foreach (var err in entity.ValidationErrors) EntityErrors.Add(name + ": " + err.ErrorMessage);
                    }
                }
            }

            IsLoading = true;

            if (OnMultiSaved != null)
            {
                Dispatcher.BeginInvoke(() => { OnMultiSaved(this, new MultiErrorEventArgs(Errors, EntityErrors)); });
            }
        }

        public void RejectMultiChanges()
        {
            FormContext.RejectChanges();
        }

        private EntitySet<RiskAssessment> _RiskAssessments;

        public EntitySet<RiskAssessment> RiskAssessments
        {
            get { return _RiskAssessments; }
            set { _RiskAssessments = value; }
        }

        private EntitySet<RiskGroup> _RiskGroups;

        public EntitySet<RiskGroup> RiskGroups
        {
            get { return _RiskGroups; }
            set { _RiskGroups = value; }
        }

        private EntitySet<RiskQuestion> _RiskQuestions;

        public EntitySet<RiskQuestion> RiskQuestions
        {
            get { return _RiskQuestions; }
            set { _RiskQuestions = value; }
        }

        private EntitySet<Question> _URLs;

        public EntitySet<Question> URLs
        {
            get { return _URLs; }
            set { _URLs = value; }
        }

        private EntitySet<Form> _Forms;

        public EntitySet<Form> Forms
        {
            get { return _Forms; }
            set { _Forms = value; }
        }

        private EntitySet<FormSection> _FormSections;

        public EntitySet<FormSection> FormSections
        {
            get { return _FormSections; }
            set { _FormSections = value; }
        }

        public bool ContextHasChanges => FormContext.HasChanges;

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            FormContext.PropertyChanged -= Context_PropertyChanged;
        }
    }
}