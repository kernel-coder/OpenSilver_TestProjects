#region Usings

using System;

#endregion

namespace Virtuoso.Core.Model
{
    public class COUNTYCode : IEquatable<COUNTYCode>
    {
        public string County { set; get; }

        public string State { set; get; }

        public string CountyFIPS { set; get; }

        public override string ToString()
        {
            return County;
        }

        public bool Equals(COUNTYCode other)
        {
            //Check whether the compared object is null.
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            //Check whether the compared object references the same data.
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            //Check whether the USStates' properties are equal.
            return County.Equals(other.County);
        }
    }
}