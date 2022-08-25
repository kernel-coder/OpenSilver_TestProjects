#region Usings

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Events;
using Virtuoso.Metrics;

#endregion

namespace Virtuoso.Core.Framework
{
    public abstract class PagedModelBase : ModelBase, IPagedCollectionView
    {
        protected CorrelationIDHelper CorrelationIDHelper { get; set; }
        protected string Location { get; set; }

        protected PagedModelBase()
        {
            CorrelationIDHelper = new CorrelationIDHelper();
            SetLocationForMonitoring();
        }

        //TODO: Refactor the implementation of SetLocationForMonitoring (since it has formating logic that would be applicable to general event logging outside of PagedModelBase - to a new static class)
        public void SetLocationForMonitoring(string locationOverride = "")
        {
            if (string.IsNullOrWhiteSpace(locationOverride))
            {
                try
                {
                    //NOTE: this code doesn't work for ChildWindows - it will lookup the URI in the Shell's Frame Control
                    var serviceLocator = Client.Core.VirtuosoContainer.Current;
                    var navSvc = serviceLocator.GetInstance<Navigation.INavigationService>();
                    var currentURI = navSvc.CurrentSource.ToString();

                    Location = currentURI;
                }
                catch (Exception e)
                {
                    //NOTE: navSvc.CurrentSource.ToString(); will throw System.UnauthorizedAccessException: Invalid cross-thread access if accessed in a background thread
                    Location = "";
                    Debug.WriteLine(e.Message);
                }
            }
            else
            {
                Location =
                    locationOverride; //this.Location = "[" + locationOverride + "]";  //when an override is passed - it MAY already be surrounded, ditch for now
            }
        }

        #region IPagedCollectionView Members

        public bool CanChangePage => true;

        bool isPageChanging;

        public bool IsPageChanging
        {
            get { return isPageChanging; }
            private set
            {
                if (isPageChanging != value)
                {
                    isPageChanging = value;
                    this.RaisePropertyChanged(p => p.IsPageChanging);
                }
            }
        }

        int itemCount;

        public int ItemCount
        {
            get { return itemCount; }
            set
            {
                if (itemCount != value)
                {
                    itemCount = value;
                    this.RaisePropertyChanged(p => p.ItemCount);
                }
            }
        }

        public bool MoveToFirstPage()
        {
            return MoveToPage(0);
        }

        public bool MoveToLastPage()
        {
            //if ((TotalItemCount % PageSize) == 0)
            //    return MoveToPage((TotalItemCount / PageSize)-1);
            //else
            return MoveToPage((TotalItemCount / PageSize));
        }

        public bool MoveToNextPage()
        {
            //if ((TotalItemCount % PageSize) == 0)
            //    return MoveToPage(PageIndex + 1);
            //else
            return MoveToPage(PageIndex + 1);
        }

        public bool MoveToPage(int index)
        {
            if (((TotalItemCount % PageSize) == 0) && ((TotalItemCount / PageSize) == index))
            {
                index--;
            }

            if (index == PageIndex || index < 0 || index > TotalItemCount / PageSize)
            {
                return false;
            }

            PageChangingEventArgs args = new PageChangingEventArgs(index);

            try
            {
                IsPageChanging = true;
                PageChanging(this, args);

                if (!args.Cancel)
                {
                    pageIndex = index;
                    LoadData();

                    this.RaisePropertyChanged(p => p.PageIndex);
                    PageChanged(this, EventArgs.Empty);

                    return true;
                }

                return false;
            }
            finally
            {
                IsPageChanging = false;
            }
        }

        public bool MoveToPreviousPage()
        {
            return MoveToPage(PageIndex - 1);
        }

        public event EventHandler<EventArgs> PageChanged = delegate { };
        public event EventHandler<PageChangingEventArgs> PageChanging = delegate { };

        int pageIndex;

        public int PageIndex
        {
            get { return pageIndex; }
            set
            {
                if (pageIndex < 0 || pageIndex > totalItemCount / PageSize)
                {
                    throw new ArgumentOutOfRangeException(
                        "PageIndex must be greater than or equal to 0 and less than the page count");
                }

                MoveToPage(value);
            }
        }

        int pageSize = 10; // default page size to 10

        public int PageSize
        {
            get { return pageSize; }
            set
            {
                if (pageSize != value)
                {
                    pageSize = value;
                    this.RaisePropertyChanged(p => p.PageSize);
                }
            }
        }

        int totalItemCount;

        public int TotalItemCount
        {
            get { return totalItemCount; }
            set
            {
                if (totalItemCount != value)
                {
                    totalItemCount = value;
                    this.RaisePropertyChanged(p => p.TotalItemCount);
                }
            }
        }

        #endregion

        protected void HandleEntityResults<T>(LoadOperation<T> results, EventHandler<EntityEventArgs<T>> handler)
            where T : Entity
        {
            HandleEntityResults(results, handler, true);
        }

        protected void HandleEntityResults<T>(LoadOperation<T> results, EventHandler<EntityEventArgs<T>> handler,
            bool isPaging) where T : Entity
        {
            var perfdata = results.UserState as MetricsTimer;
            if (perfdata != null)
            {
                perfdata.GetMetricData(CorrelationIDHelper.Sequence, EntityManager.Current.IsOnline, Location);
            }

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
                        if (isPaging)
                        {
                            ItemCount = results.TotalEntityCount;
                            TotalItemCount = results.TotalEntityCount;
                        }

                        handler(this, new EntityEventArgs<T>(results.Entities, results.TotalEntityCount));

                        IsLoading = false;
                    });
                }
            }
        }

        protected void HandleErrorResults(SubmitOperation results, EventHandler<ErrorEventArgs> handler)
        {
            if (handler != null)
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
                    handler(this, new ErrorEventArgs(results.Error, _ErrorResults, _ValidationErrorResults));

                    IsLoading = false;
                });
            }
        }

        protected void HandleSubmitOperationResults(SubmitOperation results, EventHandler<ErrorEventArgs> handler)
        {
            if (handler != null)
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
                    handler(this, new SubmitOperationEventArgs(results, _ErrorResults, _ValidationErrorResults));
                    IsLoading = false;
                });
            }
        }
    }
}