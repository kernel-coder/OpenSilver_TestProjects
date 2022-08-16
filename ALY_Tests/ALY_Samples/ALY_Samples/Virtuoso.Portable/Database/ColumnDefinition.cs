namespace Virtuoso.Portable.Database
{
    public class ColumnDefinition
    {
        public ColumnDefinition(int position, int width)
        {
            Position = position;
            Width = width;
        }
        public int Position { get; internal set; }
        public int Width { get; internal set; }
    }
}
