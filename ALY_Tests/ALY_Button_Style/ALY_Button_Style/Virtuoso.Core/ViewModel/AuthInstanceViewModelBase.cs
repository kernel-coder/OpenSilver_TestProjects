#region Usings

using System.Windows;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public enum VMTypeSelectorEnum
    {
        SupplyOrEquip,
        GEN
    }

    public abstract class AuthInstanceViewModelBase : GalaSoft.MvvmLight.ViewModelBase
    {
        public AdmissionAuthorizationInstance
            AuthInstanceSelectedItem { get; set; } //TODO: change to AdmissionAuthorizationInstance or VM...

        protected AuthMode Mode { get; set; }
        protected AuthType SelectedAuthType { get; set; }

        private VMTypeSelectorEnum __Type;

        public VMTypeSelectorEnum VMTypeSelector
        {
            get { return __Type; }
            set { __Type = value; }
        }

        protected AuthInstanceViewModelBase(VMTypeSelectorEnum type, AuthMode mode)
        {
            VMTypeSelector = type;
            Mode = mode;
        }

        public virtual void AuthTypeChanged(AuthType selectedAuthType)
        {
            SelectedAuthType = selectedAuthType;
        }

        protected Virtuoso.Client.Core.WeakReference<DependencyObject> View { get; set; }

        public void SetViewDependencyObject(DependencyObject control)
        {
            View = new Client.Core.WeakReference<DependencyObject>(control);

            SelectFirstEditableWidget();
        }

        protected void SelectFirstEditableWidget()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (View.IsAlive && View.Target != null)
                {
                    SetFocusHelper.SelectFirstEditableWidget(View.Target);
                }
            });
        }

        public abstract void CreateAuthorizationDetail(AdmissionAuthorizationInstance instance);

        public new virtual void Cleanup()
        {
            base.Cleanup();
        }
    }
}