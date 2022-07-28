#region Usings

using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Virtuoso.Core.Utility;

#endregion

namespace Virtuoso.Core.Framework
{
    public interface ITabUIManager
    {
        void ActivateNextTab(bool ifLastIndexSelectedGoToFirstTab = false);
        void ActivateTabByName(string tabName);
        void SelectFirstEditableWidget();
        void Cleanup();
    }

    //TODO: ITabUIManager implementation to work with the 'tab' interface of DynamicForm...which is an ItemsControl I believe...

    public class ListUIManager : ITabUIManager
    {
        public ListBox Element { get; protected set; }

        public static ListUIManager GetListUIManager(DependencyObject obj)
        {
            return (ListUIManager)obj.GetValue(ListUIManagerProperty);
        }

        public static void SetListUIManager(DependencyObject obj, ListUIManager value)
        {
            obj.SetValue(ListUIManagerProperty, value);
        }

        public static readonly DependencyProperty ListUIManagerProperty =
            DependencyProperty.RegisterAttached("ListUIManager", typeof(ListUIManager), typeof(ListUIManager),
                new PropertyMetadata(null, ListUIManagerChanged));

        private static void ListUIManagerChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            ListUIManager oldScope = args.OldValue as ListUIManager;
            if (oldScope != null)
            {
                oldScope.Element = null;
            }

            ListBox scopeElement = source as ListBox;
            scopeElement = source as ListBox;
            if (scopeElement == null)
            {
                throw new ArgumentException(string.Format(
                    "'{0}' is not a valid type.  TabUIManager attached property can only be specified on types inheriting from TabControl.",
                    source));
            }

            ListUIManager newScope = (ListUIManager)args.NewValue;
            if (newScope != null)
            {
                newScope.Element = scopeElement;
            }
        }

        public void ActivateNextTab(bool ifLastIndexSelectedGoToFirstTab = false)
        {
            if (Element != null)
            {
                var control = Element;

                var total_indexes = control.Items.Count; //number of tab items
                if (total_indexes <= 0)
                {
                    return;
                }

                var last_index = (total_indexes > 0) ? total_indexes - 1 : 0; //indexes are zero based
                var current_index =
                    control.SelectedIndex; //NOTE: indexes are zero based, e.g. SelectedIndex == 0 is the first tab item

                if (ifLastIndexSelectedGoToFirstTab && current_index == last_index)
                {
                    control.SelectedIndex = 0; //go to first tab
                }
                else
                {
                    if (current_index != last_index)
                    {
                        control.SelectedIndex = current_index + 1;
                    }
                }
            }
        }

        public void ActivateTabByName(string tabName)
        {
            // Not Implemented
        }

        public void SelectFirstEditableWidget()
        {
            if (Element != null)
            {
                var control = Element;
                var tabItem = control.Items[control.SelectedIndex];
                var depObject = (DependencyObject)tabItem;
                SetFocusHelper.SelectFirstEditableWidget(depObject); //Next tab item's content will not be editable
            }
        }

        public void Cleanup()
        {
            Element = null;
        }
    }

    public class TabUIManager : ITabUIManager
    {
        public TabControl TabElement { get; protected set; }

        public static TabUIManager GetTabUIManager(DependencyObject obj)
        {
            return (TabUIManager)obj.GetValue(TabUIManagerProperty);
        }

        public static void SetTabUIManager(DependencyObject obj, TabUIManager value)
        {
            obj.SetValue(TabUIManagerProperty, value);
        }

        public static readonly DependencyProperty TabUIManagerProperty =
            DependencyProperty.RegisterAttached("TabUIManager", typeof(TabUIManager), typeof(TabUIManager),
                new PropertyMetadata(null, TabUIManagerChanged));

        private static void TabUIManagerChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            TabUIManager oldScope = args.OldValue as TabUIManager;
            if (oldScope != null)
            {
                oldScope.TabElement = null;
            }

            TabControl scopeElement = source as TabControl;
            scopeElement = source as TabControl;

            if (scopeElement != null)
            {
                TabUIManager newScope = (TabUIManager)args.NewValue;
                if (newScope != null)
                {
                    newScope.TabElement = scopeElement;
                }
            }
        }

        public void ActivateNextTab(bool ifLastIndexSelectedGoToFirstTab = false)
        {
            if (TabElement != null)
            {
                var last_index = FindLastVisibleTab(TabElement);
                var current_index =
                    TabElement
                        .SelectedIndex; //NOTE: indexes are zero based, e.g. SelectedIndex == 0 is the first tab item
                if (ifLastIndexSelectedGoToFirstTab && current_index == last_index)
                {
                    TabElement.SelectedIndex = 0; //go to first tab
                }
                else
                {
                    if (current_index != last_index)
                    {
                        TabElement.SelectedIndex = FindNextVisibleTab(TabElement, current_index);
                    }
                }
            }
        }

        public void ActivateTabByName(string tabName)
        {
            if (TabElement != null)
            {
                var tabIndex = FindTabByName(TabElement, tabName);
                if (tabIndex > 0)
                {
                    TabElement.SelectedIndex = tabIndex;
                }
            }
        }

        int FindLastVisibleTab(TabControl tab)
        {
            var lastIndex = 0;
            var index = 0;
            foreach (var tabItem in tab.Items.Cast<TabItem>())
            {
                if (tabItem.Visibility == Visibility.Visible)
                {
                    lastIndex = index;
                }

                index++;
            }

            return lastIndex;
        }

        int FindTabByName(TabControl tab, String tabName)
        {
            var lastIndex = 0;
            var index = 0;
            foreach (var tabItem in tab.Items.Cast<TabItem>())
            {
                if (tabItem.Name == tabName && tabItem.Visibility == Visibility.Visible)
                {
                    lastIndex = index;
                    break;
                }

                if (tabItem.Name == tabName && tabItem.Visibility == Visibility.Collapsed)
                {
                    lastIndex = -1;
                    break;
                }

                index++;
            }

            return lastIndex;
        }

        int FindNextVisibleTab(TabControl tab, int startIndex)
        {
            var nextIndex = 0;
            var index = 0;
            foreach (var tabItem in tab.Items.Cast<TabItem>())
            {
                if (index > startIndex && tabItem.Visibility == Visibility.Visible)
                {
                    nextIndex = index;
                    break;
                }

                index++;
            }

            if (index == startIndex)
            {
                return 0;
            }

            return nextIndex;
        }

        public void SelectFirstEditableWidget()
        {
            var tabControl = TabElement;
            if (tabControl != null)
            {
                var tabItem = tabControl.Items[tabControl.SelectedIndex] as TabItem;

                var depObject = (DependencyObject)tabItem.Content;
                SetFocusHelper.SelectFirstEditableWidget(depObject);
            }
        }

        public void Cleanup()
        {
            TabElement = null;
        }
    }
}