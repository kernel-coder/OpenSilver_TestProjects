#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Framework
{
    /// <summary>
    /// A wrapper class that exposes an <see cref="EntityList{T}"/> as an implementation
    /// of <see cref="IEnumerable{T}"/>, <see cref="ICollectionView{T}"/>, <see cref="IEditableCollectionView"/>,
    /// <see cref="INotifyCollectionChanged"/>, and <see cref="INotifyPropertyChanged"/>.
    /// </summary>
    /// <typeparam name="T">
    /// The type of entity included in the collection.
    /// <para>
    /// If <typeparamref name="T"/> is unknown at design time, use the <see cref="EntityCollectionView.From"/> factory method to create
    /// an appropriate instance of <see cref="EntityCollectionView{T}"/>.
    /// </para>
    /// </typeparam>
    public class EntityCollectionView<T> : IEnumerable<T>,
        ICollectionView, IEditableCollectionView,
        INotifyCollectionChanged, INotifyPropertyChanged
        where T : Entity, IEditableObject, new()
    {
        #region PropertyChangedEventArgs

        private static PropertyChangedEventArgs CurrentItemChangedEventArgs =
            new PropertyChangedEventArgs("CurrentItem");

        private static PropertyChangedEventArgs CurrentPositionChangedEventArgs =
            new PropertyChangedEventArgs("CurrentPosition");

        private static PropertyChangedEventArgs IsCurrentBeforeFirstChangedEventArgs =
            new PropertyChangedEventArgs("IsCurrentBeforeFirst");

        #endregion

        #region Private Fields and Properties

        private EntitySet<T> _entityList;
        private T _currentAddItem;
        private T _currentEditItem;
        private SortDescriptionCollection _sortDescriptions;

        private IEnumerable<T> Enumerable => _entityList;

        #endregion

        #region Private Methods

        /// <summary>
        /// Subscribe to the <see cref="INotifyCollectionChanged"/> events on the source collection,
        /// relaying the events from ourself.
        /// </summary>
        /// <remarks>
        /// If we don't have a current item, we will attempt to set currency to the first item.
        /// </remarks>
        private void RelayCollectionChanged()
        {
            var collection = (INotifyCollectionChanged)_entityList;
            collection.CollectionChanged += OnCollectionChanged;
        }

        private void OnCollectionChanged(object s, NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged(this, e);

            if (IsCurrentBeforeFirst && e.Action == NotifyCollectionChangedAction.Add ||
                e.Action == NotifyCollectionChangedAction.Reset)
            {
                MoveCurrentToFirst();
                RaisePropertyChanged(IsCurrentBeforeFirstChangedEventArgs);
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Wrap around an <see cref="EntityList{T}"/>.
        /// </summary>
        /// <param name="entityList">The list to wrap.</param>
        public EntityCollectionView(EntitySet<T> entityList)
        {
            _entityList = entityList;
            RelayCollectionChanged();
        }

        #endregion

        protected void Cleanup()
        {
            var collection = (INotifyCollectionChanged)_entityList;
            collection.CollectionChanged -= OnCollectionChanged;
        }

        #region Public Properties (not from any interfaces)

        /// <summary>
        /// Get the count from the source collection.
        /// </summary>
        public int Count => _entityList.Count;

        #endregion

        #region Public Events (not from any interfaces)

        public event EventHandler Refreshed = delegate { };

        #endregion

        #region IEnumerable<T> Members

        public IEnumerator<T> GetEnumerator()
        {
            return _entityList.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        /// <summary>
        /// Explicitly implemented because we implicitly implement
        /// <see cref="IEnumerable<T>.GetEnumerator"/>.
        /// </summary>
        /// <returns>The <see cref="IEnumerator"/> from the source collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region ICollectionView Members

        public bool CanFilter => false;

        public bool CanGroup => false;

        public bool CanSort => true;

        public bool Contains(object item)
        {
            return Enumerable.Contains((T)item);
        }

        public System.Globalization.CultureInfo Culture
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        public event EventHandler CurrentChanged = delegate { };
        public event CurrentChangingEventHandler CurrentChanging = delegate { };

        public object CurrentItem { get; private set; }

        public int CurrentPosition { get; private set; }

        public IDisposable DeferRefresh()
        {
            return new DeferRefreshHelper(() => Refresh());
        }

        public Predicate<object> Filter
        {
            get { throw new NotSupportedException("EntityCollectionView doesn't support Filter"); }
            set { throw new NotSupportedException("EntityCollectionView doesn't support Filter"); }
        }

        public ObservableCollection<GroupDescription> GroupDescriptions => null;

        public ReadOnlyObservableCollection<object> Groups => null;

        public bool IsCurrentAfterLast => false;

        public bool IsCurrentBeforeFirst => CurrentItem == null;

        public bool IsEmpty => _entityList.Any() == false;

        public bool MoveCurrentTo(object item)
        {
            if (IsEmpty || Equals(CurrentItem, item))
            {
                return false;
            }

            for (int i = 0; i < Count; ++i)
                if (Enumerable.ElementAt(i).Equals(item))
                {
                    CurrentChangingEventArgs args = new CurrentChangingEventArgs();
                    CurrentChanging(this, args);

                    if (!args.Cancel)
                    {
                        CurrentItem = item;
                        CurrentPosition = i;

                        RaisePropertyChanged(CurrentItemChangedEventArgs);
                        RaisePropertyChanged(CurrentPositionChangedEventArgs);
                        CurrentChanged(this, EventArgs.Empty);

                        return true;
                    }

                    return false;
                }

            return false;
        }

        public bool MoveCurrentToFirst()
        {
            return MoveCurrentToPosition(0);
        }

        public bool MoveCurrentToLast()
        {
            return MoveCurrentToPosition(Count - 1);
        }

        public bool MoveCurrentToNext()
        {
            if (CurrentPosition < Count - 1)
            {
                return MoveCurrentToPosition(CurrentPosition + 1);
            }

            return false;
        }

        public bool MoveCurrentToPosition(int position)
        {
            if (IsEmpty)
            {
                return false;
            }

            CurrentChangingEventArgs e = new CurrentChangingEventArgs(true);
            CurrentChanging(this, e);

            if (e.Cancel)
            {
                return false;
            }

            if (position == -1)
            {
                CurrentItem = null;
            }
            else
            {
                CurrentItem = Enumerable.ElementAt(position);
            }

            CurrentPosition = position;

            RaisePropertyChanged(CurrentItemChangedEventArgs);
            RaisePropertyChanged(CurrentPositionChangedEventArgs);
            CurrentChanged(this, EventArgs.Empty);

            return true;
        }

        public bool MoveCurrentToPrevious()
        {
            if (CurrentPosition > 0)
            {
                return MoveCurrentToPosition(CurrentPosition - 1);
            }

            return false;
        }

        public void Refresh()
        {
            Refreshed(this, EventArgs.Empty);
        }

        public SortDescriptionCollection SortDescriptions
        {
            get
            {
                if (_sortDescriptions == null)
                {
                    _sortDescriptions = new SortDescriptionCollection();
                }

                return _sortDescriptions;
            }
        }

        public IEnumerable SourceCollection => _entityList;

        #endregion

        #region IEditableCollectionView Members

        public object AddNew()
        {
            CommitNew();
            CommitEdit();

            _currentAddItem = new T();
            _entityList.Add(_currentAddItem);

            _currentAddItem.BeginEdit();
            MoveCurrentToLast();

            return _currentAddItem;
        }

        public bool CanAddNew => _entityList.CanAdd;

        public bool CanCancelEdit => true;

        public bool CanRemove => _entityList.CanRemove;

        public void CancelEdit()
        {
            if (IsEditingItem)
            {
                _currentEditItem.CancelEdit();
            }
        }

        public void CancelNew()
        {
            if (IsAddingNew)
            {
                _currentAddItem.CancelEdit();
                MoveCurrentToLast();

                Remove(_currentAddItem);
                _currentAddItem = null;
            }
        }

        public void CommitEdit()
        {
            if (IsEditingItem)
            {
                _currentEditItem.EndEdit();
                _currentEditItem = null;
            }
        }

        public void CommitNew()
        {
            if (IsAddingNew)
            {
                _currentAddItem.EndEdit();
                _currentAddItem = null;
            }
        }

        public object CurrentAddItem => _currentAddItem;

        public object CurrentEditItem => _currentEditItem;

        public void EditItem(object item)
        {
            CommitNew();
            CommitEdit();

            _currentEditItem = item as T;
            MoveCurrentTo(item);

            if (_currentEditItem != null)
            {
                _currentEditItem.BeginEdit();
            }
        }

        public bool IsAddingNew => _currentAddItem != null;

        public bool IsEditingItem => _currentEditItem != null;

        public NewItemPlaceholderPosition NewItemPlaceholderPosition
        {
            get { return NewItemPlaceholderPosition.None; }
            set { throw new NotSupportedException(); }
        }

        public void Remove(object item)
        {
            T entity = item as T;

            if (entity != null)
            {
                _entityList.Remove(entity);

                if (IsEmpty)
                {
                    CurrentItem = null;
                    CurrentPosition = -1;

                    CurrentChanging(this, new CurrentChangingEventArgs(false));
                    RaisePropertyChanged(CurrentItemChangedEventArgs);
                    RaisePropertyChanged(CurrentPositionChangedEventArgs);
                    RaisePropertyChanged(IsCurrentBeforeFirstChangedEventArgs);
                    CurrentChanged(this, EventArgs.Empty);
                }
                else
                {
                    MoveCurrentTo(CurrentPosition);
                }
            }
        }

        public void RemoveAt(int index)
        {
            if (index < 0 || index >= _entityList.Count)
            {
                throw new IndexOutOfRangeException("index must be at least 0 and less than the Count");
            }

            Remove(_entityList.ElementAt(index));
        }

        #endregion

        #region INotifyCollectionChanged Members

        public event NotifyCollectionChangedEventHandler CollectionChanged = delegate { };

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChanged(this, args);
        }

        #endregion

        private class DeferRefreshHelper : IDisposable
        {
            private Action _callback;

            public DeferRefreshHelper(Action callback)
            {
                _callback = callback;
            }

            public void Dispose()
            {
                _callback();
            }
        }
    }
}