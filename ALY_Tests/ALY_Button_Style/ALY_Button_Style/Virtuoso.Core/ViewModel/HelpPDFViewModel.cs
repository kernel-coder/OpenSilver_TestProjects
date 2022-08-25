#region Usings

using System;
using System.ComponentModel.Composition;
using Virtuoso.Core.ViewModel;

#endregion

namespace Virtuoso.ViewModel
{
    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export]
    public class HelpPDFViewModel : ViewModelBase, INavigateClose
    {
        private string DashedPDFKeys;

        public Uri PDFDocumentToDisplay
        {
            get
            {
                string SelectedDocument = "";
                if (DashedPDFKeys != null)
                {
                    return GetUriFromDashedKeys();
                }

                return new Uri(SelectedDocument);
            }
        }

        public void SetPDFKeys(string keys)
        {
            // assumes docKeys is a dash (-) delimited list of AdmissionDocumentationKeys (A) and/or EncounterKeys (I) of form:
            // 'A_1234-A_222-I_7345'
            DashedPDFKeys = keys;
        }

        private Uri GetUriFromDashedKeys()
        {
#if DEBUG
            var _appPath = "/API/PDFDisplay/GetPDFAdmissionDocumentsAndForms";
#else
            var _appPath = String.Format("cv/API/PDFDisplay/GetPDFAdmissionDocumentsAndForms");
#endif
            var _queryString = String.Format("k={0}", DashedPDFKeys);

            var uriBuilder = new UriBuilder(System.Windows.Application.Current.Host.Source);
            uriBuilder.Path = _appPath;

            uriBuilder.Query = _queryString;

            return uriBuilder.Uri;
        }

        public void NavigateClose()
        {
            NavigateBack();
        }
    }
}