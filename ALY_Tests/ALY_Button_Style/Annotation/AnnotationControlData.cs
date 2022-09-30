using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Annotation
{
    public class AnnotationControlData
    {
        public int DisplayIndex { get; private set; }
        public AnnotationDataMap.Modes ControlMode { get; private set; }
        public ColorPair UnselectedColors { get; set; }
        public ColorPair SelectedColors { get; set; }

        private AnnotationDataMap.DataHierarchy DataHierarchy { get; set; }
        private Image BackgroundImage { get; set; }
        private AnnotationControlData(AnnotationDataMap.DataHierarchy DataHierarchy, Image image, int displayindex, ColorPair unselectedColors, ColorPair selectedColors)
        {
            this.UnselectedColors = unselectedColors;
            this.SelectedColors = selectedColors;

            this.ControlMode = AnnotationDataMap.Modes.DisplayMode; // Default
            this.DataHierarchy = DataHierarchy;
            this.DisplayIndex = displayindex;
            this.BackgroundImage = image;
        }

        public AnnotationControlData(IKeyedPolygonFactory factory, Image image, int displayindex) : this(new AnnotationDataMap.DataHierarchy(factory), image, displayindex, factory.DefaultUnselectedColor, factory.DefaultSelectedColor)
        { }

        public AnnotationControlData(AnnotationControlData anotherControl, Image image, int displayindex) : this(anotherControl.DataHierarchy, image, displayindex, anotherControl.UnselectedColors, anotherControl.SelectedColors)
        { }
        public int? FindPartUnderPoint(Coordinate point)
        {
            return this.DataHierarchy.FindPartUnderPoint(point);
        }

        public int? FindPolygonUnderPoint(Coordinate point)
        {
            return this.DataHierarchy.FindPolygonUnderPoint(point);
        }
        public int? ProcessLeftClick(Coordinate point, Canvas canvas, bool ForceSelectMode)
        {
            int? selectedPart = null;

            if (ForceSelectMode)
            {
                this.ControlMode = AnnotationDataMap.Modes.SelectMode;
            }
            if (this.ControlMode == AnnotationDataMap.Modes.SelectMode)
            {
                selectedPart = this.DataHierarchy.FindPartUnderPoint(point);
                this.DrawSelectionMode(selectedPart, canvas);
            }
            return selectedPart;
        }

        public List<ListItem> GetPartHeirarchy(int partid)
        {
            return this.DataHierarchy.GetPartHeirarchy(partid);
        }
      
        public void DrawSelectionMode(int? selectedPart, Canvas canvas)
        {
            this.ControlMode = AnnotationDataMap.Modes.SelectMode;
            var items = this.DataHierarchy.GetSelectModeUIElements(this.DisplayIndex, this.UnselectedColors, selectedPart, this.SelectedColors);
            Draw(items, canvas);           
        }

        public void DrawDisplayMode(int? selectedPart, Canvas canvas, Color? color)
        {
            this.ControlMode = AnnotationDataMap.Modes.DrawMode;
            var items = this.DataHierarchy.GetDrawModeUIElements(this.DisplayIndex, new Annotation.ColorPair(null, null), selectedPart, new Annotation.ColorPair(color, color));
            Draw(items, canvas);
        }

        public string PartDescription(int PartID)
        {
            var p = this.DataHierarchy.Parts.Where(w => w.ID == PartID).FirstOrDefault();
            if (p != null)
            {
                return p.Description;
            }
            else
            {
                return string.Empty;
            }
        }

        public List<int> DisplayIndexListForPart(int partid)
        {
            var result = this.DataHierarchy.PartDisplayIndexList(new List<int> { partid });
            return result;
        }

        public override string ToString()
        {
            return this.DataHierarchy.SerialData;
        }

        private void Draw(List<UIElement> items, Canvas canvas)
        {
            canvas.Children.Clear();
            foreach (var item in items)
            {
                canvas.Children.Add(item);
            }
            canvas.Children.Add(this.BackgroundImage);
        }    
    }
}
