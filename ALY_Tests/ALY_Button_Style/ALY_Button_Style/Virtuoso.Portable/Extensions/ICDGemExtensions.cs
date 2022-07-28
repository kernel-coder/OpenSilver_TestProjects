using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;

namespace Virtuoso.Portable.Extensions
{
    public class ICDGemsExtensions
    {
        public static void RecordSetToCachedICDGEMS(RecordSet recordSet, CachedICDGEMS entity)
        {
#if DEBUG
            var throwExceptionIfColumnNotExists = true;
#else
            var throwExceptionIfColumnNotExists = false;
#endif
            entity.ICDGEMSKey = recordSet.GetInteger("ICDGEMSKey", throwExceptionIfColumnNotExists);
            entity.Version = recordSet.GetInteger("Version", throwExceptionIfColumnNotExists);
            entity.Code9 = recordSet.GetString("Code9", throwExceptionIfColumnNotExists);
            entity.Code10 = recordSet.GetString("Code10", throwExceptionIfColumnNotExists);
            entity.Short9 = recordSet.GetString("Short9", throwExceptionIfColumnNotExists);
            entity.Short10 = recordSet.GetString("Short10", throwExceptionIfColumnNotExists);
            entity.ApproximateFlag = recordSet.GetBoolean("ApproximateFlag", throwExceptionIfColumnNotExists);
            entity.NoMapFlag = recordSet.GetBoolean("NoMapFlag", throwExceptionIfColumnNotExists);
            entity.CombinationFlag = recordSet.GetBoolean("CombinationFlag", throwExceptionIfColumnNotExists);
            entity.Scenario = recordSet.GetShort("Scenario", throwExceptionIfColumnNotExists);
            entity.ChoiceList = recordSet.GetShort("ChoiceList", throwExceptionIfColumnNotExists);
        }
    }

}
