#region Usings

using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ria.Sync.Occasional;

#endregion

namespace Virtuoso.Client.Core.Storage
{
    public class EncryptedFileStore : FileStore
    {
        public static string EncryptionKey { get; internal set; }

        static EncryptedFileStore()
        {
            SHA256Managed sha = new SHA256Managed();
            byte[] hash =
                sha.ComputeHash(
                    UTF8Encoding.UTF8.GetBytes(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)));

            // No need to encrypt on browser client, since it's already clear across the wire
#if !OPENSILVER
            EncryptionKey = Convert.ToBase64String(hash);
#endif
        }

        public EncryptedFileStore(string fileName)
            : base(fileName)
        {
        }

        public override async Task Save(string data)
        {
#if DEBUG
            await FileStorageUtil.SaveData(FileName, data, string.Empty);
#else
            await FileStorageUtil.SaveData(FileName, data, EncryptionKey);
#endif
        }

        public override async Task<string> Load()
        {
#if DEBUG
            return await FileStorageUtil.LoadData(FileName, string.Empty);
#else
            return await FileStorageUtil.LoadData(FileName, EncryptionKey);
#endif
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
        public override void SaveSync(string data)
        {
#if DEBUG
            FileStorageUtil.SaveDataSync(FileName, data, string.Empty);
#else
            FileStorageUtil.SaveDataSync(FileName, data, EncryptionKey);
#endif
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
        public override string LoadSync()
        {
#if DEBUG
            return FileStorageUtil.LoadDataSync(FileName, string.Empty);
#else
            return FileStorageUtil.LoadDataSync(FileName, EncryptionKey);
#endif
        }
#endif
    }
}