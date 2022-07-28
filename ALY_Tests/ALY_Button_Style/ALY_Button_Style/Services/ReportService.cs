#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
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
    public interface IReportService : IMultiModelDataService
    {
        EntitySet<Report> Reports { get; }
        EntitySet<Question> Questions { get; }
        EntitySet<RiskAssessment> Risks { get; }
        void GetAsync();
        void Remove(Report r);
        void Remove(ReportParameter rp);
        void Remove(ReportOutput ro);
        void Remove(UserReportParameter urp);
        void Remove(UserReportOutput uro);
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IReportService))]
    public class ReportService : PagedModelBase, IReportService
    {
        public VirtuosoDomainContext ReportContext { get; set; }
        public VirtuosoDomainContext FormContext { get; set; }
        public bool IsSubmitting => FormContext.IsSubmitting || ReportContext.IsSubmitting;

        public ReportService()
        {
            ReportContext = new VirtuosoDomainContext();
            FormContext = new VirtuosoDomainContext();
        }

        public void Remove(Report r)
        {
            r.UserReportSet.ForEach(urs => { Remove(urs); });

            r.ReportOutput.ForEach(ro => { Remove(ro); });

            r.ReportParameter.ForEach(rp => { Remove(rp); });

            ReportContext.Reports.Remove(r);
        }

        public void Remove(ReportParameter rp)
        {
            ReportContext.ReportParameters.Remove(rp);
        }

        public void Remove(ReportOutput ro)
        {
            ReportContext.ReportOutputs.Remove(ro);
        }

        public void Remove(UserReportSet urs)
        {
            urs.UserReportOutput.ForEach(uro => { Remove(uro); });

            urs.UserReportParameter.ForEach(urp => { Remove(urp); });

            ReportContext.UserReportSets.Remove(urs);
        }

        public void Remove(UserReportParameter urp)
        {
            ReportContext.UserReportParameters.Remove(urp);
        }

        public void Remove(UserReportOutput uro)
        {
            ReportContext.UserReportOutputs.Remove(uro);
        }

        public override void LoadData()
        {
            if (IsLoading || ReportContext == null)
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

                ReportContext.RejectChanges();
                ReportContext.EntityContainer.Clear();

                FormContext.RejectChanges();
                FormContext.EntityContainer.Clear();

                var reportquery = ReportContext.GetReportQuery();

                var questionquery = FormContext.GetQuestionQuery().Where(p => p.ColumnWidth != null);

                var riskquery = FormContext.GetRiskAssessmentQuery().Where(p => !p.Superceded);

                DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

                batch.Add(ReportContext.Load(reportquery, LoadBehavior.RefreshCurrent, false));
                batch.Add(FormContext.Load(questionquery, LoadBehavior.RefreshCurrent, false));
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

            if (ReportContext.HasChanges)
            {
                if (preSubmitAction != null)
                {
                    preSubmitAction.Invoke();
                }

                batch.Add(
                    ReportContext.SubmitChanges(results =>
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
            ReportContext.RejectChanges();
        }

        public EntitySet<Report> Reports => ReportContext.Reports;
        public EntitySet<Question> Questions => FormContext.Questions;
        public EntitySet<RiskAssessment> Risks => FormContext.RiskAssessments;

        public bool ContextHasChanges => ReportContext.HasChanges;

        void Context_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e != null && e.PropertyName.Equals("HasChanges"))
            {
                RaisePropertyChanged("ContextHasChanges");
            }
        }

        public void Cleanup()
        {
            ReportContext.PropertyChanged -= Context_PropertyChanged;
            //ReportContext = null; //this may cause errors with DetailControlBase/ChildControlBase
        }
    }
}