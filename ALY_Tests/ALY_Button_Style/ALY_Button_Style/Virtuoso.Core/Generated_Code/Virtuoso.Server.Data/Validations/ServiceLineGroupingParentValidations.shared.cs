using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
//    public static class ServiceLineGroupingParentValidations
//    {
//        public static ValidationResult NoOverlappingDatesOnServiceLineGroupingParents(ServiceLineGroupingParent serviceLineGroupingParent, ValidationContext validationContext)
//        {
//            if (serviceLineGroupingParent == null) return null;
//            if (serviceLineGroupingParent.ServiceLineGrouping == null) return null;
//#if SILVERLIGHT
//            if (serviceLineGroupingParent.EntityState == System.ServiceModel.DomainServices.Client.EntityState.Unmodified) 
//                return ValidationResult.Success;
////#else
////            return new ValidationResult("No Overlapping Dates On ServiceLineGroupingParents", new string[] { "EffectiveFromDate" });
//#endif
//            var serviceLineGroupingProvider = validationContext.GetService(typeof(IServiceLineGroupingProvider)) as IServiceLineGroupingProvider;
//            if (serviceLineGroupingProvider == null) return null;

//            var modifiedParent = serviceLineGroupingParent;

//            //Get the ServiceLineGroupingKey from child
//            int serviceLineGroupingKey = 0;
//            if (modifiedParent.ServiceLineGrouping1 != null) serviceLineGroupingKey = modifiedParent.ServiceLineGrouping1.ServiceLineGroupingKey;

//            var parents = serviceLineGroupingProvider.GetParentsOfThisServiceLineGrouping(serviceLineGroupingKey);
//            if (parents != null) parents = parents.Where(p => p.ServiceLineGroupingParentKey != modifiedParent.ServiceLineGroupingParentKey);

//            if (modifiedParent.EffectiveFromDate != null)
//            {
//                var OverlappingFomDateParents = parents
//                    .Where(p => p.EffectiveFromDate != null && p.EffectiveThruDate != null)
//                    .Where(p => p.EffectiveFromDate <= modifiedParent.EffectiveFromDate && modifiedParent.EffectiveFromDate <= p.EffectiveThruDate)
//                    .AsQueryable();
//                if (OverlappingFomDateParents != null && OverlappingFomDateParents.Any() == true)
//                    return new ValidationResult("No Overlapping Dates On ServiceLineGroupingParents", new string[] { "EffectiveFromDate" });
//            }

//            if (modifiedParent.EffectiveFromDate != null)
//            {
//                var OverlappingThruDateParents = parents
//                    .Where(p => p.EffectiveFromDate != null && p.EffectiveThruDate != null)
//                    .Where(p => p.EffectiveFromDate <= modifiedParent.EffectiveThruDate && modifiedParent.EffectiveThruDate <= p.EffectiveThruDate)
//                    .AsQueryable();
//                if (OverlappingThruDateParents != null && OverlappingThruDateParents.Any() == true)
//                    return new ValidationResult("No Overlapping Dates On ServiceLineGroupingParents", new string[] { "EffectiveThruDate" });
//            }

//            return ValidationResult.Success;
//        }
//    }
}
