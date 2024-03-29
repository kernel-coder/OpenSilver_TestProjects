﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Permissive License.
// See http://www.microsoft.com/resources/sharedsource/licensingbasics/sharedsourcelicenses.mspx.
// All other rights reserved.

#region Usings

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;

#endregion

namespace Virtuoso.Core.Behaviors
{
    /// <summary>
    /// Provides a rounded rectangular clipping that scales with the element.
    /// </summary>
    public class ClippingBehavior : Behavior<FrameworkElement>
    {
        /// <summary>Backing DP for the CornerRadius property</summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register("CornerRadius",
            typeof(CornerRadius), typeof(ClippingBehavior), new PropertyMetadata(new CornerRadius()));

        /// <summary>Backing DP for the Margin property</summary>
        public static readonly DependencyProperty MarginProperty = DependencyProperty.Register("Margin",
            typeof(Thickness), typeof(ClippingBehavior), new PropertyMetadata(new Thickness()));

        /// <summary>
        /// Radius of the corners
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        /// <summary>
        /// Margin of the clip to the edge of the element.
        /// </summary>
        public Thickness Margin
        {
            get { return (Thickness)GetValue(MarginProperty); }
            set { SetValue(MarginProperty, value); }
        }

        /// <summary>
        /// Initialization
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.SizeChanged += HandleSizeChanged;
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            AssociatedObject.SizeChanged -= HandleSizeChanged;
        }

        private void HandleSizeChanged(object sender, EventArgs e)
        {
            UpdateClipping();
        }

        /// <summary>
        /// Updates the clipping rectangle.
        /// </summary>
        protected virtual void UpdateClipping()
        {
            CornerRadius radius = CalculatedCornerRadius;

            double width = Math.Max(0, AssociatedObject.ActualWidth - (Margin.Left + Margin.Right));
            double height = Math.Max(0, AssociatedObject.ActualHeight - (Margin.Left + Margin.Right));

            Rect bounds = new Rect(Margin.Left, Margin.Top, width, height);

            // Take the fast path if the rectangle is simple.
            if (radius.BottomLeft == radius.BottomRight
                && radius.BottomLeft == radius.TopLeft
                && radius.BottomLeft == radius.TopRight)
            {
                AssociatedObject.Clip = new RectangleGeometry
                {
                    Rect = bounds,
                    RadiusX = radius.BottomLeft,
                    RadiusY = radius.BottomRight,
                };
            }
            else
            {
                PathGeometry geometry = new PathGeometry();
                PathFigure figure = new PathFigure();
                geometry.Figures.Add(figure);

                figure.StartPoint = new Point(bounds.Left + radius.TopLeft, bounds.Top);
                figure.Segments.Add(new LineSegment
                {
                    Point = new Point(bounds.Right - radius.TopRight, bounds.Top),
                });
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(bounds.Right, bounds.Top + radius.TopRight),
                    RotationAngle = 90,
                    IsLargeArc = false,
                    Size = new Size(radius.TopRight, radius.TopRight),
                    SweepDirection = SweepDirection.Clockwise,
                });
                figure.Segments.Add(new LineSegment
                {
                    Point = new Point(bounds.Right, bounds.Bottom - radius.BottomRight),
                });
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(bounds.Right - radius.BottomRight, bounds.Bottom),
                    RotationAngle = 90,
                    IsLargeArc = false,
                    Size = new Size(radius.BottomRight, radius.BottomRight),
                    SweepDirection = SweepDirection.Clockwise,
                });
                figure.Segments.Add(new LineSegment
                {
                    Point = new Point(bounds.Left + radius.BottomLeft, bounds.Bottom),
                });
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(bounds.Left, bounds.Bottom - radius.BottomLeft),
                    RotationAngle = 90,
                    IsLargeArc = false,
                    Size = new Size(radius.BottomLeft, radius.BottomLeft),
                    SweepDirection = SweepDirection.Clockwise,
                });
                figure.Segments.Add(new LineSegment
                {
                    Point = new Point(bounds.Left, bounds.Top + radius.TopLeft),
                });
                figure.Segments.Add(new ArcSegment
                {
                    Point = new Point(bounds.Left + radius.TopLeft, bounds.Top),
                    RotationAngle = 90,
                    IsLargeArc = false,
                    Size = new Size(radius.TopLeft, radius.TopLeft),
                    SweepDirection = SweepDirection.Clockwise,
                });
                figure.IsClosed = true;

                AssociatedObject.Clip = geometry;
            }
        }

        /// <summary>
        /// Calculates what the corner radius should be. 
        /// If this is attached to a Border, it automatically uses the Border's radius.
        /// </summary>
        protected virtual CornerRadius CalculatedCornerRadius
        {
            get
            {
                if (ReadLocalValue(CornerRadiusProperty) == DependencyProperty.UnsetValue)
                {
                    Border border = AssociatedObject as Border;
                    if (border != null)
                    {
                        return border.CornerRadius;
                    }
                }

                return CornerRadius;
            }
        }
    }
}