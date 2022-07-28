#region Usings

using System;
using GalaSoft.MvvmLight;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IAgencyParameterService : IModelDataService<TenantSetting>, ICleanup
    {
        void UpdateAutoTrackingGroups(TenantSetting TenantSetting, object sender);
        void TestAzureConnection(string ExternalAuthenticationTenantID, string ExternalAuthenticationApplicationID, string ExternalAuthenticationSecret, bool IsDecrypted);
        event Action<InvokeOperation<string>> TestAzureConnectionReturned;
    }
}