using System.Collections.Generic;
using System.Linq;

namespace Annotation
{
    public class Drawing : List<ColoredPartId>
    {
        public List<int> ToPartIds()
        {
            return this.Select(s => s.PartId).ToList();
        }

        public void Add(int partId, ColorPair colorpair)
        {
            var result = new ColoredPartId(partId, colorpair);
            if (result == null)
            {
                var x = result;
            }
            else
            {
                this.Add(result);
            }
        }
    }
}
