#region Usings

using System;
using System.ComponentModel.Composition;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Core.Model;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
using System.Linq;
using Virtuoso.Linq;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IFaxService))]
    public class FaxService : IFaxService
    {
        static readonly LogWriter logWriter;

        static FaxService()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
        }

        private VirtuosoDomainContext Context { get; set; }

        [ImportingConstructor]
        public FaxService(IUriService _uriService)
        {
            if (_uriService != null)
            {
                Context = new VirtuosoDomainContext(new Uri(_uriService.Uri,
                    "Virtuoso-Services-Web-VirtuosoDomainService.svc")); //using alternate constructor, so that it can run in a thread
            }
            else
            {
                Context = new VirtuosoDomainContext();
            }
        }

        public void FaxDocument(EncounterFaxParameters parameters, Action successCallBack, Action finallyCallback)
        {
            Log(
                $"[{nameof(FaxService)}.{nameof(FaxDocument)}] faxNumber: {parameters.FaxNumber}, physicianKey: {parameters.PhysicianKey}, formKey: {parameters.FormKey}, patientKey: {parameters.PatientKey}, encounterKey: {parameters.EncounterKey}, admissionKey: {parameters.AdmissionKey}, HideOasisQuestions: {parameters.HideOasisQuestions}",
                "FAXAPI_TRACE");

            Context.FaxDocument(
                    parameters.FaxNumber,
                    parameters.PhysicianKey,
                    parameters.FormKey,
                    parameters.PatientKey,
                    parameters.EncounterKey,
                    parameters.AdmissionKey,
                    parameters.HideOasisQuestions)
                .AsTask()
                .ContinueWith(t =>
                    {
                        try
                        {
                            if (t.IsFaulted)
                            {
                                HandleError(t, "FaxDocument");
                            }
                            else
                            {
                                successCallBack.Invoke();
                            }
                        }
                        catch (Exception exp)
                        {
                            CheckForError(exp);
                        }
                        finally
                        {
                            finallyCallback.Invoke();
                        }
                    },
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }

        public void FaxInterimOrderBatchDocuments(string interimOrderBatchKeys, Action successCallBack,
            Action finallyCallback)
        {
            Log(
                $"[{nameof(FaxService)}.{nameof(FaxInterimOrderBatchDocuments)}] Faxing InterimOrderBatchKeys: {interimOrderBatchKeys}",
                "FAXAPI_TRACE");

            var allIds = interimOrderBatchKeys.Split('|').Select(b => Convert.ToInt32(b));
            foreach (var batch in Context.InterimOrderBatches.Where(b => allIds.Contains(b.InterimOrderBatchKey)))
            {
                batch.FaxStarted = DateTime.Now;
            }

            Context.FaxInterimOrderBatchDocuments(interimOrderBatchKeys)
                .AsTask()
                .ContinueWith(t =>
                    {
                        try
                        {
                            if (t.IsFaulted)
                            {
                                HandleError(t, "FaxInterimOrderBatchDocuments");
                            }
                            else
                            {
                                successCallBack.Invoke();
                            }
                        }
                        catch (Exception exp)
                        {
                            CheckForError(exp);
                        }
                        finally
                        {
                            finallyCallback.Invoke();
                        }
                    },
                    System.Threading.CancellationToken.None,
                    System.Threading.Tasks.TaskContinuationOptions.None,
                    Client.Utils.AsyncUtility.TaskScheduler);
        }

        protected static void HandleError(System.Threading.Tasks.Task ret, string tag)
        {
            if (ret.Exception is AggregateException)
            {
                ret.Exception.Flatten()
                    .Handle(x =>
                    {
                        if (x.InnerException != null)
                        {
                            Controls.ErrorWindow.CreateNew(String.Format("[000] {0}", tag), x.InnerException);
                        }
                        else
                        {
                            Controls.ErrorWindow.CreateNew(String.Format("[001] {0}", tag), x);
                        }

                        return true; //exception was handled.
                    });
            }
            else if (ret.Exception != null)
            {
                Controls.ErrorWindow.CreateNew(String.Format("[002] {0}", tag), ret.Exception);
            }
        }

        protected void CheckForError(Events.EntityEventArgs<HomeScreenTaskPM> e)
        {
            if (e.Error != null)
            {
                Controls.ErrorWindow.CreateNew(GetType().FullName, e);
            }
        }

        protected void CheckForError(Exception e)
        {
            if (e != null)
            {
                Controls.ErrorWindow.CreateNew(GetType().FullName, e);
            }
        }

        private static void Log(string message, string subCategory,
            TraceEventType traceEventType = TraceEventType.Information)
        {
            string category = "FaxService";

            var __category = string.IsNullOrEmpty(subCategory)
                ? category
                : string.Format("{0}-{1}", category, subCategory);
            logWriter.Write(message,
                new[] { __category }, //category
                0, //priority
                0, //eventid
                traceEventType);
        }
    }
}