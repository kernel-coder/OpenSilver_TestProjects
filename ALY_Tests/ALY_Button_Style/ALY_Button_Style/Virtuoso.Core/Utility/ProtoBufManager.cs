namespace Virtuoso.Core.Utility
{
   using ProtoBuf.Meta;
   using System;
   using System.Linq;
   using System.Reflection;
   using System.Text;
   using Virtuoso.Portable.OpenSilver.Utility;
   using Virtuoso.Services.Core.Model;

   public class ProtoBufManager
   {
      public static void Initialize()
      {
         RuntimeTypeModel.Default.AutoAddProtoContractTypesOnly = true;

         RuntimeTypeModel.Default[typeof(DateTimeOffset)].SetSurrogate(typeof(DateTimeOffsetSurrogate));
         RuntimeTypeModel.Default[typeof(User)].Add(
                   nameof(User.MemberID),
                   nameof(User.UserName),
                   nameof(User.Name),
                   nameof(User.SessionID),
                   nameof(User.TenantID),
                   nameof(User.Name),
                   nameof(User.Roles)
               );
      }
   }
}
