#region Usings

using System.Linq;
using System.Threading.Tasks;
using Virtuoso.Client.Core;
using Virtuoso.Client.Infrastructure.Storage;

#endregion

namespace Virtuoso.Core.Database
{
    public class DatabaseExtensions
    {
        public static async Task RemovePriorDiskVersions(string directoryPrefix, string storageName,
            bool delete_current_version_only = false)
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //Remove OLD Siaqodb databases in OOB directory location
            var oobDirectories =
                (await VirtuosoStorageContext.Current.EnumerateDirectories( //FYI: only works for TrustedApplications
                    ApplicationStoreInfo.GetUserStoreForApplication()))
                .Where(dir => dir.Name.StartsWith(directoryPrefix, System.StringComparison.OrdinalIgnoreCase));

            foreach (var dirname in from f in oobDirectories
                     where f.Name.Equals(storageName) == delete_current_version_only
                     select f)
                //FYI: dirname = "C:\\Users\\{login}\\Documents\\{application name}\\{tenant}\\ICDCM9DBS009"
                await VirtuosoStorageContext.Current.DeleteDirectory(dirname.FullName);
        }
    }
}