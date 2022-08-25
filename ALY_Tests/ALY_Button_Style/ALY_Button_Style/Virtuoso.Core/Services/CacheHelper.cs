#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Virtuoso.Client.Infrastructure.Storage;

#endregion

namespace Virtuoso.Core.Services
{
    public static class CacheHelper
    {
        public static async Task<List<T>> Load<T>(string directory, string cachename)
        {
            try
            {
                var ret = new List<T>();

                var files = (await VirtuosoStorageContext.Current.EnumerateFiles(directory))
                    .Where(key => key.Name.StartsWith(cachename + "Cache", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(f => f.Name); // Used to be last write time. Name should work as well

                var file = files.FirstOrDefault();
                if (file != null)
                {
                    var data = await VirtuosoStorageContext.Current.ReadAsBytes(file.FullName);
                    using (var stream = new System.IO.MemoryStream(data))
                    {
                        var serializer = new DataContractSerializer(typeof(List<T>));
                        ret = (List<T>)serializer.ReadObject(stream);
                    }
                }

                return ret;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(
                    String.Format("LoadCacheFromFile failure for cache: {0}.  Exception: {1}", cachename,
                        e.ToString()));
                throw;
            }
        }

        public static async Task Save<T>(List<T> items, string fullPathToCacheName)
        {
            try
            {
                using (var stream = new System.IO.MemoryStream())
                {
                    var serializer = new DataContractSerializer(typeof(List<T>));
                    serializer.WriteObject(stream, items);
                    stream.Flush();
                    await VirtuosoStorageContext.Current.WriteToFile(fullPathToCacheName, stream.ToArray());
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(String.Format("Save failure for cache: {0}.  Exception: {1}",
                    fullPathToCacheName, e.ToString()));
                throw;
            }
        }
    }
}