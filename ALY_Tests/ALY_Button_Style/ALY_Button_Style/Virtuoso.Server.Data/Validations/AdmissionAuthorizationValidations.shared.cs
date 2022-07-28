using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Virtuoso.Server.Data;

namespace Virtuoso.Validation
{
    //Property level validators shared between AdmissionAuthorization and AdmissionAuthorizationDetail
    public static class AuthorizationPropertyValidations
    {
        public static ValidationResult ReceivedBy_Required(Guid data, ValidationContext validationContext)
        {
            if (data.Equals(Guid.Empty) || data == null)
            {
                string[] memberNames = new string[] { validationContext.MemberName }; //"ReceivedBy" };
                return new ValidationResult("Received By is required.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AuthorizationAmountForInstance_Valid(decimal value, ValidationContext validationContext)
        {
            var instance = validationContext.ObjectInstance as AdmissionAuthorizationInstance;
            if (instance == null)
                return ValidationResult.Success;
            //if (instance.IsDistributed)
            //    return ValidationResult.Success; //AdmissionAuthorizationDetail will allow ZERO, but not AdmissionAuthorizationInstance

            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            var x = value;
            if (x < 0m)  //ZERO is allowed BUG 39805
                return new ValidationResult(string.Format("{0} is required.", displayName), memberNames);

            return ValidationResult.Success;
        }

        public static ValidationResult AuthorizationAmountForDetail_Valid(decimal value, ValidationContext validationContext)
        {
            //Can only validate on client, because it is only there that you are guaranteed to have the associated AdmissionAuthorizationInstance
#if SILVERLIGHT && !SKIP_CLIENTSIDE_VALIDATIONS
            var instance = validationContext.ObjectInstance as AdmissionAuthorizationDetail;
            if (instance == null)
                return ValidationResult.Success;
            if (instance.AdmissionAuthorizationInstance != null && instance.AdmissionAuthorizationInstance.IsDistributed)
                return ValidationResult.Success; //Allowing ZERO AuthorizationAmount if it's a distributed detail 

            string[] memberNames = new string[] { validationContext.MemberName };
            string displayName = (validationContext.DisplayName == null) ? validationContext.MemberName : validationContext.DisplayName;
            var x = value;
            if (x < 0m)  //ZERO is allowed BUG 39805
                return new ValidationResult(string.Format("{0} is required.", displayName), memberNames);
#endif
            return ValidationResult.Success;
        }
    }

    //Cross-Field and Cross-Entity Validations
    public static class AdmissionAuthorizationEntityValidations
    {
        public static ValidationResult EffectiveToDate_Valid(AdmissionAuthorization currentAdmissionAuthorization)//, ValidationContext validationContext)
        {
            var effective_from_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorization.EffectiveFromDate, DateTime.MinValue);
            var effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorization.EffectiveToDate, DateTime.MaxValue);

            if (effective_thru_date < effective_from_date)
            {
                string[] memberNames = new string[] { "EffectiveToDate" };
                return new ValidationResult("Authorization effective thru date must be later than effective from date.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EffectiveFromDate_Valid(AdmissionAuthorization currentAdmissionAuthorization) //, ValidationContext validationContext)
        {
            var effective_from_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorization.EffectiveFromDate, DateTime.MinValue);
            var effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorization.EffectiveToDate, DateTime.MaxValue);

            if (effective_from_date > effective_thru_date)
            {
                string[] memberNames = new string[] { "EffectiveFromDate" };
                return new ValidationResult("Authorization effective from date must be earlier than effective thru date.", memberNames);
            }
            return ValidationResult.Success;
        }
    }

    //Cross-Field and Cross-Entity Validations
    public static class AdmissionAuthorizationInstanceEntityValidations
    {
        public static ValidationResult ServiceTypes_Valid(AdmissionAuthorizationInstance currentAdmissionAuthorizationInstance) //, ValidationContext validationContext)
        {
            //Note: BaseType - Gets the type from which the current Type directly inherits.
            //Client - {Virtuoso.Server.Data.VirtuosoEntity}
            //Server - {Name = "Object" FullName = "System.Object"}	System.Type {System.RuntimeType}
            //var running_on_server = currentAdmissionAuthorizationDetail.GetType().BaseType == typeof(System.Object);  //Checking for BaseType of Object to test is running validation on server.

            //Note: currentAdmissionAuthorizationDetail.AdmissionAuthorization will be NULL on server - on client, should have been added to Parent
            if (currentAdmissionAuthorizationInstance.AdmissionAuthorization != null)
            {
                //var allAuthorizationDetails = currentAdmissionAuthorizationInstance.AdmissionAuthorization.AdmissionAuthorizationDetail.ToList();
                var allAuthorizationDetails = currentAdmissionAuthorizationInstance.AdmissionAuthorization.AdmissionAuthorizationInstance.ToList();

                //Overlapping dates by Discipline
                if (allAuthorizationDetails.Any(aai =>
                    aai.AdmissionAuthorizationInstanceKey != currentAdmissionAuthorizationInstance.AdmissionAuthorizationInstanceKey
                    //&& (aai.DeletedDate.HasValue == false)
                    && AdmissionAuthorizationUtility.IsDiscipline(aai)
                    && aai.AdmissionDisciplineKey == currentAdmissionAuthorizationInstance.AdmissionDisciplineKey
                    && aai.ServiceTypeKey == currentAdmissionAuthorizationInstance.ServiceTypeKey
                    && aai.ServiceTypeGroupKey == currentAdmissionAuthorizationInstance.ServiceTypeGroupKey
                        // All non null dates
                    && ((currentAdmissionAuthorizationInstance.EffectiveFromDate <= aai.EffectiveToDate && currentAdmissionAuthorizationInstance.EffectiveToDate >= aai.EffectiveFromDate)
                        // row passed in has null thru date
                    || (currentAdmissionAuthorizationInstance.EffectiveToDate == null && aai.EffectiveToDate >= currentAdmissionAuthorizationInstance.EffectiveFromDate)
                        // row passed in has non null
                    || (aai.EffectiveToDate == null && currentAdmissionAuthorizationInstance.EffectiveToDate >= aai.EffectiveFromDate)
                        // both have non null thru dates
                    || (aai.EffectiveToDate == null && currentAdmissionAuthorizationInstance.EffectiveToDate == null)
                    )))
                {
//#if DEBUG
//                    var dups = allAuthorizationDetails.Where(aai =>
//                            aai.AdmissionAuthorizationInstanceKey != currentAdmissionAuthorizationInstance.AdmissionAuthorizationInstanceKey
//                            && AdmissionAuthorizationUtility.IsDiscipline(aai)
//                            && aai.AdmissionDisciplineKey == currentAdmissionAuthorizationInstance.AdmissionDisciplineKey
//                            && aai.ServiceTypeKey == currentAdmissionAuthorizationInstance.ServiceTypeKey
//                            && aai.ServiceTypeGroupKey == currentAdmissionAuthorizationInstance.ServiceTypeGroupKey
//                                // All non null dates
//                            && ((currentAdmissionAuthorizationInstance.EffectiveFromDate <= aai.EffectiveToDate && currentAdmissionAuthorizationInstance.EffectiveToDate >= aai.EffectiveFromDate)
//                                // row passed in has null thru date
//                            || (currentAdmissionAuthorizationInstance.EffectiveToDate == null && aai.EffectiveToDate >= currentAdmissionAuthorizationInstance.EffectiveFromDate)
//                                // row passed in has non null
//                            || (aai.EffectiveToDate == null && currentAdmissionAuthorizationInstance.EffectiveToDate >= aai.EffectiveFromDate)
//                                // both have non null thru dates
//                            || (aai.EffectiveToDate == null && currentAdmissionAuthorizationInstance.EffectiveToDate == null)
//                        )).ToList();
//#endif
                    //&& (aai.DeletedDate.HasValue == false)

                    return new ValidationResult("Admission authorization details must not overlap for the same discipline and/or service type.");
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EffectiveToDate_Valid(AdmissionAuthorizationInstance currentAdmissionAuthorizationInstance) //, ValidationContext validationContext)
        {
            var effective_from_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationInstance.EffectiveFromDate, DateTime.MinValue);
            var effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationInstance.EffectiveToDate, DateTime.MaxValue);

            if (effective_thru_date < effective_from_date)
            {
                string[] memberNames = new string[] { "EffectiveToDate" };
                return new ValidationResult("Effective thru date of authorization detail must be later than effective from date.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EffectiveFromDate_Valid(AdmissionAuthorizationInstance currentAdmissionAuthorizationInstance) //, ValidationContext validationContext)
        {
            var effective_from_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationInstance.EffectiveFromDate, DateTime.MinValue);
            var effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationInstance.EffectiveToDate, DateTime.MaxValue);

            if (effective_from_date > effective_thru_date)
            {
                string[] memberNames = new string[] { "EffectiveFromDate" };
                return new ValidationResult("Effective from date of authorization detail must be earlier than effective thru date.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AuthorizationType_Required(AdmissionAuthorizationInstance currentAdmissionAuthorizationInstance) //, ValidationContext validationContext)
        {
            if (AdmissionAuthorizationUtility.IsGeneralOrDiscipline(currentAdmissionAuthorizationInstance))
            {
                if (currentAdmissionAuthorizationInstance.AuthorizationType == null)
                {
                    string[] memberNames = new string[] { "AuthorizationType" };
                    return new ValidationResult("Authorization Type is required.", memberNames);
                }

            }
            return ValidationResult.Success;
        }

        public static ValidationResult DatesValidWithHeader(AdmissionAuthorizationInstance currentAdmissionAuthorizationInstance) //, ValidationContext validationContext)
        {
            ////Note: BaseType - Gets the type from which the current Type directly inherits.
            ////Client - {Virtuoso.Server.Data.VirtuosoEntity}
            ////Server - {Name = "Object" FullName = "System.Object"}	System.Type {System.RuntimeType}
            //var running_on_server = currentAdmissionAuthorizationDetail.GetType().BaseType == typeof(System.Object);  //Checking for BaseType of Object to test is running validation on server.
            //if (running_on_server)
            //{
            //    string[] memberNames = new string[] { "EffectiveToDate" };
            //    return new ValidationResult("Testing server validation...", memberNames);
            //}

            if (AdmissionAuthorizationUtility.IsGeneralOrDiscipline(currentAdmissionAuthorizationInstance))
            {
                //if we have the parent - back reference
                if (currentAdmissionAuthorizationInstance.AdmissionAuthorization != null) //E.G. #if SILVERLIGHT - meaning this validation does not run on the server, unless the parent exists in the changeset
                {
                    DateTime header_effective_from_date = currentAdmissionAuthorizationInstance.AdmissionAuthorization.EffectiveFromDate;
                    DateTime header_effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationInstance.AdmissionAuthorization.EffectiveToDate, DateTime.MaxValue);
                    DateTime detail_effective_from_date = currentAdmissionAuthorizationInstance.EffectiveFromDate;
                    DateTime detail_effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationInstance.EffectiveToDate, DateTime.MaxValue);

                    if (detail_effective_from_date < header_effective_from_date)
                    {
                        string[] memberNames = new string[] { "EffectiveFromDate" };
                        return new ValidationResult("Effective from date of authorization detail must be on or after effective from date on the authorization header.", memberNames);
                    }

                    if (detail_effective_thru_date > header_effective_thru_date)
                    {
                        string[] memberNames = new string[] { "EffectiveToDate" };
                        return new ValidationResult("Effective thru date of authorization detail must be on or before effective thru date on the authorization header.", memberNames);
                    }

                    if (detail_effective_from_date > header_effective_thru_date)
                    {
                        string[] memberNames = new string[] { "EffectiveFromDate" };
                        return new ValidationResult("Effective from date of authorization detail cannot be later than the effective thru date on the authorization header.", memberNames);
                    }

                    if (detail_effective_from_date > detail_effective_thru_date)
                    {
                        string[] memberNames = new string[] { "EffectiveFromDate" };
                        return new ValidationResult("Effective from date of authorization detail cannot be later than effective thru date.", memberNames);
                    }
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AuthAmountValidWithDetail(AdmissionAuthorizationInstance currentAdmissionAuthorizationInstance) //, ValidationContext validationContext)
        {
            //Ensure that AdmissionAuthorization.Instance.AuthorizationAmount matches any existing AdmissionAuthorizationDetail records
            return ValidationResult.Success;
        }

        public static ValidationResult DatesValidWithDetail(AdmissionAuthorizationInstance currentAdmissionAuthorizationInstance) //, ValidationContext validationContext)
        {
            //Ensure that AdmissionAuthorization.Instance.EffectiveFromDate and EffectiveThruDate matches any existing AdmissionAuthorizationDetail records
            return ValidationResult.Success;
        }
    }

    //Cross-Field and Cross-Entity Validations
    public static class AdmissionAuthorizationDetailEntityValidations
    {
        public static ValidationResult ServiceTypes_Valid(AdmissionAuthorizationDetail currentAdmissionAuthorizationDetail) //, ValidationContext validationContext)
        {
            if (currentAdmissionAuthorizationDetail.DeletedDate.HasValue) //Do not validate dates on a deleted authorization
                return ValidationResult.Success;

            //Note: BaseType - Gets the type from which the current Type directly inherits.
            //Client - {Virtuoso.Server.Data.VirtuosoEntity}
            //Server - {Name = "Object" FullName = "System.Object"}	System.Type {System.RuntimeType}
            //var running_on_server = currentAdmissionAuthorizationDetail.GetType().BaseType == typeof(System.Object);  //Checking for BaseType of Object to test is running validation on server.

            //Note: currentAdmissionAuthorizationDetail.AdmissionAuthorization will be NULL on server - on client, should have been added to Parent
            if (currentAdmissionAuthorizationDetail.AdmissionAuthorization != null)
            {
                var allAuthorizationDetails = currentAdmissionAuthorizationDetail.AdmissionAuthorization.AdmissionAuthorizationDetail.ToList();

                //Overlapping dates by Discipline
                if (allAuthorizationDetails.Any(aad => 
                    aad.AdmissionAuthorizationDetailKey != currentAdmissionAuthorizationDetail.AdmissionAuthorizationDetailKey
                    && (aad.DeletedDate.HasValue == false)
                    && (currentAdmissionAuthorizationDetail.DeletedDate.HasValue == false)
                    && AdmissionAuthorizationUtility.IsDiscipline(aad)
                    && aad.AdmissionDisciplineKey == currentAdmissionAuthorizationDetail.AdmissionDisciplineKey
                    && aad.ServiceTypeKey == currentAdmissionAuthorizationDetail.ServiceTypeKey
                    && aad.ServiceTypeGroupKey == currentAdmissionAuthorizationDetail.ServiceTypeGroupKey
                    // All non null dates
                    && ((currentAdmissionAuthorizationDetail.EffectiveFromDate <= aad.EffectiveToDate && currentAdmissionAuthorizationDetail.EffectiveToDate >= aad.EffectiveFromDate)
                    // row passed in has null thru date
                    || (currentAdmissionAuthorizationDetail.EffectiveToDate == null && aad.EffectiveToDate >= currentAdmissionAuthorizationDetail.EffectiveFromDate)
                    // row passed in has non null
                    || (aad.EffectiveToDate == null && currentAdmissionAuthorizationDetail.EffectiveToDate >= aad.EffectiveFromDate)
                    // both have non null thru dates
                    || (aad.EffectiveToDate == null && currentAdmissionAuthorizationDetail.EffectiveToDate == null)
                    )))
                {
//#if DEBUG
//                    var dups = allAuthorizationDetails.Where(aad =>
//                        aad.AdmissionAuthorizationDetailKey != currentAdmissionAuthorizationDetail.AdmissionAuthorizationDetailKey
//                        && AdmissionAuthorizationUtility.IsDiscipline(aad)
//                        && aad.ServiceTypeKey == currentAdmissionAuthorizationDetail.ServiceTypeKey
//                        && aad.ServiceTypeGroupKey == currentAdmissionAuthorizationDetail.ServiceTypeGroupKey
//                            // All non null dates
//                        && ((currentAdmissionAuthorizationDetail.EffectiveFromDate <= aad.EffectiveToDate && currentAdmissionAuthorizationDetail.EffectiveToDate >= aad.EffectiveFromDate)
//                            // row passed in has null thru date
//                        || (currentAdmissionAuthorizationDetail.EffectiveToDate == null && aad.EffectiveToDate >= currentAdmissionAuthorizationDetail.EffectiveFromDate)
//                            // row passed in has non null
//                        || (aad.EffectiveToDate == null && currentAdmissionAuthorizationDetail.EffectiveToDate >= aad.EffectiveFromDate)
//                            // both have non null thru dates
//                        || (aad.EffectiveToDate == null && currentAdmissionAuthorizationDetail.EffectiveToDate == null)
//                        )).ToList();
//#endif
                    return new ValidationResult("Admission authorization details must not overlap for the same discipline and/or service type.");
                }
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EffectiveToDate_Valid(AdmissionAuthorizationDetail currentAdmissionAuthorizationDetail) //, ValidationContext validationContext)
        {
            if (currentAdmissionAuthorizationDetail.DeletedDate.HasValue) //Do not validate dates on a deleted authorization
                return ValidationResult.Success;

            var effective_from_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationDetail.EffectiveFromDate, DateTime.MinValue);
            var effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationDetail.EffectiveToDate, DateTime.MaxValue);

            if (effective_thru_date < effective_from_date)
            {
                string[] memberNames = new string[] { "EffectiveToDate" };
                return new ValidationResult("Effective thru date of authorization detail must be later than effective from date.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult EffectiveFromDate_Valid(AdmissionAuthorizationDetail currentAdmissionAuthorizationDetail) //, ValidationContext validationContext)
        {
            if (currentAdmissionAuthorizationDetail.DeletedDate.HasValue) //Do not validate dates on a deleted authorization
                return ValidationResult.Success;

            var effective_from_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationDetail.EffectiveFromDate, DateTime.MinValue);
            var effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationDetail.EffectiveToDate, DateTime.MaxValue);
            
            if (effective_from_date > effective_thru_date)
            {
                string[] memberNames = new string[] { "EffectiveFromDate" };
                return new ValidationResult("Effective from date of authorization detail must be earlier than effective thru date.", memberNames);
            }
            return ValidationResult.Success;
        }

        public static ValidationResult AuthorizationType_Required(AdmissionAuthorizationDetail currentAdmissionAuthorizationDetail) //, ValidationContext validationContext)
        {
            if (currentAdmissionAuthorizationDetail.DeletedDate.HasValue) //Do not validate dates on a deleted authorization
                return ValidationResult.Success;

            if (AdmissionAuthorizationUtility.IsGeneralOrDiscipline(currentAdmissionAuthorizationDetail))
            {
                if (currentAdmissionAuthorizationDetail.AuthorizationType == null)
                {
                    string[] memberNames = new string[] { "AuthorizationType" };
                    return new ValidationResult("Authorization Type is required.", memberNames);
                }

            }
            return ValidationResult.Success;
        }

        public static ValidationResult DatesValidWithHeader(AdmissionAuthorizationDetail currentAdmissionAuthorizationDetail) //, ValidationContext validationContext)
        {
            if (currentAdmissionAuthorizationDetail.DeletedDate.HasValue) //Do not validate dates on a deleted authorization
                return ValidationResult.Success;

            ////Note: BaseType - Gets the type from which the current Type directly inherits.
            ////Client - {Virtuoso.Server.Data.VirtuosoEntity}
            ////Server - {Name = "Object" FullName = "System.Object"}	System.Type {System.RuntimeType}
            //var running_on_server = currentAdmissionAuthorizationDetail.GetType().BaseType == typeof(System.Object);  //Checking for BaseType of Object to test is running validation on server.
            //if (running_on_server)
            //{
            //    string[] memberNames = new string[] { "EffectiveToDate" };
            //    return new ValidationResult("Testing server validation...", memberNames);
            //}

            if (AdmissionAuthorizationUtility.IsGeneralOrDiscipline(currentAdmissionAuthorizationDetail))
            {
                //if we have the parent - back reference
                if (currentAdmissionAuthorizationDetail.AdmissionAuthorization != null) //E.G. #if SILVERLIGHT - meaning this validation does not run on the server, unless the parent exists in the changeset
                {
                    DateTime header_effective_from_date = currentAdmissionAuthorizationDetail.AdmissionAuthorization.EffectiveFromDate;
                    DateTime header_effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationDetail.AdmissionAuthorization.EffectiveToDate, DateTime.MaxValue);
                    DateTime detail_effective_from_date = currentAdmissionAuthorizationDetail.EffectiveFromDate;
                    DateTime detail_effective_thru_date = AdmissionAuthorizationUtility.GetDate(currentAdmissionAuthorizationDetail.EffectiveToDate, DateTime.MaxValue);

                    if (detail_effective_from_date < header_effective_from_date)
                    {
                        string[] memberNames = new string[] { "EffectiveFromDate" };
                        return new ValidationResult("Effective from date of authorization detail must be on or after effective from date on the authorization header.", memberNames);
                    }

                    if (detail_effective_thru_date > header_effective_thru_date)
                    {
                        string[] memberNames = new string[] { "EffectiveToDate" };
                        return new ValidationResult("Effective thru date of authorization detail must be on or before effective thru date on the authorization header.", memberNames);
                    }

                    if (detail_effective_from_date > header_effective_thru_date)
                    {
                        string[] memberNames = new string[] { "EffectiveFromDate" };
                        return new ValidationResult("Effective from date of authorization detail cannot be later than the effective thru date on the authorization header.", memberNames);
                    }

                    if (detail_effective_from_date > detail_effective_thru_date)
                    {
                        string[] memberNames = new string[] { "EffectiveFromDate" };
                        return new ValidationResult("Effective from date of authorization detail cannot be later than effective thru date.", memberNames);
                    }                    
                }
            }            
            return ValidationResult.Success;
        }
    }

    internal static class AdmissionAuthorizationUtility
    {
        public static DateTime GetDate(DateTime? date, DateTime defaultDate)
        {
            if (date.HasValue == false)
                return defaultDate;
            if (date.Value.Equals(DateTime.MinValue))
                return defaultDate;
            else
                return date.Value;
        }

        public static bool IsGeneralOrDiscipline(AdmissionAuthorizationDetail aad)
        {
            var __isGeneral = IsGeneral(aad);
            var __isDiscipline = IsDiscipline(aad);
            var ret = __isGeneral || __isDiscipline;
            return ret;
        }

        public static bool IsGeneralOrDiscipline(AdmissionAuthorizationInstance aai)
        {
            var __isGeneral = IsGeneral(aai);
            var __isDiscipline = IsDiscipline(aai);
            var ret = __isGeneral || __isDiscipline;
            return ret;
        }

        public static bool IsGeneral(AdmissionAuthorizationDetail aad)
        {
            var ret = aad.ServiceTypeGroupKey.HasValue == false
                && aad.ServiceTypeKey.HasValue == false
                && aad.AdmissionDisciplineKey.HasValue == false
                && aad.AuthorizationDiscCode.HasValue == false;
            return ret;
        }

        public static bool IsGeneral(AdmissionAuthorizationInstance aai)
        {
            var ret = aai.ServiceTypeGroupKey.HasValue == false
                && aai.ServiceTypeKey.HasValue == false
                && aai.AdmissionDisciplineKey.HasValue == false
                && aai.AuthorizationDiscCode.HasValue == false;
            return ret;
        }

        public static bool IsDiscipline(AdmissionAuthorizationDetail aad)
        {
            //Note: AdmissionAuthorizationDetail.AdmissionDisciplineKey is FK to Discipline.DisciplineKey - NOT AdmissionDiscipline.AdmissionDisciplineKey...
            //Note: neither ServiceTypeGroupKey nor ServiceTypeKey are required when AdmissionDisciplineKey (DSCP) is specified
            var ret = (aad.AdmissionDisciplineKey.HasValue && (aad.AuthorizationDiscCode.HasValue == false));
            return ret;
        }

        public static bool IsDiscipline(AdmissionAuthorizationInstance aai)
        {
            //Note: AdmissionAuthorizationDetail.AdmissionDisciplineKey is FK to Discipline.DisciplineKey - NOT AdmissionDiscipline.AdmissionDisciplineKey...
            //Note: neither ServiceTypeGroupKey nor ServiceTypeKey are required when AdmissionDisciplineKey (DSCP) is specified
            var ret = (aai.AdmissionDisciplineKey.HasValue && (aai.AuthorizationDiscCode.HasValue == false));
            return ret;
        }

        public static bool IsSupplyOrEquipment(AdmissionAuthorizationDetail aad)
        {
            //Note: AdmissionAuthorizationDetail.AuthorizationDiscCode is FK to CodeLookup <Equipment> and <Supplies>
            var ret = aad.AuthorizationDiscCode.HasValue == true
                && aad.ServiceTypeGroupKey.HasValue == false
                && aad.ServiceTypeKey.HasValue == false
                && aad.AdmissionDisciplineKey.HasValue == false;
            return ret;
        }

        public static bool IsSupplyOrEquipment(AdmissionAuthorizationInstance aai)
        {
            //Note: AdmissionAuthorizationDetail.AuthorizationDiscCode is FK to CodeLookup <Equipment> and <Supplies>
            var ret = aai.AuthorizationDiscCode.HasValue == true
                && aai.ServiceTypeGroupKey.HasValue == false
                && aai.ServiceTypeKey.HasValue == false
                && aai.AdmissionDisciplineKey.HasValue == false;
            return ret;
        }
    }
}
