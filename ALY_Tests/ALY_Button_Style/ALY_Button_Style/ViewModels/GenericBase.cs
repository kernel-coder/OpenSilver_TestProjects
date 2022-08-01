#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public abstract class GenericBase : GalaSoft.MvvmLight.ViewModelBase
    {
        static string ApplicationFolder { get; set; }
        static string SIPFolder { get; set; }

        static GenericBase()
        {
            //ApplicationFolder = ApplicationStoreInfo.GetUserStoreForApplication();
           // SIPFolder = ApplicationStoreInfo.GetUserStoreForApplication(Constants.SAVE_FOLDER);
        }

        public object Clone(object objToClone)
        {
            if (objToClone == null)
            {
                return null;
            }

            //First we create an instance of this specific type.
            object newObject = Activator.CreateInstance(objToClone.GetType());

            //We get the array of fields for the new type instance.
            FieldInfo[] fields = newObject.GetType().GetFields();

            int i = 0;

            foreach (FieldInfo fi in objToClone.GetType().GetFields())
            {
                fields[i].SetValue(newObject, fi.GetValue(objToClone));
                i++;
            }

            // get the public properties for the object
            PropertyInfo[] propInfo = newObject.GetType().GetProperties();
            i = 0;

            // loop through each property
            foreach (PropertyInfo pi in objToClone.GetType().GetProperties())
            {
                // ignore child entitysets and child entities
                if (pi.PropertyType.Namespace == "System.Data.Linq" && pi.PropertyType.Name.Contains("EntitySet"))
                {
                    i++;
                    continue;
                }

                if ((pi.PropertyType.BaseType != null) &&
                    (pi.PropertyType.BaseType.ToString().Contains("VirtuosoEntity")))
                {
                    i++;
                    continue;
                }

                if (propInfo[i].CanWrite && propInfo[i].CanRead && propInfo[i].PropertyType != typeof(EntityConflict))
                {
                    propInfo[i].SetValue(newObject, pi.GetValue(objToClone, null), null);
                }

                i++;
            }

            return newObject;
        }

        #region Properties

        //public NavigateKey NavigateKey { get; set; }

        //public EntityManager EntityManager => EntityManager.Current;

        public void RaisePropertyChangedLambda(string propertyName)
        {
            try
            {
                base.RaisePropertyChanged(propertyName);
            }
            catch
            {
            }
        }

        #endregion //Properties
    }

    public static class GenericBaseEx
    {
        public static void RaisePropertyChangedLambda<T, TProperty>(this T observableBase,
            Expression<Func<T, TProperty>> expression) where T : GenericBase
        {
            observableBase.RaisePropertyChangedLambda(observableBase.GetPropertyName(expression));
        }

        public static string GetPropertyName<T, TProperty>(this T owner, Expression<Func<T, TProperty>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;
            if (memberExpression == null)
            {
                var unaryExpression = expression.Body as UnaryExpression;
                if (unaryExpression != null)
                {
                    memberExpression = unaryExpression.Operand as MemberExpression;

                    if (memberExpression == null)
                    {
                        throw new NotImplementedException();
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }

            var propertyName = memberExpression.Member.Name;
            return propertyName;
        }
    }

    public class EntityBase : GenericBase, INotifyDataErrorInfo
    {
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public EntityBase()
        {
            _currentErrors = new Dictionary<string, List<string>>();
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }
    }


    public class NotifyDataErrorInfoViewModelBase : GalaSoft.MvvmLight.ViewModelBase, INotifyDataErrorInfo
    {
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors;

        public NotifyDataErrorInfoViewModelBase()
        {
            _currentErrors = new Dictionary<string, List<string>>();
        }

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                //FYI: if you are not supporting entity level errors, it is acceptable to return null
                var ret = _currentErrors.Values.Where(c => c.Any());
                return ret.Any() ? ret : null;
            }

            MakeOrCreatePropertyErrorList(propertyName);
            if (_currentErrors[propertyName].Any())
            {
                return _currentErrors[propertyName];
            }

            return null;
        }

        public IEnumerable<string> GetAllErrors()
        {
            return _currentErrors.Values.SelectMany(x => x).ToList();
        }

        public bool HasErrors
        {
            get { return _currentErrors.Where(c => c.Value.Any()).Any(); }
        }

        void FireErrorsChanged(string property)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(property));
            }
        }

        public void ClearErrorFromProperty(string property)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Clear();
            FireErrorsChanged(property);
        }

        public void AddErrorForProperty(string property, string error)
        {
            MakeOrCreatePropertyErrorList(property);
            _currentErrors[property].Add(error);
            FireErrorsChanged(property);
        }

        void MakeOrCreatePropertyErrorList(string propertyName)
        {
            if (!_currentErrors.ContainsKey(propertyName))
            {
                _currentErrors[propertyName] = new List<string>();
            }
        }
    }

    // A base ViewModel class with an INotifyDataErrorInfo implementation that supports System.ComponentModel.DataAnnotations on ViewModel properties
    public class ViewModelValidationBase : GalaSoft.MvvmLight.ViewModelBase, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

        private readonly object _lock = new object();

        public bool HasErrors
        {
            get { return _errors.Any(propErrors => propErrors.Value != null && propErrors.Value.Count > 0); }
        }

        public bool IsValid => HasErrors;

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                if (_errors.ContainsKey(propertyName) && (_errors[propertyName] != null) && _errors[propertyName].Count > 0)
                {
                    return _errors[propertyName].ToList();
                }

                return null;
            }

            return _errors.SelectMany(err => err.Value.ToList());
        }

        public void OnErrorsChanged(string propertyName)
        {
            if (ErrorsChanged != null)
            {
                ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        public void ValidateProperty(object value, [CallerMemberName] string propertyName = null)
        {
            lock (_lock)
            {
                var validationContext = new ValidationContext(this, null, null);
                validationContext.MemberName = propertyName;
                var validationResults = new List<ValidationResult>();
                Validator.TryValidateProperty(value, validationContext, validationResults);

                //clear previous _errors from tested property  
                if (_errors.ContainsKey(propertyName))
                {
                    _errors.Remove(propertyName);
                }

                OnErrorsChanged(propertyName);
                HandleValidationResults(validationResults);
            }
        }

        public void Validate()
        {
            lock (_lock)
            {
                var validationContext = new ValidationContext(this, null, null);
                var validationResults = new List<ValidationResult>();
                Validator.TryValidateObject(this, validationContext, validationResults, true);

                //clear all previous _errors  
                var propNames = _errors.Keys.ToList();
                _errors.Clear();
                propNames.ForEach(pn => OnErrorsChanged(pn));
                HandleValidationResults(validationResults);
            }
        }

        private void HandleValidationResults(List<ValidationResult> validationResults)
        {
            //Group validation results by property names  
            var resultsByPropNames = from res in validationResults
                from mname in res.MemberNames
                group res by mname
                into g
                select g;
            //add _errors to dictionary and inform binding engine about _errors  
            foreach (var prop in resultsByPropNames)
            {
                var messages = prop.Select(r => r.ErrorMessage).ToList();
                _errors.Add(prop.Key, messages);
                OnErrorsChanged(prop.Key);
            }
        }
    }
}