#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Interface;
using Virtuoso.Core.Occasional;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Services
{
    [PartCreationPolicy(CreationPolicy.Shared)]
    [Export]
    public class AlertManagerService : IAlertManagerMemento
    {
        VirtuosoDomainContext AlertsContext;
        VirtuosoDomainContext TaskContext;

        public AlertManagerService()
        {
            AlertsContext = new VirtuosoDomainContext();
            TaskContext = new VirtuosoDomainContext();

            var contextServiceProvider = new SimpleServiceProvider();
            contextServiceProvider.AddService<ICodeLookupDataProvider>(new CodeLookupDataProvider());
            contextServiceProvider.AddService<IWoundDataProvider>(new WoundDataProvider());
            contextServiceProvider.AddService<IPatientContactDataProvider>(new PatientContactDataProvider());
        }

        #region Events

        public event EventHandler<EntityEventArgs<UserAlertsJoin>> LoadAlertNotificationsComplete;
        public event EventHandler<ErrorEventArgs> OnTaskContextSaved;
        public event EventHandler<ErrorEventArgs> OnAlertContextSaved;
        public event EventHandler<ErrorEventArgs> OnClientLoaded; //to know when all data loaded from disk

        #endregion

        #region AlertsContext

        public EntitySet<UserAlertsJoin> AlertsContext_UserAlertsJoins => AlertsContext.UserAlertsJoins;
        public EntitySet<Task> AlertsContext_Tasks => AlertsContext.Tasks;
        public EntitySet<Encounter> AlertsContext_Encounters => AlertsContext.Encounters;
        public EntitySet<UserAlertsJoinCount> AlertsContext_UserAlertsJoinCounts => AlertsContext.UserAlertsJoinCounts;
        public EntitySet<UserAlertStatus> AlertsContext_UserAlertStatus => AlertsContext.UserAlertStatus;

        public event EventHandler<EntityEventArgs<UserAlertsJoinCount>> OnUserAlertsJoinCountLoaded;

        public void GetUserAlertsJoinCountAsync()
        {
            AlertsContext.UserAlertsJoinCounts.Clear();

            var query = AlertsContext.GetUserAlertsJoinCountQuery();

            query.IncludeTotalCount = true;

            AlertsContext.Load(
                query,
                LoadBehavior.RefreshCurrent,
                UserAlertsJoinCountLoaded,
                null);
        }

        private void UserAlertsJoinCountLoaded(LoadOperation<UserAlertsJoinCount> results)
        {
            HandleEntityResults(results, OnUserAlertsJoinCountLoaded);
        }

        public void GetExceptionsAndAlertsForUserAsync(Guid UserId, int ExceptAlertKey)
        {
            var query = AlertsContext.GetAlertsForUserQuery(UserId, ExceptAlertKey);
            AlertsContext.Load(
                query,
                LoadBehavior.RefreshCurrent,
                g => HandleEntityResults(g, LoadAlertNotificationsComplete), //LoadUserNotificationsComplete, 
                null);
        }

        public void AlertsContext_SubmitChanges(int UserAlertStatusKey)
        {
            if (EntityManager.Current.IsOnline)
            {
                AlertsContext.SubmitChanges(g => HandleErrorResults(g, OnAlertContextSaved), UserAlertStatusKey);
            }
            else
            {
                MessageBox.Show("Cannot save task.  No network connectivity");
            }
        }

        #endregion

        #region TaskContext

        public EntitySet<Task> TaskContext_Tasks => TaskContext.Tasks;

        public void TaskContext_SubmitChanges()
        {
            if (EntityManager.Current.IsOnline)
            {
                TaskContext.SubmitChanges(g => HandleErrorResults(g, OnTaskContextSaved), null);
            }
            else
            {
                MessageBox.Show("Cannot save task.  No network connectivity");
            }
        }

        #endregion

        #region IAlertManagerMemento

        AlertManagerMemento IAlertManagerMemento.GetMemento()
        {
            var memento = new AlertManagerMemento();

            var AlertsContextOfflineList = AlertsContext.Export();
            memento.DomainContextJSONDictionary[PatientHomeMementoEnum.Alert] =
                AlertsContext.Serialize(AlertsContextOfflineList);

            return memento;
        }

        void IAlertManagerMemento.SetMemento(AlertManagerMemento memento)
        {
            var AlertsContextOfflineList = memento.DomainContextJSONDictionary[PatientHomeMementoEnum.Alert];
            if (AlertsContextOfflineList != null)
            {
                var data = AlertsContext.Deserialize(AlertsContextOfflineList);
                AlertsContext.Import(data);
            }
        }

        #endregion

        public void LoadFromMementoAsync(AlertManagerMemento memento)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                try
                {
                    ((IAlertManagerMemento)(this)).SetMemento(memento);

                    RaiseEvents();
                }
                catch (Exception e)
                {
                    if (OnClientLoaded != null)
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(() =>
                        {
                            OnClientLoaded(this, new ErrorEventArgs(e, DataLoadType.LOCAL));
                        });
                    }
                }
            });
        }

        void RaiseEvents()
        {
            if (LoadAlertNotificationsComplete != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    LoadAlertNotificationsComplete(this,
                        new EntityEventArgs<UserAlertsJoin>(new List<UserAlertsJoin>(),
                            loadType: DataLoadType.LOCAL));
                });
            }

            if (OnClientLoaded != null)
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    OnClientLoaded(this, new ErrorEventArgs(null, DataLoadType.LOCAL));
                });
            }
        }

        #region Error Handling

        protected void HandleEntityResults<T>(LoadOperation<T> results, EventHandler<EntityEventArgs<T>> handler)
            where T : Entity
        {
            if (results.HasError)
            {
                results.MarkErrorAsHandled(); //doing this so that an exception is not raised to Application_UnhandledException()

                if (handler != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        handler(this, new EntityEventArgs<T>(results.Error));
                    });
                }
            }
            else
            {
                if (handler != null)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        handler(this, new EntityEventArgs<T>(results.Entities));
                    });
                }
            }
        }

        protected void HandleErrorResults(SubmitOperation results, EventHandler<ErrorEventArgs> handler)
        {
            if (handler != null)
            {
                if (results.HasError) //NOTE: can get a RIA error without entity errors - in this case - do not throw Application_UnhandledException
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

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    handler(this,
                        new ErrorEventArgs(results.Error, _ErrorResults, _ValidationErrorResults)
                        { UserState = results.UserState });
                });
            }
        }

        #endregion
    }
}