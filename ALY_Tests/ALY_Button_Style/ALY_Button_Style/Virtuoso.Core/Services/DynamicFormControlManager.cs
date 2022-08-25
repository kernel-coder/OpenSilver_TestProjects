#region Usings

using System.Collections.Generic;
using GalaSoft.MvvmLight;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IDynamicFormControl
    {
        bool RefreshAfterSave();
    }

    public class DynamicFormControlManager : ICleanup
    {
        List<IDynamicFormControl> ControlsToRefresh = new List<IDynamicFormControl>();

        public void RegisterControl(IDynamicFormControl controlToRegister)
        {
            if (controlToRegister == null)
            {
                return;
            }

            if (ControlsToRefresh.Contains(controlToRegister))
            {
                return;
            }

            ControlsToRefresh.Add(controlToRegister);
        }

        public void UnRegisterControl(IDynamicFormControl controlToUnRegister)
        {
            if (controlToUnRegister == null)
            {
                return;
            }

            if (ControlsToRefresh.Contains(controlToUnRegister))
            {
                ControlsToRefresh.Remove(controlToUnRegister);
            }
        }

        public bool RefreshAllControls()
        {
            bool AllSuccessful = true;
            foreach (var ctrl in ControlsToRefresh) AllSuccessful = ctrl.RefreshAfterSave();
            return AllSuccessful;
        }

        public void Cleanup()
        {
            if (ControlsToRefresh == null)
            {
                return;
            }

            ControlsToRefresh.Clear();
            ControlsToRefresh = null;
        }
    }
}