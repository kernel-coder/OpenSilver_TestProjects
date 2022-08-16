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
    public interface IFormMaintenanceService : IMultiModelDataService, ICleanup
    {
        EntitySet<Form> Forms { get; }
        EntitySet<Section> Sections { get; }
        EntitySet<RiskAssessment> RiskAssessments { get; }
        EntitySet<Question> Questions { get; }
        EntitySet<QuestionGroup> QuestionGroups { get; }
        void GetAsync();
        void Remove(Form f);
        void Remove(FormSection fs);
        void Remove(FormSectionQuestion fsq);
        void Remove(QuestionGroup qg);
        void Remove(QuestionGroupQuestion qgq);
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IFormMaintenanceService))]
    public class FormMaintenanceService : PagedModelBase, IFormMaintenanceService
    {
        public VirtuosoDomainContext FormContext { get; set; }

        public bool IsSubmitting => FormContext.IsSubmitting;

        public FormMaintenanceService()
        {
            FormContext = new VirtuosoDomainContext();
            FormContext.PropertyChanged += Context_PropertyChanged;
        }

        public void Remove(Form f)
        {
            f.FormSection.ForEach(fs => Remove(fs));
            FormContext.Forms.Remove(f);
        }

        public void Remove(FormSection fs)
        {
            fs.FormSectionQuestion.ForEach(fsq => Remove(fsq));
            FormContext.FormSections.Remove(fs);
        }

        public void Remove(FormSectionQuestion fsq)
        {
            fsq.FormSectionQuestionAttribute.ForEach(attr => Remove(attr));
            FormContext.FormSectionQuestions.Remove(fsq);
        }

        public void Remove(FormSectionQuestionAttribute fsqa)
        {
            FormContext.FormSectionQuestionAttributes.Remove(fsqa);
        }

        public void Remove(QuestionGroup qg)
        {
            qg.QuestionGroupQuestion.ForEach(qgq => Remove(qgq));
            FormContext.QuestionGroups.Remove(qg);
        }

        public void Remove(QuestionGroupQuestion qgq)
        {
            FormContext.QuestionGroupQuestions.Remove(qgq);
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

                var formquery = FormContext.GetFormsForMaintenanceQuery();

                var sectionquery = FormContext.GetSectionQuery();

                var questionquery = FormContext.GetQuestionQuery();

                var questiongroupquery = FormContext.GetQuestionGroupQuery();

                var riskquery = FormContext.GetRiskAssessmentQuery();

                DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

                batch.Add(FormContext.Load(formquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(sectionquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(questionquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(questiongroupquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(riskquery, LoadBehavior.RefreshCurrent, false));
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
                    Forms = FormContext.Forms;
                    Sections = FormContext.Sections;
                    RiskAssessments = FormContext.RiskAssessments;
                    Questions = FormContext.Questions;
                    QuestionGroups = FormContext.QuestionGroups;

                    IsLoading = false;

                    OnMultiLoaded(this, new MultiErrorEventArgs(LoadErrors));
                });
            }
            else
            {
                IsLoading = false;
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
                if (preSubmitAction != null)
                {
                    preSubmitAction.Invoke();
                }

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
                IsLoading = false;

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
                Dispatcher.BeginInvoke(() =>
                {
                    IsLoading = false;
                    OnMultiSaved(this, new MultiErrorEventArgs(Errors, EntityErrors));
                });
            }
        }

        public void RejectMultiChanges()
        {
            FormContext.RejectChanges();
        }

        private EntitySet<Form> _Forms;

        public EntitySet<Form> Forms
        {
            get { return _Forms; }
            set { _Forms = value; }
        }

        private EntitySet<Section> _Sections;

        public EntitySet<Section> Sections
        {
            get { return _Sections; }
            set { _Sections = value; }
        }

        private EntitySet<RiskAssessment> _RiskAssessments;

        public EntitySet<RiskAssessment> RiskAssessments
        {
            get { return _RiskAssessments; }
            set { _RiskAssessments = value; }
        }

        private EntitySet<Question> _Questions;

        public EntitySet<Question> Questions
        {
            get { return _Questions; }
            set { _Questions = value; }
        }

        private EntitySet<QuestionGroup> _QuestionGroups;

        public EntitySet<QuestionGroup> QuestionGroups
        {
            get { return _QuestionGroups; }
            set { _QuestionGroups = value; }
        }

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public bool ContextHasChanges => FormContext?.HasChanges ?? false;

        public void Cleanup()
        {
            FormContext.PropertyChanged -= Context_PropertyChanged;
        }
    }
}