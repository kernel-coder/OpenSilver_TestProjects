#region Usings

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public static class EntityExtensions
    {
        public static bool Validate(this Entity entity)
        {
            if (entity.ValidationErrors.Count > 0)
            {
                entity.ValidationErrors.Clear();
            }

            ICollection<ValidationResult> validationResults = new List<ValidationResult>();

            var contextServiceProvider = new SimpleServiceProvider();
            contextServiceProvider.AddService<ICodeLookupDataProvider>(new CodeLookupDataProvider());
            contextServiceProvider.AddService<IWoundDataProvider>(new WoundDataProvider());
            contextServiceProvider.AddService<IPatientContactDataProvider>(new PatientContactDataProvider());
            contextServiceProvider.AddService<IAdmissionDataProvider>(new AdmissionDataProvider());
            contextServiceProvider.AddService<INonServiceTypeDataProvider>(new NonServiceTypeDataProvider());
            contextServiceProvider.AddService<IInsuranceDataProvider>(new InsuranceDataProvider());
            contextServiceProvider.AddService<IServiceLineTypeProvider>(new ServiceLineTypeProvider());
            contextServiceProvider.AddService<IUniquenessCheckProvider>(new UniquenessCheckProvider());
            contextServiceProvider.AddService<IPhysicianDataProvider>(new PhysicianDataProvider());

            var validationContext = new ValidationContext(entity, contextServiceProvider, null);

            if (Validator.TryValidateObject(entity, validationContext, validationResults, true) == false)
            {
                foreach (ValidationResult error in validationResults) entity.ValidationErrors.Add(error);
                return false;
            }

            return true;
        }

        //clearEntityErrors is TRUE - clears entity ValidationErrors collection and re-add them back
        public static bool Validate(this Entity entity, ObservableCollection<string> _errors,
            bool clearEntityErrors = true)
        {
            if (clearEntityErrors)
            {
                if (entity.ValidationErrors.Count > 0)
                {
                    entity.ValidationErrors.Clear();
                }
            }

            ICollection<ValidationResult> validationResults = new List<ValidationResult>();

            var contextServiceProvider = new SimpleServiceProvider();
            contextServiceProvider.AddService<ICodeLookupDataProvider>(new CodeLookupDataProvider());
            contextServiceProvider.AddService<IServiceLineTypeProvider>(new ServiceLineTypeProvider());
            contextServiceProvider.AddService<IUniquenessCheckProvider>(new UniquenessCheckProvider());
            contextServiceProvider.AddService<IWoundDataProvider>(new WoundDataProvider());
            contextServiceProvider.AddService<IPatientContactDataProvider>(new PatientContactDataProvider());

            var validationContext = new ValidationContext(entity, contextServiceProvider, null);

            if (Validator.TryValidateObject(entity, validationContext, validationResults, true) == false)
            {
                foreach (ValidationResult error in validationResults)
                {
                    _errors.Add(error.ErrorMessage);

                    if (clearEntityErrors)
                    {
                        entity.ValidationErrors.Add(error);
                    }
                }

                return false;
            }

            return true;
        }
    }
}