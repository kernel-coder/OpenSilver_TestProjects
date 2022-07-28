#region Usings

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interactivity;
using System.Windows.Media;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Interface;

#endregion

namespace Virtuoso.Core.Utility
{
    public static class SetFocusHelper
    {
        public static bool SelectFirstEditableWidget(DependencyObject rootObject)
        {
            bool found = false;
            if (rootObject != null && typeof(UserControl).IsAssignableFrom(rootObject.GetType()))
            {
                var userControl = rootObject as UserControl;
                if (userControl != null)
                {
                    found = SelectFirstEditableWidget(userControl.Content);
                }
            }
            else if (rootObject != null &&
                     rootObject.GetType() == typeof(EditTabItem) ||
                     rootObject.GetType() == typeof(TabItem))
            {
                var tabItem = rootObject as ContentControl;
                if (tabItem != null)
                {
                    var tabItemContent = tabItem.Content;
                    if (tabItemContent != null)
                    {
                        found = SelectFirstEditableWidget((DependencyObject)tabItemContent);
                    }
                }
            }
            else if (rootObject != null &&
                     rootObject.GetType() == typeof(ContentPresenter))
            {
                var ctrl = rootObject as ContentPresenter;
                var child = VisualTreeHelper.GetChild(ctrl, 0);
                if (child != null)
                {
                    found = SelectFirstEditableWidget(child);
                }
            }
            else
            {
                found = SetFocusOnDependencyObject(rootObject);
            }

            return found;
        }

        private static bool SetFocusOnDependencyObject(DependencyObject rootObject)
        {
            bool found = false;
            bool IsCustomControl = false;
            FrameworkElement castRootObject = rootObject as FrameworkElement;
            if (castRootObject == null)
            {
                return false;
            }

            var supFields = SupportedWidgets;

            if (castRootObject is codeLookupRadio)
            {
                found = SelectFirstEditableWidget(((codeLookupRadio)castRootObject).InternalStackPanel);
                if (!found)
                {
                    SelectFirstEditableWidget(((codeLookupRadio)castRootObject).InternalWrapPanel);
                }
            }

            var descendants = castRootObject.GetVisualDescendants()
                .Where(flds =>
                    (supFields.Contains(flds.GetType().ToString()) ||
                     supFields.Contains(flds.GetType().BaseType.ToString())))
                .ToList();

            if (descendants.Any())
            {
                foreach (DependencyObject dobj in descendants)
                {
                    IsCustomControl = (dobj is ICustomCtrlContentPresenter);
                    // a couple custom controls are wrapped in Panels.
                    if (dobj is StackPanel)
                    {
                        found = SelectFirstEditableWidget(dobj);
                    }
                    else
                    {
                        if ((((Control)dobj).IsTabStop ||
                             (IsCustomControl && ((ICustomCtrlContentPresenter)dobj).IsTabStopCustom) ||
                             dobj is TimePicker) && ((Control)dobj).IsHitTestVisible
                                                 && ((Control)dobj).IsEnabled &&
                                                 (((Control)dobj).Visibility == Visibility.Visible))
                        {
                            if (dobj != null)
                            {
                                found = true;
                                ((Control)dobj).Focus();
                                break;
                            }
                        }
                    }
                }
            }

            return found;
        }

        public static bool SelectFirstEditableWidgetReflection(DependencyObject rootObject)
        {
            var descendants = rootObject.GetVisualDescendants();
            Type baseType = null;
            bool found = false;
            
            if (rootObject != null && typeof(UserControl).IsAssignableFrom(rootObject.GetType()))
            {
                var userControl = rootObject as UserControl;
                if (userControl != null)
                {
                    found = SelectFirstEditableWidget(userControl.Content);
                }
            }
            else if (rootObject != null &&
                     rootObject.GetType() == typeof(EditTabItem) ||
                     rootObject.GetType() == typeof(TabItem))
            {
                var tabItem = rootObject as ContentControl;
                if (tabItem != null)
                {
                    var tabItemContent = tabItem.Content;
                    if (tabItemContent != null)
                    {
                        found = SelectFirstEditableWidget((DependencyObject)tabItemContent);
                    }
                }
            }
            else
            {
                foreach (DependencyObject d in descendants)
                {
                    baseType = d.GetType();
                    if (baseType != null &&
                        (baseType == typeof(Panel) ||
                         baseType == typeof(Grid) ||
                         baseType == typeof(Border) ||
                         baseType == typeof(ScrollViewer) ||
                         baseType == typeof(ScrollContentPresenter) ||
                         baseType == typeof(StackPanel)))
                    {
                        found = SelectFirstEditableWidget(d);
                        if (found)
                        {
                            break;
                        }
                    }
                    else
                    {
                        found = SetFocusOnDescendant(d);
                        if (found)
                        {
                            break;
                        }
                    }
                }
            }

            return found;
        }

        private static bool SetFocusOnDescendant(DependencyObject child)
        {
            bool found = false;
            bool supportedControl = false;
            supportedControl = child.GetType() == typeof(TextBox) || child.GetType() == typeof(ComboBox) ||
                               child.GetType().BaseType == typeof(ComboBox)
                               || child.GetType() == typeof(RadioButton) || child.GetType() == typeof(DatePicker)
                               || child.GetType() == typeof(vDatePicker);

            if (supportedControl
                && ((Control)child).IsTabStop && ((Control)child).IsHitTestVisible
                && ((Control)child).IsEnabled && (((Control)child).Visibility == Visibility.Visible))
            {
                found = true;
                ((Control)child).Focus();
            }

            return found;
        }

        private static bool SetFocusOnDescendantReflection(DependencyObject child)
        {
            bool found = false;
            bool supportedControl = false;
            supportedControl = child.GetType() == typeof(TextBox) || child.GetType() == typeof(ComboBox) ||
                               child.GetType().BaseType == typeof(ComboBox)
                               || child.GetType() == typeof(RadioButton) || child.GetType() == typeof(DatePicker)
                               || child.GetType() == typeof(vDatePicker);

            if (supportedControl
                && ((Control)child).IsTabStop && ((Control)child).IsHitTestVisible
                && ((Control)child).IsEnabled && (((Control)child).Visibility == Visibility.Visible))
            {
                found = true;
                ((Control)child).Focus();
            }

            return found;
        }

        private static String[] _supWidgets;

        private static String[] SupportedWidgets
        {
            get
            {
                if (_supWidgets == null)
                {
                    return _supWidgets = new[]
                    {
                        "System.Windows.Controls.TextBox", "System.Windows.Controls.ComboBox",
                        "System.Windows.Controls.CheckBox",
                        "System.Windows.Controls.RadioButton", "System.Windows.Controls.DatePicker",
                        "Virtuoso.Core.Controls.vDatePicker",
                        "Virtuoso.Core.Controls.codeLookup", "Virtuoso.Core.Controls.codeLookupRadio",
                        "Virtuoso.Core.Controls.codeLookupRadioButton",
                        "Virtuoso.Core.Controls.autoCompleteCombo", "Virtuoso.Core.Controls.codeLookupTunneling",
                        "Virtuoso.Core.Controls.codeLookupUndermining",
                        "Virtuoso.Core.Controls.vAsyncComboBox", "System.Windows.Controls.TimePicker",
                        "System.Windows.Controls.Button", "System.Windows.Documents.HyperLink",
                        "System.Windows.Controls.HyperlinkButton"
                    };
                }

                return _supWidgets;
            }
        }
    }

    public class SetFocusAction : TriggerAction<DependencyObject>
    {
        public static readonly DependencyProperty TargetProperty =
            DependencyProperty.Register("Target", typeof(Control), typeof(SetFocusAction), new PropertyMetadata(null));

        public Control Target
        {
            get { return (Control)GetValue(TargetProperty); }
            set { SetValue(TargetProperty, value); }
        }

        protected override void Invoke(object parameter)
        {
            if (Target != null)
            {
                SetFocusHelper.SelectFirstEditableWidget(Target);
            }
        }
    }

    public class SelectAllAction : TriggerAction<TextBox>
    {
        protected override void Invoke(object parameter)
        {
            if (AssociatedObject != null)
            {
                AssociatedObject.SelectAll();
            }
        }
    }

    //http://www.telerik.com/help/silverlight/radbusyindicator-how-to-restore-the-focus.html
    public class FocusHelper
    {
        private static void OnEnsureFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == false)
            {
                (d as Control).Focus();
            }
        }

        public static bool GetEnsureFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(EnsureFocusProperty);
        }

        public static void SetEnsureFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(EnsureFocusProperty, value);
        }

        public static readonly DependencyProperty EnsureFocusProperty =
            DependencyProperty.RegisterAttached(
                "EnsureFocus",
                typeof(bool),
                typeof(FocusHelper),
                new PropertyMetadata(OnEnsureFocusChanged));
    }
}