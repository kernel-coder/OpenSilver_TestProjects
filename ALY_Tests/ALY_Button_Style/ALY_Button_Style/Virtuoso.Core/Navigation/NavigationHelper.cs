#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Virtuoso.Core.Navigation
{
    public class NavigationHelper
    {
        private NavigateKey NavigationKeyInternal;

        public NavigationHelper(NavigateKey NavigationKeyParm)
        {
            NavigationKeyInternal = NavigationKeyParm;
        }

        public List<NavigateKey> GetActiveDataItems(string application_suite)
        {
            var list = new List<NavigateKey>();
            if (NavigationKeyInternal != null)
            {
                foreach (var item in NavigationKeyInternal.ActivePages.Pages.Values)
                {
                    var nk = item.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;
                    if (nk != null && nk.ApplicationSuite == application_suite)
                    {
                        list.Add(nk);
                    }
                }
            }

            return list;
        }

        public bool AreOtherNewAdmissionsOpen(int? KeyParm)
        {
            var OpenAdmitPages = GetActiveDataItems(Constants.APPLICATION_ADMISSION);
            string patString = string.Format("&patient={0}", KeyParm);

            return OpenAdmitPages.Any(a => a.UriString.Contains("AdmissionList.xaml") && a.UriString.Contains(patString)
                && a.KeyAsInt != null && a.KeyAsInt <= 0);
        }

        public object GetEntityObject(String Type, int Sequence, bool newOnly = false)
        {
            var OpenPages = GetActiveDataItems(Type).Where(op => !newOnly || op.KeyAsInt <= 0).ToArray();

            NavigateKey retPage = null;
            if (OpenPages.Count() > Sequence)
            {
                retPage = OpenPages[Sequence];
            }
            else
            {
                retPage = OpenPages.LastOrDefault();
            }

            return retPage?.EntityObject;
        }
    }
}