#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Ria.Sync;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.Form)]
    [Export(typeof(ICache))]
    public class DynamicFormCache : ReferenceCacheBase
    {
        public static DynamicFormCache Current { get; private set; }
        private Action Callback;

        [ImportingConstructor]
        public DynamicFormCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Form, "013")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("DynamicFormCache already initialized.");
            }

            Current = this;
        }

        public override async System.Threading.Tasks.Task Load(DateTime? lastUpdatedDate, bool isOnline,
            Action callback, bool force = false)
        {
            Callback = callback;
            LastUpdatedDate = lastUpdatedDate;
            Ticks = LastUpdatedDate?.Ticks ?? 0;
            TotalRecords = 0;

            await RemovePriorVersion();

            if (isLoading)
            {
                return;
            }

            isLoading = true;

            Context.EntityContainer.Clear();

            if ((isOnline && Ticks > 0)
                || (Ticks == 0 && isOnline &&
                    await CacheExists() ==
                    false)) //Ticks = 0, but online, got LastUpdatedDdate = NULL from GetReferenceDataInfo, still need to query server for data to build cache
            {
                if ((await RefreshReferenceCacheAsync(Ticks)) || force)
                {
                    DomainContextLoadBatch batch = new DomainContextLoadBatch(DataLoadComplete);

                    batch.Add(Context.Load(Context.GetDynamicFormQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetSectionQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetQuestionGroupQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetQuestionQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetQuestionNotificationQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetQuestionGoalQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetQuestionOasisMappingQuery(), LoadBehavior.RefreshCurrent, false));
                    batch.Add(Context.Load(Context.GetRiskAssessmentQuery(), LoadBehavior.RefreshCurrent, false));
                }
                else
                {
                    LoadFromDisk(callback);
                }
            }
            else
            {
                LoadFromDisk(callback);
            }
        }

        protected override void OnCacheNotFoundException()
        {
            var totalFormCount = TotalRecords = Context.Forms.Count();
            var totalSectionCount = Context.Sections.Count();
            var totalQuestionCount = Context.Questions.Count();
            var totalFormSectionCount = Context.FormSections.Count();

            var haveIncorrectFormData = (totalFormCount == 0 || totalSectionCount == 0 ||
                                         totalQuestionCount == 0 || totalFormSectionCount == 0);

            Log(TraceEventType.Information,
                string.Format(
                    "{0} Cache.  Directory not found.  Check that data was returned from server.  Possible that no data saved to disk.",
                    CacheName));

            if(haveIncorrectFormData)
            {
                Log(TraceEventType.Error, "No form data loaded from Form cache.");
            }
        }

        Dictionary<int, Form> FormDictionary = new Dictionary<int, Form>();

        private void BuildSearchDataStructures()
        {
            FormDictionary = Context.Forms.ToDictionary(f => f.FormKey);
        }

        protected override void OnRIACacheLoaded()
        {
            BuildSearchDataStructures();
            TotalRecords = Context.Forms.Count();
        }

        private async void DataLoadComplete(DomainContextLoadBatch batch)
        {
            List<Exception> LoadErrors = new List<Exception>();

            if (batch.FailedOperationCount > 0)
            {
                foreach (var fop in batch.FailedOperations)
                    if (fop.HasError)
                    {
                        Log(TraceEventType.Warning, "Form cache load error", fop.Error);
                        LoadErrors.Add(fop.Error);
                    }

                Context.EntityContainer.Clear();
                LoadFromDisk(Callback);

            }
            else
            {
                await PurgeAndSave();

                BuildSearchDataStructures();

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    TotalRecords = 0;
                    foreach (var _set in Context.EntityContainer.EntitySets)
                        TotalRecords += _set.Count;
                    isLoading = false;
                    Callback?.Invoke();
                    Messenger.Default.Send(CacheName, "CacheLoaded");
                });
            }
        }

        public static Form GetCMSFormByName(string cmsForm)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return null;
            }

            string formDescription = "CMSForm" + (string.IsNullOrWhiteSpace(cmsForm) ? "?" : cmsForm);
            Form f = Current.Context.Forms
                .Where(p => ((p.Description == formDescription) && p.IsAttachedForm && (p.Superceded == false)))
                .FirstOrDefault();
            if (f == null)
            {
                MessageBox.Show(String.Format(
                    "Error DynamicFormCache.GetCMSFormByName: {0} is not defined.  Contact your system administrator.",
                    formDescription));
            }

            return f;
        }

        public static Form GetFormByKey(int formkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return null;
            }

            {
                Form form = null;
                Current.FormDictionary.TryGetValue(formkey, out form);
                return form;
            }
        }

        public static Form GetAttemptedForm()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return null;
            }

            return Current.Context.Forms.Where(p => (p.IsAttempted && (p.Superceded = false))).FirstOrDefault();
        }

        public static string GetAdmissionDocumentationFormTypeDescriptionByKey(int formkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return null;
            }

            Form f = Current.Context.Forms.Where(p => p.FormKey == formkey).FirstOrDefault();
            if (f == null)
            {
                return null;
            }

            return f.AdmissionDocumentationFormTypeDescription;
        }

        public static IEnumerable<Section> GetSections()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Sections == null))
            {
                return null;
            }

            return Current.Context.Sections;
        }

        public static IEnumerable<FormSection> GetReEvaluateFormSectionsByFormKey(int formkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.FormSections == null))
            {
                return null;
            }

            return Current.Context.FormSections.Where(p => p.FormKey == formkey && p.ReEvaluate)
                .OrderBy(p => p.Sequence);
        }

        public static FormSection GetFormSectionByKey(int formsectionkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.FormSections == null))
            {
                return null;
            }

            return Current.Context.FormSections.Where(p => p.FormSectionKey == formsectionkey).FirstOrDefault();
        }

        public static FormSectionQuestion GetFormSectionQuestionByKey(int? formsectionquestionkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.FormSectionQuestions == null) ||
                (formsectionquestionkey == null))
            {
                return null;
            }

            return Current.Context.FormSectionQuestions.Where(p => p.FormSectionQuestionKey == formsectionquestionkey)
                .FirstOrDefault();
        }

        public static int GetSectionKeyFromFormSectionKey(int formsectionkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.FormSections == null))
            {
                return -1;
            }

            var item = Current.Context.FormSections.Where(p => p.FormSectionKey == formsectionkey).FirstOrDefault();
            return item == null ? -1 : item.SectionKey.Value;
        }

        public static IEnumerable<Question> GetEquipmentQuestions()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null))
            {
                return null;
            }

            return Current.Context.Questions.Where(q => q.DataTemplate == "Equipment").OrderBy(q => q.LookupType);
        }

        public static Question GetQuestionByKey(int questionKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null))
            {
                return null;
            }

            return Current.Context.Questions.Where(p => p.QuestionKey == questionKey).FirstOrDefault();
        }

        public static int GetQuestionKeyByLabel(string questionLabel)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null) ||
                string.IsNullOrWhiteSpace(questionLabel))
            {
                return 0;
            }

            string label = questionLabel.ToLower();
            Question q = Current.Context.Questions.Where(p => ((p.Label != null) && (p.Label.ToLower() == label)))
                .FirstOrDefault();
            return (q == null) ? 0 : q.QuestionKey;
        }

        public static int GetQuestionKeyByLabelStartsWith(string questionLabelStartsWith)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null) ||
                string.IsNullOrWhiteSpace(questionLabelStartsWith))
            {
                return 0;
            }

            string labelStartsWith = questionLabelStartsWith.ToLower();
            Question q = Current.Context.Questions
                .Where(p => ((p.Label != null) && p.Label.ToLower().StartsWith(labelStartsWith))).FirstOrDefault();
            return (q == null) ? 0 : q.QuestionKey;
        }

        public static int GetQuestionKeyByLabelAndDataTemplate(string questionLabel, string dataTemplate)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null) ||
                string.IsNullOrWhiteSpace(questionLabel) || string.IsNullOrWhiteSpace(dataTemplate))
            {
                return 0;
            }

            string label = questionLabel.ToLower();
            string template = dataTemplate.ToLower();
            Question q = Current.Context.Questions.Where(p =>
                ((p.Label != null) && (p.Label.ToLower() == label) && (p.DataTemplate != null) &&
                 (p.DataTemplate.ToLower() == template) && (p.TenantID == null))).FirstOrDefault();
            return (q == null) ? 0 : q.QuestionKey;
        }

        public static int GetQuestionKeyByLabelAndLookupType(string questionLabel, string lookupType)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null) ||
                string.IsNullOrWhiteSpace(questionLabel) || string.IsNullOrWhiteSpace(lookupType))
            {
                return 0;
            }

            string label = questionLabel.ToLower();
            string lookup = lookupType.ToLower();
            Question q = Current.Context.Questions.Where(p =>
                ((p.Label != null) && (p.Label.ToLower() == label) && (p.LookupType != null) &&
                 (p.LookupType.ToLower() == lookup))).FirstOrDefault();
            return (q == null) ? 0 : q.QuestionKey;
        }

        public static List<Question> GetQuestionByDataTemplate(string dataTemplate)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null))
            {
                return null;
            }

            return Current.Context.Questions.Where(p => p.DataTemplate.Contains(dataTemplate)).ToList();
        }

        public static Question GetSingleQuestionByDataTemplate(string dataTemplate)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null) ||
                string.IsNullOrWhiteSpace(dataTemplate))
            {
                return null;
            }

            return Current.Context.Questions.Where(p => p.DataTemplate == dataTemplate).FirstOrDefault();
        }

        public static List<Question> GetQuestionByLabelContains(string questionLabel)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null))
            {
                return null;
            }

            return Current.Context.Questions.Where(p => p.Label.Contains(questionLabel)).ToList();
        }

        public static List<Question> GetRFDQuestions()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null))
            {
                return null;
            }

            return Current.Context.Questions.Where(p => p.UseFunctionalDeficit).ToList();
        }

        public static List<Question> GetQuestionsBySearch(string value)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null))
            {
                return null;
            }

            string[] templates =
            {
                "FuncDeficit", "CodeLookupMulti", "PTLevelofAssist", "PTLevelofAssistNoLabel", "LocationMulti",
                "GaitPerformance", "Stairs", "Surface", "DisciplineRefer"
            };
            string[] factories = { "QuestionBase" };

            //If user searches for all questions(ie. no search criteria entered), then must set value to ''
            if (string.IsNullOrEmpty(value))
            {
                value = "";
            }

            return Current.Context.Questions
                .Where(p => templates.Contains(p.DataTemplate) && factories.Contains(p.BackingFactory) &&
                            p.Label.ToLower().Contains(value.ToLower())).ToList();
        }

        public static QuestionGroup GetQuestionGroupByKey(int questiongroupKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.QuestionGroups == null))
            {
                return null;
            }

            return Current.Context.QuestionGroups.Where(p => p.QuestionGroupKey == questiongroupKey).FirstOrDefault();
        }

        public static bool PrintAsSSRS(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.PrintAsSSRS == 1;
            }

            return false;
        }

        public static bool IsOrderEntry(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsOrderEntry;
            }

            return false;
        }

        public static bool IsHospiceElectionAddendum(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsHospiceElectionAddendum;
            }

            return false;
        }

        public static bool IsPlanOfCare(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsPlanOfCare;
            }

            return false;
        }

        public static bool IsTeamMeeting(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsTeamMeeting;
            }

            return false;
        }

        public static bool IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry(int formKey)
        {
            Current?.EnsureCacheReady();
            return (IsPlanOfCare(formKey) || IsTeamMeeting(formKey) || IsOrderEntry(formKey));
        }

        public static bool IsVisit(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form == null)
            {
                return false;
            }

            if (form.IsVisitTeleMonitoring)
            {
                return false;
            }

            return form.IsVisit;
        }

        public static bool IsWOCN(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form == null)
            {
                return false;
            }

            return form.IsWOCN;
        }

        public static bool IsAuthorizationRequest(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form == null)
            {
                return false;
            }

            return form.IsAuthorizationRequest;
        }

        public static bool IsVisitTeleMonitoring(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form == null)
            {
                return false;
            }

            return form.IsVisitTeleMonitoring;
        }

        public static bool IsPreEval(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsPreEval;
            }

            return false;
        }

        public static bool IsEval(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsEval;
            }

            return false;
        }

        public static bool IsResumption(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsResumption;
            }

            return false;
        }

        public static bool IsAttempted(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsAttempted;
            }

            return false;
        }

        public static bool IsDischarge(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsDischarge;
            }

            return false;
        }

        public static bool IsTransfer(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsTransfer;
            }

            return false;
        }

        public static bool IsCOTI(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsCOTI;
            }

            return false;
        }

        public static bool IsVerbalCOTI(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsVerbalCOTI;
            }

            return false;
        }

        public static bool IsHospiceF2F(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsHospiceF2F;
            }

            return false;
        }

        public static bool IsOasis(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsOasis;
            }

            return false;
        }

        public static bool IsHIS(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Forms == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            if (form != null)
            {
                return form.IsHIS;
            }

            return false;
        }

        public static bool FormExists(int formKey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.Questions == null))
            {
                return false;
            }

            Form form = GetFormByKey(formKey);
            return (form != null);
        }

        public static RiskAssessment GetRiskAssessmentByKey(int rakey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.RiskAssessments == null) ||
                (Current.Context.RiskAssessmentLayouts == null))
            {
                return null;
            }

            return Current.Context.RiskAssessments.Where(p => p.RiskAssessmentKey == rakey).FirstOrDefault();
        }

        public static RiskAssessment GetRiskAssessmentByLabel(string label)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.RiskAssessments == null) ||
                (Current.Context.RiskAssessmentLayouts == null))
            {
                return null;
            }

            return Current.Context.RiskAssessments.Where(p => ((p.Label == label) && (p.Superceded == false)))
                .FirstOrDefault();
        }

        public static RiskRange GetRiskRangeByKey(int rakey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.RiskAssessmentLayouts == null))
            {
                return null;
            }

            return Current.Context.RiskRanges.Where(p => p.RiskRangeKey == rakey).FirstOrDefault();
        }

        public static string GetRiskRangeLabelByKey(int? rakey)
        {
            Current?.EnsureCacheReady();
            if (rakey == null)
            {
                return null;
            }

            RiskRange rr = GetRiskRangeByKey((int)rakey);
            return (rr == null) ? null : ((string.IsNullOrWhiteSpace(rr.Label)) ? null : rr.Label.Trim());
        }

        public static IEnumerable<RiskAssessmentLayout> GetRiskAssessmentLayoutByKey(int rakey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.RiskAssessmentLayouts == null))
            {
                return null;
            }

            return Current.Context.RiskAssessmentLayouts.Where(p => p.RiskAssessmentKey == rakey)
                .OrderBy(p => p.Sequence);
        }

        public static RiskRange GetRiskRangeByKeyandScore(int rakey, int score)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.RiskRanges == null))
            {
                return null;
            }

            return Current.Context.RiskRanges
                .Where(p => p.RiskAssessmentKey == rakey && p.LowValue <= score && p.HighValue >= score)
                .FirstOrDefault();
        }

        public static List<Form> GetForms(int TenantID)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) || (Current.Context.FormSections == null))
            {
                return null;
            }

            return Current.Context.Forms.Where(p => p.TenantID == TenantID && p.Superceded == false).ToList();
        }
    }
}