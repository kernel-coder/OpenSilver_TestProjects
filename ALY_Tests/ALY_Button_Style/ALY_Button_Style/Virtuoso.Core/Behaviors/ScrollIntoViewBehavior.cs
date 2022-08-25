#region Usings

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public class ScrollIntoViewBehavior : Behavior<ScrollViewer>
    {
        // This entire ScrollIntoViewBehavior behavior has been replaced with a single GotFocus event on the associated ScrollViewer

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.GotFocus += ScrollViewer_GotFocus;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.GotFocus -= ScrollViewer_GotFocus;
        }

        private void ScrollViewer_GotFocus(object sender, RoutedEventArgs e)
        {
            FrameworkElement fe = e.OriginalSource as FrameworkElement;
            ScrollViewer sv = AssociatedObject;
            if ((fe == null) || (sv == null) || (fe == sv))
            {
                return;
            }

            try
            {
                GeneralTransform gt = fe.TransformToVisual(sv);
                Double controlTop = gt.Transform(new Point(0, 0)).Y;

                var viewportHeight = sv.ViewportHeight;
#if OPENSILVER // sv.ViewportHeight is 0, it reflects actual value only with sv.CustomLayout=true,
               // but setting it may cause performance issues
                viewportHeight = sv.ActualHeight;
#endif

                if ((controlTop < 0 || controlTop + fe.ActualHeight > viewportHeight) == false)
                {
                    return;
                }

                double newOffset = controlTop + sv.VerticalOffset;
                newOffset = (newOffset > (viewportHeight / 2)) ? newOffset - (viewportHeight / 2) : 0;
                if (sv.VerticalOffset != newOffset)
                {
                    sv.ScrollToVerticalOffset(newOffset);
                }
            }
            catch
            {
            }
        }
    }
}