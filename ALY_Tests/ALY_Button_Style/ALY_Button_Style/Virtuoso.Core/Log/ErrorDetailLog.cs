#region Usings

using System;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Virtuoso.Client.Core;
using Virtuoso.Client.Offline;
using Virtuoso.Client.Infrastructure.Storage;
using Virtuoso.Client.Utils;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

#endregion

namespace Virtuoso.Core.Log
{
    public class ErrorDetailLog
    {
        public VirtuosoDomainContext Context;

        public ErrorDetailLog()
        {
            Context = new VirtuosoDomainContext();
        }

        // Get any logs that were written while offline
        public async System.Threading.Tasks.Task LoadFromDisk()
        {
            if (EntityManager.Current.IsOnline)
            {
                var folder = ApplicationStoreInfo.ApplicationStoreErrorDetailLogs;

                foreach (var file in (await VirtuosoStorageContext.Current.EnumerateFiles(folder)))
                {
                    string xml = await VirtuosoStorageContext.Current.Read(file.FullName);
                    var errorDetail = Deserialize<ErrorDetail>(xml);
                    Add(errorDetail);
                }

                // clean up
                foreach (var file in await VirtuosoStorageContext.Current.EnumerateFiles(folder))
                    await VirtuosoStorageContext.Current.DeleteFile(file.FullName);
            }
        }

        // Save one to disk
        public void SaveToDisk(ErrorDetail errorDetail)
        {
            // Serialize the object and save to disk
            string serializedErrorDetail = Serialize(errorDetail);

            var folder = ApplicationStoreInfo.ApplicationStoreErrorDetailLogs + "\\";

            AsyncUtility.Run(() =>
                VirtuosoStorageContext.Current.WriteToFile(
                    folder + DateTime.Now.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture) + ".log",
                    serializedErrorDetail));
        }

        public void Add(ErrorDetail errorDetail, bool submitChanges = true)
        {
            // we don't want to do anything to exacerbate the reason for logging the error
            try
            {
                Context.ErrorDetails.Add(errorDetail);
                if (submitChanges)
                {
                    Context.SubmitChanges(submitOp =>
                    {
                        // Set error or result
                        if (submitOp.HasError)
                        {
                            submitOp.MarkErrorAsHandled();

                            //Since writing to the database failed, save to disk
                            SaveToDisk(errorDetail);
                        }
                    }, null);
                }
            }
            catch
            {
            }
        }

        public void SubmitChanges()
        {
            Context.SubmitChanges();
        }

        public string Serialize<T>(T data)
        {
            using (var memoryStream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(memoryStream, data);

                memoryStream.Seek(0, SeekOrigin.Begin);

                var reader = new StreamReader(memoryStream);
                string content = reader.ReadToEnd();
                return content;
            }
        }

        public T Deserialize<T>(string xml)
        {
            using (var stream = new MemoryStream(Encoding.Unicode.GetBytes(xml)))
            {
                var serializer = new DataContractSerializer(typeof(T));
                T theObject = (T)serializer.ReadObject(stream);
                return theObject;
            }
        }
    }
}