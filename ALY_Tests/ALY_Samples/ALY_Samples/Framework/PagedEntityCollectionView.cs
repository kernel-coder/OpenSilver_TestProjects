#region Usings

using System;
using System.ComponentModel;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Framework
{
    public class PagedEntityCollectionView<T> : EntityCollectionView<T>, IPagedCollectionView, INotifyPropertyChanged,
        ICleanup
        where T : Entity, IEditableObject, new()
    {
        private IPagedCollectionView _pagingView;

        public PagedEntityCollectionView(EntitySet<T> entityList, IPagedCollectionView delegatePagingView)
            : base(entityList)
        {
            _pagingView = delegatePagingView;

            _pagingView.PageChanging += PageChanging;
            _pagingView.PageChanged += PageChanged;

            INotifyPropertyChanged propertyChanged = _pagingView as INotifyPropertyChanged;

            if (propertyChanged != null)
            {
                propertyChanged.PropertyChanged += OnPropertyChangedOnPropertyChanged;
            }
        }

        #region IPagedCollectionView Members

        public bool CanChangePage => _pagingView.CanChangePage;

        public bool IsPageChanging => _pagingView.IsPageChanging;

        public int ItemCount => _pagingView.ItemCount;

        public bool MoveToFirstPage()
        {
            return _pagingView.MoveToFirstPage();
        }

        public bool MoveToLastPage()
        {
            return _pagingView.MoveToLastPage();
        }

        public bool MoveToNextPage()
        {
            return _pagingView.MoveToNextPage();
        }

        public bool MoveToPage(int pageIndex)
        {
            return _pagingView.MoveToPage(pageIndex);
        }

        public bool MoveToPreviousPage()
        {
            return _pagingView.MoveToPreviousPage();
        }

        public event EventHandler<EventArgs> PageChanged = delegate { };
        public event EventHandler<PageChangingEventArgs> PageChanging = delegate { };

        public int PageIndex => _pagingView.PageIndex;

        public int PageSize
        {
            get { return _pagingView.PageSize; }
            set { _pagingView.PageSize = value; }
        }

        public int TotalItemCount => _pagingView.TotalItemCount;

        #endregion

        public new void Cleanup()
        {
            base.Cleanup();

            if (_pagingView != null)
            {
                _pagingView.PageChanging -= PageChanging;
                _pagingView.PageChanged -= PageChanged;

                INotifyPropertyChanged notifyPropertyChanged = _pagingView as INotifyPropertyChanged;
                if (notifyPropertyChanged != null)
                {
                    notifyPropertyChanged.PropertyChanged -= OnPropertyChangedOnPropertyChanged;
                }
            }

            _pagingView = null;
        }

        private void OnPropertyChangedOnPropertyChanged(object s, PropertyChangedEventArgs e)
        {
            RaisePropertyChanged(e);
        }
    }
}