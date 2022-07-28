using Virtuoso.Portable.Database;
using Virtuoso.Portable.Extensions;
using Virtuoso.Portable.Model;

namespace Virtuoso.Portable.Extensions
{
    public static class AllergyCacheExtensions
    {
        public static void RecordSetToCachedAllergyCode(RecordSet recordSet, CachedAllergyCode entity, bool calculateFullText = false)
        {
#if DEBUG
            var throwExceptionIfColumnNotExists = true;
#else
            var throwExceptionIfColumnNotExists = false;
#endif
            entity.AllergyCodeKey = recordSet.GetInteger("AllergyCodeKey", throwExceptionIfColumnNotExists);

            entity.UNII = recordSet.GetString("UNII", throwExceptionIfColumnNotExists);
            entity.SubstanceName = recordSet.GetString("SubstanceName", throwExceptionIfColumnNotExists);
            entity.PreferredSubstanceName = recordSet.GetString("PreferredSubstanceName", throwExceptionIfColumnNotExists);

            entity.EffectiveFrom = recordSet.GetNullableDateTime("EffectiveFrom", throwExceptionIfColumnNotExists);
            entity.EffectiveThru = recordSet.GetNullableDateTime("EffectiveThru", throwExceptionIfColumnNotExists);

            if (calculateFullText)
            {
                entity.FullText = CalculateFullText(entity);
            }
        }

        public static string CalculateFullText(CachedAllergyCode entity)
        {
            return StringExtensions.CombineStrings(entity.UNII, entity.SubstanceName, entity.PreferredSubstanceName, entity.DisplayName).GetValueOrDefault().ToLower();
        }
    }
}
