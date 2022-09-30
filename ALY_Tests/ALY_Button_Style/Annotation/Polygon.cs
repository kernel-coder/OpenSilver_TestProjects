using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Annotation
{
    public class Polygon : List<Coordinate>
    {
        public Polygon() { }
        public Polygon(List<Coordinate> args)
        {
            foreach (var item in args)
            {
                this.Add(item);
            }
        }
        public Polygon(params Coordinate[] args) : this(args.ToList()) { }
        internal Polygon(string serialdata) :this(serialdata.Split('|').Select(s => new Coordinate(s)).ToList()) { }

        private RectangularArea boundingBox;
        public RectangularArea BoundingBox
        {
            get
            {
                if (boundingBox == null && this.Count > 3) // We don't have a closed polygon unless there are 4+ points
                {
                    double minX = this[0].X;
                    double minY = this[0].Y;
                    double maxX = minX;
                    double maxY = minY;
                    Coordinate pt;

                    for (int x = 1; x < this.Count; x++)
                    {
                        pt = this[x];
                        minX = System.Math.Min(pt.X, minX);
                        maxX = System.Math.Max(pt.X, maxX);
                        minY = System.Math.Min(pt.Y, minY);
                        maxY = System.Math.Max(pt.Y, maxY);
                    }

                    boundingBox = new RectangularArea(minX, minY, maxX, maxY);
                }

                return boundingBox;
            }
        }

        public UIElement UIElement(ColorPair colorpair)
        {
            if (this.Count == 0)
            {
                return null;
            }
            else if (this.Count > 1)
            {
                PathGeometry path = new PathGeometry();
                path.Figures.Add(this.PathFigure);

                System.Windows.Shapes.Path p = new System.Windows.Shapes.Path();
                p.Data = path;
                
                p.Stroke = colorpair.LineColor == null ? null : new SolidColorBrush(colorpair.LineColor.Value);
                p.Fill = colorpair.FillColor == null ? null : new SolidColorBrush(colorpair.FillColor.Value);
               
                return p;
            }
            else
            {
                var e = this.FirstPoint.UIElement(4, Colors.Black);
                return e;
            }
        }

        private void RemoveDuplicates()
        {
            int i = this.Count - 1;
            while (i > 1)
            {
                if (this[i].IsEqual(this[i-1]))
                {
                    this.RemoveAt(i);
                }
                i--;
            }
        }

        public PathFigure PathFigure
        {
            get
            {
                if (this.Count > 1)
                {
                    var pathFigure = new PathFigure();
                    pathFigure.StartPoint = this.FirstPoint.ToPoint();

                    for (int x = 1; x < this.Count; x++)
                    {
                        pathFigure.Segments.Add(new LineSegment { Point = this[x].ToPoint() });
                    }

                    return pathFigure;
                }
                else
                {
                    return null;
                }
            }
        }
        public new void Add(Coordinate point)
        {
            boundingBox = null;
            if (point != null)
            {
                Coordinate last = this.LastPoint;
                if (last == null || !last.Equals(point))
                {
                    base.Add(point);
                }
            }
        }

        public void AddAt(Point point, int index)
        {
            boundingBox = null;
            if (point != null)
            {
                this.Add(new Coordinate(0,0));
                for(int x = this.Count-1; x > index; x--)
                {
                    this[x].MoveTo(this[x - 1]);
                }
                this[index].MoveTo(point);
            }
        }

        public void ClearPoints()
        {
            boundingBox = null;
            base.Clear();
        }

        public void OpenPolygon()
        {
            Coordinate pt1 = this.FirstPoint;
            Coordinate pt2 = this.LastPoint;

            if (pt1.X == pt2.X && pt1.Y == pt2.Y)
            {
                this.RemoveAt(this.Count - 1);
            }
        }

        public bool IsClosed
        {
            get
            {
                if (this.Count < 4)
                {
                    return false; // Unless there are 4+ points, it can't be closed.
                }
                else
                {
                    Coordinate pt1 = this.FirstPoint;
                    Coordinate pt2 = this.LastPoint;
                    return pt1.IsEqual(pt2);
                }
            }
        }

        public bool ClosePolygon()
        {
            // makes sure the first point equals the last point, if possible.
            if (this.Count > 2) // Can't close a polygon unless there are at least 3 points
            {
                Coordinate pt1 = this.FirstPoint;
                Coordinate pt2 = this.LastPoint;

                if (!pt1.IsEqual(pt2)) // Only close if it is not closed already
                {
                    Coordinate point = new Coordinate(pt1.X, pt1.Y);  // create a new point = the first one and add it.
                    this.Add(point);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public Coordinate FirstPoint
        {
            get
            {
                if (this.Count > 0)
                {
                    return this[0];
                }
                else
                {
                    return null;
                }
            }
        }

        public Coordinate LastPoint
        {
            get
            {
                if (this.Count > 0)
                {
                    return this[this.Count - 1];
                }
                else
                {
                    return null;
                }
            }
        }

        public bool ContainsPoint(Coordinate point)
        {
            bool inside = false;

            if (this.IsClosed) // It's not a closed polygon unless it has at least 4 points.
            {
                if (point.X >= BoundingBox.Left && point.X <= BoundingBox.Right && point.Y >= BoundingBox.Top && point.Y <= BoundingBox.Bottom)
                {
                    Coordinate endPoint = this.LastPoint;

                    int polygonLength = this.Count, i = 0;

                    // x, y for tested point.
                    double pointX = point.X;
                    double pointY = point.Y;
                    // start / end point for the current polygon segment.
                    double startX, startY, endX, endY;
                    endX = endPoint.X;
                    endY = endPoint.Y;
                    while (i < polygonLength)
                    {
                        startX = endX;
                        startY = endY;
                        endPoint = this[i++];
                        endX = endPoint.X;
                        endY = endPoint.Y;
                        // flag toggle counting even/odd ray casting
                        inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                                  && /* if so, test if it is under the segment */
                                  ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
                    }

                }
            }
            return inside;
        }
        public override string ToString()
        {
            if (this.IsClosed)
            {
                return string.Join("|", this.Select(s => s.ToString()).ToList());
            }
            else
            {
                return "unknown";
            }
        }

        public string ToJson()
        {
            // Returns Polygon serialized to GeoJSON RFC 7946
            if (this.IsClosed)
            {
                var sb = new StringBuilder();

                bool pastfirst = true;
                sb.Append("{ \"type\": \"Polygon\", \"coordinates\": [ [");
                foreach (Coordinate c in this)
                {
                    if (pastfirst)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(c.ToString());
                }
                sb.Append("] ] }");
                return sb.ToString();
            }
            else
            {
                return string.Empty;
            }
        }

        public string ToCSharp()
        {
            RemoveDuplicates();

            var sb = new StringBuilder();
            bool isPastFirst = false;
            sb.Append(" new Polygon (");
            foreach (var item in this)
            {
                if (isPastFirst)
                {
                    sb.Append(", ");
                }
                else
                {
                    isPastFirst = true;
                }
                sb.Append(item.ToCSharp());
            }
            sb.Append(")");
            return sb.ToString();
        }
    }
}
