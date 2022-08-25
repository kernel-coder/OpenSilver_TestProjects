using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;

namespace Virtuoso.Core.Controls
{
    /// <summary>
    /// Provides helper methods for the Visual tree
    /// </summary>
    public static class VisualTreeHelperEx
    {
        public static IEnumerable<DependencyObject> GetVisualDescendants(this DependencyObject element)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                yield return child;
                foreach (var descendant in child.GetVisualDescendants())
                {
                    yield return descendant;
                }
            }
        }
    }
}
