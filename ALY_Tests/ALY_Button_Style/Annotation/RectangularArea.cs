using System;

namespace Annotation
{
    public class RectangularArea
    {
        public Coordinate Point1 { get; private set; }
        public Coordinate Point2 { get; private set; } 

        public RectangularArea(Coordinate pt1, Coordinate pt2) : this(pt1.X, pt1.Y, pt2.X, pt2.Y) { }

        public RectangularArea(double X1, double Y1, double X2, double Y2)
        {
            this.Point1 = new Coordinate(Math.Min(X1, X2), Math.Min(Y1, Y2));
            this.Point2 = new Coordinate(Math.Max(X1, X2), Math.Max(Y1, Y2));
        }

        public double Left { get { return this.Point1.X; } }
        public double Top { get { return this.Point1.Y; } }
        public double Right { get { return this.Point2.X; } }
        public double Bottom { get { return this.Point2.Y; } }
        public double Width { get { return this.Right - this.Left; } }
        public double Height { get { return this.Bottom - this.Top; } }

        public double CenterX { get { return this.Left + (this.Width / 2); } }
        public double CenterY { get { return this.Top + (this.Height / 2); } }
        public Coordinate CenterCoordinate { get { return new Coordinate(this.CenterX, this.CenterY); } }

        public override string ToString()
        {
            return string.Format("{ \"point1\": {0}, \"point2\": {1} }", Point1.ToString(), Point2.ToString());
        }

        public string ToCSharp()
        {
            return string.Format(" new Annotation.RectangularArea({0},{1})", this.Point1.ToCSharp(), this.Point2.ToCSharp());
        }
    }
}
