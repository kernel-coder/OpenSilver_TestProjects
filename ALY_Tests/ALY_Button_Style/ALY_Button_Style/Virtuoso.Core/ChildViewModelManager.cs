#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

#endregion

namespace Virtuoso.Core
{
    public interface IChildViewModel
    {
        event EventHandler OnIsBusyChanged;
        string BusyMessage { get; set; }
        bool IsBusy { get; set; }
        bool HasChanges { get; }

        bool IsEdit
        {
            get;
            set;
        } // for VirtuosoCoreControls:OKCancelEditStackPanel and VirtuosoCoreControls:EditTabItem

        bool OKVisible { get; } // for VirtuosoCoreControls:OKCancelEditStackPanel
        string Title { get; set; } // for VirtuosoCoreControls:EditTabItem
        void InitializeViewModel();
        void FamilyMessageRegister(string Message, Action<int> action);
        void FamilyMessageSend(string Message);
        void RejectChanges();
        void SetIsBusy(bool isBusy, string busyMessage = "Please wait...");
        void Cleanup();
    }

    public class ChildViewModelBase : ObservableObject, IChildViewModel, ICleanup, INotifyDataErrorInfo
    {
        #region IChildViewModel

        public event EventHandler OnIsBusyChanged;

        private string _BusyMessage;

        public string BusyMessage
        {
            get { return _BusyMessage; }
            set { Set(() => BusyMessage, ref _BusyMessage, value); }
        }

        private bool _IsBusy;

        public bool IsBusy
        {
            get { return _IsBusy; }
            set
            {
                Set(() => IsBusy, ref _IsBusy, value);
                if (OnIsBusyChanged != null)
                {
                    OnIsBusyChanged(this, EventArgs.Empty);
                }
            }
        }

        protected Guid? _FamilyGuid = null;
        public virtual bool HasChanges => false;
        private bool _IsEdit;

        public bool IsEdit
        {
            get { return _IsEdit; }
            set
            {
                Set(() => IsEdit, ref _IsEdit, value);
                Notify(() => OKVisible);
            }
        }

        public virtual bool OKVisible => (IsEdit) ? true : false; // for now - I hate the HasChanges bullshit
        private string _Title = "Child Tab";

        public string Title
        {
            get { return _Title; }
            set { Set(() => Title, ref _Title, value); }
        }

        public virtual void InitializeViewModel()
        {
        }

        public void FamilyMessageRegister(string Message, Action<int> action)
        {
            Messenger.Default.Register(this, Message + _FamilyGuid, action);
        }

        public void FamilyMessageSend(string Message)
        {
            Messenger.Default.Send(0, Message + _FamilyGuid);
        }

        public virtual void RejectChanges()
        {
        }

        public void SetIsBusy(bool isBusy, string busyMessage = "Please wait...")
        {
            BusyMessage = busyMessage;
            IsBusy = isBusy;
        }

        #endregion IChildViewModel

        #region ICleanup

        public virtual void Cleanup()
        {
            Messenger.Default.Unregister(this);
            RejectChanges();
        }

        #endregion ICleanup

        #region INotifyDataErrorInfo

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        readonly Dictionary<string, List<string>> _currentErrors = new Dictionary<string, List<string>>();

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

        public void ClearErrors()
        {
            if (_currentErrors == null)
            {
                return;
            }

            _currentErrors.Clear();
        }

        #endregion INotifyDataErrorInfo

        protected void Notify([CallerMemberName] String propertyName = "")
        {
            RaisePropertyChanged(propertyName);
        }

        protected void Notify<T>(Expression<Func<T>> propertyExpression)
        {
            var expression = propertyExpression.Body as MemberExpression;

            if (expression == null)
            {
                throw new ArgumentException(propertyExpression.Body.ToString());
            }

            Notify(expression.Member.Name);
        }
    }

    public class ChildViewModelManager
    {
        private Guid familyGuid = Guid.NewGuid();

        private List<IChildViewModel> cVMList = new List<IChildViewModel>();

        public IChildViewModel RegisterChild(Type t)
        {
            if (t == null)
            {
                throw new Exception(String.Format("Invalid factory for class {0}", t.Name));
            }

            MethodInfo m = t.GetMethod("Create");
            if (m == null)
            {
                throw new Exception(String.Format("Invalid factory for class {0}", t.Name));
            }

            IChildViewModel cVM = (IChildViewModel)m.Invoke(null, new Object[] { familyGuid });
            if (cVM == null)
            {
                throw new Exception(String.Format("Invalid factory for class {0}", t.Name));
            }

            cVMList.Add(cVM);
            return cVM;
        }

        public bool HasChanges
        {
            get { return cVMList.Where(p => p.HasChanges).Any(); }
        }

        public bool IsBusy
        {
            get { return cVMList.Where(p => p.IsBusy).Any(); }
        }

        private readonly string BUSYMESSAGEDefault = "Please wait...";

        public string BusyMessage
        {
            get
            {
                int busyCount = cVMList.Where(p => p.IsBusy).Count();
                if (busyCount != 1)
                {
                    return BUSYMESSAGEDefault;
                }

                IChildViewModel cVM = cVMList.Where(p => p.IsBusy).FirstOrDefault();
                return (cVM == null) ? BUSYMESSAGEDefault : cVM.BusyMessage;
            }
        }

        public void FamilyMessageRegister(string Message, Action<int> action)
        {
            Messenger.Default.Register(this, Message + familyGuid, action);
        }

        public void FamilyMessageSend(string Message)
        {
            Messenger.Default.Send(0, Message + familyGuid);
        }

        public void RejectChanges()
        {
            foreach (IChildViewModel cvm in cVMList) cvm.RejectChanges();
        }

        public void Cleanup()
        {
            Messenger.Default.Unregister(this);
            foreach (IChildViewModel cvm in cVMList) cvm.Cleanup();
            cVMList = null;
        }
    }
}