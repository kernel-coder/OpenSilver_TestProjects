using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Annotation
{
    public static partial class AnnotationDataMap
    {
        internal class Drawable : KeyedPolygon
        {  
            public int? PartID { get; private set; }  
            public List<int> SelectList { get; private set; }

            public Drawable(int polygonID, int? partid, int drawingNumber, string serialpolygondata, string serialselectdata) : base(polygonID, drawingNumber, new Polygon(serialpolygondata))
            {
                this.PolygonID = polygonID;
                this.PartID = partid;
                this.DrawingNumber = drawingNumber;

                if (serialselectdata.Length > 0)
                {
                    this.SelectList = serialselectdata.Split(',').Select(s => int.Parse(s)).ToList();
                }
                else
                {
                    this.SelectList = new List<int>();
                }             
            }

            public bool IsShownInSelectMode
            {
                get
                {
                    return this.DrawingNumber == 1;
                }
            }

            public bool HasSelectableParts
            {
                get
                {
                    return this.SelectList.Any() == true;
                }
            }

            public override string ToString()
            {
                var sb = new StringBuilder("dataHeirarchy.AddPolygon(");
                sb.Append(this.PolygonID);
                sb.Append(",");
                sb.Append(this.PartID);
                sb.Append(",");
                sb.Append(this.DrawingNumber);
                sb.Append(",\"");
                sb.Append(this.Polygon.ToString());
                sb.Append("\",\"");
                string comma = string.Empty;
                foreach (var s in this.SelectList)
                {
                    sb.Append(comma);
                    sb.Append(s);
                    comma = ",";
                }
                sb.Append("\");");
                return sb.ToString();
            }
        }
    }
}
