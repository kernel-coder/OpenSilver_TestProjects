using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Portable.Database;
using Virtuoso.Portable.Model;

namespace Virtuoso.Portable.Extensions
{
    public static class AddressMapExtensions
    {
        public static void RecordSetToCachedAddressMapping(RecordSet recordSet, CachedAddressMapping entity)
        {
#if DEBUG
            var throwExceptionIfColumnNotExists = true;
#else
            var throwExceptionIfColumnNotExists = false;
#endif
            entity.AddressMapKey = recordSet.GetInteger("AddressMapKey", throwExceptionIfColumnNotExists);
            entity.CBSAHomeHealth = recordSet.GetString("CBSAHomeHealth", throwExceptionIfColumnNotExists);
            entity.CBSAHomeHealthEffectiveFrom = recordSet.GetDateTime("CBSAHomeHealthEffectiveFrom", throwExceptionIfColumnNotExists);
            entity.CBSAHomeHealthEffectiveTo = recordSet.GetDateTime("CBSAHomeHealthEffectiveTo", throwExceptionIfColumnNotExists);
            entity.CBSAHospice = recordSet.GetString("CBSAHospice", throwExceptionIfColumnNotExists);
            entity.CBSAHospiceEffectiveFrom = recordSet.GetDateTime("CBSAHospiceEffectiveFrom", throwExceptionIfColumnNotExists);
            entity.CBSAHospiceEffectiveTo = recordSet.GetDateTime("CBSAHospiceEffectiveTo", throwExceptionIfColumnNotExists);
            entity.City = recordSet.GetString("City", throwExceptionIfColumnNotExists);
            entity.CountyCode = recordSet.GetString("County", throwExceptionIfColumnNotExists);
            entity.State = recordSet.GetString("State", throwExceptionIfColumnNotExists);
            entity.ZipCode = recordSet.GetString("ZipCode", throwExceptionIfColumnNotExists);
        }

    }
}
