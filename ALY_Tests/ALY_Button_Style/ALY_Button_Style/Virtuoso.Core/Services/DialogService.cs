#region Usings

using System;
using System.Linq;
using System.Windows.Controls;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IDialogService
    {
        bool? DialogResult { get; set; }

        bool CanClose();
        void CloseDialog();

        string Caption { get; }
        bool ResizeWindow { get; }
        bool DynamicSize { get; }
        bool SetMaxWidthAndHeight { get; }

        double? Height { get; } // This is really MaxHeight
        double? MinHeight { get; }
        double? Width { get; } // This is really MaxWidth
        double? MinWidth { get; }
        void SetSelectFirstEditableWidgetAction(Action editableWidgetSelector);
    }

    internal class DialogServiceHelper
    {
        CustomWindow Window { get; set; }
        Action<bool?> OnClosed { get; }

        public DialogServiceHelper(Action<bool?> onClosed)
        {
            OnClosed = onClosed;
        }

        public void ShowDialog(IDialogService viewModel)
        {
            Window = new CustomWindow(viewModel);
            Window.Closed += Window_Closed;
            Window.Show();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Window.Closed -= Window_Closed;
            var result = ((ChildWindow)sender).DialogResult;
            OnClosed?.Invoke(result);
            var controlsAll = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(Window).ToList();
            var controls = controlsAll.Distinct();
            var uc = controls.OfType<UserControl>().ToList();
            foreach (var rc in uc) rc.DataContext = null;
            // Now call cleanup on them all.
            var icl = controls.OfType<ICleanup>().ToList();
            foreach (var rc in icl) rc.Cleanup();
            var pop = controls.Where(c => c.Name.StartsWith("PopupRoot") || c.Name == "_PopupPresenter").ToList();
            foreach (var rc in pop)
            {
                rc.DataContext = null;
                VirtuosoObjectCleanupHelper.CleanupAll(rc);
            }

            VirtuosoObjectCleanupHelper.CleanupAll(this);

            Window.Cleanup();
        }
    }

    public class DialogService
    {
        public void ShowDialog(IDialogService viewModel, Action<bool?> onClosed)
        {
            var helper = new DialogServiceHelper(onClosed);
            helper.ShowDialog(viewModel);
        }
    }
}