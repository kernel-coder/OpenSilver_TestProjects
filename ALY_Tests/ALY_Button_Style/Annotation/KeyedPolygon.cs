namespace Annotation
{
   
    public class KeyedPolygon
    {
        public KeyedPolygon() { }
        public KeyedPolygon(int polygonID, int drawingNumber, Polygon polygon)
        {
            this.PolygonID = polygonID;
            this.DrawingNumber = drawingNumber;
            this.Polygon = polygon;
        }

        public int PolygonID { get; set; }
        public int DrawingNumber { get; set; }
        public Polygon Polygon { get; set; }

        public bool ContainsPoint(Coordinate point)
        {
            if (this.Polygon != null)
            {
                return this.Polygon.ContainsPoint(point);
            }
            else
            {
                return false;
            }
        }
        
    }
}
