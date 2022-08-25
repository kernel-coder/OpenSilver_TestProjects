#region Usings

using System;
using System.Linq;
using Virtuoso.Core.Cache;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class ServiceType
    {
        private Form __FormTypeCode;
        private bool? __IsAttempted;
        private bool? __IsCOTI;
        private bool? __IsHospiceElectionAddendum;
        private bool? __IsHospiceF2F;
        private bool? __IsPlanOfCare;
        private bool? __IsVerbalCOTI;

        public string DisciplineSupervisedServiceTypeLabel
        {
            get
            {
                var discrow = Discipline ?? DisciplineCache.GetDisciplineFromKey(DisciplineKey);
                return discrow.SupervisedServiceTypeLabel;
            }
        }

        private Form FormTypeCode
        {
            get
            {
                if (__FormTypeCode == null)
                {
                    return DynamicFormCache.GetFormByKey((int)FormKey);
                }

                return __FormTypeCode;
            }
        }

        public bool IsAttempted
        {
            get
            {
                if (__IsAttempted.HasValue == false)
                {
                    if (FormKey == null)
                    {
                        __IsAttempted = false;
                        return __IsAttempted.Value;
                    }

                    var f = FormTypeCode;
                    if (f == null)
                    {
                        __IsAttempted = false;
                        return __IsAttempted.Value;
                    }

                    __IsAttempted = f.IsAttempted;
                }

                return __IsAttempted.Value;
            }
        }

        public bool IsHospiceElectionAddendum
        {
            get
            {
                if (__IsHospiceElectionAddendum.HasValue == false)
                {
                    if (FormKey == null)
                    {
                        __IsHospiceElectionAddendum = false;
                        return __IsHospiceElectionAddendum.Value;
                    }

                    var f = FormTypeCode;
                    if (f == null)
                    {
                        __IsHospiceElectionAddendum = false;
                        return __IsHospiceElectionAddendum.Value;
                    }

                    __IsHospiceElectionAddendum = f.IsHospiceElectionAddendum;
                }

                return __IsHospiceElectionAddendum.Value;
            }
        }

        public bool IsCOTI
        {
            get
            {
                if (__IsCOTI.HasValue == false)
                {
                    if (FormKey == null)
                    {
                        __IsCOTI = false;
                        return __IsCOTI.Value;
                    }

                    var f = FormTypeCode;
                    if (f == null)
                    {
                        __IsCOTI = false;
                        return __IsCOTI.Value;
                    }

                    __IsCOTI = f.IsCOTI;
                }

                return __IsCOTI.Value;
            }
        }

        public bool IsVerbalCOTI
        {
            get
            {
                if (__IsVerbalCOTI.HasValue == false)
                {
                    if (FormKey == null)
                    {
                        __IsVerbalCOTI = false;
                        return __IsVerbalCOTI.Value;
                    }

                    var f = FormTypeCode;
                    if (f == null)
                    {
                        __IsVerbalCOTI = false;
                        return __IsVerbalCOTI.Value;
                    }

                    __IsVerbalCOTI = f.IsVerbalCOTI;
                }

                return __IsVerbalCOTI.Value;
            }
        }

        public bool IsHospiceF2F
        {
            get
            {
                if (__IsHospiceF2F.HasValue == false)
                {
                    if (FormKey == null)
                    {
                        __IsHospiceF2F = false;
                        return __IsHospiceF2F.Value;
                    }

                    var f = FormTypeCode;
                    if (f == null)
                    {
                        __IsHospiceF2F = false;
                        return __IsHospiceF2F.Value;
                    }

                    __IsHospiceF2F = f.IsHospiceF2F;
                }

                return __IsHospiceF2F.Value;
            }
        }

        public bool IsPlanOfCare
        {
            get
            {
                if (__IsPlanOfCare.HasValue == false)
                {
                    if (FormKey == null)
                    {
                        __IsPlanOfCare = false;
                        return __IsPlanOfCare.Value;
                    }

                    var f = FormTypeCode;
                    if (f == null)
                    {
                        __IsPlanOfCare = false;
                        return __IsPlanOfCare.Value;
                    }

                    __IsPlanOfCare = f.IsPlanOfCare;
                }

                return __IsPlanOfCare.Value;
            }
        }

        public string AssistantLabel
        {
            get
            {
                var label = string.Format("Is {0} Service Type", DisciplineSupervisedServiceTypeLabel);
                return label;
            }
        }

        public string RequireOrderCount
        {
            get
            {
                if (InsuranceAuthOrderTherapy == null)
                {
                    return "";
                }

                return FormatCountString(InsuranceAuthOrderTherapy
                    .Where(ia => ia.ComplianceType == "ORDER" && ia.Inactive == false).Count());
            }
        }

        public string RequireAuthCount
        {
            get
            {
                if (InsuranceAuthOrderTherapy == null)
                {
                    return "";
                }

                return FormatCountString(InsuranceAuthOrderTherapy
                    .Where(ia => ia.ComplianceType == "AUTH" && ia.Inactive == false).Count());
            }
        }

        public string RequireTherapyCount
        {
            get
            {
                if (InsuranceAuthOrderTherapy == null)
                {
                    return "";
                }

                return FormatCountString(InsuranceAuthOrderTherapy
                    .Where(ia => ia.ComplianceType == "THER" && ia.Inactive == false).Count());
            }
        }

        partial void OnInactiveChanged()
        {
            __FormTypeCode = null;
            __IsAttempted = null;
            __IsHospiceElectionAddendum = null;
            __IsCOTI = null;
            __IsHospiceF2F = null;
            __IsPlanOfCare = null;
            __IsVerbalCOTI = null;
            if (IsDeserializing)
            {
                return;
            }

            if (Inactive)
            {
                InactiveDate = DateTime.UtcNow;
            }
            else
            {
                InactiveDate = null;
            }
        }

        partial void OnFormKeyChanged()
        {
            __FormTypeCode = null;
            __IsAttempted = null;
            __IsHospiceElectionAddendum = null;
            __IsCOTI = null;
            __IsHospiceF2F = null;
            __IsPlanOfCare = null;
            __IsVerbalCOTI = null;
            if (IsDeserializing)
            {
            }
        }

        private string FormatCountString(int countParm)
        {
            if (countParm <= 0)
            {
                return "(0)";
            }

            return "(" + countParm + ")";
        }
    }
}