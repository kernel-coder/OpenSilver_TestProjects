#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Framework
{
    public abstract class ModelBase : INotifyPropertyChanged
    {
        static LogWriter logWriter;

        static ModelBase()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
        }

        bool _isLoading;

        public bool IsLoading
        {
            get { return _isLoading; }
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                }

                this.RaisePropertyChanged(p => p.IsLoading);
            }
        }

        bool _PendingSubmit;

        public bool PendingSubmit
        {
            get { return _PendingSubmit; }
            set
            {
                if (_PendingSubmit != value)
                {
                    _PendingSubmit = value;
                    this.RaisePropertyChanged(p => p.PendingSubmit);
                }
            }
        }

        List<SearchParameter> _SearchParameters = new List<SearchParameter>();

        public List<SearchParameter> SearchParameters
        {
            get { return _SearchParameters; }
            set
            {
                if (_SearchParameters != value)
                {
                    _SearchParameters = value;
                    this.RaisePropertyChanged(p => p.SearchParameters);
                }
            }
        }

        public abstract void LoadData();

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        protected void HandleEntityResults<T>(LoadOperation<T> results, EventHandler<EntityEventArgs<T>> handler,
            Action post_callback) where T : Entity
        {
            if (results.HasError)
            {
                results.MarkErrorAsHandled(); //doing this so that an exception is not raised to Application_UnhandledException()

                if (handler != null)
                {
                    Dispatcher.BeginInvoke(() => { handler(this, new EntityEventArgs<T>(results.Error)); });
                }
            }
            else
            {
                if (handler != null)
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        handler(this, new EntityEventArgs<T>(results.Entities));

                        if (post_callback != null)
                        {
                            post_callback();
                        }
                    });
                }
            }
        }

        protected void HandleErrorResults(SubmitOperation results, EventHandler<ErrorEventArgs> client_handler,
            Action post_callback)
        {
            if (client_handler != null)
            {
                if ((results.HasError) && (results.EntitiesInError.All(t => t.HasValidationErrors)))
                {
                    results.MarkErrorAsHandled(); //doing this so that an exception is not raised to Application_UnhandledException()
                }

                var _ErrorResults = new ObservableCollection<String>();
                var _ValidationErrorResults =
                    new ObservableCollection<System.ComponentModel.DataAnnotations.ValidationResult>();

                foreach (var entity in results.EntitiesInError)
                {
                    foreach (var err in entity.ValidationErrors)
                    {
                        _ErrorResults.Add(err.ErrorMessage);
                        _ValidationErrorResults.Add(err);
                    }
                }

                Dispatcher.BeginInvoke(() =>
                {
                    client_handler(this, new ErrorEventArgs(results.Error, _ErrorResults, _ValidationErrorResults));

                    if (post_callback != null)
                    {
                        post_callback();
                    }
                });
            }
            else
            {
                if (post_callback != null)
                {
                    post_callback();
                }
            }
        }

        protected Dispatcher Dispatcher => Deployment.Current.Dispatcher;

        private IEnumerable<Type> _entityTypes;

        public bool OpenOrInvalidObjects(DomainContext domainContext, string tag = "", bool log = false)
        {
            var open_edits = false;
            var invalid_objects = false;

            if (domainContext == null)
            {
                return false;
            }

            if (_entityTypes == null)
            {
                _entityTypes = GetEntityTypes(domainContext.GetType());
            }

            var category = string.Format("ModelBase:{0}", tag);

            foreach (Type entityType in _entityTypes)
            {
                EntitySet entityList = domainContext.EntityContainer.GetEntitySet(entityType);
                foreach (Entity entity in entityList)
                {
                    var e = entity as VirtuosoEntity;
                    open_edits = (open_edits || e.IsEditting);
                    var isValid = e.Validate();
                    invalid_objects = (invalid_objects || ((e.IsNew || e.IsModified) && isValid == false));
                    if (log)
                    {
                        if (e.IsEditting || ((e.IsNew || e.IsModified) && isValid == false))
                        {
                            var msg = string.Format(
                                "OpenEdit= {0}, New= {1}, Modified= {2}, Valid= {3}\r\n Entity: {4}",
                                e.IsEditting,
                                e.IsNew,
                                e.IsModified,
                                isValid,
                                e.ToString());

                            logWriter.Write(
                                msg,
                                new[] { category }, //category
                                0, //priority
                                0, //eventid
                                TraceEventType.Warning);
#if DEBUG
                            System.Diagnostics.Debug.WriteLine(msg);
#endif
                        }
                    }
                }
            }

            return (open_edits || invalid_objects);
        }

        protected void CommitAllOpenEdits(DomainContext domainContext, bool log = false)
        {
            if (_entityTypes == null)
            {
                _entityTypes = GetEntityTypes(domainContext.GetType());
            }

            foreach (Type entityType in _entityTypes)
            {
                EntitySet entityList = domainContext.EntityContainer.GetEntitySet(entityType);
                foreach (Entity entity in entityList)
                {
                    var e = entity as VirtuosoEntity;
                    if (e != null)
                    {
                        if (e.IsEditting)
                        {
                            var obj = e as IEditableObject;
                            if (obj != null)
                            {
                                obj.EndEdit();

                                logWriter.Write(
                                    string.Format("Auto ended edit on Entity: {0}", e.ToString()),
                                    new[] { "ModelBase" }, //category
                                    0, //priority
                                    0, //eventid
                                    TraceEventType.Warning);
                            }
                        }
                    }
                }
            }
        }

        // Gets list of entity types exposed in the domain context
        private static IEnumerable<Type> GetEntityTypes(Type domainContextType)
        {
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            Type entityListType = typeof(EntitySet);

            IEnumerable<PropertyInfo> entityListProperties =
                domainContextType.GetProperties(bindingFlags)
                    .Where(p => entityListType.IsAssignableFrom(p.PropertyType));
            IEnumerable<Type> entityTypes =
                entityListProperties.Select(p => p.PropertyType.GetGenericArguments()[0]);

            return entityTypes;
        }
    }

    public static class ViewModelBaseEx
    {
        public static void RaisePropertyChanged<T, TProperty>(this T observableBase,
            Expression<Func<T, TProperty>> expression) where T : ModelBase
        {
            observableBase.RaisePropertyChanged(observableBase.GetPropertyName(expression));
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
}