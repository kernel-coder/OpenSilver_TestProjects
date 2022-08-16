#region Usings

using System;
using System.Reflection;

#endregion

namespace Virtuoso.Core.Framework
{
    public class PropertyChange<TT2>
    {
        public PropertyInfo PropertyDetails { get; private set; }
        public TT2 OrigValue { get; private set; }
        public TT2 NewValue { get; private set; }
        public string Location { get; private set; }

        public PropertyChange(PropertyInfo propertyDetails, TT2 origValue, TT2 newValue, string location = "")
        {
            if (propertyDetails == null)
            {
                throw new ArgumentException("PropertyDetails");
            }

            PropertyDetails = propertyDetails;
            OrigValue = origValue;
            NewValue = newValue;
            Location = location;
        }
    }
}