#region Usings

using System;
using Virtuoso.Core.Interface;

#endregion

namespace Virtuoso.Core.Model
{
    public class USAState : IUSAState, IEquatable<USAState>
    {
        public USAState(string stateCode, string stateName)
        {
            StateCode = stateCode;
            StateName = stateName;
        }

        public string StateCode { get; set; }
        public string StateName { get; set; }
        public string StateCodeName => StateCode + " - " + StateName;

        public string StateNameCode => StateName + " - " + StateCode;

        public bool Equals(USAState otherUSState)
        {
            //Check whether the compared object is null.
            if (ReferenceEquals(otherUSState, null))
            {
                return false;
            }

            //Check whether the compared object references the same data.
            if (ReferenceEquals(this, otherUSState))
            {
                return true;
            }

            //Check whether the USStates' properties are equal.
            return StateCode.Equals(otherUSState.StateCode);
        }

        public override int GetHashCode()
        {
            //Get hash code for the State field if it is not null.
            int hashProductName = StateCode == null ? 0 : StateCode.GetHashCode();

            //Calculate the hash code for the product.
            return hashProductName;
        }
    }
}