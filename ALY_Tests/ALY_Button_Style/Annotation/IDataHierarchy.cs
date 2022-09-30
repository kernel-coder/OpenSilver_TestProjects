namespace Annotation
{
    public interface IDataHierarchy
    {
        void AddPart(int id, string description, string serialdisplaypolygons, int? sortWeight);
        void AddPolygon(int id, int? partid, int displayindex, string serialpolygonpoints, string serialselectlist);
    }
}
