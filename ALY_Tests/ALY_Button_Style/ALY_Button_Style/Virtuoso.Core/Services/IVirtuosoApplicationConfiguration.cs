#region Usings

using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IVirtuosoApplicationConfiguration
    {
        bool ApplicationInitialized { get; set; }
        string HomeScreenView { get; set; }
        string LastLogin { get; set; }
        string SessionID { get; }
        TenantSetting Setting { get; }
        string VirtuosoVersion { get; set; }
    }
}