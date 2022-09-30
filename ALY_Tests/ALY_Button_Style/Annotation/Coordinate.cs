using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Annotation
{
    public class Coordinate
    {
        public double X { get; private set; }
        public double Y { get; private set; }
        public Coordinate(double x, double y)
        {
            this.X = x;
            this.Y = y;
        }
        public Coordinate(Point point) : this(point.X, point.Y) { }
        internal Coordinate(string serialdata) : this(0, 0)
        {
            var s = (serialdata + ",").Split(',');
            int v;
            if (int.TryParse(s[0], out v))
            {
                this.X = v;
            }

            if (int.TryParse(s[1], out v))
            {
                this.Y = v;
            }
        }

        public UIElement UIElement(double size, Color color)
        {
            var e = CreateEllipse(size, size, this.X, this.Y);
            e.Fill = new SolidColorBrush(color);
            return e;
        }

        public static Ellipse CreateEllipse(double width, double height, double desiredCenterX, double desiredCenterY)
        {
            Ellipse ellipse = new Ellipse { Width = width, Height = height };
            double left = desiredCenterX - (width / 2);
            double top = desiredCenterY - (height / 2);

            ellipse.Margin = new Thickness(left, top, 0, 0);
            return ellipse;
        }

        public Point ToPoint()
        {
            return new Point { X = this.X, Y = this.Y };
        }

        public Coordinate Add(Coordinate offset)
        {
            return new Coordinate(this.X + offset.X, this.Y + offset.Y);
        }

        public Coordinate Add(Point offset)
        {
            return new Coordinate(this.X + offset.X, this.Y + offset.Y);
        }


        public bool IsEqual(Coordinate pt2)
        {
            return (this.X == pt2.X && this.Y == pt2.Y);
        }

        public bool IsEqual(Point pt2)
        {
            return (this.X == pt2.X && this.Y == pt2.Y);
        }

        public override string ToString()
        {
            return string.Format("{0},{1}", this.X, this.Y);
        }

        public void MoveTo(Point point)
        {
            this.X = point.X;
            this.Y = point.Y;
        }
        public void MoveTo(Coordinate point)
        {
            this.X = point.X;
            this.Y = point.Y;
        }

        public double DistanceTo(Coordinate pt2)
        {
            var d1 = (this.X - pt2.X);
            var d2 = (this.Y - pt2.Y);

            return Math.Sqrt((d1 * d1) + (d2 * d2));
        }
        public double DistanceTo(Point pt2)
        {
            var d1 = (this.X - pt2.X);
            var d2 = (this.Y - pt2.Y);

            return Math.Sqrt((d1 * d1) + (d2 * d2));
        }
        public string ToCSharp()
        {
            return string.Format("new Coordinate({0}, {1})", string.Format("{0:0}", this.X), string.Format("{0:0}", this.Y));
        }
    }
}
