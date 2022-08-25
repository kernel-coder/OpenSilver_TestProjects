using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Virtuoso.Client.Core;

namespace Virtuoso.Core.Helpers
{
    internal class ApiHelper
    {   public static  async Task<HttpResponseMessage> MakeApiCall(string endpoint, string query = null)
        {
            var client = new HttpClient();

            var bdr = new UriBuilder(System.Windows.Application.Current.GetServerBaseUri());

#if DEBUG
            bdr.Path = $"api/{endpoint}";
#else
            bdr.Path = $"cv/api/{endpoint}";
#endif

            bdr.Query = query;
            var response = await client.GetAsync(bdr.Uri);

            response.EnsureSuccessStatusCode();

            return response;
        }
    }
}
