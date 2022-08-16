#region Usings

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

#endregion

namespace Virtuoso.Core.Utility
{
    public class ValidationSummaryHelper
    {
        public static ValidationSummary GetValidationSummary(FrameworkElement element)
        {
            int count = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < count; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child != null)
                {
                    ValidationSummary vs = null;
                    vs = child as ValidationSummary;
                    if (vs != null)
                    {
                        return vs;
                    }

                    vs = GetValidationSummary(child);
                    if (vs != null)
                    {
                        return vs;
                    }
                }
            }

            return null;
        }
    }
}