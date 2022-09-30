using Annotation;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Diagnostics;
using System.Linq;
using System.Windows.Media;

namespace Virtuoso.Core.Controls.AniMan
{
    public class AniManForWoundLocation
    {
        public class SubDrawingDisplays
        {
            public bool AniManLeftDetailShown { get; set; }          
          
            public bool AniManRightDetailShown { get; set; }          
        }

        public Annotation.AnnotationControlData AniManCtrl1 { get; private set; }
        public Annotation.AnnotationControlData AniManCtrl2 { get; private set; }
        public Annotation.AnnotationControlData AniManCtrl3 { get; private set; }
        public Annotation.AnnotationControlData AniManCtrl4 { get; private set; }
        public Annotation.AnnotationControlData AniManCtrl5 { get; private set; }
        public Annotation.AnnotationControlData AniManCtrl6 { get; private set; }
        public Annotation.AnnotationControlData AniManCtrl7 { get; private set; }

        public int? SelectedPart { get; set; }

        public AniManForWoundLocation(Image Image1, Image Image2, Image Image3, Image Image4, Image Image5, Image Image6, Image Image7 )
        {
            var mainfactory = new AnimanPolygonFactory();
            this.AniManCtrl1 = new Annotation.AnnotationControlData(mainfactory, Image1, 1);
            this.AniManCtrl2 = new Annotation.AnnotationControlData(AniManCtrl1, Image2, 2);
            this.AniManCtrl3 = new Annotation.AnnotationControlData(AniManCtrl1, Image3, 3);
            this.AniManCtrl4 = new Annotation.AnnotationControlData(AniManCtrl1, Image4, 4);
            this.AniManCtrl5 = new Annotation.AnnotationControlData(AniManCtrl1, Image5, 5);
            this.AniManCtrl6 = new Annotation.AnnotationControlData(AniManCtrl1, Image6, 6);
            this.AniManCtrl7 = new Annotation.AnnotationControlData(AniManCtrl1, Image7, 7);
        }

        [Conditional("DEBUG")]
        public void ToSQL()
        {
            var s = this.AniManCtrl1.ToString();

            s = s.Replace("\"", "'");
            s = s.Replace("dataHeirarchy.AddPart", "INSERT INTO [dbo].[WoundLocation] (WoundLocationKey,Description,DisplayPolygonList,UpdatedDate,SortWeight) VALUES ");
            s = s.Replace("dataHeirarchy.AddPolygon", "INSERT INTO [dbo].[WoundPolygon] ([WoundPolygonKey],[WoundLocationKey],[DisplayIndex],[PolygonData],[SelectWoundLocationList],[UpdatedDate]) VALUES ");
            s = s.Replace(");", ",GetDate());");
            s = s.Replace(",,", ",null,");
            s = s.Replace("\r\n", ((char)13).ToString());

            var f = s.Split((char)13).Select(x => x).ToList();

            foreach (var l in f)
            {
                Debug.WriteLine(l);
            }
        }

        public SubDrawingDisplays DisplayDetailBasedOnSelectedPart(Color color, Canvas AniManCanvas2, Canvas AniManCanvas3, Canvas AniManCanvas4, Canvas AniManCanvas5, Canvas AniManCanvas6, Canvas AniManCanvas7)
        {
            var displayIndexList = this.SelectedPart.HasValue ? this.AniManCtrl1.DisplayIndexListForPart(this.SelectedPart.Value) : new List<int>();
            bool ll = this.ConditionallyDisplay(displayIndexList, 2, this.AniManCtrl2, AniManCanvas2, color);
            bool rl  = this.ConditionallyDisplay(displayIndexList, 3, this.AniManCtrl3, AniManCanvas3, color);
            bool rm = this.ConditionallyDisplay(displayIndexList, 4, this.AniManCtrl4, AniManCanvas4, color);
            bool lm = this.ConditionallyDisplay(displayIndexList, 5, this.AniManCtrl5, AniManCanvas5, color);

            bool ld = this.ConditionallyDisplay(displayIndexList, 6, this.AniManCtrl6, AniManCanvas6, color);
            bool rd = this.ConditionallyDisplay(displayIndexList, 7, this.AniManCtrl7, AniManCanvas7, color);

            var result = new SubDrawingDisplays { AniManLeftDetailShown = ll || lm || ld, AniManRightDetailShown = rl || rm || rd };
            return result;
        }
        public int? FindPartUnderPoint(Coordinate point)
        {
            var partID = this.AniManCtrl1.FindPartUnderPoint(point);
            return partID;
        }

        public int? FindPolygonUnderPoint(Coordinate point)
        {
            var polygonID = this.AniManCtrl1.FindPolygonUnderPoint(point);
            return polygonID;
        }

        public int? AnimanLeftClick(Coordinate point, Canvas AniManCanvas, bool ForceSelectMode)
        {
            var s = this.AniManCtrl1.ProcessLeftClick(point, AniManCanvas, ForceSelectMode);
            return s;
        }
        
        private bool ConditionallyDisplay(List<int> displayIndexList, int displayindex, AnnotationControlData data, Canvas canvas, System.Windows.Media.Color? color)
        {
            if (displayIndexList.Contains(displayindex))
            {
                data.DrawDisplayMode(this.SelectedPart, canvas, color);
                return true;
            }
            else
            {
                data.DrawDisplayMode(null, canvas, color);
                return false;
            }
        }

        public string PartDescription(int partID, int? polygonID)
        {
            string result = this.AniManCtrl1.PartDescription(partID);
#if DEBUG                
            result += " [" + partID.ToString() + "]";
            if (polygonID.HasValue && polygonID.Value != partID)
            {
                result += " [" + polygonID.Value.ToString() + "]";
            }
#endif                
            return result;
        }

    }
}
