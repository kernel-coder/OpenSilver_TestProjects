using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using OpenRiaServices.DomainServices.Client;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data; //primarily for EntityExtensions
using Virtuoso.Client.Offline;
using Virtuoso.Core.Framework;
using Virtuoso.Core.Helpers;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility; //for ObservableCollection.ForEach
using Virtuoso.Core.ViewModel;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public enum UserControlBaseCommandType
    {
        OK,
        Cancel
    }
    public interface IParentViewModel
    {
        Admission CurrentAdmission { get; set; }
        Object PopupDataContext { get; set; }
    }
    public interface IParentLBoxViewModel
    {
        RelayCommand AddNewRowCommand
        {
            get;
            set;
        }
        RelayCommand<object> RemoveRowCommand
        {
            get;
            set;
        }
    }
    public interface ISelectedItem<T>
    {
        T SelectedItem { get; set; }
        Boolean IsEdit { get; set; }
        void BeginEditting();
        void RaiseItemSelected(EventArgs args);
        void RaiseCanExecuteChanged();
        void SelectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e);
    }

    public interface IFilterInterfaceCore<E> where E : global::OpenRiaServices.DomainServices.Client.Entity
    {
        //void AutoEdit();
        void ProcessFilteredItems();
        //void RaiseEncounterChangedEvent();
        //void RaiseParentViewModelChangedEvent();
        void RaisePropertyChanged(string propertyName);
        EntityCollection<E> ItemsSource { get; set; }
        //Encounter Encounter { get; set; }
        //LJN 520
        //AdmissionDocumentation AdmissionDocumentation { get; set; }
        //void UserControlBase_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e);
    }

    public interface IFilterInterface<E> where E : global::OpenRiaServices.DomainServices.Client.Entity
    {
        void AutoEdit();
        void ProcessFilteredItems();
        void RaiseEncounterChangedEvent();
        void RaiseParentViewModelChangedEvent();
        void RaisePropertyChanged(string propertyName);
        EntityCollection<E> ItemsSource { get; set; }
        Encounter Encounter { get; set; }
        //LJN 520
        AdmissionDocumentation AdmissionDocumentation { get; set; }
        void UserControlBase_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e);
    }

    public interface IListInterface<E> where E : global::OpenRiaServices.DomainServices.Client.Entity
    {
        //ObservableCollection<T> ItemsCollection { get; set; }
        void ItemsSource_PropertyChanged(object sender, PropertyChangedEventArgs e);
        NotifyCollectionChangedEventHandler ListUserControlBase_CollectionChanged { get; set; }
        //EntityCollection<T> ItemsSource { get; set; }
        EntityCollection<E> ItemsSource { get; set; }
        CollectionViewSource FilteredItemsCollection { get; set; }
        void RaiseItemSelected(EventArgs args);
        void RaiseCanExecuteChanged();
    }

    //E = InsuranceAddress
    //C = InsuranceAddressUserControl
    public class DetailUserControlBase<C, E> : UserControl, ISelectedItem<E>, INotifyPropertyChanged, ICleanup
        where C : UserControl
        where E : VirtuosoEntity
    {
        //public ICollectionView internalItemSource { get; set; }
        //public EntityCollectionView<InsuranceAddress> internalItemSource { get; set; }

        bool _IsOnline;
        protected bool IsOnline
        {
            get
            {
                //DynamicForm
                //ParentViewModel="{Binding DynamicFormViewModel, Mode=TwoWay}" - INFO: not all maint. controls bind to this property...
                //Encounter="{Binding Encounter, Mode=TwoWay}"
                //Model="{Binding FormModel}" - INFO: defined on derived class...

                //trick it into thinking it is online, so that the control doesn't disable itself in anyway when used on DynamicForm
                if (ParentViewModel != null && ParentViewModel.GetType() == typeof(DynamicFormViewModel) || (this.GetType().ToString().EndsWith("PrintUserControlBase")))
                    return true;
                else
                    return _IsOnline;
            }
            set
            {
                _IsOnline = value;
            }
        }

        protected CommandManager CommandManager { get; set; }

        public DetailUserControlBase()
        {
            //TODO: think I need to initialize each dependency property so that they are unique across instances of user controls
            //      that use this base class...
            //      reference this url: http://msdn.microsoft.com/en-us/library/cc903961(VS.95).aspx
            //SetValue(InsuranceAddressesProperty, new EntityCollection<InsuranceAddress>();

            SetupCommands();

            CommandManager = new CommandManager(this);
            IsOnline = EntityManager.Current.IsOnlineCached; // EntityManager.Current.IsOnline;

            Messenger.Default.Register<bool>(this, Constants.Messaging.NetworkAvailability,
              (IsAvailable) =>
              {
                  this.IsOnline = IsAvailable;
                  this.CommandManager.RaiseCanExecuteChanged();
              });

            Messenger.Default.Register<Virtuoso.Core.ViewModel.ViewModelBase>(this, "ViewModelClosing",
            (item) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (this.DataContext == item) this.Cleanup();
                });
            });

            //this.Loaded += new RoutedEventHandler(DetailUserControlBase_Loaded);
            this.BindingValidationError += new EventHandler<ValidationErrorEventArgs>(ChildControlBase_BindingValidationError);
        }
        public virtual void Cleanup()
        {
            if (SelectedItem != null)
            {
                try
                {
                    SelectedItem.PropertyChanged -= SelectedItem_PropertyChanged;
                }
                catch { }
            }

            Messenger.Default.Unregister(this);

            this.CommandManager.CleanUp();

            this.ClearValue(SelectedItemProperty);
            this.ClearValue(ParentViewModelProperty);
            this.ClearValue(IsEditProperty);

            this.DataContext = null;
            this.ParentViewModel = null;
            //this.ClearValue(ParentViewModelProperty);
            //this.ClearValue(SelectedItemProperty);            
            //this.ClearValue(IsEditProperty);

            try
            {
                this.BindingValidationError -= ChildControlBase_BindingValidationError;
            }
            catch (Exception)
            {

            }

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // First disconnect the data context so no bindings pop.
            var controlsAll = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(this).ToList();
            var controls = controlsAll.Distinct();
            var uc = controls.OfType<UserControl>().ToList();
            foreach (var rc in uc)
            {
                rc.DataContext = null;
            }
            // Now call cleanup on them all.
            var icl = controls.OfType<ICleanup>().ToList();
            foreach (var rc in icl)
            {
                ((ICleanup)rc).Cleanup();
            }
            var pop = controls.Where(c => c.Name.StartsWith("PopupRoot") || c.Name == "_PopupPresenter").ToList();
            foreach (var rc in pop)
            {
                rc.DataContext = null;
                VirtuosoObjectCleanupHelper.CleanupAll(rc);
            }
            VirtuosoObjectCleanupHelper.CleanupAll(this);
        }
        //void DetailUserControlBase_Loaded(object sender, RoutedEventArgs e)
        //{
        //    //doesn't always work, sometimes SelectedItem is null - async...
        //    if (this.SelectedItem.IsNew)
        //        SetFocusHelper.SelectFirstEditableWidget(this);
        //}

        void ChildControlBase_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            ValidationSummary valSum = ValidationSummaryHelper.GetValidationSummary(this);
            if (valSum != null)
            {
                ValidationSummaryItem valsumremove = valSum.Errors.Where(v => v.Message.Equals(e.Error.ErrorContent.ToString())).FirstOrDefault();

                if (e.Action == ValidationErrorEventAction.Removed)
                {
                    valSum.Errors.Remove(valsumremove);
                }
                else if (e.Action == ValidationErrorEventAction.Added)
                {
                    if (valsumremove == null)
                    {
                        ValidationSummaryItem vsi = new ValidationSummaryItem() { Message = e.Error.ErrorContent.ToString(), Context = e.OriginalSource };
                        vsi.Sources.Add(new ValidationSummaryItemSource(String.Empty, e.OriginalSource as Control));

                        valSum.Errors.Add(vsi);

                        e.Handled = true;
                    }
                }
            }

            this.RaiseCanExecuteChanged();
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(E), typeof(C),
        new PropertyMetadata(SelectedItemPropertyChanged));//Callback invoked on property value has changes

        private static void SelectedItemPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            //args.NewValue = Insurance object
            //sender = InsuranceDetailUserControl
            var i = args.NewValue as E;
            if (i != null)
            {
                //If the object we're bound to is a 'new' object - set IsEdit to true.
                //When we cancel - we'll raise the Cancel event - caller will remove the added entity.
                //if (i.InsuranceKey <= 0)
                //if (i.EntityState == System.ServiceModel.DomainServices.Client.EntityState.New)
                //    ((InsuranceDetailUserControl)(sender)).SetValue(IsEditProperty, true);
                var s = sender as ISelectedItem<E>;
                if (s != null)
                {
                    try
                    {
                        if (i.IsNew)
                            s.IsEdit = true;

                        s.RaiseItemSelected(EventArgs.Empty);
                        i.PropertyChanged += new PropertyChangedEventHandler(s.SelectedItem_PropertyChanged);
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(String.Format("DEBUG: {0}", e.Message));
                        throw;
                    }
                }
            }
            else
            {
                //Don't have a new value - SelectedItem may now be NULL, so need to re-evaluate the buttons...
                var s = sender as ISelectedItem<E>;
                if (s != null)
                {
                    s.RaiseItemSelected(EventArgs.Empty);  //this will call RaiseCanExecuteChanged()
                }
            }

        }
        public IParentLBoxViewModel ParentViewModel
        {
            get { return (IParentLBoxViewModel)GetValue(ParentViewModelProperty); }
            set { SetValue(ParentViewModelProperty, value); }
        }

        public static readonly DependencyProperty ParentViewModelProperty =
            DependencyProperty.Register("ParentViewModel", typeof(IParentLBoxViewModel), typeof(C),
            new PropertyMetadata(ParentViewModelPropertyChanged)); //Callback invoked on property value has changes

        private static void ParentViewModelPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var s = sender as ISelectedItem<E>;
                if (s != null)
                {
                    try
                    {
                        s.RaiseCanExecuteChanged();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(String.Format("DEBUG: {0}", e.Message));
                        throw;
                    }
                }
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }
        public void SelectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaiseCanExecuteChanged();
        }

        public static readonly DependencyProperty IsEditProperty =
            DependencyProperty.Register("IsEdit", typeof(Boolean), typeof(C), new PropertyMetadata(IsEditPropertyChanged));

        private static void IsEditPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var val = args.NewValue as Nullable<bool>;
            if ((val.HasValue) && (val.Value == true))
            {
                var s = sender as ISelectedItem<E>;
                if ((s != null) && (s.SelectedItem != null))
                {
                    s.SelectedItem.BeginEditting();
                    s.BeginEditting();  //Allow derived VM to begin edit on anything it has - which is in addition to SelectedItem (E.G. patient photo)
                    s.RaiseCanExecuteChanged();
                }
            }
        }

        //INFO on custom events - http://msdn.microsoft.com/en-us/library/dd833067(VS.95).aspx
        //public event EventHandler AddPressed;
        public void RaiseEncounterChangedEvent()
        {
            if (EncounterChanged != null) EncounterChanged(this, EventArgs.Empty);
        }
        public event EventHandler EncounterChanged;
        public event EventHandler ItemSelected;
        public event EventHandler<UserControlBaseEventArgs<E>> EditPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> OKPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> OKPressedInValid;
        public event EventHandler<UserControlBaseEventArgs<E>> OKPressedValidate;
        public event EventHandler<UserControlBaseEventArgs<E>> CancelPressed;
        public void RaiseItemSelected(EventArgs args)
        {
            RaiseCanExecuteChanged();

            if (ItemSelected != null)
                ItemSelected(this, EventArgs.Empty);

            //Alert that item was 'changed' - client will need to check that it is not null
            //Doing this so that client can RaiseCanExecuteChanged on any of its own RelayCommands
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.RaiseCanExecuteChanged();
        }

        private bool _OKVisible = false;
        public bool OKVisible
        {
            get { return _OKVisible; }
            set
            {
                _OKVisible = value;
                RaisePropertyChanged("OKVisible");
            }
        }

        public virtual System.Threading.Tasks.Task<bool> CancelAsync()
        {
            var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<bool>();
            taskCompletionSource.TrySetResult(true);
            return taskCompletionSource.Task;
        }
        public virtual System.Threading.Tasks.Task<bool> ValidateAsync()
        {
            var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<bool>();
            taskCompletionSource.TrySetResult(true);
            return taskCompletionSource.Task;
        }
        public virtual bool Validate() { return true; }  //Patient Photo
        public virtual void EndEditting() { return; }  //Patient Photo
        public virtual void CancelEditting() { return; }  //Patient Photo

        //Inheriting class overrides this method to begin edit for entities in addition to SelectedItem - E.G. (Patient Photo)
        public virtual void BeginEditting() { return; }

        //Inheriting class overrides this method to return HasChanges for entities in addition to SelectedItem
        public virtual bool HasChanges() { return false; }

        //Inheriting class overrides this method to do anyting special when one of it's 'children' change.
        public virtual void OnChildRowChanged() { }

        public virtual void RemoveFromModel() { }  //Patient Photo

        //Inheriting class overrides this method to remove the child/related entity from the Model(DomainContext)
        public virtual void RemoveFromModel(E entity) { }

        //Inheriting class overrides this method to save the child/related entity using the Model(DomainContext)
        public virtual void SaveModel(UserControlBaseCommandType command) { }

        public E SelectedItem
        {
            get
            {
                return (E)GetValue(SelectedItemProperty);
            }
            set
            {
                //Auto set newly added items into edit mode...
                //if (value.InsuranceKey <= 0)
                //if ((value != null) &&
                //    (value.EntityState == System.ServiceModel.DomainServices.Client.EntityState.New))
                //    IsEdit = true;
                if (SelectedItem != null)
                {
                    try
                    {
                        SelectedItem.PropertyChanged -= SelectedItem_PropertyChanged;
                    }
                    catch { }
                }

                SetValue(SelectedItemProperty, value);
            }
        }

        public Boolean IsEdit
        {
            get
            {
                return (Boolean)GetValue(IsEditProperty);
                //var ret1 = (Boolean)GetValue(IsEditProperty);
                //var ret2 = (SelectedItem != null);
                //return ret1 && ret2;
            }
            set
            {
                SetValue(IsEditProperty, value);
                RaiseCanExecuteChanged();
            }
        }

        #region Commands
        //public RelayCommand Add_Command
        //{
        //    get;
        //    protected set;
        //}
        public RelayCommand Select_Command
        {
            get;
            protected set;
        }
        public RelayCommand Edit_Command
        {
            get;
            protected set;
        }
        public RelayCommand OK_Command
        {
            get;
            protected set;
        }
        public RelayCommand Cancel_Command
        {
            get;
            protected set;
        }
        public RelayCommand ChildRowChanged_Command
        {
            get;
            protected set;
        }
        #endregion

        private void SetupCommands()
        {
            Edit_Command = new RelayCommand(() =>
            {
                IsEdit = true;
                if (EditPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                    EditPressed(this, e);
                    if (e.Cancel)
                    {
                        IsEdit = false;
                        return;
                    }
                }
                SetFocusHelper.SelectFirstEditableWidget(this);
            }, () => this.IsOnline);

            Select_Command = new RelayCommand(() =>
            {
                IsEdit = true;
                if (EditPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                    EditPressed(this, e);
                    if (e.Cancel)
                    {
                        IsEdit = false;
                        return;
                    }
                }
            }, () => this.IsOnline);

            OK_Command = new RelayCommand(() =>
            {
                var val1 = SelectedItem.Validate();
                var val2 = Validate(); //want to make sure this runs in addition to SelectedItem.Validate()

                if ((val1 && val2) == false) //validate on client
                {
                    if (OKPressedInValid != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                        OKPressedInValid(this, e);
                    }
                }
                else
                {
                    ValidateAsync()  //validate on server if validation passed on client
                        .ContinueWith(task =>
                        {
                            bool result = task.Result;
                            if (result)
                            {
                                //((IEditableObject)SelectedItem).EndEdit();
                                SelectedItem.EndEditting();
                                this.EndEditting();
                                IsEdit = false;
                                if ((SelectedItem.IsNew) || (SelectedItem.HasChanges) || this.HasChanges())
                                {
                                    if (OKPressedValidate != null)
                                    {
                                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                                        OKPressedValidate(this, e);
                                        if (e.Cancel) return;
                                    }
                                    if (OKPressed != null)
                                    {
                                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                                        OKPressed(this, e);
                                    }
                                }
                                SaveModel(UserControlBaseCommandType.OK); //allow the overriding class to save to the database
                            }
                            else
                            {
                                //if (task.Exception != null)
                                //{
                                //    if (task.Exception.Message != null) MessageBox.Show(task.Exception.Message, "HishRiskMedicationAgrsErrorMessage", MessageBoxButton.OK);
                                //    if (task.Exception.InnerException != null) MessageBox.Show(task.Exception.InnerException.Message, "HishRiskMedicationAgrsErrorInnerException", MessageBoxButton.OK);
                                //}
                                //async validation failed
                                IsEdit = true;            //keep in open edit to allow user to modify or cancel the edit action
                                RaiseCanExecuteChanged(); //ensure OK button visible
                            }
                        },
                        System.Threading.CancellationToken.None,
                        System.Threading.Tasks.TaskContinuationOptions.None,
                        Client.Utils.AsyncUtility.TaskScheduler);
                }
            }, () =>
            {
                if (this.IsOnline == false)
                    return false;
                OrderEntry oe = SelectedItem as OrderEntry;
                if (oe != null)
                    return OKVisible = IsEdit;
                return OKVisible = SelectedItem != null && IsEdit && (SelectedItem.HasChanges || SelectedItem.IsNew || this.HasChanges());
            });

            Cancel_Command = new RelayCommand(() =>
            {
                CancelAsync()  //a hook for derived classes to call back to the server on a cancel
                    .ContinueWith(task =>
                    {
                        
                            IsEdit = false;
                            //((IEditableObject)SelectedItem).CancelEdit();
                            SelectedItem.CancelEditting();
                            this.CancelEditting();

                            RemoveFromModel();  //allow overriding class to remove child entities of SelectedItem - e.g. Patient Photo

                            //If we're cancelling an ADD - then remove it from the Model(DomainContext)
                            if ((SelectedItem != null) && (SelectedItem.IsNew))
                            {
                                //allow the overriding class to handle removing the parent entity from the Model
                                //Removing from model - before calling CancelPressed - because CancelPressed is issuing
                                //Physicians.MoveCurrentToFirst(); when SelectedItem.EntityState == EntityState.New
                                RemoveFromModel(SelectedItem);
                                //NOTE: if the inheriting class overrides RemoveFromMode() - AND removes the NEWly added
                                //      entity from the Model, then after returning from RemoveFromModel - SelectedItem will
                                //      equal NULL.  The dependency code for the SelectedItem changed will also execute.
                            }

                            if (CancelPressed != null)
                            {
                                //NOTE if SelectedItem.EntityState == EntityState.New, then SelectedItem will be NULL here
                                var e = new UserControlBaseEventArgs<E>(SelectedItem);
                                CancelPressed(this, e);
                            }

                            //allow the overriding class to save to the database
                            //This must be done for CANCEL also, because there could be pending saves.
                            SaveModel(UserControlBaseCommandType.Cancel);
                    },
                        System.Threading.CancellationToken.None,
                        System.Threading.Tasks.TaskContinuationOptions.None,
                        Client.Utils.AsyncUtility.TaskScheduler);
            }, () => this.IsOnline);

            ChildRowChanged_Command = new RelayCommand(() =>
            {
                OnChildRowChanged();
                RaiseCanExecuteChanged();
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //http://amazedsaint.blogspot.com/2009/12/silverlight-listening-to-dependency.html
        /// Listen for change of the dependency property
        public void RegisterForNotification(string propertyName, FrameworkElement element, PropertyChangedCallback callback)
        {
            //Bind to a dependency property
            Binding b = new Binding(propertyName) { Source = element };
            var prop = System.Windows.DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                typeof(UserControl),
                new System.Windows.PropertyMetadata(callback));

            element.SetBinding(prop, b);
        }
    }

    public class ChildControlBase<C, E> : UserControl, ISelectedItem<E>, IFilterInterface<E>, INotifyPropertyChanged, ICleanup
        where C : UserControl
        where E : VirtuosoEntity
    {
        static LogWriter logWriter;
        static ChildControlBase()
        {
            logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
        }
        bool _IsOnline;
        protected bool IsOnline
        {
            get
            {
                //DynamicForm
                //ParentViewModel="{Binding DynamicFormViewModel, Mode=TwoWay}" - INFO: not all maint. controls bind to this property...
                //Encounter="{Binding Encounter, Mode=TwoWay}"
                //Model="{Binding FormModel}" - INFO: defined on derived class...

                //trick it into thinking it is online, so that the control doesn't disable itself in anyway when used on DynamicForm
                //if (ParentViewModel != null && ParentViewModel.GetType() == typeof(DynamicFormViewModel) || (this.GetType().ToString().EndsWith("PrintUserControlBase")))
                if (Encounter != null || (this.GetType().ToString().EndsWith("PrintUserControlBase")))
                    return true;
                else
                    return _IsOnline;
            }
            set
            {
                _IsOnline = value;
            }
        }
        protected CommandManager CommandManager { get; set; }

        public ChildControlBase()
        {
            Protected = false;
            _FilteredItemsSource = new CollectionViewSource();
            //LJN 520
            _AdmissionDocumentations = new CollectionViewSource();
            //
            AlwaysAllowEdit = false;
            SetupCommands();
            CommandManager = new CommandManager(this);
            IsOnline = EntityManager.Current.IsOnlineCached; // EntityManager.Current.IsOnline;

            Messenger.Default.Register<bool>(this, Constants.Messaging.NetworkAvailability,
              (IsAvailable) =>
              {
                  this.IsOnline = IsAvailable;
                  this.CommandManager.RaiseCanExecuteChanged();
              });

            Messenger.Default.Register<Virtuoso.Core.ViewModel.ViewModelBase>(this, "ViewModelClosing",
            (item) =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (this.DataContext == item) this.Cleanup();
                    // cleanup even if the UC is currently unloaded from visual tree 
                    // - as in Admission/PatientMaintenance when you move from tab to tab - loading/unloading Meds, Labs, Allergies...
                    // - DynamicForm cleans up diffenertly - it loops over the form questions whose backingfactory calls DataTemplateHelper.Cleanup() on the cached visual tree (question DataTemplate) 
                    else if (this.myInitialDataContext == item) this.Cleanup();
                });
            });

            this.DataContextChanged += ChildControlBase_DataContextChanged;

            this.BindingValidationError += new EventHandler<ValidationErrorEventArgs>(ChildControlBase_BindingValidationError);
            //this.DataContextChanged += new DependencyPropertyChangedEventHandler(ChildControlBase_DataContextChanged);
        }
        private object myInitialDataContext = null;
        private void ChildControlBase_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ChildControlBase<C, E> me = sender as ChildControlBase<C, E>;
            if (me == null) return;
            if (me.DataContext != null) myInitialDataContext = DataContext;
        }

        public virtual void Cleanup()
        {
            this.DataContextChanged -= ChildControlBase_DataContextChanged;

            Messenger.Default.Unregister(this);

            if (DataTemplateHelper != null) DataTemplateHelper.Cleanup();

            this.CommandManager.CleanUp();

            try { 
                this.BindingValidationError -= ChildControlBase_BindingValidationError;
            }
            catch (Exception)
            {
                // ignore exception
            }

            if (SelectedItem != null)
            {
                SelectedItem.PropertyChanged -= SelectedItem_PropertyChanged;
                SelectedItem = null;
            }

            if (ItemsSource != null)
            {
                try
                {
                    ((INotifyCollectionChanged)ItemsSource).CollectionChanged -= UserControlBase_CollectionChanged;
                }
                catch { }
            }

            if (this.Encounter != null)
            {
                this.Encounter.Cleanup();
                this.Encounter = null;
            }

            this.ClearValue(ParentViewModelProperty);  //Clear the DependencyProperty
            this.ClearValue(OrderEntryManagerProperty);
            this.ClearValue(AdmissionDocumentationProperty);
            this.ClearValue(ICDModeProperty);
            this.ClearValue(EncounterProperty);
            this.ClearValue(ItemsSourceProperty);
            this.ClearValue(AlwaysAllowEditProperty);
            this.ClearValue(PrintFilteredItemsSourceProperty);
            this.ClearValue(IsEditProperty);
            this.ClearValue(IgnoreToggleProperty);
            this.ClearValue(SortOrderProperty);
            this.ClearValue(ForceCancelProperty);
            this.ClearValue(ProtectedProperty);

            this.DataContext = null;
            this.ParentViewModel = null;
            _FilteredItemsSource.Source = null;

            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // First disconnect the data context so no bindings pop.
            var controlsAll = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(this).ToList();
            var controls = controlsAll.Distinct();
            var uc = controls.OfType<UserControl>().ToList();
            foreach (var rc in uc)
            {
                rc.DataContext = null;
            }
            // Now cleanup the control
            var icl = controls.OfType<ICleanup>().ToList();
            foreach (var rc in icl)
            {
                ((ICleanup)rc).Cleanup();
            }
            var pop = controls.Where(c => c.Name.StartsWith("PopupRoot") || c.Name == "_PopupPresenter").ToList();
            foreach (var rc in pop)
            {
                rc.DataContext = null;
                VirtuosoObjectCleanupHelper.CleanupAll(rc);
            }
            VirtuosoObjectCleanupHelper.CleanupAll(this);
        }

        void ChildControlBase_BindingValidationError(object sender, ValidationErrorEventArgs e)
        {
            ValidationSummary valSum = ValidationSummaryHelper.GetValidationSummary(this);
            if (valSum != null)
            {
                ValidationSummaryItem valsumremove = valSum.Errors.Where(v => v.Message.Equals(e.Error.ErrorContent.ToString())).FirstOrDefault();

                if (e.Action == ValidationErrorEventAction.Removed)
                {
                    valSum.Errors.Remove(valsumremove);
                }
                else if (e.Action == ValidationErrorEventAction.Added)
                {
                    if (valsumremove == null)
                    {
                        ValidationSummaryItem vsi = new ValidationSummaryItem() { Message = e.Error.ErrorContent.ToString(), Context = e.OriginalSource };
                        vsi.Sources.Add(new ValidationSummaryItemSource(String.Empty, e.OriginalSource as Control));

                        valSum.Errors.Add(vsi);

                        e.Handled = true;
                    }
                }
            }

            this.RaiseCanExecuteChanged();
        }
        //void ChildControlBase_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //}
        public IParentViewModel ParentViewModel
        {
            get { return (IParentViewModel)GetValue(ParentViewModelProperty); }
            set { SetValue(ParentViewModelProperty, value); RaiseParentViewModelChangedEvent(); }
        }

        public static readonly DependencyProperty ParentViewModelProperty =
            DependencyProperty.Register("ParentViewModel", typeof(IParentViewModel), typeof(C),
            new PropertyMetadata(ParentViewModelPropertyChanged)); //Callback invoked on property value has changes

        private static void ParentViewModelPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var instance = sender as IFilterInterface<E>;
                if (instance != null) instance.RaiseParentViewModelChangedEvent(); 
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }
        public void RaiseParentViewModelChangedEvent()
        {
            if (ParentViewModelChanged != null)
            {
                var e = new UserControlBaseEventArgs<E>(null);
                ParentViewModelChanged(this, e);
            }
        }
        public OrderEntryManager OrderEntryManager
        {
            get { return (OrderEntryManager)GetValue(OrderEntryManagerProperty); }
            set { SetValue(OrderEntryManagerProperty, value); }
        }

        public static readonly DependencyProperty OrderEntryManagerProperty =
            DependencyProperty.Register("OrderEntryManager", typeof(OrderEntryManager), typeof(C), null);

        public static readonly DependencyProperty QuestionBackingFactoryProperty =
            DependencyProperty.Register("QuestionBackingFactory", typeof(object), typeof(C), null);

        //LJN 520
        public AdmissionDocumentation AdmissionDocumentation
        {
            get { return (AdmissionDocumentation)GetValue(AdmissionDocumentationProperty); }
            set { SetValue(AdmissionDocumentationProperty, value); }
        }

        public static readonly DependencyProperty AdmissionDocumentationProperty =
            DependencyProperty.Register("AdmissionDocumentation", typeof(AdmissionDocumentation), typeof(C),
            new PropertyMetadata(AdmissionDocumentationPropertyChanged)); //Callback invoked on property value has changes

        private static void AdmissionDocumentationPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var instance = sender as IFilterInterface<E>;

                instance.RaisePropertyChanged("IsEncounter");
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        public string ICDMode
        {
            get { return (string)GetValue(ICDModeProperty); }
            set { SetValue(ICDModeProperty, value); }
        }

        public static readonly DependencyProperty ICDModeProperty =
            DependencyProperty.Register("ICDMode", typeof(string), typeof(C),
            new PropertyMetadata(ICDModePropertyChanged)); //Callback invoked on property value has changes

        private static void ICDModePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
        }
        //LJN 520
        public Encounter Encounter
        {
            get { return (Encounter)GetValue(EncounterProperty); }
            set { SetValue(EncounterProperty, value); }
        }

        public static readonly DependencyProperty EncounterProperty =
            DependencyProperty.Register("Encounter", typeof(Encounter), typeof(C),
            new PropertyMetadata(EncounterPropertyChanged)); //Callback invoked on property value has changes
       
        private static void EncounterPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                IFilterInterface<E> instance = sender as IFilterInterface<E>;
                instance.ProcessFilteredItems();
                instance.RaisePropertyChanged("IsEncounter");
                instance.RaiseEncounterChangedEvent();
                var s = sender as ISelectedItem<E>;
                if (s != null)
                {
                    try
                    {
                        s.RaiseCanExecuteChanged();
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(String.Format("DEBUG: {0}", e.Message));
                        throw;
                    }
                }
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        public void AutoEdit()
        {
            if ((FilteredItemsSource != null) && (Encounter != null) && (Encounter.IsNew == false))
            {
                //logWriter.Write(
                //                string.Format("[Before AutoEdit] Encounter.IsEditting: {0}", Encounter.IsEditting),
                //                new[] { "ChildControlBase" },  //category
                //                0,  //priority
                //                0,  //eventid
                //                TraceEventType.Information);

                foreach (E item in FilteredItemsSource)
                {
                    item.BeginEditting();
                }

                //logWriter.Write(
                //                string.Format("[After AutoEdit] Encounter.IsEditting: {0}", Encounter.IsEditting),
                //                new[] { "ChildControlBase" },  //category
                //                0,  //priority
                //                0,  //eventid
                //                TraceEventType.Information);
            }
        }

        public virtual bool IsEncounter
        {
            get
            {
                if (Encounter == null) return false;
                // Encounter Mode - auto edit
                IsEdit = true;
                AutoEdit();

                return true;
            }
        }
        private string _PopupDataTemplate = null;
        public string PopupDataTemplate
        {
            get { return _PopupDataTemplate; }
            set
            {
                _PopupDataTemplate = value;
                this.RaisePropertyChanged("PopupDataTemplate");
                this.RaisePropertyChanged("PopupDataTemplateLoaded");
            }
        }
        private DataTemplateHelper DataTemplateHelper = null;
        public DependencyObject PopupDataTemplateLoaded
        {
            get
            {
                if (DataTemplateHelper == null) DataTemplateHelper = new DataTemplateHelper();
                //Causing infinate binding loop - if (DataTemplateHelper.IsDataTemplateLoaded(PopupDataTemplate))
                //{
                //    Deployment.Current.Dispatcher.BeginInvoke(() =>
                //    {
                //        this.RaisePropertyChanged(null);
                //    });
                //}
                return  DataTemplateHelper.LoadAndFocusDataTemplate(PopupDataTemplate);
            }
        }
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(EntityCollection<E>), typeof(C),
            new PropertyMetadata(ItemsSourcePropertyChanged)); //Callback invoked on property value has changes

        private static void ItemsSourcePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            //((ISelectedItem<E>)(sender)).SelectedItem = ((EntityCollection<E>)(((C)(sender))
            //    .GetValue(ItemSourceProperty))).FirstOrDefault();

            try
            {
                var itemsSource = (EntityCollection<E>)(((C)(sender)).GetValue(ItemsSourceProperty));
                var s = sender as ISelectedItem<E>;
                if (s != null)
                {
                    try
                    {
                        if ((itemsSource != null) && (s.SelectedItem != null))
                        {
                            s.SelectedItem = null;
                            s.RaiseItemSelected(EventArgs.Empty);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(String.Format("DEBUG: {0}", e.Message));
                        throw;
                    }
                }
                var instance = sender as IFilterInterface<E>;
                if (instance != null)
                {
                    instance.ProcessFilteredItems();

                    instance.RaisePropertyChanged("ItemsSource");
                    if (itemsSource != null)
                    {
                        //register changes to the collection - not changes to properties in the collection...
                        ((INotifyCollectionChanged)instance.ItemsSource).CollectionChanged +=
                            new NotifyCollectionChangedEventHandler(instance.UserControlBase_CollectionChanged);
                    }
                    instance.AutoEdit();
                }
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }

            //((C)(sender)).SelectedItem = ((EntityCollection<E>)(((C)(sender))
            //    .GetValue(ItemSourceProperty))).FirstOrDefault();
        }

        public void UserControlBase_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ProcessFilteredItems();

        }

        #region AlwaysAllowEdit dependency property
        public static readonly DependencyProperty AlwaysAllowEditProperty =
        DependencyProperty.Register("AlwaysAllowEdit", typeof(bool), typeof(UserControl), null);
        public bool AlwaysAllowEdit
        {
            get
            {
                return (bool)GetValue(AlwaysAllowEditProperty);
            }
            set
            {
                SetValue(AlwaysAllowEditProperty, value);
            }
        }
        #endregion

        public CollectionViewSource _FilteredItemsSource;
        public ICollectionView FilteredItemsSource
        {
            get { return _FilteredItemsSource.View; }
        }

        //LJN 519
        private CollectionViewSource _AdmissionDocumentations;
        public ICollectionView AdmissionDocumentations
        {
            get { return _AdmissionDocumentations.View; }
        }

        //
        public ICollectionView PrintFilteredItemsSource
        {
            get
            {
                return (ICollectionView)GetValue(PrintFilteredItemsSourceProperty);
            }
            set
            {
                SetValue(PrintFilteredItemsSourceProperty, value);
            }
        }

        public static readonly DependencyProperty PrintFilteredItemsSourceProperty =
            DependencyProperty.Register("PrintFilteredItemsSource", typeof(ICollectionView), typeof(C), null);

        public Boolean IsEdit
        {
            get { return (Boolean)GetValue(IsEditProperty); }
            set { SetValue(IsEditProperty, value); }
        }
        public static DependencyProperty IsEditProperty =
            DependencyProperty.Register("IsEdit", typeof(Boolean), typeof(C), new PropertyMetadata(IsEditPropertyChanged)); //Callback invoked on property value has changes


        private static void IsEditPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ChildControlBase<C, E> cd = sender as ChildControlBase<C, E>;
            if (cd.IsEditChanged != null)
            {
                var e = new UserControlBaseEventArgs<E>(cd.SelectedItem);
                cd.IsEditChanged(cd, e);
            }
        }

        public static readonly DependencyProperty IgnoreToggleProperty =
            DependencyProperty.Register("IgnoreToggle", typeof(Boolean), typeof(C), null);

        public static readonly DependencyProperty SortOrderProperty =
            DependencyProperty.Register("SortOrder", typeof(string), typeof(C), new PropertyMetadata(SortOrderPropertyChanged));

        private static void SortOrderPropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            ChildControlBase<C, E> cd = sender as ChildControlBase<C, E>;
            if ((cd == null) || (cd.SortOrder == null) && ((cd.ItemsSource == null))) return;
            if (cd.ProcessFilteredItemsOnSortOrderPropertyChanged == true) cd.ProcessFilteredItems();
        }
        private bool _ProcessFilteredItemsOnSortOrderPropertyChanged = true;
        public bool ProcessFilteredItemsOnSortOrderPropertyChanged
        {
            get { return _ProcessFilteredItemsOnSortOrderPropertyChanged; }
            set { _ProcessFilteredItemsOnSortOrderPropertyChanged = value;  this.RaisePropertyChanged("ProcessFilteredItemsOnSortOrderPropertyChanged"); }
        }
        public virtual void BeginEditting() { return; } //do nothing - DetailUserControlBase needs this in interface ISelectedItem
        public virtual void EndEditting() { return; }  //Patient Photo
        public virtual void CancelEditting() { return; }  //Patient Photo

        #region ForceCancel
        public int ForceCancel
        {
            get { return (int)GetValue(ForceCancelProperty); }
            set { SetValue(ForceCancelProperty, value); }
        }
        public static readonly DependencyProperty ForceCancelProperty =
            DependencyProperty.Register("ForceCancel", typeof(int), typeof(ChildControlBase<C, E>), new PropertyMetadata(0, new PropertyChangedCallback(ForceCancelChanged)));

        private static void ForceCancelChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ChildControlBase<C, E> me = sender as ChildControlBase<C, E>;
            if (me == null) return;
            if ((me.ForceCancel > 0) && (me.IsEdit)) me.OnForceCancel();
        }
        public virtual void OnForceCancel() {}
        #endregion ForceCancel

        //INFO on custom events - http://msdn.microsoft.com/en-us/library/dd833067(VS.95).aspx
        public void RaiseEncounterChangedEvent()
        {
            if (EncounterChanged != null) EncounterChanged(this, EventArgs.Empty);
        }
        public event EventHandler EncounterChanged;
        public event EventHandler ItemSelected;
        public event EventHandler AddMultiPressed;
        public event EventHandler SearchClosed;
        public event EventHandler<UserControlBaseEventArgs<E>> AddPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> PostAddPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> EditPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> IsEditChanged;
        public event EventHandler<UserControlBaseEventArgs<E>> EditMultiPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> OKPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> OKPressedValidate;
        public event EventHandler<UserControlBaseEventArgs<E>> OKPressedValidateFailed;
        public event EventHandler<UserControlBaseEventArgs<E>> OKPressedPreValidate;
        public event EventHandler<UserControlBaseEventArgs<E>> CancelPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> CancelMultiPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> DeletePressed;
        public event EventHandler<UserControlBaseEventArgs<E>> SelectedPressed;
        public event EventHandler<UserControlBaseEventArgs<E>> FilteredItemsSourceChanged;
        public event EventHandler<UserControlBaseEventArgs<E>> ParentViewModelChanged;

        public void RaiseItemSelected(EventArgs args)
        {
            if (ItemSelected != null)
            {
                ItemSelected(this, EventArgs.Empty);
            }
        }

        private E _SelectedItem;
        public E SelectedItem
        {
            get
            {
                return _SelectedItem;
                //return (E)GetValue(SelectedItemProperty);
            }
            set
            {
                if (_SelectedItem != null)
                {
                    try
                    {
                        SelectedItem.PropertyChanged -= SelectedItem_PropertyChanged;
                    }
                    catch { }
                }

                _SelectedItem = value;

                if (SelectedItem != null)
                    SelectedItem.PropertyChanged += SelectedItem_PropertyChanged;
                RaiseCanExecuteChanged();

                //RaiseItemSelected(EventArgs.Empty); - this was causing a nested re-call to this setter with a null causing the add to 're-point' to the first item in the list
                RaisePropertyChanged("SelectedItem");
                RaiseItemSelected(EventArgs.Empty);
            }
        }

        public void SelectedItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            RaiseCanExecuteChanged();
        }

        public void RaiseCanExecuteChanged()
        {
            //CommandManager.RaiseCanExecuteChanged(); //since this function is called from SelectedItem_PropertyChanged(), do not re-evaluate all commands, for some controls doing so was too slow
            if (Edit_Command != null) Edit_Command.RaiseCanExecuteChanged();
            if (OK_Command != null) OK_Command.RaiseCanExecuteChanged();
            if (Cancel_Command != null) Cancel_Command.RaiseCanExecuteChanged();
        }

        private bool _OKVisible = false;
        public bool OKVisible
        {
            get { return _OKVisible; }
            set
            {
                _OKVisible = value;
                RaisePropertyChanged("OKVisible");
            }
        }

        public EntityCollection<E> ItemsSource
        {
            get
            {
                return (EntityCollection<E>)GetValue(ItemsSourceProperty);
            }
            set
            {
                if (ItemsSource != null)
                {
                    try
                    {
                        ((INotifyCollectionChanged)ItemsSource).CollectionChanged -=
                            new NotifyCollectionChangedEventHandler(UserControlBase_CollectionChanged);
                    }
                    catch { }
                }
                SetValue(ItemsSourceProperty, value);
            }
        }


        public Boolean IgnoreToggle
        {
            get
            {
                return (Boolean)GetValue(IgnoreToggleProperty);
            }
            set
            {
                SetValue(IgnoreToggleProperty, value);
            }
        }

        public string SortOrder
        {
            get
            {
                return (string)GetValue(SortOrderProperty);
            }
            set
            {
                SetValue(SortOrderProperty, value);
            }
        }

        public string ToggleDisplayText { get; set; }
        public bool ToggleFilteredItems { get; set; }

        public void ProcessFilteredItems()
        {
            OnPreProcessFilteredItems();

            FilteredCount = 0;

            ToggleDisplayText = ToggleFilteredItems ? "Current" : "All";
            this.RaisePropertyChanged("ToggleDisplayText");

            if (ItemsSource != null)
            {
                //FilteredItemsSource = new PagedCollectionView(ItemsSource);
                _FilteredItemsSource.Source = ItemsSource;

                FilteredItemsSource.SortDescriptions.Clear();
                if (!string.IsNullOrEmpty(SortOrder))
                {
                    string[] sorts = SortOrder.Split('|');
                    for (int i = 0; i < sorts.Length; i++)
                    {
                        if (sorts[i].StartsWith("-"))
                            FilteredItemsSource.SortDescriptions.Add(new SortDescription(sorts[i].Substring(1), ListSortDirection.Descending));
                        else
                            FilteredItemsSource.SortDescriptions.Add(new SortDescription(sorts[i], ListSortDirection.Ascending));
                    }
                }
              
                FilteredItemsSource.Refresh();
                FilteredItemsSource.Filter = new Predicate<object>(FilterItems);
                if (SelectedItem == null)
                {
                    FilteredItemsSource.MoveCurrentToFirst();
                    SelectedItem = (E)FilteredItemsSource.CurrentItem;
                }
                this.RaisePropertyChanged("FilteredItemsSource");

                this.PrintFilteredItemsSource = FilteredItemsSource;

            }

            AnyItemsCheck = (FilteredItemsSource == null || FilteredCount == 0);

            if (FilteredItemsSourceChanged != null)
            {
                var e = new UserControlBaseEventArgs<E>(null);
                FilteredItemsSourceChanged(this, e);
            }
            OnProcessFilteredItems();
        }

        public virtual void OnPreProcessFilteredItems()
        {
        }
        public virtual void OnProcessFilteredItems()
        {
        }
        public virtual bool? FilterItemsOverride(object item)
        {
            return null; // by default no override
        }
        public bool FilterItems(object item)
        {
            var properties = item.GetType().GetProperties();

            //I know this may look odd but I think historykey has taken on different meanings for different entities
            //For items that can be changed during an encounter (meds, icd, allergies, labs) historyfor is "new" row
            //but for items like contacts, addresses, phones, etc it is the "old" row
            //So if you have a superceded column honor it else honor the historyfor column 

            // Note - legecy encounters can have Superceded items
            if (Encounter != null)
            {
                // What about contention with multiple encounders open fro same patient?
                var ce = properties.Where(p => p.Name.Equals("CurrentEncounter", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (ce != null)
                {
                    item.GetType().GetProperty(ce.Name).SetValue(item, Encounter, null);
                }
            }
            bool? accepted = FilterItemsOverride(item);
            if (accepted != null)
            {
                if (accepted == true) FilteredCount++;
                return (bool)accepted;
            }

            if (Encounter == null)
            {
                var prop = properties.Where(p => p.Name.Equals("Superceded", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (prop != null)
                {
                    bool value = (bool)item.GetType().GetProperty(prop.Name).GetValue(item, null);
                    if (value)
                        return false;
                }
                else
                {
                    prop = properties.Where(p => p.Name.Equals("HistoryKey", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (prop != null)
                    {
                        var value = item.GetType().GetProperty(prop.Name).GetValue(item, null);
                        if (value != null)
                            return false;
                    }
                }
            }
            VirtuosoEntity v = item as VirtuosoEntity;
            // If we have an Encounter and the item is not new, only include the item if it is in this encounter
            if ((Encounter != null) && (!v.IsNew))
            {
                PatientAllergy pa = item as PatientAllergy;
                if (pa != null)
                {
                    EncounterAllergy ea = Encounter.EncounterAllergy.Where(p => ((p.PatientAllergy != null) && (p.PatientAllergy.PatientAllergyKey == pa.PatientAllergyKey) && (!pa.Inactive))
                                                                           ).FirstOrDefault();
                    if (ea == null) return false;
                }

                AdmissionEquipment ae = item as AdmissionEquipment;
                if (ae != null)
                {
                    EncounterEquipment ee = Encounter.EncounterEquipment.Where(p => ((p.AdmissionEquipment != null) && (p.AdmissionEquipment.AdmissionEquipmentKey == ae.AdmissionEquipmentKey)
                                                                                     && (!ae.Inactive)
                                                                                    )
                                                                              ).FirstOrDefault();
                    if (ee == null)
                    {
                        return false;
                    }
                }

                PatientMedication pm = item as PatientMedication;
                if (pm != null)
                {
                    EncounterMedication em = Encounter.EncounterMedication.Where(p => p.PatientMedication.PatientMedicationKey == pm.PatientMedicationKey).FirstOrDefault();
                    if (em == null) return false;
                }
                AdmissionLevelOfCare al = item as AdmissionLevelOfCare;
                if (al != null)
                {
                    EncounterLevelOfCare el = Encounter.EncounterLevelOfCare.Where(p => p.AdmissionLevelOfCare.AdmissionLevelOfCareKey == al.AdmissionLevelOfCareKey).FirstOrDefault();
                    if (el == null) return false;
                }
                AdmissionPainLocation ap = item as AdmissionPainLocation;
                if (ap != null)
                {
                    EncounterPainLocation ep = Encounter.EncounterPainLocation.Where(p => p.AdmissionPainLocation.AdmissionPainLocationKey == ap.AdmissionPainLocationKey).FirstOrDefault();
                    if (ep == null) return false;
                }
                AdmissionIVSite ai = item as AdmissionIVSite;
                if (ai != null)
                {
                    EncounterIVSite ei = Encounter.EncounterIVSite.Where(p => p.AdmissionIVSite.AdmissionIVSiteKey == ai.AdmissionIVSiteKey).FirstOrDefault();
                    if (ei == null) return false;
                }
                AdmissionWoundSite aw = item as AdmissionWoundSite;
                if ((aw != null) && (Encounter != null) && (Encounter.EncounterWoundSite != null))
                {
                    EncounterWoundSite ew = Encounter.EncounterWoundSite.Where(p => ((p.AdmissionWoundSite != null) && (p.AdmissionWoundSite.AdmissionWoundSiteKey == aw.AdmissionWoundSiteKey))).FirstOrDefault();
                    if (ew == null) return false;
                }
                AdmissionGoal ag = item as AdmissionGoal;
                if (ag != null)
                {
                    EncounterGoal eg = Encounter.EncounterGoal.Where(p => p.AdmissionGoalKey == ag.AdmissionGoalKey).FirstOrDefault();
                    if (eg == null) return false;
                }
                AdmissionGoalElement age = item as AdmissionGoalElement;
                if (age != null)
                {
                    EncounterGoalElement ege = Encounter.EncounterGoalElement.Where(p => p.AdmissionGoalElementKey == age.AdmissionGoalElementKey).FirstOrDefault();
                    if (ege == null) return false;
                }
                AdmissionDisciplineFrequency adf = item as AdmissionDisciplineFrequency;
                if (adf != null)
                {
                    EncounterDisciplineFrequency edf = Encounter.EncounterDisciplineFrequency.Where(p => p.AdmissionDisciplineFrequency.DisciplineFrequencyKey == adf.DisciplineFrequencyKey).FirstOrDefault();
                    if (edf == null) return false;
                }
                AdmissionInfection aif = item as AdmissionInfection;
                if (aif != null)
                {
                    EncounterInfection ef = Encounter.EncounterInfection.Where(p => p.AdmissionInfection != null && p.AdmissionInfection.AdmissionInfectionKey == aif.AdmissionInfectionKey && !p.AdmissionInfection.Superceded).FirstOrDefault();
                    if (ef == null) return false;
                }

                PatientInfection aif2 = item as PatientInfection;
                if (aif2 != null)
                {
                    EncounterPatientInfection ef = Encounter.EncounterPatientInfection.Where(p => p.PatientInfection != null && p.PatientInfection.PatientInfectionKey == aif2.PatientInfectionKey && !p.PatientInfection.Superceded).FirstOrDefault();
                    if (ef == null) return false;
                }

                PatientAdverseEvent pae = item as PatientAdverseEvent;
                if (pae != null)
                {
                    EncounterPatientAdverseEvent epae = Encounter.EncounterPatientAdverseEvent.Where(p => p.PatientAdverseEvent != null && p.PatientAdverseEvent.PatientAdverseEventKey == pae.PatientAdverseEventKey).FirstOrDefault();
                    if (epae == null) return false;
                }

                PatientLab pl = item as PatientLab;
                if (pl != null)
                {
                    EncounterLab el = Encounter.EncounterLab.Where(p => p.PatientLab != null && p.PatientLab.PatientLabKey == pl.PatientLabKey && !p.PatientLab.Superceded).FirstOrDefault();
                    if (el == null) return false;
                }
            }
            if (Encounter != null)
            {
                // Filter out Added-in-error medications from Plan of Care and OrderEntry
                PatientMedication pm = item as PatientMedication;
                if (pm != null)
                {
                    if ((pm.AddedInError == true) && (Encounter.IsPlanOfCareOrIsTeamMeetingOrIsOrderEntry)) return false;
                }
                // Filter out inactive equipment
                AdmissionEquipment ae = item as AdmissionEquipment;
                if (ae != null)
                {
                    if (ae.Inactive) return false;
                }
                // Filter out deleted IVs
                AdmissionIVSite ai = item as AdmissionIVSite;
                if (ai != null)
                {
                    if (ai.DeletedDate != null) return false;
                }
            }
            if ((!ToggleFilteredItems) && (IgnoreToggle == false))
            {
                // all other types but AdmissionDiagnosis
                var prop = properties.Where(p => p.Name.Equals("Inactive", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (prop != null)
                {
                    bool value = (bool)item.GetType().GetProperty(prop.Name).GetValue(item, null);
                    if (value)
                        return false;
                }

                // Ignore discontinued
                prop = properties.Where(p => p.Name.Equals("MedicationStatus", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if (prop != null)
                {
                    var value = item.GetType().GetProperty(prop.Name).GetValue(item, null);
                    if (value != null && value.ToString() == "2")
                        return false;
                }
                else
                {
                    prop = properties.Where(p => p.Name.Equals("EffectiveThru", StringComparison.OrdinalIgnoreCase) ||
                                            p.Name.EndsWith("EndDate", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (prop != null)
                    {
                        DateTime encounterDate = DateTime.Today;
                        if (this.Encounter != null)
                        {
                            if (this.Encounter.EncounterStartDate != null)
                                if (this.Encounter.EncounterStartDate != DateTime.MinValue)
                                    encounterDate = (DateTime)this.Encounter.EncounterStartDate.Value.Date;
                        }
                        var value = item.GetType().GetProperty(prop.Name).GetValue(item, null);
                        if ((IsModifiedPatientAllergy(item) == false) && (value != null) && ((DateTime)value < encounterDate))
                            return false;
                    }
                }
            }

            FilteredCount++;
            return true;
        }
        private bool IsModifiedPatientAllergy(object item)
        {
            // override filtering out newly 'inactivated'/'discontinued' allergies so they stay on the edit board during this session
            PatientAllergy pa = item as PatientAllergy;
            if (pa == null) return false;
            if (pa.IsNew == true) return true;
            if (pa.IsModified == true) return true;
            return false;
        }
        private bool _AnyItemsCheck = false;
        public bool AnyItemsCheck
        {
            get { return _AnyItemsCheck; }
            set
            {
                if (value != _AnyItemsCheck)
                {
                    _AnyItemsCheck = value;
                    this.RaisePropertyChanged("AnyItemsCheck");

                    if (!value && FilteredCount == 0)
                    {
                        if (typeof(E).FullName.Contains("PatientAllergy"))
                            AddMulti_Command.Execute(null);
                        else if (typeof(E).FullName.Contains("PatientMedication"))
                            Add_Command.Execute(null);
                        else if (typeof(E).FullName.Contains("AdmissionIVSite"))
                            Add_Command.Execute(null);
                    }
                }
            }
        }

        private int _FilteredCount;
        public int FilteredCount
        {
            get { return _FilteredCount; }
            set
            {
                _FilteredCount = value;
                this.RaisePropertyChanged("FilteredCount");
            }
        }

        private bool _SearchOpen;
        public bool SearchOpen
        {
            get { return _SearchOpen; }
            set
            {
                _SearchOpen = value;
                OnSearchOpenChanged(SearchOpen);
                this.RaisePropertyChanged("SearchOpen");
                this.RaisePropertyChanged("SearchOpenICDAdmissionMaint");
                this.RaisePropertyChanged("SearchOpenICDDynamicForm");
            }
        }
        public bool SearchOpenICDAdmissionMaint
        {
            get
            {
                if ((SearchOpen) && (Encounter == null)) return true;
                return false;
            }
        }
        public bool SearchOpenICDDynamicForm
        {
            get
            {
                if ((SearchOpen) && (Encounter != null)) return true;
                return false;
            }
        }
        public virtual void OnSearchOpenChanged(bool SearchOpen) { }
        #region Commands
        public RelayCommand Add_Command
        {
            get;
            protected set;

        }
        public RelayCommand AddMulti_Command
        {
            get;
            protected set;
        }
        public RelayCommand Select_Command
        {
            get;
            protected set;
        }
        public RelayCommand Edit_Command
        {
            get;
            protected set;
        }
        public RelayCommand EditMulti_Command
        {
            get;
            protected set;
        }
        public RelayCommand<E> EditItem_Command
        {
            get;
            protected set;
        }
        public RelayCommand OK_Command
        {
            get;
            protected set;
        }
        public RelayCommand OKMulti_Command
        {
            get;
            protected set;
        }
        public RelayCommand Cancel_Command
        {
            get;
            protected set;
        }
        public RelayCommand Cancel2_Command
        {
            get;
            protected set;
        }
        public RelayCommand Delete_Command
        {
            get;
            protected set;
        }
        public RelayCommand<E> DeleteItem_Command
        {
            get;
            protected set;
        }
        public RelayCommand CancelMulti_Command
        {
            get;
            protected set;
        }
        public RelayCommand CloseSearch_Command
        {
            get;
            set;
        }
        public RelayCommand ToggleDisplay
        {
            get;
            protected set;
        }
        public RelayCommand ChildRowChanged_Command
        {
            get;
            protected set;
        }
        #endregion

        public void RaiseOKPressedEvent()
        {
            if (OKPressed != null)
            {
                var e = new UserControlBaseEventArgs<E>(SelectedItem);
                OKPressed(this, e);
            }
        }
        //Inheriting class overrides this method to return HasChanges for entities in addition to SelectedItem
        public virtual bool HasChanges() { return false; }

        //Inheriting class overrides this method to do anyting special when one of it's 'children' change.
        public virtual void OnChildRowChanged() { }

        private void SetupCommands()
        {
            AddMulti_Command = new RelayCommand(() =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() => { SearchOpen = true; });              
                IsEdit = true;
                if (AddMultiPressed != null) AddMultiPressed(this, EventArgs.Empty);

                //J.E. - not sure that we need to put existing items into edit mode???
                //foreach (E item in FilteredItemsSource)
                //{
                //    item.BeginEditting();
                //}
            }, () => this.IsOnline);

            Add_Command = new RelayCommand(() =>
            {
                E instance = Activator.CreateInstance<E>(); 
                if ((Encounter != null) && (Encounter.EncounterKey > 0)) 
                {
                    var properties = instance.GetType().GetProperties();
                    var ce = properties.Where(p => p.Name.Equals("AddedFromEncounterKey", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                    if (ce != null) instance.GetType().GetProperty(ce.Name).SetValue(instance, Encounter.EncounterKey, null);
                }

                if (AddPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(instance);
                    AddPressed(this, e);
                }

                ItemsSource.Add(instance);  //add to current insurance
                ProcessFilteredItems();

                SelectedItem = instance;
                if (SelectedItem != null)
                    SelectedItem.BeginEditting();
                IsEdit = true;
                if (PostAddPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(instance);
                    PostAddPressed(this, e);
                }
                if ((this.PopupDataTemplate is string && String.IsNullOrEmpty(this.PopupDataTemplate as string)) || (this.PopupDataTemplate == null))
                    SetFocusHelper.SelectFirstEditableWidget(this);
            }, () => this.IsOnline && AllowAdd());

            ActivityCommand = new RelayCommand<AdmissionDocumentationItem>((t) =>
            {
                string uri = string.Empty;
                if (t.EncounterKey.HasValue)
                {
                    if (t.TaskKey.HasValue)
                    {
                        uri = NavigationUriBuilder.Instance.GetDynamicFormURI(
                            t.PatientKey,
                            t.AdmissionKey.GetValueOrDefault(),
                            t.FormKey.GetValueOrDefault(),
                            t.ServiceTypeKey.GetValueOrDefault(),
                            t.TaskKey.GetValueOrDefault());
                    }
                    else
                    {
                        uri = NavigationUriBuilder.Instance.GetOrderEntryURI(t.AdmissionKey.GetValueOrDefault(), t.SourceKey);
                    }

                    Messenger.Default.Send<Uri>(new Uri(uri, UriKind.Relative), "NavigationRequest");
                }
            }, (t) => this.IsOnline);

            EditItem_Command = new RelayCommand<E>((item) =>
            {
                SelectedItem = item;
                Edit_Command.Execute(null);
            });

            Edit_Command = new RelayCommand(() =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    IsEdit = true;
                });                                     

                if (SelectedItem != null)
                {
                    SelectedItem.BeginEditting();
                    BeginEditting();
                }

                if (EditPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                    EditPressed(this, e);
                    if (e.Cancel)
                    {
                        IsEdit = false;
                        if (SelectedItem != null) SelectedItem.CancelEditting();
                        return;
                    }
                }
                if ((this.PopupDataTemplate is string && String.IsNullOrEmpty(this.PopupDataTemplate as string)) || (this.PopupDataTemplate == null))
                    SetFocusHelper.SelectFirstEditableWidget(this);
            }, () =>
            {
                if (this.IsOnline == false) return false;
                if (CanExecute_EditCommand() == false) return false;

                return AlwaysAllowEdit ? true : SelectedItem != null;
            });  //if we have no currently selected item - then do not enable the Edit button!

            Select_Command = new RelayCommand(() =>
            {
                IsEdit = true;

                if (SelectedItem != null)
                {
                    SelectedItem.BeginEditting();
                    BeginEditting();
                }

                if (SelectedPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                    SelectedPressed(this, e);
                }
            }, () => AlwaysAllowEdit ? true : SelectedItem != null);  //if we have no currently selected item - then do not enable the Edit button!

            EditMulti_Command = new RelayCommand(() =>
            {
                IsEdit = true;
                foreach (E item in FilteredItemsSource)
                {
                    item.BeginEditting();
                }
                foreach (var item in ItemsSource)
                {
                    item.BeginEditting();
                }
                if (EditMultiPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                    EditMultiPressed(this, e);
                }
            }, () =>
            {
                if (this.IsOnline == false)
                    return false;
                E instance = Activator.CreateInstance<E>();
                AdmissionDiagnosis pd = instance as AdmissionDiagnosis;
                return ((FilteredCount > 0) || (pd != null));
            });

            OK_Command = new RelayCommand(() =>
            {
                if (OKPressedPreValidate != null)
                {
                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                    OKPressedPreValidate(this, e);
                    if (e.Cancel) return;
                }

                var val1 = SelectedItem.Validate();
                var val2 = Validate(); //want to make sure this runs in addition to SelectedItem.Validate()

                if (val1 && val2)
                {
                    ValidateAsync() //validate on server if validation passed on client
                        .ContinueWith(task =>
                        {
                            bool result = task.Result;
                            if (result)
                            {
                                if (OKPressedValidate != null)
                                {
                                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                                    OKPressedValidate(this, e);
                                    if (e.Cancel) return;
                                }
                                IsEdit = false;

                                EndEditting();
                                if (SelectedItem != null)
                                {
                                    ((VirtuosoEntity)SelectedItem).IsOKed = true;
                                    if (SelectedItem.IsEditting)
                                        SelectedItem.EndEditting();

                                    if (SelectedItem.IsNew || SelectedItem.HasChanges || HasChanges())
                                    {
                                        if (OKPressed != null)
                                        {
                                            var e = new UserControlBaseEventArgs<E>(SelectedItem);
                                            OKPressed(this, e);
                                        }
                                    }
                                }

                                SaveModel(UserControlBaseCommandType.OK);
                                //allow the overriding class to save to the database

                                if (Encounter != null)
                                {
                                    E entity = Activator.CreateInstance<E>();
                                    AdmissionDiagnosis PDentity = entity as AdmissionDiagnosis;
                                    if (PDentity != null)
                                        Messenger.Default.Send(Encounter.Admission,
                                                               string.Format("OasisDiagnosisChanged{0}",
                                                                             Encounter.AdmissionKey.ToString().Trim()));
                                    PatientMedication PMentity = entity as PatientMedication;
                                    if (PMentity != null)
                                        Messenger.Default.Send(Encounter.Patient,
                                                               string.Format("OasisMedicationChanged{0}",
                                                                             Encounter.PatientKey.ToString().Trim()));
                                    AdmissionPainLocation APLentity = entity as AdmissionPainLocation;
                                    if (APLentity != null)
                                        Messenger.Default.Send(Encounter.Admission,
                                                               string.Format("OasisPainLocationChanged{0}",
                                                                             Encounter.AdmissionKey.ToString()
                                                                                      .Trim()));
                                    AdmissionWoundSite AWSentity = entity as AdmissionWoundSite;
                                    if (AWSentity != null)
                                        Messenger.Default.Send(Encounter.Admission,
                                                               string.Format("OasisWoundSiteChanged{0}",
                                                                             Encounter.AdmissionKey.ToString()
                                                                                      .Trim()));
                                }

                                ProcessFilteredItems();
                            }
                            else
                            {
                                //async validation failed
                                IsEdit = true;            //keep in open edit to allow user to modify or cancel the edit action
                                RaiseCanExecuteChanged(); //ensure OK button visible
                            }
                        },
                        System.Threading.CancellationToken.None,
                        System.Threading.Tasks.TaskContinuationOptions.None,
                        Client.Utils.AsyncUtility.TaskScheduler);
                    //
                    //FilteredItemsSource.Refresh();
                    //OnProcessFilteredItems();
                }
                else
                {
                    if (OKPressedValidateFailed != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                        OKPressedValidateFailed(this, e);
                    }
                }
            }, () =>
            {
                bool? canExecute  = CanExecute_OK_CommandOverride();
                if (canExecute == null)
                {
                    OKVisible = this.IsOnline && SelectedItem != null && IsEdit && (SelectedItem.HasChanges || SelectedItem.IsNew || HasChanges());
                }
                else
                {
                    OKVisible = (bool)canExecute;
                }
                return OKVisible;
            });

            OKMulti_Command = new RelayCommand(() =>
            {
                bool AllValid = true;
                foreach (E item in ItemsSource)
                {
                    AdmissionWoundSite aw = (item == null) ? null : item as AdmissionWoundSite;
                    bool isNew = (aw == null) ? ((item == null) ? false : item.IsNew) : aw.IsBrandNew;
                    if ((isNew || item.HasChanges) && !item.Validate())
                    {
                        AllValid = false;
                    }
                }

                if (AllValid)
                {
                    if (OKPressedValidate != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                        OKPressedValidate(this, e);
                        if (e.Cancel) return;
                }

                }
                if (AllValid)
                {
                    foreach (E item in ItemsSource)
                    {
                        item.EndEditting();
                    }

                    SearchOpen = false;
                    IsEdit = false;

                    ProcessFilteredItems();

                    if (OKPressed != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                        OKPressed(this, e);
                    }
                    //FilteredItemsSource.Refresh();
                    //OnProcessFilteredItems();
                }

                EditMulti_Command.RaiseCanExecuteChanged();
            }, () => this.IsOnline);

            Cancel_Command = new RelayCommand(() =>
            {
                IsEdit = false;

                if (SelectedItem != null)
                {
                    SelectedItem.CancelEditting();
                    CancelEditting();

                    RemoveFromModel();  //allow overriding class to remove child entities of SelectedItem - e.g. Patient Photo

                    AdmissionWoundSite aw = (SelectedItem == null) ? null : SelectedItem as AdmissionWoundSite;
                    bool isNew = (aw == null) ? ((SelectedItem == null) ? false : SelectedItem.IsNew) : aw.IsBrandNew;
                    if (isNew && ItemsSource != null)
                    {
                        var itemtoremove = SelectedItem;
                        if (ItemsSource.Contains(itemtoremove)) ItemsSource.Remove(itemtoremove); //detach from parent
                        RemoveFromModel(itemtoremove);    //delegate to model to remove child entity from child's entitycollection
                        SelectedItem = ItemsSource.FirstOrDefault();
                    }
                }

                //allow the overriding class to save to the database
                //This must be done for CANCEL also, because there could be pending saves.
                SaveModel(UserControlBaseCommandType.Cancel);

                //if (ItemsSource != null)
                //    SelectedItem = ItemsSource.FirstOrDefault();
                //if (FilteredItemsSource != null)
                //    FilteredItemsSource.MoveCurrentToFirst();
                if (CancelPressed != null)
                {
                    var e = new UserControlBaseEventArgs<E>(SelectedItem);
                    CancelPressed(this, e);
                }
            });

            Cancel2_Command = new RelayCommand(
                () =>
                {

                    SearchOpen = false;
                    IsEdit = false;

                    SelectedItem.CancelEditting();

                    if (((VirtuosoEntity)SelectedItem).IsOKed == false)
                    {
                        // Canel while adding new item - remove it
                        AdmissionWoundSite aw = (SelectedItem == null) ? null : SelectedItem as AdmissionWoundSite;
                        bool isNew = (aw == null) ? ((SelectedItem == null) ? false : SelectedItem.IsNew) : aw.IsBrandNew;
                        if (isNew && ItemsSource != null)
                        {
                            var itemtoremove = SelectedItem;
                            if (ItemsSource.Contains(itemtoremove)) ItemsSource.Remove(itemtoremove); //detach from parent
                            RemoveFromModel(itemtoremove);    //delegate to model to remove child entity from child's entitycollection
                            SelectedItem = null; // Break the bindings first; occasionally the bindings hold on long enough to change the new selected items values to the canceled rows values.
                            SelectedItem = ItemsSource.FirstOrDefault();
                        }
                    }
                    //allow the overriding class to save to the database
                    //This must be done for CANCEL also, because there could be pending saves.
                    SaveModel(UserControlBaseCommandType.Cancel);

                    //if (ItemsSource != null)
                    //    SelectedItem = ItemsSource.FirstOrDefault();
                    if (CancelPressed != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                        CancelPressed(this, e);
                    }

                }, () => this.IsOnline);

            DeleteItem_Command = new RelayCommand<E>((item) =>
            {
                SelectedItem = item;
                SelectedItem.CancelEditting();

                // Remove the item
                var itemtoremove = SelectedItem;
                AdmissionIVSite itemtoremoveAdmissionIVSite = itemtoremove as AdmissionIVSite;
                if (itemtoremoveAdmissionIVSite == null)
                {
                    if (ItemsSource.Contains(itemtoremove)) ItemsSource.Remove(itemtoremove); //detach from parent
                    RemoveFromModel(itemtoremove);    //delegate to model to remove child entity from child's entitycollection
                }
                else
                {
                    if ((itemtoremoveAdmissionIVSite.AdmissionIVSiteKey > 0) && (ItemsSource.Contains(itemtoremove)))
                    {
                        itemtoremoveAdmissionIVSite.DeletedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                    }
                    else
                    {
                        if (ItemsSource.Contains(itemtoremove)) ItemsSource.Remove(itemtoremove); //detach from parent
                        RemoveFromModel(itemtoremove);    //delegate to model to remove child entity from child's entitycollection
                    }                   
                }
                ProcessFilteredItems();
                RaisePropertyChanged("AnyItemsCheck");
                if (Encounter != null)
                {
                    E entity = Activator.CreateInstance<E>();
                    AdmissionDiagnosis PDentity = entity as AdmissionDiagnosis;
                    if (PDentity != null) Messenger.Default.Send(Encounter.Admission, string.Format("OasisDiagnosisChanged{0}", Encounter.AdmissionKey.ToString().Trim()));
                    PatientMedication PMentity = entity as PatientMedication;
                    if (PMentity != null) Messenger.Default.Send(Encounter.Patient, string.Format("OasisMedicationChanged{0}", Encounter.PatientKey.ToString().Trim()));
                    AdmissionPainLocation APLentity = entity as AdmissionPainLocation;
                    if (APLentity != null) Messenger.Default.Send(Encounter.Admission, string.Format("OasisPainLocationChanged{0}", Encounter.AdmissionKey.ToString().Trim()));
                    AdmissionWoundSite AWSentity = entity as AdmissionWoundSite;
                    if (AWSentity != null) Messenger.Default.Send(Encounter.Admission, string.Format("OasisWoundSiteChanged{0}", Encounter.AdmissionKey.ToString().Trim()));
                }

                if (ItemsSource != null)
                    SelectedItem = ItemsSource.FirstOrDefault();
            });

            Delete_Command = new RelayCommand(
                () =>
                {
                    IsEdit = false;

                    SelectedItem.CancelEditting();

                    // Remove the item
                    var itemtoremove = SelectedItem;
                    AdmissionIVSite itemtoremoveAdmissionIVSite = itemtoremove as AdmissionIVSite;
                    if (itemtoremoveAdmissionIVSite == null)
                    {
                        if (ItemsSource.Contains(itemtoremove)) ItemsSource.Remove(itemtoremove); //detach from parent
                        RemoveFromModel(itemtoremove);    //delegate to model to remove child entity from child's entitycollection
                    }
                    else
                    {
                        if ((itemtoremoveAdmissionIVSite.AdmissionIVSiteKey > 0) && (ItemsSource.Contains(itemtoremove)))
                        {
                            itemtoremoveAdmissionIVSite.DeletedDate = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                        }
                        else
                        {
                            if (ItemsSource.Contains(itemtoremove)) ItemsSource.Remove(itemtoremove); //detach from parent
                            RemoveFromModel(itemtoremove);    //delegate to model to remove child entity from child's entitycollection
                        }
                    }
                    ProcessFilteredItems();
                    RaisePropertyChanged("AnyItemsCheck");
                    if (Encounter != null)
                    {
                        E entity = Activator.CreateInstance<E>();
                        AdmissionDiagnosis PDentity = entity as AdmissionDiagnosis;
                        if (PDentity != null) Messenger.Default.Send(Encounter.Admission, string.Format("OasisDiagnosisChanged{0}", Encounter.AdmissionKey.ToString().Trim()));
                        PatientMedication PMentity = entity as PatientMedication;
                        if (PMentity != null) Messenger.Default.Send(Encounter.Patient, string.Format("OasisMedicationChanged{0}", Encounter.PatientKey.ToString().Trim()));
                        AdmissionPainLocation APLentity = entity as AdmissionPainLocation;
                        if (APLentity != null) Messenger.Default.Send(Encounter.Admission, string.Format("OasisPainLocationChanged{0}", Encounter.AdmissionKey.ToString().Trim()));
                        AdmissionWoundSite AWSentity = entity as AdmissionWoundSite;
                        if (AWSentity != null) Messenger.Default.Send(Encounter.Admission, string.Format("OasisWoundSiteChanged{0}", Encounter.AdmissionKey.ToString().Trim()));
                    }

                    if (DeletePressed != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(itemtoremove);
                        DeletePressed(this, e);
                    }

                    //allow the overriding class to save to the database
                    //This must be done for CANCEL also, because there could be pending saves.
                    SaveModel(UserControlBaseCommandType.Cancel);

                    if (ItemsSource != null)
                        SelectedItem = ItemsSource.FirstOrDefault();
                }, () => this.IsOnline);

            CancelMulti_Command = new RelayCommand(
                () =>
                {
                    SearchOpen = false;
                    IsEdit = false;

                    if (ItemsSource != null)
                    {
                        var itemsToRemove = ItemsSource.Where(p => p.IsNew);
                        foreach (E item in itemsToRemove.Reverse())
                        {
                            if (item.ValidationErrors != null) item.ValidationErrors.Clear();
                            ItemsSource.Remove(item);
                            RemoveFromModel(item);
                        }
                        foreach (E item in ItemsSource)
                        {
                            if (item.ValidationErrors != null) item.ValidationErrors.Clear();
                            try { item.CancelEditting(); }
                            catch { }
                        }
                    }
                    if (CancelMultiPressed != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                        CancelMultiPressed(this, e);
                    }
                    ProcessFilteredItems();
                }, () => this.IsOnline);

            CloseSearch_Command = new RelayCommand(() =>
            {
                SearchClosing();
                IsEdit = false;
                ProcessFilteredItems();
                Deployment.Current.Dispatcher.BeginInvoke(() => { SearchOpen = false; });              
            });

            ToggleDisplay = new RelayCommand(
                () =>
                {
                    ToggleFilteredItems = !ToggleFilteredItems;
                    //FilteredItemsSource.Refresh();
                    ProcessFilteredItems();
                });

            ChildRowChanged_Command = new RelayCommand(() =>
            {
                OnChildRowChanged();
                RaiseCanExecuteChanged();
            });
        }

        public virtual bool? CanExecute_OK_CommandOverride()
        {
            //Allow inheriting class to override CanExecute_EditCommand behaviour
            return null;
        }
        public virtual bool CanExecute_EditCommand()
        {
            //Allow inheriting class to override CanExecute_EditCommand behaviour
            return true;
        }
       protected virtual bool AllowAdd()
        {
            //Allow inheriting class to override ADD behaviour
            return true;
        }

        public RelayCommand<AdmissionDocumentationItem> ActivityCommand { get; protected set; }
        public virtual void SearchClosing()
        {
            if (SearchClosed != null) SearchClosed(this, EventArgs.Empty);
        }
        private Action _SearchAction;
        public Action SelectAction
        {
            get { return this._SearchAction; }
            set { _SearchAction = value; }
        }
        //Inheriting class overrides this method to remove the child/related entity from the Model(DomainContext)
        public virtual System.Threading.Tasks.Task<bool> ValidateAsync()
        {
            var taskCompletionSource = new System.Threading.Tasks.TaskCompletionSource<bool>();
            taskCompletionSource.TrySetResult(true);
            return taskCompletionSource.Task;
        }
        public virtual bool Validate() { return true; }
        public virtual void RemoveFromModel() { }
        public virtual void RemoveFromModel(E entity) { }

        //Inheriting class overrides this method to save the child/related entity using the Model(DomainContext)
        public virtual void SaveModel(UserControlBaseCommandType command) { }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                try
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
                catch
                {

                }
            }
        }

        //http://amazedsaint.blogspot.com/2009/12/silverlight-listening-to-dependency.html
        /// Listen for change of the dependency property
        public void RegisterForNotification(string propertyName, FrameworkElement element, PropertyChangedCallback callback)
        {
            //Bind to a dependency property
            Binding b = new Binding(propertyName) { Source = element };
            var prop = System.Windows.DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                typeof(UserControl),
                new System.Windows.PropertyMetadata(callback));

            element.SetBinding(prop, b);
        }
        public bool Protected
        {
            get { return (bool)GetValue(ProtectedProperty); }
            set { SetValue(ProtectedProperty, value); RaisePropertyChanged("Protected"); }
        }
        public static readonly DependencyProperty ProtectedProperty =
            DependencyProperty.Register("Protected", typeof(bool), typeof(ChildControlBase<C, E>), null);

        protected static void HandleError(System.Threading.Tasks.Task ret, string tag)
        {
            if (ret.Exception is AggregateException)
            {
                //_task.Exception
                ret.Exception.Flatten()
                    .Handle((x) =>
                    {
                        if (x.InnerException != null)
                        {
                            Virtuoso.Core.Controls.ErrorWindow.CreateNew(String.Format("[000] {0}", tag), x.InnerException);
                        }
                        else
                        {
                            Virtuoso.Core.Controls.ErrorWindow.CreateNew(String.Format("[001] {0}", tag), x);
                        }
                        return true; //exception was handled.
                    });
            }
            else if (ret.Exception != null)
            {
                //task.Exception.Flatten().Message
                Virtuoso.Core.Controls.ErrorWindow.CreateNew(String.Format("[002] {0}", tag), ret.Exception);
            }
        }

        protected void CheckForError(Core.Events.EntityEventArgs<HomeScreenTaskPM> e)
        {
            if (e.Error != null)
            {
                Virtuoso.Core.Controls.ErrorWindow.CreateNew(this.GetType().FullName, e);
            }
        }

        protected void CheckForError(Exception e)
        {
            if (e != null)
            {
                Virtuoso.Core.Controls.ErrorWindow.CreateNew(this.GetType().FullName, e);
            }
        }
    }

    public interface IRaiseCanExecuteChanged
    {
        void RaiseCanExecuteChanged();
    }

    //Defers to parent DataContext for adds
    //Only binds SelectedItem for purpose of deletes
    //E = GoalElementInGoal - an entity mapped to an association table
    //C = GoalElementUserControl
    public class ListUserControlBase<C, E> : UserControl, IListInterface<E>, INotifyPropertyChanged, ICleanup, IRaiseCanExecuteChanged
        where C : UserControl
        where E : VirtuosoEntity
    {
        protected bool IsOnline { get; set; }
        protected CommandManager CommandManager { get; set; }
        public NotifyCollectionChangedEventHandler ListUserControlBase_CollectionChanged { get; set; }

        public ListUserControlBase()
        {
            //TODO: think I need to initialize each dependency property so that they are unique across instances of user controls
            //      that use this base class...
            //      reference this url: http://msdn.microsoft.com/en-us/library/cc903961(VS.95).aspx
            //SetValue(InsuranceAddressesProperty, new EntityCollection<InsuranceAddress>();

            SetupCommands();
            CommandManager = new CommandManager(this);
            IsOnline = EntityManager.Current.IsOnline;
        }

        private CollectionViewSource _FilteredItemsCollection;
        public CollectionViewSource FilteredItemsCollection
        {
            get { return _FilteredItemsCollection; }
            set
            {
                _FilteredItemsCollection = value;
                RaisePropertyChanged("FilteredItemsCollection");
            }
        }

        private bool _OKVisible = false;
        public bool OKVisible
        {
            get { return _OKVisible; }
            set
            {
                _OKVisible = value;
                RaisePropertyChanged("OKVisible");
            }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(EntityCollection<E>), typeof(C), //null);
        new PropertyMetadata(ItemsSourcePropertyChanged)); //Callback invoked on property value has changes

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(E), typeof(C), null);

        private static void ItemsSourcePropertyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                var itemsSource = (EntityCollection<E>)(((C)(sender)).GetValue(ItemsSourceProperty));
                if (itemsSource != null)
                {
                    var instance = sender as IListInterface<E>;
                    if (instance != null)
                    {
                        instance.RaiseItemSelected(EventArgs.Empty);

                        instance.ListUserControlBase_CollectionChanged = new NotifyCollectionChangedEventHandler((s, e) =>
                        {
                            if (e.Action == NotifyCollectionChangedAction.Add)
                            {
                                instance.RaiseCanExecuteChanged();
                            }
                        });

                        //registers changes to the collection - not changes to properties in the collection...
                        ((INotifyCollectionChanged)instance.ItemsSource).CollectionChanged -= instance.ListUserControlBase_CollectionChanged;
                        ((INotifyCollectionChanged)instance.ItemsSource).CollectionChanged += instance.ListUserControlBase_CollectionChanged;

                        //Registering for property changes that that if the underlying model changes
                        //entity.RemoveFromView - we know to update our filtered 'view'
                        foreach (var entity in instance.ItemsSource)
                            entity.PropertyChanged += new PropertyChangedEventHandler(instance.ItemsSource_PropertyChanged);

                        try
                        {
                            //instance.ItemsCollection = new ObservableCollection<E>(itemsSource);
                            //instance.ItemsCollection.ForEach(x => x.RemoveFromView = false); 

                            instance.FilteredItemsCollection = new CollectionViewSource();
                            instance.FilteredItemsCollection.Source = itemsSource;
                            //instance.FilteredItemsCollection.Source = instance.ItemsCollection;
                            instance.FilteredItemsCollection.Filter -= FilteredItemsCollection_Filter;
                            instance.FilteredItemsCollection.Filter += FilteredItemsCollection_Filter;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(String.Format("DEBUG: {0}", e.Message));
                            throw;
                        }
                    }
                }
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("DEBUG: {0}", oe.Message));
                throw;
            }
        }

        private static void FilteredItemsCollection_Filter(object sender, FilterEventArgs e)
        {
            //instance.FilteredItemsCollection.Filter += (s, e) =>
            //{
            //    E item = e.Item as E;

            //    //e.Accepted = ((item.EntityState != EntityState.Deleted) || (item.EntityState != EntityState.Detached));
            //    e.Accepted = !item.RemoveFromView;
            //};

            VirtuosoEntity item = e.Item as VirtuosoEntity;

            //e.Accepted = ((item.EntityState != EntityState.Deleted) || (item.EntityState != EntityState.Detached));
            e.Accepted = !item.RemoveFromView;
        }

        public void ItemsSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("RemoveFromView"))
            {
                RaiseCanExecuteChanged();
                if (this.FilteredItemsCollection.View != null)
                    this.FilteredItemsCollection.View.Refresh();
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.RaiseCanExecuteChanged();
        }

        public static DependencyProperty IsEditProperty =
            DependencyProperty.Register("IsEdit", typeof(Boolean), typeof(C), null);

        public event EventHandler ItemSelected;
        public event EventHandler EditPressed;
        public event EventHandler OKPressed;
        public event EventHandler CancelPressed;

        public void RaiseItemSelected(EventArgs args)
        {
            if (ItemSelected != null)
            {
                ItemSelected(this, EventArgs.Empty);
            }
        }

        public void RefreshView()
        {
            if (this.FilteredItemsCollection.View != null)
                this.FilteredItemsCollection.View.Refresh();
        }

        public E SelectedItem
        {
            get
            {
                return (E)GetValue(SelectedItemProperty);
            }
            set
            {
                SetValue(SelectedItemProperty, value);
            }
        }

        public EntityCollection<E> ItemsSource
        {
            get
            {
                return (EntityCollection<E>)GetValue(ItemsSourceProperty);
            }
            set
            {
                SetValue(ItemsSourceProperty, value);
            }
        }

        public Boolean IsEdit
        {
            get
            {
                return (Boolean)GetValue(IsEditProperty);
            }
            set
            {
                SetValue(IsEditProperty, value);
            }
        }

        #region Commands
        public RelayCommand Add_Command
        {
            get;
            protected set;
        }
        public RelayCommand<E> Remove_Command
        {
            get;
            protected set;
        }
        public RelayCommand Edit_Command
        {
            get;
            protected set;
        }
        public RelayCommand OK_Command
        {
            get;
            protected set;
        }
        public RelayCommand Cancel_Command
        {
            get;
            protected set;
        }
        #endregion

        private void SetupCommands()
        {
            Add_Command = new RelayCommand(
                () =>
                {
                    IsEdit = true;
                    SetFocusHelper.SelectFirstEditableWidget(this);
                }
                //() => true
                );

            Remove_Command = new RelayCommand<E>(
                (item) =>
                {
                    item.RemoveFromView = true;
                    if (this.FilteredItemsCollection.View != null)
                        this.FilteredItemsCollection.View.Refresh();
                }
                //() => true
                );

            Edit_Command = new RelayCommand(
                () =>
                {
                    IsEdit = true;

                    if (EditPressed != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(SelectedItem);
                        EditPressed(this, e);
                        if (e.Cancel)
                        {
                            IsEdit = false;
                            return;
                        }
                    }
                    SetFocusHelper.SelectFirstEditableWidget(this);
                }//, () => SelectedItem != null  //if we have no currently selected item - then do not enable the Edit button!
                );

            OK_Command = new RelayCommand(
                () =>
                {
                    //var removedItems = (from i in ItemsSource
                    //                    where i.RemoveFromView == true
                    //                    select i).ToList();
                    //removedItems.ForEach(e => ItemsSource.Remove(e));

                    IsEdit = false;
                    RaiseCanExecuteChanged();
                    //this SelectedItem not set - this is an association table - need a different base class....
                    if (OKPressed != null)
                    {
                        var e = new UserControlBaseEventArgs<E>(null);
                        OKPressed(this, e);
                    }
                },
                () =>
                {
                    return OKVisible = ItemsSource != null && IsEdit && (ItemsSource.Where(p => p.RemoveFromView || p.HasChanges || p.IsNew).Any());
                });

            Cancel_Command = new RelayCommand(
                () =>
                {
                    if (ItemsSource != null)
                    {
                        //remove adds
                        ItemsSource.Where(e => e.IsNew).ToList().ForEach(e => ItemsSource.Remove(e));

                        //put back those items that are removed? - NOTE: we don't remove the items from the EntityCollection
                        //we set Removed = true - the CollectionViewSource - removes those entities from the UI.
                        //((IRevertibleChangeTracking)ItemsSource).RejectChanges();
                        ItemsSource.ForEach(e => e.RemoveFromView = false);
                        if (this.FilteredItemsCollection.View != null)
                            this.FilteredItemsCollection.View.Refresh();
                    }

                    IsEdit = false;
                    if (CancelPressed != null)
                    {
                        //var e = new InsuranceAddressEventArgs(SelectedItem); 
                        var e = new UserControlBaseEventArgs<E>(null);
                        CancelPressed(this, e);
                    }
                }
                //() => true
                );
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        //http://amazedsaint.blogspot.com/2009/12/silverlight-listening-to-dependency.html
        /// Listen for change of the dependency property
        public void RegisterForNotification(string propertyName, FrameworkElement element, PropertyChangedCallback callback)
        {
            //Bind to a dependency property
            Binding b = new Binding(propertyName) { Source = element };
            var prop = System.Windows.DependencyProperty.RegisterAttached(
                "ListenAttached" + propertyName,
                typeof(object),
                typeof(UserControl),
                new System.Windows.PropertyMetadata(callback));

            element.SetBinding(prop, b);
        }

        public void Cleanup()
        {
            Messenger.Default.Unregister(this);

            //TODO: cleanup NotifyCollectionChanged event handler

            //this.ClearValue(ItemsSourceProperty);
            //this.ClearValue(SelectedItemProperty);
            //this.ClearValue(IsEditProperty);

            if (this.ItemsSource != null)
            {
                ((INotifyCollectionChanged)this.ItemsSource).CollectionChanged -= this.ListUserControlBase_CollectionChanged;

                foreach (var entity in this.ItemsSource)
                    entity.PropertyChanged -= ItemsSource_PropertyChanged;
            }

            if (this.FilteredItemsCollection != null)
            {
                this.FilteredItemsCollection.Source = null;
                this.FilteredItemsCollection.Filter -= FilteredItemsCollection_Filter;
                this.FilteredItemsCollection = null;
            }

            this.DataContext = null;

            /////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // First disconnect the data context so no bindings pop.
            var controlsAll = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(this).ToList();
            var controls = controlsAll.Distinct();

            var uc = controls.OfType<UserControl>().ToList();
            foreach (var rc in uc)
            {
                rc.DataContext = null;
            }
            // Now cleanup the control
            var icl = controls.OfType<ICleanup>().ToList();
            foreach (var rc in icl)
            {
                ((ICleanup)rc).Cleanup();
            }
            var pop = controls.Where(c => c.Name.StartsWith("PopupRoot") || c.Name == "_PopupPresenter").ToList();
            foreach (var rc in pop)
            {
                rc.DataContext = null;
                VirtuosoObjectCleanupHelper.CleanupAll(rc);
            }
            VirtuosoObjectCleanupHelper.CleanupAll(this);
        }
    }
}
