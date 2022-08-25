#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class OasisAdmissionsForInsurance
    {
        private DateTime? _ClientLastTransmissionDate;
        private CollectionViewSource _SurveyList;

        public string RFADescription =>
            OasisCache.GetOasisSurveyRFADescriptionByRFA(OasisCache.GetOasisMaxVersion("OASIS"), RFA);

        public string ServiceLineName
        {
            get
            {
                var sl = ServiceLineCache.GetServiceLineFromKey(ServiceLineKey);
                if (sl == null)
                {
                    return "Service Line ?";
                }

                return string.IsNullOrWhiteSpace(sl.Name) ? "Service Line ?" : sl.Name;
            }
        }

        public ICollectionView SurveyListView =>
            _SurveyList == null || _SurveyList.View == null ? null : _SurveyList.View;

        public DateTime? ClientLastTransmissionDate
        {
            get { return _ClientLastTransmissionDate; }
            set
            {
                _ClientLastTransmissionDate = value;
                RaisePropertyChanged("ClientLastTransmissionDate");
            }
        }

        public string LastTransmissionDateString
        {
            get
            {
                if (LastTransmissionDate == null && ClientLastTransmissionDate == null)
                {
                    return null;
                }

                var dt = ClientLastTransmissionDate == null
                    ? (DateTime)LastTransmissionDate
                    : (DateTime)ClientLastTransmissionDate;
                var date = dt.ToShortDateString();
                try
                {
                    date = date + " " + (TenantSettingsCache.Current.TenantSetting.UseMilitaryTime
                        ? dt.ToString("HHmm")
                        : dt.ToShortTimeString());
                }
                catch
                {
                }

                if (dt.Date == DateTime.Today.Date)
                {
                    date = date + "  (today)";
                }

                return date;
            }
        }

        public void SetupSurveyListView(List<OasisAdmissionsForInsurance> oList)
        {
            if (oList == null || oList.Any() == false)
            {
                _SurveyList = null;
            }
            else
            {
                _SurveyList = new CollectionViewSource();
                _SurveyList.Source = oList;
                SurveyListView.SortDescriptions.Add(new SortDescription("M0090Date", ListSortDirection.Ascending));
                SurveyListView.Refresh();
            }

            RaisePropertyChanged("SurveyListView");
        }
    }
}