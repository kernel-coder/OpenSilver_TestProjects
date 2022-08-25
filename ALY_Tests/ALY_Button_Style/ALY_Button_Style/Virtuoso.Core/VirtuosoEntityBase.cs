#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Infrastructure;

#endregion

namespace Virtuoso.Server.Data
{
    [DataContract]
    public class VirtuosoEntity : Entity
    {
        protected VirtuosoEntity()
        {
        }

        public bool IgnoreChanged => (IsDeserializing || IsReadOnly || (EntityState == EntityState.Detached));

        public void ThisChanged()
        {
            RaisePropertyChanged("This");
        }

        public VirtuosoEntity This => this;
        private bool _RemoveFromView;

        [DataMember] //VERY IMPORTANT - needs persisted to save file for offline
        public bool RemoveFromView
        {
            get { return _RemoveFromView; }
            set
            {
                _RemoveFromView = value;
                RaisePropertyChanged("RemoveFromView");
            }
        }

        private bool _IsEditting;

        [DataMember] //VERY IMPORTANT - needs persisted to save file for offline
        public bool IsEditting
        {
            get { return _IsEditting; }
            set
            {
                _IsEditting = value;
                RaisePropertyChanged("IsEditting");
            }
        }

        private bool _IsInCancel;

        [DataMember] //VERY IMPORTANT - needs persisted to save file for offline
        public bool IsInCancel
        {
            get { return _IsInCancel; }
            set
            {
                _IsInCancel = value;
                RaisePropertyChanged("IsInCancel");
            }
        }

        private bool _IsOKed;

        [DataMember] //VERY IMPORTANT - needs persisted to save file for offline, so that newly added wound photos are not deleted when open RE_EVAL and cancel dialog
        public bool IsOKed
        {
            get { return _IsOKed; }
            set
            {
                _IsOKed = value;
                RaisePropertyChanged("IsOKed");
                RaisePropertyChanged("CanDelete");
            }
        }

        public virtual bool CanFullEdit => true;
        public virtual bool CanDelete => false;

        public void BeginEditting()
        {
            IsEditting = true;
            BeginEdit();
        }

        public void CancelEditting()
        {
            IsInCancel = true;
            CancelEdit();
            IsEditting = false;
            IsInCancel = false;
        }

        public void EndEditting()
        {
            IsEditting = false;
            EndEdit();
        }

        public bool IsModified => EntityState == EntityState.Modified;

        public virtual bool IsNew =>
            (EntityState == EntityState.New) ||
            (EntityState == EntityState.Detached);

        public bool IsInvalid => (HasValidationErrors);

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (Deployment.Current.Dispatcher.CheckAccess() == false) //If NOT on main thread exit
            {
                return;
            }

            if (!IsReadOnly)
            {
                base.OnPropertyChanged(e);

                if (String.CompareOrdinal(e.PropertyName, "EntityState") == 0)
                {
                    RaisePropertyChanged("IsNew");
                    RaisePropertyChanged("IsModified");
                }

                if ((IsInvalid == false) && (String.CompareOrdinal(e.PropertyName, "IsInvalid") != 0))
                {
                    RaisePropertyChanged("IsInvalid");
                }
            }
#if DEBUG
            else
            {
                if (Application.Current.IsRunningOutOfBrowser)
                {
                    var logWriter = Microsoft.Practices.EnterpriseLibrary.Common.Configuration
                        .EnterpriseLibraryContainer.Current
                        .GetInstance<Microsoft.Practices.EnterpriseLibrary.Logging.LogWriter>();
                    var propertyValue = new Object();
                    PropertyInfo property = GetType().GetProperty(e.PropertyName);
                    if (property != null)
                    {
                        propertyValue = property.GetValue(this, null);
                    }

                    logWriter.Write(
                        string.Format(
                            "[OnPropertyChanged] Not raising property changed.  IsReadOnly: {0}, PropertyName: {1}, PropertyValue: {2}",
                            IsReadOnly,
                            e.PropertyName,
                            propertyValue),
                        new[] { "VirtuosoEntity" }, //category
                        0, //priority
                        0, //eventid
                        Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics.TraceEventType.Warning);
                }
            }
#endif
        }

        public bool Validate(ValidationContext validationContext)
        {
            ValidationErrors.Clear();

            IServiceProvider serviceProvider = validationContext;
            IDictionary<object, object> items = null;
            if (validationContext != null)
            {
                items = validationContext.Items;
            }

            ValidationContext vc = new ValidationContext(this, serviceProvider, items);
            ICollection<ValidationResult> validationResults = new List<ValidationResult>();
            if (Validator.TryValidateObject(this, vc, validationResults, true) == false)
            {
                foreach (ValidationResult error in validationResults) ValidationErrors.Add(error);

                RaisePropertyChanged("IsInvalid");

                return false;
            }

            RaisePropertyChanged("IsInvalid");

            return true;
        }

        private static Dictionary<Type, PropertyInfo[]> PropertyCacheByType = new Dictionary<Type, PropertyInfo[]>();

        public void RaisePropertyChangedWithPrefix(string nameStartsWith)
        {
            Type type = GetType();
            if (!PropertyCacheByType.ContainsKey(type))
            {
                PropertyCacheByType[type] = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            }

            foreach (var prop in PropertyCacheByType[type])
                if (prop.Name.StartsWith(nameStartsWith) && prop.CanRead)
                {
                    RaisePropertyChanged(prop.Name);
                }
        }

        public object Clone()
        {
            return Clone(this);
        }

        public object Clone(object objToClone)
        {
            StreamingContext dummy = new StreamingContext();
            //First we create an instance of this specific type.
            object newObject = Activator.CreateInstance(objToClone.GetType());
            ((Entity)newObject).OnDeserializing(dummy);

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

                if (pi.PropertyType.Name.Contains("RelayCommand"))
                {
                    i++;
                    continue;
                }

                if ((pi.PropertyType.BaseType != null) &&
                    pi.PropertyType.BaseType.ToString().Contains("VirtuosoEntity"))
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

            ((Entity)newObject).OnDeserialized(dummy);
            return newObject;
        }

        public void CopyProperties(object to, string ingoreProperties, bool dataMembersOnly = false)
        {
            FieldInfo[] fields = to.GetType().GetFields();
            int i = 0;
            foreach (FieldInfo fi in GetType().GetFields()) fields[i++].SetValue(to, fi.GetValue(this));
            // get the public properties for the object
            PropertyInfo[] propInfo = to.GetType().GetProperties();
            i = 0;
            foreach (PropertyInfo pi in GetType().GetProperties())
            {
                // ignore ...
                if ((pi.PropertyType.Namespace == "System.Data.Linq" && pi.PropertyType.Name.Contains("EntitySet")) ||
                    (pi.PropertyType.Name.Contains("RelayCommand")) ||
                    ((pi.PropertyType.BaseType != null) &&
                     pi.PropertyType.BaseType.ToString().Contains("VirtuosoEntity")))
                {
                    i++;
                    continue;
                }

                if (dataMembersOnly)
                {
                    object[] dataMember = pi.GetCustomAttributes(typeof(DataMemberAttribute), true);
                    if ((dataMember == null) || (dataMember.Length <= 0))
                    {
                        i++;
                        continue;
                    }
                }

                if ((propInfo[i].CanWrite && propInfo[i].CanRead) &&
                    (propInfo[i].PropertyType != typeof(EntityConflict)) &&
                    (ingoreProperties.Contains("|" + propInfo[i].Name + "|") == false))
                {
                    propInfo[i].SetValue(to, pi.GetValue(this, null), null);
                }

                i++;
            }
        }
    }

    public static class EntityExtensions
    {
        public static void TrackChangedProperties(this Entity entity, List<string> changedProperties,
            string propertyName)
        {
            if (!changedProperties.Contains(propertyName))
            {
                changedProperties.Add(propertyName);
            }

            if (propertyName == "EntityState" && entity.EntityState == EntityState.Unmodified)
            {
                changedProperties.Clear();
            }
        }
    }
}