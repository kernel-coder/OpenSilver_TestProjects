using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;

namespace Annotation
{
    public static partial  class AnnotationDataMap
    {
        internal class DataHierarchy : IDataHierarchy
        {
            private class WeightedColoredPart
            {
                public Part Part { get; private set; }
                public ColorPair PartColor { get; private set; }
                public int Depth { get; private set; }

                public WeightedColoredPart(ColoredPartId coloredPartId, Part part, int depth)
                {
                    this.Part = part;
                    this.PartColor = coloredPartId.PartColor;
                    this.Depth = depth;
                }
            }

            private IKeyedPolygonFactory Factory { get; set; }

            public DataHierarchy(IKeyedPolygonFactory factory)
            {
                this.Factory = factory;
                this.Clear();
                this.Factory.FillDataHeirarchy(this);

                // Serialize Data If Needed Here
                string s = this.SerialData;
                Debug.WriteLine(s);
            }

            internal List<Drawable> Drawables { get; set; }

            internal List<Part> Parts { get; set; }

            public void Clear()
            {
                this.Drawables = new List<Drawable>();
                this.Parts = new List<Part>();
            }

            public string SerialData
            {
                get
                {
                    var sb = new StringBuilder();

                    foreach (var p in this.Parts)
                    {
                        sb.AppendLine(p.ToString());
                    }

                    foreach (var d in this.Drawables)
                    {
                        sb.AppendLine(d.ToString());
                    }
     
                    return sb.ToString();
                }             
            }
            
            
            private List<Drawable> SelectableDrawables
            {
                get
                {
                    return this.Drawables.Where(d => d.HasSelectableParts).ToList();
                }
            }

            internal int? FindPartUnderPoint(Coordinate point)
            {
                var partIds = PolygonsContainingPoint(this.SelectableDrawables, point).Where(w => w.PartID.HasValue).Select(s => s.PartID.Value).Distinct().ToList();               
                if (partIds != null && partIds.Any() == true)
                {
                    return partIds.FirstOrDefault();                
                }
                else
                {
                    return null;
                }
            }

            internal int? FindPolygonUnderPoint(Coordinate point)
            {
                var polygonIDs = PolygonsContainingPoint(this.SelectableDrawables, point).Select(s => s.PolygonID).Distinct().ToList();
                if (polygonIDs != null && polygonIDs.Any() == true)
                {
                    return polygonIDs.FirstOrDefault();
                }
                else
                {
                    return null;
                }
            }

            private static List<Drawable> PolygonsContainingPoint(List<Drawable> SearchSet, Coordinate point)
            {
                var Drawables = new List<Drawable>();

                foreach (Drawable item in SearchSet)
                {
                    if (item.ContainsPoint(point))
                    {
                        Drawables.Add(item);
                    }
                }
                return Drawables;
            }
            
            //internal List<int> SelectableDisplayIndexList()
            //{
            //    return this.SelectableDrawables.Select(x => x.DrawingNumber).Distinct().ToList();
            //}

            internal List<int> PartDisplayIndexList(List<int> PartIds)
            {
                var drawableIds = new List<int>();
                
                foreach (var id in PartIds)
                {
                    var p = this.Parts.Where(w => w.ID == id).FirstOrDefault();
                    if (p != null)
                    {
                        drawableIds.AddRange(p.DisplayList);
                    }                  
                }
                var result = this.Drawables.Where(w => drawableIds.Contains(w.PolygonID)).Select(s => s.DrawingNumber).Distinct().ToList();
                return result;
            }

            internal List<int> DrawingDisplayIndexList(Drawing drawing)
            {
                return PartDisplayIndexList(drawing.ToPartIds());
            }

            internal List<UIElement> ConvertDrawablesToUIElements(List<Drawable> drawables, ColorPair colorpair)
            {
                var result = new List<UIElement>();
                foreach (Drawable d in drawables)
                {
                    result.Add(d.Polygon.UIElement(colorpair));
                }
                return result;
            }

            internal List<UIElement> ConvertDrawingToUIElements(Drawing drawing, int displayindex)
            {
                var result = new List<UIElement>();
                var weightedList = new List<WeightedColoredPart>();

                foreach (var item in drawing)
                {
                    Part p = this.Parts.Where(w => w.ID == item.PartId).FirstOrDefault();

                    if (p != null)
                    {
                        var single = new WeightedColoredPart(item, p, p.DisplayList.Count);
                        weightedList.Add(single);
                    }
                }

                var orderedlist = weightedList.OrderBy(o => o.Depth).ThenBy(t => t.Part.ID).ToList();

                foreach (var item in orderedlist)
                {
                    var drawableids = item.Part.DisplayList.ToList();
                   
                    var drawablesinset = this.Drawables.Where(w => drawableids.Contains(w.PolygonID) && w.DrawingNumber == displayindex).ToList();

                    result.AddRange(ConvertDrawablesToUIElements(drawablesinset, item.PartColor));
                }

                return result;
            }

            private List<UIElement> GetUIElements(List<Drawable> drawables, ColorPair unselectedColors, int? selectedPart, ColorPair selectedColors, Modes mode)
            {
                var drawableidsforPart = new List<int>();

                if (selectedPart.HasValue)
                {
                    var p = this.Parts.Where(w => w.ID == selectedPart.Value).FirstOrDefault();
                    if (p != null)
                    {
                        drawableidsforPart = p.DisplayList;
                    }
                }

                var result = new List<UIElement>();
                foreach (Drawable d in drawables)
                {
                    if (!drawableidsforPart.Contains(d.PolygonID))
                    {
                        result.Add(d.Polygon.UIElement(unselectedColors));
                    }
                }

                foreach (Drawable d in drawables)
                {
                    if (drawableidsforPart.Contains(d.PolygonID))
                    {
                        result.Add(d.Polygon.UIElement(selectedColors));
                    }
                }

                return result;
            }

            internal List<UIElement> GetDrawModeUIElements(int displayindex, ColorPair unselectedColors, int? selectedPart, ColorPair selectedColors)
            {
                var drawables = this.Drawables.Where(w => w.DrawingNumber == displayindex).ToList();
                return GetUIElements(drawables, unselectedColors, selectedPart, selectedColors, Modes.DrawMode);
            }

            internal List<UIElement> GetSelectModeUIElements(int displayindex, ColorPair unselectedColors, int? selectedPart, ColorPair selectedColors)
            {
                var drawables = this.Drawables.Where(d => d.HasSelectableParts && d.DrawingNumber == displayindex).ToList();
                return GetUIElements(drawables, unselectedColors, selectedPart, selectedColors, Modes.SelectMode);                
            }
            private class LIPair
            {
                public LIPair(int id, string description, bool selected, int weight)
                {
                    this.listitem = new ListItem(id, description, selected);
                    this.sortWeight = weight;

                    string sansLeft = description.Replace("Left ", "");
                    string sansRight = description.Replace("Right ", "");
                    if (sansLeft != description)
                    {
                        this.shortsort = sansLeft + " 1";
                    }
                    else if (sansRight != description)
                    {
                        this.shortsort = sansRight + " 2";
                    }
                    else
                    {
                        this.shortsort = description;
                    }                  
                }

                public ListItem listitem { get; private set; }
                public int sortWeight { get; private set; }
                public string shortsort { get; private set; }
            }

            internal List<ListItem> GetPartHeirarchy(int partid)
            {              
                // List<AnnotationPartPOCO>
                var result = new List<LIPair>();

                // WalkParents
                Part selected = this.Parts.Where(w => w.ID == partid).FirstOrDefault();

                if (selected != null)
                {
                    List<int> OptionsForSelect = this.Drawables.Where(w => w.PartID == selected.ID).SelectMany(s => s.SelectList).ToList();

                    foreach (var item in OptionsForSelect)
                    {
                        Part option = this.Parts.Where(w => w.ID == item).FirstOrDefault();
                        if (option != null && !result.Where(w => w.listitem.Id == option.ID).Any())
                        {
                            string desc = option.Description;
#if DEBUG
                            desc += " [" + option.ID.ToString() + "]";
#endif       
                            LIPair i = new LIPair(option.ID, desc, option.ID == selected.ID, option.SortWeight);
                            result.Add(i);
                        }
                    }                    
                }

                var final = result.OrderBy(o => o.sortWeight).ThenBy(t => t.shortsort).Select(s => s.listitem).ToList();
                return final;
            }

            public void AddPart(int id, string description, string serialdisplaypolygons, int? sortWeight)
            {
                if (!this.Parts.Where(w => w.ID == id).Any())
                {
                    var p = new Part(id, description, serialdisplaypolygons, sortWeight);
                    this.Parts.Add(p);
                }
            }

            public void AddPolygon(int id, int? partid, int displayindex, string serialpolygonpoints, string serialselectlist)
            {
                if (!this.Drawables.Where(w => w.PolygonID == id).Any())
                {
                    var d = new Drawable(id, partid, displayindex, serialpolygonpoints, serialselectlist);
                    this.Drawables.Add(d);
                }
            }

        }
    }
}
