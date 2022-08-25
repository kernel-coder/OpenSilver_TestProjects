#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Resources;
using System.Threading;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Infrastructure;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    internal class OasisLayoutIndex
    {
        Dictionary<int, OasisLayout> OasisLayoutIdx = new Dictionary<int, OasisLayout>();
        Dictionary<int, OasisAnswer> OasisAnswerIdx = new Dictionary<int, OasisAnswer>();
        Dictionary<int, OasisQuestion> OasisQuestionIdx = new Dictionary<int, OasisQuestion>();

        Dictionary<int, Dictionary<string, Dictionary<string, OasisLayout>>> OasisLayoutByVersionRecordTypeCMSFieldIdx =
            new Dictionary<int, Dictionary<string, Dictionary<string, OasisLayout>>>();

        Dictionary<int, ICollection<OasisAnswer>> OasisAnswerByLayoutKeyIdx = new Dictionary<int, ICollection<OasisAnswer>>();

        public void IndexOasisAnswerByKeys(EntitySet<OasisAnswer> set)
        {
            foreach (var oa in set)
            {
                OasisAnswerIdx.Add(oa.OasisAnswerKey, oa);
                oa.SetCachedOasisLayout(GetOasisLayoutByOasisLayoutKey(oa.OasisLayoutKey));
            }
        }

        public void IndexOasisQuestionByKeys(EntitySet<OasisQuestion> set)
        {
            foreach (var oq in set)
            {
                OasisQuestionIdx.Add(oq.OasisQuestionKey, oq);
                oq.SetCachedOasisLayout(GetOasisLayoutByOasisLayoutKey(oq.OasisLayoutKey));
            }
        }

        public void IndexOasisLayoutWithOasisAnswers(OasisVersion oasisVersion)
        {
            try
            {
                foreach (var oasisLayout in oasisVersion.OasisLayout)
                {
                    OasisLayoutIdx[oasisLayout.OasisLayoutKey] = oasisLayout;

                    if (OasisLayoutByVersionRecordTypeCMSFieldIdx.ContainsKey(oasisLayout.OasisVersionKey) == false)
                    {
                        OasisLayoutByVersionRecordTypeCMSFieldIdx[oasisLayout.OasisVersionKey] =
                            new Dictionary<string, Dictionary<string, OasisLayout>>();
                    }

                    if (OasisLayoutByVersionRecordTypeCMSFieldIdx[oasisLayout.OasisVersionKey]
                            .ContainsKey(oasisLayout.RecordType) == false)
                    {
                        OasisLayoutByVersionRecordTypeCMSFieldIdx[oasisLayout.OasisVersionKey][oasisLayout.RecordType] =
                            new Dictionary<string, OasisLayout>();
                    }

                    OasisLayoutByVersionRecordTypeCMSFieldIdx[oasisLayout.OasisVersionKey][oasisLayout.RecordType][
                        oasisLayout.CMSField] = oasisLayout;

                    OasisAnswerByLayoutKeyIdx[oasisLayout.OasisLayoutKey] = oasisLayout.OasisAnswer;
                }
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("[OasisLayoutIndex.Add] oasisVersion.OasisVersionKey: {0}",
                    oasisVersion.OasisVersionKey);
            }
        }

        public OasisAnswer GetOasisAnswerByKey(int oasisAnswerKey)
        {
            try
            {
                return OasisAnswerIdx[oasisAnswerKey];
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("[GetOasisAnswerByKey] Could not find oasisAnswerKey: {0}",
                    oasisAnswerKey);
                return null;
            }
        }

        public OasisQuestion GetOasisQuestionByKey(int oasisQuestionKey)
        {
            try
            {
                return OasisQuestionIdx[oasisQuestionKey];
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine("[GetOasisQuestionByKey] Could not find oasisQuestionKey: {0}",
                    oasisQuestionKey);
                return null;
            }
        }

        public OasisLayout GetOasisLayoutByOasisLayoutKey(int oasisLayoutKey)
        {
            try
            {
                return OasisLayoutIdx[oasisLayoutKey];
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[GetOasisLayoutByOasisLayoutKey] Could not find oasisLayoutKey: {0}", oasisLayoutKey);
                return null;
            }
        }

        public OasisLayout GetOasisLayout(int oasisVersionKey, string recordType, string cmsField)
        {
            try
            {
                return OasisLayoutByVersionRecordTypeCMSFieldIdx[oasisVersionKey][recordType][cmsField];
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[GetOasisLayout] Could not find oasisVersionKey: {0} - recordType: {1} - cmsField: {2}",
                    oasisVersionKey, recordType, cmsField);
                return null;
            }
        }

        public ICollection<OasisAnswer> GetOasisAnswers(int oasisVersionKey, string recordType, string cmsField)
        {
            try
            {
                var oasisLayout = OasisLayoutByVersionRecordTypeCMSFieldIdx[oasisVersionKey][recordType][cmsField];
                return OasisAnswerByLayoutKeyIdx[oasisLayout.OasisLayoutKey];
            }
            catch (Exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    "[GetOasisAnswers] Could not find oasisVersionKey: {0} - recordType: {1} - cmsField: {2}",
                    oasisVersionKey, recordType, cmsField);
                return null;
            }
        }
    }

    [ExportMetadata("CacheName", ReferenceTableName.Oasis)]
    [Export(typeof(ICache))]
    public class OasisCache : ReferenceCacheBase<OasisVersion>
    {
        public static OasisCache Current { get; private set; }
        private OasisLayoutIndex OasisLayoutIndex { get; set; }

        [ImportingConstructor]
        public OasisCache(ILogger logManager)
            : base(logManager, ReferenceTableName.Oasis, "011")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("OasisCache already initialized.");
            }

            Current = this;
            OasisLayoutIndex = new OasisLayoutIndex();
        }

        protected override EntitySet EntitySet => Context.OasisVersions;

        protected override EntityQuery<OasisVersion> GetEntityQuery()
        {
            return Context.GetOasisForCacheQuery();
        }

        protected override void OnRIACacheLoaded()
        {
            IndexOasisData();
        }

        protected override void OnCacheSaved()
        {
            IndexOasisData();
        }

        private void IndexOasisData()
        {
            //Create in-memory index for OasisLayout
            foreach (var ov in Context.OasisVersions) OasisLayoutIndex.IndexOasisLayoutWithOasisAnswers(ov);
            OasisLayoutIndex.IndexOasisAnswerByKeys(Context.OasisAnswers);
            OasisLayoutIndex.IndexOasisQuestionByKeys(Context.OasisQuestions);
        }

        public static bool isOasisCacheLoaded()
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null) ||
                (Current.Context.OasisVersions.FirstOrDefault() == null))
            {
                MessageBox.Show("Error OasisCache: OASIS Cache not loaded.  Contact your system administrator.");
                return false;
            }

            return true;
        }

        public static bool IsOasisVersion2point0(int versionKey)
        {
            OasisVersion ov = GetOasisVersionByVersionKey(versionKey);
            if (ov == null)
            {
                return false;
            }

            return (ov.VersionCD1 == "02.00") ? true : false;
        }

        public static string OasisHelpResourceURI(int versionKey)
        {
            //Should return a string of the format: "/Virtuoso;component/Assets/Resources/Oasis_0X.XX_Chapter_3.htm"; 
            OasisVersion ov = GetOasisVersionByVersionKey(versionKey);
            if (ov == null)
            {
                return null;
            }

            if (!isOasisCacheLoaded())
            {
                return null;
            }

            //oasis_chapter_html_files = 
            //      "assets/resources/oasis_02.00_chapter_3.htm"
            //      "assets/resources/oasis_02.11_chapter_3.htm"
            //      "assets/resources/oasis_02.12_chapter_3.htm"
            List<string> oasis_chapter_html_files = GetOasisResources();
            var oasis_file_match = oasis_chapter_html_files.Where(f => f.IndexOf(ov.VersionCD2) > 0).FirstOrDefault();
            if (oasis_file_match != null)
            {
                //TESTING - return "/virtuoso;component/assets/resources/oasis_02.12_chapter_3.htm";
                return "/virtuoso;component/" + oasis_file_match;
            }

            return "/virtuoso;component/assets/resources/oasis_02.00_chapter_3.htm"; //couldn't find a match, return default
        }

        static List<string> _oasis_resource_files;

        private static List<string> GetOasisResources()
        {
            //RETURN list of following strings:
            //      "assets/resources/oasis_02.00_chapter_3.htm"
            //      "assets/resources/oasis_02.11_chapter_3.htm"
            //      "assets/resources/oasis_02.12_chapter_3.htm"
            //      <plus any others added that start with - "assets/resources/oasis" 

            if (_oasis_resource_files == null)
            {
                //Code lifted from - http://blogs.microsoft.co.il/alex_golesh/2009/11/13/silverlight-tip-enumerating-embedded-resources/
                List<string>
                    embeddedResources = new List<string>(); //Will use this list as source for ListBox later in code
                var thisExe = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Split(',')[0] == "Virtuoso")
                    .FirstOrDefault();
                string[] resources = thisExe.GetManifestResourceNames();
                // Build the string of resources.
                foreach (string resource in resources)
                {
                    ResourceManager
                        rm = new ResourceManager(resource.Replace(".resources", ""),
                            thisExe); //All resources has “.resources” in the name – so I have to get rid of it
                    //Stream DUMMY = rm.GetStream("app.xaml"); //Seems like some issue here, but without getting any real stream next statement doesn't work...only needed when rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, false, true);
                    ResourceSet rs = rm.GetResourceSet(Thread.CurrentThread.CurrentUICulture, true, false);
                    IDictionaryEnumerator enumerator = rs.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        //embeddedResources.Add((string)enumerator.Key);
                        var res = (string)enumerator.Key; //E.G. res = "assets/resources/oasis_02.00_chapter_3.htm"
                        if (res.StartsWith("assets/resources/oasis"))
                        {
                            embeddedResources.Add((string)enumerator.Key);
                        }
                        //It is also possible to get the value and key instead of just key
                        //object obj2 = enumerator.Value;
                        //table.Add((string)enumerator.Key, obj2);
                    }
                }

                _oasis_resource_files = embeddedResources;
                return _oasis_resource_files;
            }

            return _oasis_resource_files;
        }

        public static OasisVersion GetOasisVersionByVersionKey(int versionKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisVersion ov = Current.Context.OasisVersions.Where(o => o.OasisVersionKey == versionKey)
                .FirstOrDefault();
            if (ov == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisVersionByVersionKey: VersionKey {0} is not defined.  Contact your system administrator.",
                    versionKey));
            }

            return ov;
        }

        public static OasisVersion GetOasisVersionBySYSCDandEffectiveDate(string SYSCD, DateTime EffectiveDate)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisVersion ov = Current.Context.OasisVersions
                .Where(o => ((o.SYS_CD == SYSCD) && (o.EffectiveDate != null) && (EffectiveDate >= o.EffectiveDate)))
                .OrderByDescending(o => o.EffectiveDate).FirstOrDefault();
            if (ov == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisVersionBySYSCDandEffectiveDate: {0} Version for Effective Date {1} is not defined.  Contact your system administrator.",
                    SYSCD, EffectiveDate.ToShortDateString()));
            }

            return ov;
        }

        public static OasisVersion GetOasisVersionBySYSCDandVersionCD1(string SYSCD, string VersionCD1)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisVersion ov = Current.Context.OasisVersions
                .Where(o => ((o.SYS_CD == SYSCD) && (o.VersionCD1.Trim() == VersionCD1))).FirstOrDefault();
            if (ov == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisVersionBySYSCDandVersionCD1: {0} Version for VersionCD1 {1} is not defined.  Contact your system administrator.",
                    SYSCD, VersionCD1));
            }

            return ov;
        }

        public static OasisSurvey GetOasisSurveyByOasisVersionKeyAndRFA(int oasisVersionKey, string rfa)
        {
            Current?.EnsureCacheReady();
            if (string.IsNullOrWhiteSpace(rfa))
            {
                return null;
            }

            OasisVersion ov = GetOasisVersionByVersionKey(oasisVersionKey);
            if (ov == null)
            {
                return null;
            }

            OasisSurvey os = Current.Context.OasisSurveys
                .Where(o => o.OasisVersionKey == ov.OasisVersionKey && o.RFA == rfa).FirstOrDefault();
            if (os == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisSurveyByOasisVersionKeyAndRFA: Survey for OasisVersionKey {0}, RFA {1} is not defined.  Contact your system administrator.",
                    oasisVersionKey.ToString(), rfa));
            }

            return os;
        }

        public static List<OasisSurveyGroup> GetOasisSurveyGroupByOasisVersionKeyAndRFA(int oasisVersionKey, string rfa)
        {
            Current?.EnsureCacheReady();
            OasisSurvey os = GetOasisSurveyByOasisVersionKeyAndRFA(oasisVersionKey, rfa);
            if (os == null)
            {
                return null;
            }

            List<OasisSurveyGroup> l = Current.Context.OasisSurveyGroups
                .Where(o => o.OasisSurveyKey == os.OasisSurveyKey).OrderBy(o => o.Sequence).ToList();
            if (l == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisSurveyGroupByOasisVersionKeyAndRFA: Survey groups for OasisVersionKey {0}, RFA {1} are not defined.  Contact your system administrator.",
                    oasisVersionKey.ToString(), rfa));
                return null;
            }

            if (l.Any() == false)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisSurveyGroupByOasisVersionKeyAndRFA: Survey groups for OasisVersionKey {0}, RFA {1} are not defined.  Contact your system administrator.",
                    oasisVersionKey.ToString(), rfa));
                return null;
            }

            return l;
        }

        public static OasisSurveyGroup GetOasisSurveyGroupByKey(int oasisSurveyGroupKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisSurveyGroup og = Current.Context.OasisSurveyGroups
                .Where(o => o.OasisSurveyGroupKey == oasisSurveyGroupKey).FirstOrDefault();
            if (og == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisSurveyGroupByKey: Group for OasisSurveyGroupKey {0} is not defined.  Contact your system administrator.",
                    oasisSurveyGroupKey.ToString()));
            }

            return og;
        }

        public static OasisAlert GetOasisAlertsByOasisAlertKey(int oasisAlertKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            return Current.Context.OasisAlerts.Where(o => (o.OasisAlertKey == oasisAlertKey)).FirstOrDefault();
        }

        public static List<OasisAlert> GetOasisAlertsByOasisVersionKey(int oasisVersionKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            List<OasisAlert> l = Current.Context.OasisAlerts.Where(o => (o.OasisVersionKey == oasisVersionKey))
                .ToList();
            if (l == null)
            {
                return null;
            }

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static List<OasisSurvey> GetOasisSurveys()
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisVersion ov = GetOasisVersionBySYSCDandEffectiveDate("OASIS", DateTime.Today.Date);
            if (ov == null)
            {
                return null;
            }

            List<OasisSurvey> l = Current.Context.OasisSurveys
                .Where(os => ((os.OasisVersionKey == ov.OasisVersionKey) && (os.RFA != null))).OrderBy(os => os.RFA)
                .ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static int GetOasisMaxVersion(string SYS_CD)
        {
            if (!isOasisCacheLoaded())
            {
                return 6;
            }

            OasisVersion o = Current.Context.OasisVersions.Where(ov => (ov.SYS_CD == SYS_CD)).FirstOrDefault();
            return (o == null) ? 6 : o.OasisVersionKey;
        }

        public static string GetOasisSurveyRFADescriptionByRFA(int oasisVersionKey, string RFA)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisSurvey o = Current.Context.OasisSurveys
                .Where(os => ((os.OasisVersionKey == oasisVersionKey) && (os.RFA == RFA))).FirstOrDefault();
            if ((o == null) || (string.IsNullOrWhiteSpace(o.RFA)))
            {
                return " RFA Description '" + RFA + "' Unknown";
            }

            return o.RFADescription;
        }

        public static string GetOASISOasisSurveyRFADescriptionLongByRFA(string RFA)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisVersion ov = GetOasisVersionBySYSCDandEffectiveDate("OASIS", DateTime.Today.Date);
            if (ov == null)
            {
                return null;
            }

            OasisSurvey o = Current.Context.OasisSurveys
                .Where(os => ((os.OasisVersionKey == ov.OasisVersionKey) && (os.RFA == RFA))).FirstOrDefault();
            if (o == null)
            {
                return null;
            }

            return (string.IsNullOrWhiteSpace(o.RFA)) ? "RFA unknown" : o.RFADescriptionLong;
        }

        public static string GetHISOasisSurveyRFADescriptionLongByRFA(string RFA)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisVersion ov = GetOasisVersionBySYSCDandEffectiveDate("HOSPICE", DateTime.Today.Date);
            if (ov == null)
            {
                return null;
            }

            OasisSurvey o = Current.Context.OasisSurveys
                .Where(os => ((os.OasisVersionKey == ov.OasisVersionKey) && (os.RFA == RFA))).FirstOrDefault();
            if (o == null)
            {
                return null;
            }

            return (string.IsNullOrWhiteSpace(o.RFA)) ? "RFA unknown" : o.RFADescriptionLong;
        }

        public static List<OasisAlert> GetOasisAlertsByOasisVersionKeyAndOasisQuestionKey(int oasisVersionKey,
            int oasisQuestionKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            List<OasisAlert> l = Current.Context.OasisAlerts.Where(o =>
                    ((o.OasisVersionKey == oasisVersionKey) &&
                     (o.OasisAlertContainsOasisQuestionKey(oasisQuestionKey))))
                .ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static OasisQuestion GetOasisQuestionByKey(int oasisQuestionKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisQuestion oq = Current.OasisLayoutIndex.GetOasisQuestionByKey(oasisQuestionKey);
            if (oq == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisQuestionByKey: Question for OasisQuestionKey {0} is not defined.  Contact your system administrator.",
                    oasisQuestionKey.ToString()));
            }

            return oq;
        }

        public static OasisQuestion GetOasisQuestionByQuestion(int oasisVersionKey, string oasisQuestion)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisQuestion oq = Current.Context.OasisQuestions.Where(o =>
                    ((o.Question == oasisQuestion) && (o.CachedOasisLayout.OasisVersionKey == oasisVersionKey)))
                .FirstOrDefault();
            if (oq == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisQuestionByQuestion: OasisVersionKey {0}, Question {1} is not defined.  Contact your system administrator.",
                    oasisVersionKey.ToString(), oasisQuestion));
            }

            return oq;
        }

        public static int GetOasisLayoutMaxEndPos(int oasisVersionKey, string recordType = "B1")
        {
            if (!isOasisCacheLoaded())
            {
                return 5000;
            }

            int maxEndPos = 0;
            maxEndPos = Current.Context.OasisLayouts
                .Where(o => (o.OasisVersionKey == oasisVersionKey) && (o.RecordType == recordType)).Max(o => o.EndPos);
            if (maxEndPos == 0)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisLayoutMaxEndPos for OasisVersionKey {0}, RecordType {1}, is not defined.  Contact your system administrator.",
                    oasisVersionKey.ToString(), recordType));
            }

            return (maxEndPos == 0) ? 5000 : maxEndPos;
        }

        public static bool DoesExistGetOasisLayoutByCMSField(int oasisVersionKey, string cmsField,
            string recordType = "B1")
        {
            if (!isOasisCacheLoaded())
            {
                return false;
            }

            OasisLayout ol = Current.Context.OasisLayouts.Where(o =>
                    (o.OasisVersionKey == oasisVersionKey) && (o.RecordType == recordType) && (o.CMSField == cmsField))
                .FirstOrDefault();
            return (ol == null) ? false : true;
        }

        public static OasisLayout GetOasisLayoutByCMSField(int oasisVersionKey, string cmsField,
            string recordType = "B1", bool showMessage = true)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisLayout ol = Current.OasisLayoutIndex.GetOasisLayout(oasisVersionKey, recordType, cmsField);
            if ((ol == null) && (showMessage))
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisLayoutByCMSField Layout for OasisVersionKey {0}, CMSField {1}, is not defined.  Contact your system administrator.",
                    oasisVersionKey.ToString(), cmsField));
            }

            return ol;
        }

        public static OasisLayout GetOasisLayoutByCMSFieldAndRFA(int oasisVersionKey, string cmsField, string rfa,
            string recordType = "B1", bool showMessage = true)
        {
            try
            {
                if (!isOasisCacheLoaded())
                {
                    return null;
                }

                OasisLayout _ol = Current.OasisLayoutIndex.GetOasisLayout(oasisVersionKey, recordType, cmsField);
                OasisLayout ol = (_ol?.RFAs.Contains(rfa) == true) ? _ol : null;
                if ((ol == null) && (showMessage))
                {
                    MessageBox.Show(String.Format(
                        "Error OasisCache.GetOasisLayoutByCMSField Layout for OasisVersionKey {0}, CMSField {1}, is not defined.  Contact your system administrator.",
                        oasisVersionKey.ToString(), cmsField));
                }

                return ol;
            }
            catch
            {
                return null;
            }
        }

        public static OasisLayout GetOasisLayoutByCMSFieldNoMessageBox(int oasisVersionKey, string cmsField,
            string recordType = "B1")
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisLayout ol = Current.OasisLayoutIndex.GetOasisLayout(oasisVersionKey, recordType, cmsField);
            return ol;
        }

        public static bool DoesExistGetOasisAnswerByCMSField(int oasisVersionKey, string cmsField)
        {
            if (!isOasisCacheLoaded())
            {
                return false;
            }

            OasisAnswer oa = Current.Context.OasisAnswers.Where(o =>
                (o.CachedOasisLayout.RecordType == "B1") && (o.CachedOasisLayout.CMSField == cmsField) &&
                (o.CachedOasisLayout.OasisVersionKey == oasisVersionKey)).FirstOrDefault();
            return (oa == null) ? false : true;
        }

        public static OasisAnswer GetOasisAnswerByCMSField(int oasisVersionKey, string cmsField, bool showmsg = true)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisAnswer oa = Current.OasisLayoutIndex.GetOasisAnswers(oasisVersionKey, "B1", cmsField)
                ?.FirstOrDefault();
            if (oa == null)
            {
                if (showmsg)
                {
                    MessageBox.Show(String.Format(
                        "Error OasisCache.GetOasisAnswerByCMSField: Answer for OasisVersionKey {0}, CMSField {1}.  Contact your system administrator.",
                        oasisVersionKey.ToString(), cmsField));
                }
            }

            return oa;
        }

        public static bool DoesExistGetOasisAnswerByCMSFieldAndSequence(int oasisVersionKey, string cmsField,
            int sequence)
        {
            if (!isOasisCacheLoaded())
            {
                return false;
            }

            OasisAnswer oa = Current.Context.OasisAnswers.Where(o =>
                (o.CachedOasisLayout.RecordType == "B1") && (o.CachedOasisLayout.CMSField == cmsField) &&
                (o.Sequence == sequence) && (o.CachedOasisLayout.OasisVersionKey == oasisVersionKey)).FirstOrDefault();
            return (oa == null) ? false : true;
        }

        public static OasisAnswer GetOasisAnswerByCMSFieldAndSequence(int oasisVersionKey, string cmsField,
            int sequence)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            var oa = Current.OasisLayoutIndex.GetOasisAnswers(oasisVersionKey, "B1", cmsField)
                ?.Where(o => o.Sequence == sequence).FirstOrDefault();
            if (oa == null)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisAnswerByCMSField: Answer for OasisVersionKey {0}, CMSField {1}, sequence {2} is not defined.  Contact your system administrator.",
                    oasisVersionKey.ToString(), cmsField, sequence.ToString()));
            }

            return oa;
        }

        public static OasisAnswer GetOasisAnswerByKey(int oasisAnswerKey, bool showMessage = true)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisAnswer oa = Current.OasisLayoutIndex.GetOasisAnswerByKey(oasisAnswerKey);
            if ((oa == null) && showMessage)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisAnswerByKey: Answer for OasisAnswerKey {0} is not defined.  Contact your system administrator.",
                    oasisAnswerKey.ToString()));
            }

            return oa;
        }

        public static OasisLayout GetOasisLayoutByKey(int oasisLayoutKey, bool showMessage = true)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            OasisLayout ol = Current.OasisLayoutIndex.GetOasisLayoutByOasisLayoutKey(oasisLayoutKey);
            if ((ol == null) && showMessage)
            {
                MessageBox.Show(String.Format(
                    "Error OasisCache.GetOasisLayoutByKey: Answer for OasisLayoutKey {0} is not defined.  Contact your system administrator.",
                    oasisLayoutKey.ToString()));
            }

            return ol;
        }

        public static List<OasisAnswer> GetAllAnswers(OasisAnswer a)
        {
            Current?.EnsureCacheReady();
            List<OasisAnswer> l = Current.Context.OasisAnswers.Where(o => (o.OasisQuestionKey == a.OasisQuestionKey))
                .ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static List<OasisAnswer> GetAllAnswersButMe(OasisAnswer a)
        {
            Current?.EnsureCacheReady();
            List<OasisAnswer> l = Current.Context.OasisAnswers.Where(o =>
                ((o.OasisQuestionKey == a.OasisQuestionKey) && (o.OasisAnswerKey != a.OasisAnswerKey))).ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static List<OasisAnswer> GetAllExclusiveAnswersButMe(OasisAnswer a)
        {
            Current?.EnsureCacheReady();
            int t = (int)OasisType.CheckBoxExclusive;
            List<OasisAnswer> l = Current.Context.OasisAnswers.Where(o =>
                ((o.OasisQuestionKey == a.OasisQuestionKey) && (o.OasisAnswerKey != a.OasisAnswerKey) &&
                 (o.CachedOasisLayout.Type == t))).ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static List<OasisAnswer> GetAllNonExclusiveAnswersButMe(OasisAnswer a)
        {
            Current?.EnsureCacheReady();
            int t = (int)OasisType.CheckBox;
            List<OasisAnswer> l = Current.Context.OasisAnswers.Where(o =>
                ((o.OasisQuestionKey == a.OasisQuestionKey) && (o.OasisAnswerKey != a.OasisAnswerKey) &&
                 (o.CachedOasisLayout.Type == t))).ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static List<OasisAnswer> GetAllICDAnswersAfterMe(OasisAnswer a)
        {
            Current?.EnsureCacheReady();
            int t1 = (int)OasisType.ICD;
            int t2 = (int)OasisType.ICD10;
            List<OasisAnswer> l = Current.Context.OasisAnswers.Where(o =>
                    ((o.OasisQuestionKey == a.OasisQuestionKey) && (o.OasisAnswerKey != a.OasisAnswerKey) &&
                     ((o.CachedOasisLayout.Type == t1) || (o.CachedOasisLayout.Type == t2)) &&
                     (o.Sequence > a.Sequence)))
                .OrderBy(o => o.Sequence).ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static List<OasisAnswer> GetOasisAnswersByQuestionKey(int oasisQuestionKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            List<OasisAnswer> l = Current.Context.OasisAnswers.Where(o => (o.OasisQuestionKey == oasisQuestionKey))
                .OrderBy(o => o.Sequence).ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }

        public static List<OasisLayout> GetAllAnswersByVersionAndRFA(int oasisVersionKey, string rfa)
        {
            Current?.EnsureCacheReady();
            try
            {
                List<OasisLayout> olList = Current.Context.OasisLayouts.Where(o =>
                    (o.OasisVersionKey == oasisVersionKey) && (o.RecordType == "B1") && (o.IsForRFA(rfa))).ToList();

                if (olList.Any() == false)
                {
                    return null;
                }

                return olList;
            }
            catch
            {
                return null;
            }
        }

        public static List<OasisQuestionCodingRule> GetOasisQuestionCodingRulesByQuestionKey(int oasisQuestionKey)
        {
            if (!isOasisCacheLoaded())
            {
                return null;
            }

            List<OasisQuestionCodingRule> l = Current.Context.OasisQuestionCodingRules
                .Where(o => (o.OasisQuestionKey == oasisQuestionKey)).OrderBy(o => o.Sequence).ToList();

            if (l.Any() == false)
            {
                return null;
            }

            return l;
        }
    }
}