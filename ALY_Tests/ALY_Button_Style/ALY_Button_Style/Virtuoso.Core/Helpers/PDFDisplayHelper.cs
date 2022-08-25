#region Usings

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using Virtuoso.Core.View;
using Virtuoso.ViewModel;

#endregion

namespace Virtuoso.Core.Helpers
{
    public static class PDFDisplayHelper
    {
        private static object _lock = new object();
        public static double defaultWidth = 1024;
        public static double defaultHeight = 756;

        public static void OpenPDFinWindow(string windowTitle, string pdfKeys)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(pdfKeys))
                {
                    return;
                }

                Window PDFnew = new Window();
                {
                    HelpPDF helpWin = CreateNewHelpWindow(windowTitle, PDFnew);
                    ((HelpPDFViewModel)helpWin.DataContext).SetPDFKeys(pdfKeys);

                    PDFnew.Visibility = Visibility.Visible;
                    helpWin.LoadPDF();
                }
            }
        }

        private static HelpPDF CreateNewHelpWindow(string windowTitle, Window PDFnew)
        {
            HelpPDF helpWin = new HelpPDF();
            PDFnew.Content = helpWin;
            PDFnew.Width = defaultWidth;
            PDFnew.Height = defaultHeight;
            PDFnew.Title = windowTitle;
            return helpWin;
        }

        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<HttpStatusCode> IsPDFValid(byte[] pdfToValidate)
        {
#if DEBUG
            var _appPath = "/API/PDFDisplay/ValidatePDFDocument";
#else
            var _appPath = String.Format("cv/API/PDFDisplay/ValidatePDFDocument");
#endif
            var uriBuilder = new UriBuilder(System.Windows.Application.Current.Host.Source)
            {
                Path = _appPath
            };

            using (var content = new ByteArrayContent(pdfToValidate, 0, pdfToValidate.Length))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
                content.Headers.ContentLength = pdfToValidate.Length;
                using (var response = await httpClient.PostAsync(uriBuilder.Uri, content))
                {
                    return response.StatusCode; // == HttpStatusCode.Accepted);
                }
            }
        }
    }
}