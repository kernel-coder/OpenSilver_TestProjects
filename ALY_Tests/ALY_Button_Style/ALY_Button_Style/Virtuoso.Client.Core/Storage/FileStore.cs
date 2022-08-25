#region Usings

using System.Threading.Tasks;
using System.Windows.Ria.Sync.Occasional;

#endregion

namespace Virtuoso.Client.Core.Storage
{
    public class FileStore : IStore
    {
        protected string FileName { get; set; }

        public FileStore(string fileName)
        {
            FileName = fileName;
        }

        public virtual async Task Save(string data)
        {
            await FileStorageUtil.SaveData(FileName, data, string.Empty);
        }

        public virtual async Task<string> Load()
        {
            return await FileStorageUtil.LoadData(FileName, string.Empty);
        }

#if !OPENSILVER
        /// <summary>
        /// *** DO NOT REFERENCE THIS FUNCTION ***
        ///
        /// This is only used by offline authentication and will eventually go away
        /// when Silverlight is deprecated. We don't want to abstract this since
        /// it will not be used in Blazor / OpenSilver.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public virtual void SaveSync(string data)
        {
            FileStorageUtil.SaveDataSync(FileName, data, string.Empty);
        }

        /// <summary>
        /// *** DO NOT REFERENCE THIS FUNCTION ***
        ///
        /// This is only used by offline authentication and will eventually go away
        /// when Silverlight is deprecated. We don't want to abstract this since
        /// it will not be used in Blazor / OpenSilver.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public virtual string LoadSync()
        {
            return FileStorageUtil.LoadDataSync(FileName, string.Empty);
        }
#endif
    }
}