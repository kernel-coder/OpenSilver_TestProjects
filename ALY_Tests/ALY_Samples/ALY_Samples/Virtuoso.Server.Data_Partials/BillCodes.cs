#region Usings

using System;
using System.Linq;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class BillCodes
    {
        public bool Inactive => InactiveDate.HasValue;

        public bool IsSupply
        {
            get
            {
                if (SourceType != "SUP")
                {
                    return false;
                }

                return Supply != null;
            }
        }

        public bool IsEquipment
        {
            get
            {
                if (SourceType != "EQP")
                {
                    return false;
                }

                return Equipment != null;
            }
        }

        public string EffectiveThruDate
        {
            get
            {
                BillCodes nextDate = null;
                var result = "-";
                if (IsEquipment)
                {
                    nextDate = Equipment.BillCodes.Where(w => !w.InactiveDate.HasValue
                                                              && w.BillCodeKey != BillCodeKey
                                                              && w.BillCodeType == BillCodeType
                                                              && w.EffectiveFromDate > EffectiveFromDate)
                        .OrderBy(o => o.EffectiveFromDate).FirstOrDefault();
                }
                else if (IsSupply)
                {
                    nextDate = Supply.BillCodes.Where(w => !w.InactiveDate.HasValue
                                                           && w.BillCodeKey != BillCodeKey
                                                           && w.BillCodeType == BillCodeType
                                                           && w.EffectiveFromDate > EffectiveFromDate)
                        .OrderBy(o => o.EffectiveFromDate).FirstOrDefault();
                }

                if (nextDate != null)
                {
                    result = nextDate.EffectiveFromDate.AddDays(-1).ToShortDateString();
                }

                return result;
            }
        }

        public DateTime EffectiveFromChanger
        {
            get { return EffectiveFromDate; }
            set
            {
                EffectiveFromDate = value;
                if (IsDeserializing)
                {
                    return;
                }

                if (Supply != null)
                {
                    Supply.BillCodesChanged();
                }

                if (Equipment != null)
                {
                    Equipment.BillCodesChanged();
                }
            }
        }

        public void BillCodesInit(Supply supply)
        {
            SourceType = "SUP";
            SupplyKey = supply.SupplyKey;
            Supply = supply;
            EffectiveFromDate = DateTime.Today.Date;
        }

        public void BillCodesInit(Equipment equipment)
        {
            SourceType = "EQP";
            EquipmentKey = equipment.EquipmentKey;
            Equipment = equipment;
            EffectiveFromDate = DateTime.Today.Date;
        }

        private void ChangeThrower()
        {
            if (IsEquipment)
            {
                Equipment.BillCodesChanged();
            }
            else if (IsSupply)
            {
                Supply.BillCodesChanged();
            }
        }

        partial void OnBillCodeTypeChanged()
        {
            ChangeThrower();
        }

        partial void OnBillingIDChanged()
        {
            ChangeThrower();
        }

        partial void OnModifierChanged()
        {
            ChangeThrower();
        }

        partial void OnEffectiveFromDateChanged()
        {
            ChangeThrower();
        }

        public void TriggeredChange()
        {
            RaisePropertyChanged("EffectiveThruDate");
        }

        public void UndoChanges()
        {
            RejectChanges();
        }
    }
}