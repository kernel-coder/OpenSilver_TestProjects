#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
using Virtuoso.Validation;
//for ObservableCollection.ForEach

#endregion

namespace Virtuoso.Core.Services
{
    public class OasisServiceLineGroupingItem
    {
        public int ServiceLineGroupingKey { get; set; }
        public bool ServiceLineGroupingIsChecked { get; set; }
        public int OasisTotalAvail { get; set; }
        public int OasisOnHold { get; set; }

        public string ServiceLineGroupingLabelAndName
        {
            get
            {
                ServiceLineGrouping slg = ServiceLineCache.GetServiceLineGroupingFromKey(ServiceLineGroupingKey);
                return (slg == null) ? "?" : slg.LabelAndName;
            }
        }

        public string ServiceLineGroupingPhrase => ServiceLineGroupingLabelAndName + " Surveys Available: " +
                                                   OasisTotalAvail + ",   Surveys On Hold: " + OasisOnHold;

        public bool IsActiveUserInServiceLineGrouping =>
            ServiceLineCache.IsActiveUserInServiceLineGrouping(ServiceLineGroupingKey);
    }

    public interface IOASISService
    {
        void CMSMarkRetransmit(string SYS_CD, int OasisFileKey);

        void CMSTransmission(string SYS_CD, DateTime dt, bool TEST_FILE, int[] serviceLineGroupingKeyArray, object userState);

        void CMSInfoPotential(string SYS_CD);
        void GetOasisFileBySYSCD(string SYS_CD);
        void GetEncounterOasisByOasisFileKey(int OasisFileKey);
        int OasisTotalAvail(List<EncounterOasisPotential> eopList);
        int OasisOnHold(List<EncounterOasisPotential> eopList);
        List<OasisServiceLineGroupingItem> OasisServiceLineGroupingList(List<EncounterOasisPotential> eopList);
        event Action<InvokeOperation<byte[]>> ZipReturned;
        event Action<InvokeOperation<int>> MarkRetransmitReturned;
        event Action<InvokeOperation<List<EncounterOasisPotential>>> TallyPotentialReturned;
        event Action<InvokeOperation<List<OasisFile>>> GetOasisFileReturned;

        EntitySet<OasisAdmissionsForInsurance> EntitySet_OasisAdmissionsForInsurance { get; }

        void GetOasisAdmissionsForInsuranceAsync(int InsuranceKey, string FirstName, string LastName, string MRN,
            DateTime? M0090Start, DateTime? M0090End, string RFAs);

        event EventHandler<EntityEventArgs<OasisAdmissionsForInsurance>> OnGetOasisAdmissionsForInsuranceLoaded;

        void CMSTransmissionSingleAsync(string SYS_CD, DateTime dt, int EncounterOasisKey, int InsuranceKey,
            bool TEST_FILE, object userState);

        event Action<InvokeOperation<byte[]>> OnCMSTransmissionSingleAsyncReturned;

        void CMSTransmissionBatchAsync(string SYS_CD, DateTime dt, int InsuranceKey, DateTime? M0090Start,
            DateTime? M0090End, string RFAs, bool TEST_FILE,
            object userState);

        event Action<InvokeOperation<byte[]>> OnCMSTransmissionBatchAsyncReturned;

        #region AlertsContext

        EntitySet<UserAlertsJoin> AlertsContext_UserAlertsJoins { get; }
        EntitySet<Task> AlertsContext_Tasks { get; }
        EntitySet<Encounter> AlertsContext_Encounters { get; }
        event EventHandler<EntityEventArgs<UserAlertsJoin>> OnGetExceptionsAndAlertsForUserLoaded;
        event EventHandler<EntityEventArgs<EncounterOasis>> OnGetEncounterOasisLoaded;
        void GetExceptionsAndAlertsForUserAsync(Guid UserId, int ExceptAlertKey);

        #endregion
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IOASISService))]
    public class OASISService : PagedModelBase, IOASISService
    {
        #region PagedModelBase Members

        public override void LoadData()
        {
        }

        #endregion

        public VirtuosoDomainContext context { get; set; }

        public OASISService()
        {
            context = new VirtuosoDomainContext();
            AlertsContext = new VirtuosoDomainContext();
            var contextServiceProvider = new SimpleServiceProvider();
            contextServiceProvider.AddService<ICodeLookupDataProvider>(new CodeLookupDataProvider());
            contextServiceProvider.AddService<IWoundDataProvider>(new WoundDataProvider());
            contextServiceProvider.AddService<IPatientContactDataProvider>(new PatientContactDataProvider());

            context.ValidationContext = new ValidationContext(this, contextServiceProvider, null);
        }

        public void CMSTransmission(string SYS_CD, DateTime dt, bool TEST_FILE, int[] serviceLineGroupingKeyArray, object userState)
        {
            context.GetCMSTransmission(SYS_CD, dt, TEST_FILE, serviceLineGroupingKeyArray, ZipReturned, userState);
        }

        public void CMSInfoPotential(string SYS_CD)
        {
            if (SYS_CD == "OASIS")
            {
                context.GetPotentialOASISTransmission(TallyPotentialReturned, null);
            }
            else if (SYS_CD == "HOSPICE")
            {
                context.GetPotentialHISTransmission(TallyPotentialReturned, null);
            }
            else
            {
                MessageBox.Show(String.Format(
                    "Error OASISService.CMSInfoPotential: SYS_CD {0} is not known.  Contact your system administrator.",
                    SYS_CD));
            }
        }

        public void GetOasisFileBySYSCD(string SYS_CD)
        {
            context.GetOasisFileBySYSCD(SYS_CD, GetOasisFileReturned, null);
        }

        public void GetEncounterOasisByOasisFileKey(int OasisFileKey)
        {
            context.EncounterOasis.Clear();
            var query = context.GetEncounterOasisByOasisFileKeyQuery(OasisFileKey);
            context.Load(query, LoadBehavior.RefreshCurrent, GetEncounterOasisLoaded, null);
        }

        private void GetEncounterOasisLoaded(LoadOperation<EncounterOasis> results)
        {
            HandleEntityResults(results, OnGetEncounterOasisLoaded);
        }

        public void CMSMarkRetransmit(string SYS_CD, int OasisFileKey)
        {
            if (SYS_CD == "OASIS")
            {
                context.OASISMarkRetransmit(OasisFileKey, MarkRetransmitReturned, null);
            }
            else if (SYS_CD == "HOSPICE")
            {
                context.HISMarkRetransmit(OasisFileKey, MarkRetransmitReturned, null);
            }
            else
            {
                MessageBox.Show(String.Format(
                    "Error OASISService.CMSMarkRetransmit: SYS_CD {0} is not known.  Contact your system administrator.",
                    SYS_CD));
            }
        }

        public event Action<InvokeOperation<byte[]>> ZipReturned;
        public event Action<InvokeOperation<int>> MarkRetransmitReturned;
        public event Action<InvokeOperation<List<EncounterOasisPotential>>> TallyPotentialReturned;
        public event Action<InvokeOperation<List<OasisFile>>> GetOasisFileReturned;

        public int OasisTotalAvail(List<EncounterOasisPotential> eopList)
        {
            if (eopList == null)
            {
                return 0;
            }

            return eopList.Count(o => ((o.OnHold == false) && (o.ServiceLineGroupingKey != null) && o.IsActiveUserInServiceLineGrouping));
        }

        public int OasisOnHold(List<EncounterOasisPotential> eopList)
        {
            if (eopList == null)
            {
                return 0;
            }

            return eopList.Count(o => (o.OnHold && (o.ServiceLineGroupingKey != null) && o.IsActiveUserInServiceLineGrouping));
        }

        private int OasisTotalAvail(List<EncounterOasisPotential> eopList, int serviceLineGroupingKey)
        {
            if (eopList == null)
            {
                return 0;
            }

            return eopList.Count(o => ((o.OnHold == false) 
                                       && (o.ServiceLineGroupingKey == serviceLineGroupingKey) 
                                       && o.IsActiveUserInServiceLineGrouping));
        }

        private int OasisOnHold(List<EncounterOasisPotential> eopList, int serviceLineGroupingKey)
        {
            if (eopList == null)
            {
                return 0;
            }

            return eopList.Count(o => (o.OnHold && (o.ServiceLineGroupingKey == serviceLineGroupingKey) && o.IsActiveUserInServiceLineGrouping));
        }

        public List<OasisServiceLineGroupingItem> OasisServiceLineGroupingList(List<EncounterOasisPotential> eopList)
        {
            List<OasisServiceLineGroupingItem> oasisServiceLineGroupingList = new List<OasisServiceLineGroupingItem>();
            IEnumerable<int?> intList = (from o in eopList select o.ServiceLineGroupingKey).Distinct();
            foreach (int? serviceLineGroupingKey in intList)
                if (serviceLineGroupingKey != null)
                {
                    oasisServiceLineGroupingList.Add(new OasisServiceLineGroupingItem
                    {
                        ServiceLineGroupingKey = (int)serviceLineGroupingKey,
                        OasisTotalAvail = OasisTotalAvail(eopList, (int)serviceLineGroupingKey),
                        OasisOnHold = OasisOnHold(eopList, (int)serviceLineGroupingKey),
                    });
                }

            return oasisServiceLineGroupingList;
        }

        public EntitySet<OasisAdmissionsForInsurance> EntitySet_OasisAdmissionsForInsurance => context?.OasisAdmissionsForInsurances;

        public void GetOasisAdmissionsForInsuranceAsync(int InsuranceKey, string FirstName, string LastName, string MRN,
            DateTime? M0090Start, DateTime? M0090End, string RFAs)
        {
            context.OasisAdmissionsForInsurances.Clear();

            var query = context.GetOasisAdmissionsForInsuranceWithParamsQuery(InsuranceKey, FirstName, LastName, MRN,
                M0090Start, M0090End, RFAs);
            context.Load(
                query,
                LoadBehavior.RefreshCurrent,
                g => HandleEntityResults(g, OnGetOasisAdmissionsForInsuranceLoaded),
                null);
        }

        public event EventHandler<EntityEventArgs<OasisAdmissionsForInsurance>> OnGetOasisAdmissionsForInsuranceLoaded;

        public void CMSTransmissionSingleAsync(string SYS_CD, DateTime dt, int EncounterOasisKey, int InsuranceKey,
            bool TEST_FILE, object userState)
        {
            context.GetCMSTransmissionSingle(SYS_CD, dt, EncounterOasisKey, InsuranceKey, TEST_FILE,
                OnCMSTransmissionSingleAsyncReturned, userState);
        }

        public event Action<InvokeOperation<byte[]>> OnCMSTransmissionSingleAsyncReturned;

        public void CMSTransmissionBatchAsync(string SYS_CD, DateTime dt, int InsuranceKey, DateTime? M0090Start,
            DateTime? M0090End, string RFAs, bool TEST_FILE, object userState)
        {
            context.GetCMSTransmissionBatch(SYS_CD, dt, InsuranceKey, M0090Start, M0090End, RFAs, TEST_FILE,
                OnCMSTransmissionBatchAsyncReturned, userState);
        }

        public event Action<InvokeOperation<byte[]>> OnCMSTransmissionBatchAsyncReturned;

        #region AlertsContext

        VirtuosoDomainContext AlertsContext;

        public EntitySet<UserAlertsJoin> AlertsContext_UserAlertsJoins => AlertsContext.UserAlertsJoins;
        public EntitySet<Task> AlertsContext_Tasks => AlertsContext.Tasks;
        public EntitySet<Encounter> AlertsContext_Encounters => AlertsContext.Encounters;
        public event EventHandler<EntityEventArgs<UserAlertsJoin>> OnGetExceptionsAndAlertsForUserLoaded;
        public event EventHandler<EntityEventArgs<EncounterOasis>> OnGetEncounterOasisLoaded;

        public void GetExceptionsAndAlertsForUserAsync(Guid UserId, int ExceptAlertKey)
        {
            AlertsContext_UserAlertsJoins.Clear();
            AlertsContext_Tasks.Clear();
            AlertsContext_Encounters.Clear();

            var query = AlertsContext.GetAlertsForUserQuery(UserId, ExceptAlertKey);
            AlertsContext.Load(query, LoadBehavior.RefreshCurrent, GetExceptionsAndAlertsForUserLoaded, null);
        }

        private void GetExceptionsAndAlertsForUserLoaded(LoadOperation<UserAlertsJoin> results)
        {
            HandleEntityResults(results, OnGetExceptionsAndAlertsForUserLoaded);
        }

        #endregion
    }
}