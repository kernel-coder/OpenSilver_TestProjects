namespace Annotation
{
    public class ColoredPartId 
    {
        public ColoredPartId (int partId, ColorPair colorpair) 
        {
            this.PartId = partId;
            this.PartColor = colorpair;
        }
        public int PartId { get; protected set; }

        public ColorPair PartColor { get; protected set; }
    }
}
