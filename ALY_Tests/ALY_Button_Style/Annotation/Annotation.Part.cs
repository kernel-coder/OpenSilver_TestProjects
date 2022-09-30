using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Annotation
{
    public static partial class AnnotationDataMap
    {
        internal class Part 
        {
            public List<int> DisplayList { get; private set; }
            public Part(int id, string description, string serialdisplaylist, int? sortWeight)
            {
                this.ID = id;
                this.Description = description;
                this.SortWeight = sortWeight.HasValue ? sortWeight.Value : (int.MaxValue - 1) ;
                if (serialdisplaylist.Length > 0)
                {
                    this.DisplayList = serialdisplaylist.Split(',').Select(s => int.Parse(s)).ToList();
                }
                else
                {
                    this.DisplayList = new List<int>();
                }
            }
            public int ID { get; protected set; }

            public string Description { get; protected set; }

            public int SortWeight { get; protected set; }

            public override string ToString()
            {
                var sb = new StringBuilder("dataHeirarchy.AddPart(");
                sb.Append(this.ID);
                sb.Append(",\"");
                sb.Append(this.Description);
                sb.Append("\",\"");
                string comma = string.Empty;
                foreach (var s in this.DisplayList)
                {
                    sb.Append(comma);
                    sb.Append(s);
                    comma = ",";
                }
                sb.Append("\");");         
                return sb.ToString();
            }

            internal Part Clone()
            {
                return new Part(this.ID, this.Description, string.Join(",", this.DisplayList), this.SortWeight);
            }
        }
    }
}
