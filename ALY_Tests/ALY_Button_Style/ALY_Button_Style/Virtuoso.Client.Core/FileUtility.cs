#region Usings

using System;
using System.Threading.Tasks;
using Virtuoso.Client.Infrastructure.Storage;

#endregion

namespace Virtuoso.Client.Core
{
    public class FileUtility
    {
        public static async Task DeleteFilesInFolder(string folderName, bool enumerateDirectories = true,
            Func<StorageItem, bool> fileSearchPattern = null)
        {
            if (fileSearchPattern == null)
            {
                fileSearchPattern = item => true;
            }

            foreach (var file in await VirtuosoStorageContext.Current.EnumerateFiles(folderName))
            {
                if (!fileSearchPattern(file))
                {
                    continue;
                }

                try
                {
                    System.Diagnostics.Debug.WriteLine(file + "will be deleted");
                    await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
                }
                catch (Exception e)
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine(
                        String.Format("File delete failed for file: {0}.  Exception: {1}", file, e.ToString()));
#endif
                }
            }

            if (enumerateDirectories)
            {
                foreach (var directory in await VirtuosoStorageContext.Current.EnumerateDirectories(folderName))
                    try
                    {
                        System.Diagnostics.Debug.WriteLine(directory + "will be deleted");
                        await VirtuosoStorageContext.Current.DeleteDirectory(directory.FullName);
                    }
                    catch (Exception e)
                    {
#if DEBUG
                        System.Diagnostics.Debug.WriteLine(String.Format(
                            "Directory delete failed for file: {0}.  Exception: {1}", directory, e.ToString()));
#endif
                    }
            }
        }
    }
}