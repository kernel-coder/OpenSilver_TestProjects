using System.Windows.Media;
using Annotation;
//using Virtuoso.Core.Cache;
using System.Diagnostics;

namespace Virtuoso.Core.Controls.AniMan
{
    internal class AnimanPolygonFactory : IKeyedPolygonFactory
    {
        public AnimanPolygonFactory() { }

        public void FillDataHeirarchy(IDataHierarchy dataHeirarchy)
        {
            //var x = WoundLocationCache.Current.GetWoundLocations();
            //foreach (var item in x)
            //{
            //    dataHeirarchy.AddPart(item.WoundLocationKey, item.Description, item.DisplayPolygonList, item.SortWeight);

            //    foreach (var p in item.WoundPolygon)
            //    {
            //        dataHeirarchy.AddPolygon(p.WoundPolygonKey, item.WoundLocationKey, p.DisplayIndex, p.PolygonData, p.SelectWoundLocationList);
            //    }
            //}
        }

        public ColorPair DefaultUnselectedColor
        {
            get
            {
                return new ColorPair(Colors.LightGray, null);
            }
        }

        public ColorPair DefaultSelectedColor
        {
            get
            {
                return new ColorPair(Colors.Black, Colors.Orange);
            }
        }        
    }
}
