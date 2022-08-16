namespace Virtuoso.Portable.OpenSilver.Utility
{
   using ProtoBuf;
   using System;
   using System.Collections.Generic;
   using System.Text;

   [ProtoContract(SkipConstructor = true)]
   public class DateTimeOffsetSurrogate
   {
      [ProtoMember(1)]
      public DateTime DateTime { get; set; }

      [ProtoMember(2)]
      public TimeSpan Offset { get; set; }

      public static implicit operator DateTimeOffsetSurrogate(DateTimeOffset dateTimeOffset)
      {
         var surrogate = new DateTimeOffsetSurrogate()
         {
            DateTime = dateTimeOffset.DateTime,
            Offset = dateTimeOffset.Offset
         };

         return surrogate;
      }

      public static implicit operator DateTimeOffset(DateTimeOffsetSurrogate surrogate)
      {
         var dateTimeOffset = new DateTimeOffset(surrogate.DateTime, surrogate.Offset);
         return dateTimeOffset;
      }
   }
}
