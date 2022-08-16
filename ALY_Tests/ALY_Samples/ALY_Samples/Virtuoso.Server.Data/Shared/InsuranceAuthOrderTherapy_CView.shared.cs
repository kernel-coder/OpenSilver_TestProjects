using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Virtuoso.Server.Data
{
    public partial class GetInsuranceAuthOrderTherapyView_Result
    {
        public GetInsuranceAuthOrderTherapyView_Result Clone(GetInsuranceAuthOrderTherapyView_Result objectToClone)
        {
            GetInsuranceAuthOrderTherapyView_Result newItem = new GetInsuranceAuthOrderTherapyView_Result();
            newItem.ComplianceType = objectToClone.ComplianceType;
            newItem.DisciplineKey = objectToClone.DisciplineKey;
            newItem.InsuranceKey = objectToClone.InsuranceKey;
            newItem.InsuranceName = objectToClone.InsuranceName;
            newItem.InsuranceReqKey = objectToClone.InsuranceReqKey;
            newItem.ServiceLineKey = objectToClone.ServiceLineKey;
            newItem.ServiceLineName = objectToClone.ServiceLineName;
            newItem.ServiceTypeKey = objectToClone.ServiceTypeKey;
            newItem.TenantID = objectToClone.TenantID;
            newItem.TypeIsRequired = objectToClone.TypeIsRequired;
            newItem.TypeIsRequiredOriginal = objectToClone.TypeIsRequired == null ? false : (bool)objectToClone.TypeIsRequired;
            newItem.ServiceType = objectToClone.ServiceType;
            newItem.Discipline = objectToClone.Discipline;

            return newItem;
        }
    }

    public partial class GetInsuranceAuthOrderTherapyViewByServiceTypeKey_Result
    {
        public GetInsuranceAuthOrderTherapyViewByServiceTypeKey_Result Clone(GetInsuranceAuthOrderTherapyViewByServiceTypeKey_Result objectToClone)
        {
            GetInsuranceAuthOrderTherapyViewByServiceTypeKey_Result newItem = new GetInsuranceAuthOrderTherapyViewByServiceTypeKey_Result();
            newItem.ComplianceType = objectToClone.ComplianceType;
            newItem.DisciplineKey = objectToClone.DisciplineKey;
            newItem.InsuranceKey = objectToClone.InsuranceKey;
            newItem.InsuranceName = objectToClone.InsuranceName;
            newItem.InsuranceReqKey = objectToClone.InsuranceReqKey;
            newItem.ServiceLineKey = objectToClone.ServiceLineKey;
            newItem.ServiceLineName = objectToClone.ServiceLineName;
            newItem.ServiceTypeKey = objectToClone.ServiceTypeKey;
            newItem.TenantID = objectToClone.TenantID;
            newItem.TypeIsRequired = objectToClone.TypeIsRequired;
            newItem.TypeIsRequiredOriginal = objectToClone.TypeIsRequired == null ? false : (bool)objectToClone.TypeIsRequired;
            newItem.ServiceType = objectToClone.ServiceType;
            newItem.Discipline = objectToClone.Discipline;

            return newItem;
        }
    }
}
