using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;

namespace Virtuoso.Portable.Extensions
{
    public static class MedispanMedicationCacheExtensions
    {
        public static void RecordSetToCachedMediSpanMedication(RecordSet recordSet, CachedMediSpanMedication entity)
        {
#if DEBUG
            var throwExceptionIfColumnNotExists = true;
#else
            var throwExceptionIfColumnNotExists = false;
#endif
            entity.MedKey = recordSet.GetInteger("MedKey", throwExceptionIfColumnNotExists);
            entity.DDID = recordSet.GetNullableInteger("DDID", throwExceptionIfColumnNotExists);
            entity.RDID = recordSet.GetInteger("RDID", throwExceptionIfColumnNotExists);
            entity.MedType = recordSet.GetInteger("MedType", throwExceptionIfColumnNotExists);
            entity.Name = recordSet.GetString("Name", throwExceptionIfColumnNotExists);
            entity.MedUnit = recordSet.GetString("MedUnit", throwExceptionIfColumnNotExists);
            entity.Route = recordSet.GetString("Route", throwExceptionIfColumnNotExists);
            entity.RXType = recordSet.GetInteger("RXType", throwExceptionIfColumnNotExists);
            entity.MedNarcotic = recordSet.GetBoolean("MedNarcotic", throwExceptionIfColumnNotExists);
            entity.ODate = recordSet.GetNullableDateTime("ODate", throwExceptionIfColumnNotExists);
        }
    }
}
