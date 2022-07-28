#region Usings

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#endregion

namespace Virtuoso.Core.Utility
{
    public static class VirtuosoObjectCleanupHelper
    {
        private static object _lock = new object();

        public static void CleanupAll(object obj, bool CleanEntities = true)
        {

        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                bool hasChildrren = false;
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child == null)
                    {
                        continue;
                    }

                    hasChildrren = true;
                    if (child is ScrollViewer)
                    {
                        var dobj = ((ScrollViewer)child).Content as DependencyObject;
                        if (dobj is T)
                        {
                            yield return (T)dobj;
                        }

                        foreach (T childOfChild in FindVisualChildren<T>(dobj)) yield return childOfChild;
                    }

                    if (child is ContentControl)
                    {
                        var dobj = ((ContentControl)child).Content as DependencyObject;
                        if (dobj is T)
                        {
                            yield return (T)dobj;
                        }

                        foreach (T childOfChild in FindVisualChildren<T>(dobj)) yield return childOfChild;
                    }

                    if (child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child)) yield return childOfChild;
                }

                if ((depObj is ContentControl) && (hasChildrren == false))
                {
                    var dobj = ((ContentControl)depObj).Content as DependencyObject;
                    foreach (T childOfChild in FindVisualChildren<T>(dobj)) yield return childOfChild;
                }
            }
        }
    }

    public class ObjectCleaner
    {
        public void CleanupAllInternal(object obj, bool CleanEntities)
        {

        }

        private static void CleanupRelayCommands(object obj)
        {

        }

        private static void CleanupCollections(object obj, bool CleanEntities)
        {

        }

        public void CleanEntitiesInternal(object obj)
        {

        }

        public static List<FieldInfo> GetAllFields(Type classObj, int levels)
        {
            List<FieldInfo> localFields = new List<FieldInfo>();
            return localFields;
        }
    }
}