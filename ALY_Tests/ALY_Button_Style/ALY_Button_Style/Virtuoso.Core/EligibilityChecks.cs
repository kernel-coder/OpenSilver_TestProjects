#region Usings

using System;
using System.Net.Http;
using System.Windows;
using Virtuoso.Client.Infrastructure.Storage;

#endregion

namespace Virtuoso.Core
{
    public class EligibilityChecks
    {
        private Uri GetUriFromKey(int RequestKey)
        {
#if DEBUG
            var _appPath = "/API/EligibilityCheck/Check";
#else
            var _appPath = String.Format("cv/API/EligibilityCheck/Check");
#endif

            var _queryString =
                String.Format("RequestKey=\"{0}\"&UniqueParam={1}", RequestKey, Guid.NewGuid().ToString());

            var uriBuilder = new UriBuilder(System.Windows.Application.Current.Host.Source);
            uriBuilder.Path = _appPath;

            uriBuilder.Query = _queryString;

            return uriBuilder.Uri;
        }

        public async void SubmitEligibilityCheck(int id, Action<string> callback)
        {
            try
            {
                var client = new HttpClient();

                var uri = GetUriFromKey(id);

                var response = await client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (callback != null)
                        {
                            callback(string.Empty);
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(response.ReasonPhrase);
                    Deployment.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (callback != null)
                        {
                            callback(response.ReasonPhrase);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (callback != null)
                    {
                        callback(ex.Message);
                    }
                });

                throw new Exception("Error getting HTML response.", ex);
            }
        }

        public async void Create270(string keyList, string fileName, DateTime fromDate, int insuranceKey,
            Action<bool> callback)
        {
            try
            {
                var client = new HttpClient();

                var bdr = new UriBuilder(System.Windows.Application.Current.Host.Source);

#if DEBUG
                bdr.Path = "api/EligibilityCheck/Create270";
#else
				bdr.Path = "cv/api/EligibilityCheck/Create270";
#endif

                bool realTimeRequest = !string.IsNullOrEmpty(fileName);
                bdr.Query = String.Format("KeyList={0}&FromDate={1}&InsuranceKey={2}&RealTimeRequest={3}",
                    keyList, fromDate.ToShortDateString(), insuranceKey, realTimeRequest);
                var response = await client.GetAsync(bdr.Uri);

                response.EnsureSuccessStatusCode();

                byte[] file = await response.Content.ReadAsByteArrayAsync();
                if (!string.IsNullOrEmpty(fileName))
                {
                    await VirtuosoStorageContext.Current.WriteToFile(fileName, file);
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (callback != null)
                    {
                        callback(true);
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                throw new Exception("Could not create 270 file.");
                //Downloading file failed - attempt to load from disk
            }
        }
    }
}