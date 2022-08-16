using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;

namespace Virtuoso.Portable.Extensions
{
    public class ICDCodeExtensions
    {
        public static void RecordSetToCachedICDCode(RecordSet recordSet, CachedICDCode entity)
        {

#if DEBUG
            var throwExceptionIfColumnNotExists = true;
#else
            var throwExceptionIfColumnNotExists = false;
#endif
            entity.ICDCodeKey = recordSet.GetInteger("ICDCodeKey", throwExceptionIfColumnNotExists);

            //entity.DisplayName = recordSet.GetString("DisplayName", throwExceptionIfColumnNotExists);
            entity.Code = recordSet.GetString("Code", throwExceptionIfColumnNotExists);
            entity.Version = recordSet.GetInteger("Version", throwExceptionIfColumnNotExists);
            entity.Short = recordSet.GetString("Short", throwExceptionIfColumnNotExists);
            entity.EffectiveFrom = recordSet.GetNullableDateTime("EffectiveFrom", throwExceptionIfColumnNotExists);
            entity.EffectiveThru = recordSet.GetNullableDateTime("EffectiveThru", throwExceptionIfColumnNotExists);
            //public string Description { get { return string.IsNullOrEmpty(DisplayName) ? Short : DisplayName; } } //FYI: do not map this property to the flat file...
            entity.GEMSCount = recordSet.GetShort("GEMSCount", throwExceptionIfColumnNotExists);
            entity.Diagnosis = recordSet.GetBoolean("Diagnosis", throwExceptionIfColumnNotExists);
            entity.RequiresAdditionalDigit = recordSet.GetNullableBoolean("RequiresAdditionalDigit", throwExceptionIfColumnNotExists);
            entity.PDGMClinicalGroup = recordSet.GetString("PDGMClinicalGroup", throwExceptionIfColumnNotExists);
            entity.PDGMComorbidityGroup = recordSet.GetString("PDGMComorbidityGroup", throwExceptionIfColumnNotExists);

            entity.FullText = CalculateFullText(entity);
        }

        public static string CalculateFullText(CachedICDCode entity)
        {
            return StringExtensions.CombineStrings(entity.Code, entity.Short).GetValueOrDefault().ToLower();
        }
    }
}
