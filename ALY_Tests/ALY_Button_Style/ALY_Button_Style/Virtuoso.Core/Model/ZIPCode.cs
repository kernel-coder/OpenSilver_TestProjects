#region Usings

using System;
using Virtuoso.Core.Interface;

#endregion

namespace Virtuoso.Core.Model
{
    public class ZIPCode : IZIPCode, IEquatable<ZIPCode>
    {
        public string ZipCode { get; set; }

        public string City { get; set; }

        public string State { get; set; }

        public string County { get; set; }

        public string ZipCodeCityState => string.Format("{0} - {1} - {2} - {3}", ZipCode, State, City, County);

        public bool Equals(ZIPCode otherZipCode)
        {
            //Check whether the compared object is null.
            if (ReferenceEquals(otherZipCode, null))
            {
                return false;
            }

            //Check whether the compared object references the same data.
            if (ReferenceEquals(this, otherZipCode))
            {
                return true;
            }

            //Check whether the USStates' properties are equal.
            return ZipCode.Equals(otherZipCode.ZipCode);
        }

        public override int GetHashCode()
        {
            //Get hash code for the State field if it is not null.
            int hashProductName = ZipCode == null ? 0 : ZipCode.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductName;
        }

        public override string ToString()
        {
            return ZipCodeCityState;
        }
    }
}