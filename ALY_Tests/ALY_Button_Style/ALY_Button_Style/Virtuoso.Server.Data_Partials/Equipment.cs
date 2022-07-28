#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Equipment
    {
        public List<BillCodes> BillCodesAsList
        {
            get
            {
                var result = new List<BillCodes>();
                foreach (var item in BillCodes) result.Add(item);
                return result.OrderBy(o => o.CodeTypeDescription).ThenBy(t => t.EffectiveFromDate)
                    .ThenBy(f => f.BillCodeKey).ToList();
            }
        }

        public bool HasBillCodes
        {
            get
            {
                foreach (var item in BillCodes)
                    if (!item.Inactive)
                    {
                        return true;
                    }

                return false;
            }
        }

        public string TabHeader
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(ItemCode))
                {
                    return string.Format("Equipment {0}", ItemCode.Trim());
                }

                return IsNew ? "New Equipment" : "Edit Equipment";
            }
        }

        public bool Inactive
        {
            get
            {
                var today = DateTime.SpecifyKind(DateTime.Today.Date, DateTimeKind.Unspecified).Date;
                return today < EffectiveFrom.Date || EffectiveThru.HasValue && today > ((DateTime)EffectiveThru).Date;
            }
        }

        // Used to display * in SearchResultsView
        public string IsInactiveIndicator
        {
            get
            {
                if (Inactive)
                {
                    return "*";
                }

                return string.Empty;
            }
        }

        public void AddNewBillCodes()
        {
            var p = new BillCodes();
            p.BillCodesInit(this);
            BillCodes.Add(p);
            TriggerBillCodesChanges();
        }

        public void RemoveBillCodes(BillCodes p)
        {
            BillCodes.Remove(p);
            TriggerBillCodesChanges();
        }

        public void TriggerBillCodesChanges()
        {
            var bcs = BillCodesAsList;
            foreach (var item in bcs) item.TriggeredChange();

            RaisePropertyChanged("BillCodes");
            RaisePropertyChanged("BillCodesAsList");
            RaisePropertyChanged("HasBillCodes");
        }

        public void BillCodesChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            TriggerBillCodesChanges();
        }

        partial void OnItemCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("TabHeader");
        }
    }
}