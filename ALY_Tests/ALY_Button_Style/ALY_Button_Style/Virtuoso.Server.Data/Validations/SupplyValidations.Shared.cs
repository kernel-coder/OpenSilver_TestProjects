using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    public static class SupplyValidations
    {
        public static ValidationResult ValidateItemCode(string ItemCode, ValidationContext validationContext)
        {
            return ValidationResult.Success;
        }

        public static ValidationResult ValidateSupplyInformation(Supply sup, ValidationContext validationContext)
        {
            var codeLookupDataProvider = validationContext.GetService(typeof(ICodeLookupDataProvider)) as ICodeLookupDataProvider;

             var uniquenessCheckProvider = validationContext.GetService(typeof(IUniquenessCheckProvider)) as IUniquenessCheckProvider;

            if (codeLookupDataProvider != null)
            {
                string code = sup.PackageCode == null ? "" : codeLookupDataProvider.GetCodeLookupCodeFromKey(sup.PackageCode);
                string codeToShow = "BLIS, CTN, DOZ, HDOZ, CS, GR";

                if (!codeToShow.Contains(code))
                {
                    sup.MayBreakPack = false;
                    sup.PackageQty = null;
                }
                if (codeToShow.Contains(code) && !sup.StdPackCharge.HasValue)
                {
                    string[] memberNames = new string[] { "StdPackCharge" };
                    return new ValidationResult("Standard Pack Charge is Required", memberNames);
                }
                else if (!sup.StdUnitCharge.HasValue)
                {
                    string[] memberNames = new string[] { "StdUnitCharge" };
                    return new ValidationResult("Standard Unit Charge is Required", memberNames);
                }
            }
            else
            {
                string[] memberNames = new string[] { "PackageCode" };
                return new ValidationResult("CodeLookup data provider is NULL", memberNames);
            }

            if (uniquenessCheckProvider.IsSupplyItemCodeUnique(sup.SupplyKey, sup.ItemCode))
            {
                return ValidationResult.Success;
            }
            else
            {
                string[] memberNames = new string[] { "ItemCode" };
                return new ValidationResult("Item Code must be unique", memberNames);
            }
        }
    }
}
