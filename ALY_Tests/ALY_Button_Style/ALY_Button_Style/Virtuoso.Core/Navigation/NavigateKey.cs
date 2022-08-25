#region Usings

using System;
using System.Windows;
using Virtuoso.Core.ViewModel;

#endregion

namespace Virtuoso.Core.Navigation
{
    public class NavigateKey : GenericBase
    {
        #region Properties

        public Type ViewType { get; private set; }
        public String CurrentSource { get; set; }
        public String ApplicationSuite { get; set; }
        public String Action => "edit";
        public String Mode { get; set; }
        public String Title { get; set; }
        public String ServiceLine { get; set; }
        private String _Key;

        public String Key
        {
            get { return _Key; }
            set
            {
                ResetMaintenanceListURI(value);
                _Key = value;
            }
        }

        public int? KeyAsInt
        {
            get
            {
                int tmpInt = 0;
                if (Int32.TryParse(Key, out tmpInt))
                {
                    return tmpInt;
                }

                return null;
            }
        }

        private String _SubKey;

        public String SubKey
        {
            get { return _SubKey; }
            set { _SubKey = value; }
        }

        public int? SubKeyAsInt
        {
            get
            {
                int tmpInt = 0;
                if (Int32.TryParse(Key, out tmpInt))
                {
                    return tmpInt;
                }

                return null;
            }
        }

        public object EntityObject { get; set; }
        public String ParentUriOriginalString { get; private set; }
        public String UriString { get; private set; }
        public String TrackingKey { get; private set; }
        public Boolean IsChainable { get; set; }
        public INonLinearNavigationActivePages ActivePages { get; private set; }

        private void ResetMaintenanceListURI(string newKey)
        {
            _Key = newKey;

            bool isNew = newKey.StartsWith("-") || newKey.Equals("0");

            if (ApplicationSuite == null)
            {
                Mode = (isNew) ? Constants.ADDING : Constants.EDITING;
            }
            else
            {
                Mode = (isNew) ? Constants.ADDING : Constants.EDITING;
            }

            // do not play with key zero
            if (isNew)
            {
                return;
            }

            if (newKey == "00000000-0000-0000-0000-000000000000")
            {
                return; //UserProfile screen
            }

            //"/Virtuoso.Home;component/Views/DynamicForm.xaml?patient=3637&admission=1978&form=1893&service=1610&task=1801&encounter=1030"
            // do not play with dynamic form or cases of no key
            //if (this.UriString.ToLower().Contains("component/views/dynamicform.xaml?patient=") == true) return;

            if (string.IsNullOrWhiteSpace(newKey))
            {
                return;
            }

            DependencyObject content = null;
            string newUriString = null;
            if (UriString.Contains("AdmissionList.xaml?"))
            {
                // admission maintenance is a special case
                if (UriString.EndsWith("&admission=0") == false)
                {
                    return;
                }

                newUriString = UriString.Replace("&admission=0", "&admission=" + newKey);
            }
            else
            {
                // change new add to edit if need be
                if (UriString.Contains("?trackingKey=") == false)
                {
                    return;
                }

                string[] delimiters = { "?trackingKey=" };
                string[] valuesSplit = UriString.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (valuesSplit.Length != 2)
                {
                    return;
                }

                newUriString = valuesSplit[0] + "?id=" + newKey;
            }

            content = ActivePages.GetPage(UriString) as DependencyObject;
            if (content != null)
            {
                //when re-keying - don't want to use RemovePage() - because this makes the page Cleanup() itself up,
                //sometimes that will be skipped though, e.g. if when CanExit() returns TRUE
                ActivePages.RekeyPage(UriString, newUriString);

                UriString = newUriString;
                TrackingKey = "";
            }
        }

        #endregion //Properties

        #region Constructor

        public NavigateKey(Type viewType, String uriString, String parentUriOriginalString,
            INonLinearNavigationActivePages activePages, String trackingKey)
        {
            ViewType = viewType;
            UriString = uriString;
            ParentUriOriginalString = parentUriOriginalString;
            TrackingKey = trackingKey;
            ActivePages = activePages;
        }

        #endregion //Constructor

        public override void Cleanup()
        {
            EntityObject = null;

            base.Cleanup();
        }
    }
}