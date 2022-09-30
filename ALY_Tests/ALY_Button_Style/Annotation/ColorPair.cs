using System.Windows.Media;

namespace Annotation
{
    public class ColorPair
    {
        public ColorPair(Color? lineColor, Color? fillColor)
        {
            this.LineColor = lineColor;
            this.FillColor = fillColor;
        }
        public Color? LineColor { get; protected set; }
        public Color? FillColor { get; protected set; }    
    }
}
