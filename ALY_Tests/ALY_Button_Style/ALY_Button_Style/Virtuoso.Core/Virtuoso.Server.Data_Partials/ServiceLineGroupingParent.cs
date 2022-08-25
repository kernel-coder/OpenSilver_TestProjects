#region Usings

using System;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class ServiceLineGroupingParent
    {
        private bool _CanEditEffectiveFromDate;

        private bool _CanEditEffectiveThruDate;
        private bool _CanEditParentServiceLineGroupingKey;

        public ServiceLineGrouping Parent => ServiceLineGrouping;

        public ServiceLineGrouping Child => ServiceLineGrouping1;

        public string ParentName => Parent.MyName;

        public string ChildName => Child.MyName;

        public bool CanEditEffectiveThruDate
        {
            get { return _CanEditEffectiveThruDate; }
            set
            {
                _CanEditEffectiveThruDate = value;
                RaisePropertyChanged("CanEditEffectiveThruDate");
            }
        }

        public bool CanEditEffectiveFromDate
        {
            get { return _CanEditEffectiveFromDate; }
            set
            {
                _CanEditEffectiveFromDate = value;
                RaisePropertyChanged("CanEditEffectiveFromDate");
            }
        }

        public bool CanEditParentServiceLineGroupingKey
        {
            get { return _CanEditParentServiceLineGroupingKey; }
            set
            {
                _CanEditParentServiceLineGroupingKey = value;
                RaisePropertyChanged("CanEditParentServiceLineGroupingKey");
            }
        }

        public bool IsEffectiveOnDate(DateTime date)
        {
            return (EffectiveFromDate == null || EffectiveFromDate != null && EffectiveFromDate <= date) &&
                   (EffectiveThruDate == null || EffectiveThruDate != null && EffectiveThruDate > date);
        }
    }
}