#region Usings

using System;

#endregion

namespace Virtuoso.Core.Utility
{
    public class GuidCombGenerator
    {
        /// <summary>
        /// Generate a new <see cref="Guid"/> using the comb algorithm.
        /// </summary>
        /// <param name="session">The <see cref="ISessionImplementor"/> this id is being generated in.</param>
        /// <param name="obj">The entity for which the id is being generated.</param>
        /// <returns>The new identifier as a <see cref="Guid"/>.</returns>
        public object Generate()
        {
            return GenerateComb();
        }

        /// <summary>
        /// Generate a new <see cref="Guid"/> using the comb algorithm.
        /// </summary>
        public static Guid GenerateComb()
        {
            byte[] guidArray = Guid.NewGuid().ToByteArray();

            DateTime baseDate = new DateTime(1900, 1, 1);
            DateTime now = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

            // Get the days and milliseconds which will be used to build the byte string 
            TimeSpan days = new TimeSpan(now.Ticks - baseDate.Ticks);
            TimeSpan msecs = now.TimeOfDay;

            // Convert to a byte array 
            // Note that SQL Server is accurate to 1/300th of a millisecond so we divide by 3.333333 
            byte[] daysArray = BitConverter.GetBytes(days.Days);
            byte[] msecsArray = BitConverter.GetBytes((long)(msecs.TotalMilliseconds / 3.333333));

            // Reverse the bytes to match SQL Servers ordering 
            Array.Reverse(daysArray);
            Array.Reverse(msecsArray);

            // Copy the bytes into the guid 
            Array.Copy(daysArray, daysArray.Length - 2, guidArray, guidArray.Length - 6, 2);
            Array.Copy(msecsArray, msecsArray.Length - 4, guidArray, guidArray.Length - 4, 4);

            return new Guid(guidArray);
        }
    }
}