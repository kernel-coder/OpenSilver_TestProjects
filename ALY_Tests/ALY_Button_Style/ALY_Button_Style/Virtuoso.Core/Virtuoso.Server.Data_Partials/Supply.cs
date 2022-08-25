#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class Supply
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
                    return string.Format("Supply {0}", ItemCode.Trim());
                }

                return IsNew ? "New Supply" : "Edit Supply";
            }
        }

        public bool Inactive => DateTime.Now.Date < EffectiveFrom ||
                                EffectiveThru.HasValue && DateTime.Now.Date > EffectiveThru.Value;

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

        public bool ShowPackageQty
        {
            get
            {
                if (this == null)
                {
                    return true;
                }

                var code = PackageCode == null ? "" : CodeLookupCache.GetCodeFromKey(PackageCode);
                var codeToShow = "BLIS, CTN, DOZ, HDOZ, CS, GR";
                return codeToShow.Contains(code);
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

        partial void OnPackageCodeChanged()
        {
            if (IsDeserializing)
            {
                return;
            }

            RaisePropertyChanged("ShowPackageQty");
        }
    }
}