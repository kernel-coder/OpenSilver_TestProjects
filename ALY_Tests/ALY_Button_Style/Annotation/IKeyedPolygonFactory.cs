namespace Annotation
{
    public interface IKeyedPolygonFactory
    {
        void FillDataHeirarchy(IDataHierarchy dataHeirarchy);

        ColorPair DefaultUnselectedColor { get; }
        ColorPair DefaultSelectedColor { get; }
    }
}
