#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Xml.Linq;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Logging;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.ViewModel
{
    public class MenuItem
    {
        public int ID { get; set; }
        
        public string Label { get; set; }
        public string IconLabel { get; set; }
        public string URL { get; set; }
        public object Object { get; set; }
        public string Resource { get; set; }
        public object ViewModel { get; set; }

        // defaulted to true, if we are in DynamicForm AND Offline, menu buttons will be disabled
        public bool IsEnabled { get; set; } = true;

        public bool IsMenuItemEnabledInDynamicFormOffline
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Label))
                {
                    return false;
                }

                if (IconLabel.ToLower() == "close")
                {
                    return true;
                }

                if (IconLabel.ToLower() == "patientmessage")
                {
                    return true;
                }

                if (IconLabel.ToLower() == "dnr")
                {
                    return true;
                }

                if (IconLabel.ToLower() == "translator")
                {
                    return true;
                }

                if (IconLabel.ToLower() == "guard")
                {
                    return true;
                }

                if (IconLabel.ToLower() == "escort")
                {
                    return true;
                }

                return false;
            }
        }

        private bool visible;

        public bool Visible
        {
            get
            {
                if (Label == "Tools" && visible)
                {
                    if (!RoleAccessHelper.CheckPermission("HISorOASISCoordinatorOrIntakeOrAdminOrHospicePharmacyIVOrICDCoder"))
                    {
                        visible = ((TenantSettingsCache.Current.TenantSetting.PurchasedInsuranceEligibility) &&
                                   (RoleAccessHelper.CheckPermission("IntakeAdmin")));
                    }
                }

                if (Label == "Calendar" && visible)
                {
                    visible = TenantSettingsCache.Current.TenantSetting.DisplayScheduleButton;
                }

                if (Label == "Reports" || Label == "Dashboards")
                {
                    visible = TenantSettingsCache.Current.TenantSetting.IsDemoEnvironment;
                }

                if (Label == "Intake" && visible)
                {
                    visible = TenantSettingsCache.Current.TenantSetting.PurchasedExternalReferralManagement;
                }

                return visible;
            }
            set { visible = value; }
        } //NOTE: TopLevelMenu menu items where ServerOnly=True will not be Visible when OFFLINE
    }

    public class SubMenuItem : MenuItem
    {
    }

    public class MainMenuItem : MenuItem
    {
        public bool ServerOnly { get; set; }
        public List<SubMenuItem> SubMenuItems { get; set; }

        public void Cleanup()
        {
            if (SubMenuItems != null)
            {
                SubMenuItems.Clear();
            }

            SubMenuItems = null;
            Object = null;
            ViewModel = null;
        }

        public bool SurveyorMenuItem { get; set; }
    }

    [Export(typeof(MenuViewModel))]
    public class MenuViewModel : GalaSoft.MvvmLight.ViewModelBase
    {
        public EntityManager EntityManager => EntityManager.Current;

        //NOTE: this class is a VM for a UserControl instantiated on the Shell UI - this UI is the root visual - so it is instantiated
        //      even before the end user logs in - as such - processing roles - needs to occur at runtime - e.g. after the ctor...
        ILogger Logger { get; set; } //Applogger is wrapper around LogManager - writes to UILogger and 'debug' logger
        VirtuosoApplicationConfiguration _Configuration;

        public VirtuosoApplicationConfiguration Configuration
        {
            get { return _Configuration; }
            private set
            {
                _Configuration = value;
                RaisePropertyChanged("Configuration");
            }
        }

        //Design-Time Constructor
        public MenuViewModel()
        {
            MainMenuItems = new List<MainMenuItem>
            {
                new MainMenuItem
                    { ID = 0, Label = "About", IconLabel = "About", URL = "/About", ServerOnly = false },
                new MainMenuItem
                    { ID = 2, Label = "Home", IconLabel = "Home", URL = "/Home", ServerOnly = false },
                new MainMenuItem
                    { ID = 3, Label = "Patient", IconLabel = "Patient", URL = "/Patient", ServerOnly = false },
                new MainMenuItem
                {
                    ID = 4, Label = "Setup", IconLabel = "Setup", URL = "/Maintenance/AgencyParameter", ServerOnly = true
                }
            };

            FilteredMainMenuItemCollection = new CollectionViewSource();
            FilteredMainMenuItemCollection.Source = MainMenuItems;

            var subMenuItems = new List<SubMenuItem>
            {
                new SubMenuItem
                    { ID = 5, Label = "Agency Setup", IconLabel = "Agency Setup", URL = "/Maintenance/AgencyParameter" },
                new SubMenuItem
                    { ID = 6, Label = "Allergy", IconLabel = "Allergy", URL = "/Maintenance/AllergyCode" },
                new SubMenuItem
                    { ID = 10, Label = "CMS Header", IconLabel = "CMS Header", URL = "/Maintenance/OasisHeader" },
                new SubMenuItem
                    { ID = 7, Label = "Code Lookup", IconLabel = "Code Lookup", URL = "/Maintenance/CodeLookup" },
                new SubMenuItem
                    { ID = 8, Label = "Discipline", IconLabel = "Discipline", URL = "/Maintenance/Discipline" },
                new SubMenuItem
                    { ID = 9, Label = "Equipment", IconLabel = "Equipment", URL = "/Maintenance/Equipment" },
                new SubMenuItem
                    { ID = 10, Label = "Goals", IconLabel = "Goals", URL = "/Maintenance/Goal" },
                new SubMenuItem
                    { ID = 11, Label = "Goal Elements", IconLabel = "Goal Elements", URL = "/Maintenance/GoalElement" },
                new SubMenuItem
                    { ID = 10, Label = "Facility", IconLabel = "Facility", URL = "/Maintenance/Facility" },
                new SubMenuItem
                    { ID = 11, Label = "ServiceLine", IconLabel = "ServiceLine", URL = "/Maintenance/ServiceLine" },
                new SubMenuItem
                    { ID = 10, Label = "Vendor", IconLabel = "Vendor", URL = "/Maintenance/Vendor" },
                new SubMenuItem
                {
                    ID = 10, Label = "OrdersTracking", IconLabel = "Orders Tracking", URL = "/Maintenance/OrdersTracking"
                },
                new SubMenuItem
                    { ID = 12, Label = "InsuranceCoordinator", IconLabel = "InsuranceCoordinator", URL = "/Patient" }
            };

            FilteredSubMenuItemCollection = new CollectionViewSource
            {
                Source = subMenuItems
            };
        }

        [ImportingConstructor]
        public MenuViewModel(VirtuosoApplicationConfiguration config, ILogger logger)
        {
            Messenger.Default.Register<bool>(this, "PageOpened", haveAppSuite => OnPageOpened(haveAppSuite));
            Messenger.Default.Register<bool>(this, "AlertsChanged", s => OnAlertsChanged());
            Messenger.Default.Register<Uri>(this, "LogoutRequest", uri => ProcessLogoutRequest(uri));
            Messenger.Default.Register<bool>(this, "FilterMenu", b =>
            {
                if (FilteredMainMenuItemCollection.View != null)
                {
                    FilteredMainMenuItemCollection.View.Refresh();
                }
            });

            Configuration = config;
            Logger = logger;

            try
            {
#if OPENSILVER
                var stream = GetType().Assembly.GetManifestResourceStream(@"..\Virtuoso\Controls\MenuComponents.xml");
                XDocument menuComponents = XDocument.Load(stream);
#else
                XDocument menuComponents = XDocument.Load("/Virtuoso;component/Controls/MenuComponents.xml");
#endif

                MainMenuItems = (from menuComponent in menuComponents.Descendants("TopLevelMenu")
                    where menuComponent.Attribute("displayInNavigation").Value.Equals("true")
                    select new MainMenuItem
                    {
                        //NOTE: destinationURL may not exist - in which case default to first submenu item with non-null destinationURL
                        ID = IDGenerater(),
                        SurveyorMenuItem =
                            ((menuComponent.Attribute("isSurveyorMenuItem") == null) ||
                             (menuComponent.Attribute("isSurveyorMenuItem").Value.Equals("false")))
                                ? false
                                : true,
                        Label = (menuComponent.Attribute("tooltip") == null)
                            ? ((menuComponent.Attribute("label") == null)
                                ? String.Empty
                                : menuComponent.Attribute("label").Value)
                            : menuComponent.Attribute("tooltip").Value,
                        IconLabel = (menuComponent.Attribute("label") == null)
                            ? String.Empty
                            : menuComponent.Attribute("label").Value,
                        URL = (menuComponent.Attribute("destinationURL") == null)
                            ? String.Empty
                            : menuComponent.Attribute("destinationURL").Value,
                        ServerOnly = (menuComponent.Attribute("serverOnly") == null)
                            ? false
                            : Boolean.Parse(menuComponent.Attribute("serverOnly").Value),
                        Resource = (menuComponent.Attribute("role") == null)
                            ? string.Empty
                            : menuComponent.Attribute("role").Value,
                        ViewModel = this,
                        SubMenuItems = menuComponent.Descendants("SubMenuItem").Any()
                            ? (
                                menuComponent.Descendants("SubMenuItem")
                                    .Where(s => s.Attribute("displayInNavigation").Value.Equals("true"))
                                    .Select(s => new SubMenuItem
                                    {
                                        ID = IDGenerater(),
                                        Label = (s.Attribute("label") == null)
                                            ? String.Empty
                                            : s.Attribute("label").Value,
                                        IconLabel = (s.Attribute("label") == null)
                                            ? String.Empty
                                            : s.Attribute("label").Value,
                                        URL = (s.Attribute("destinationURL") == null)
                                            ? String.Empty
                                            : s.Attribute("destinationURL").Value,
                                        Resource = (s.Attribute("role") == null)
                                            ? string.Empty
                                            : s.Attribute("role").Value
                                    })
                            ).ToList()
                            : new List<SubMenuItem>(),
                    }).ToList();
            }
            catch (Exception e)
            {
                Logger.Log(TraceEventType.Critical, "MenuViewModel", e);
            }

            //If URL = "", then initialize to first URL of SubMenuItems - Reporting menu is like this...
            MainMenuItems.ForEach(InitURL);

            MainMenuItems.ForEach(LogURL);

            FilteredMainMenuItemCollection = new CollectionViewSource();
            FilteredMainMenuItemCollection.Source = MainMenuItems;
            FilteredMainMenuItemCollection.Filter += (s, e) =>
            {
                MainMenuItem item = e.Item as MainMenuItem;
                if (item == null)
                {
                    e.Accepted = false;
                    return;
                }

                if (RoleAccessHelper.IsSurveyor && (item.SurveyorMenuItem == false))
                {
                    e.Accepted = false;
                    return;
                }

                //check that the currently logged in end user should see the menu item - based on that items roles...
                var _accepted = RoleAccessHelper.CheckPermission(item.Resource);
                e.Accepted = _accepted;
            };

            UpdateVisibilityForMainMenuItems(EntityManager.IsOnlineCached);
            FilterMenu();

            EntityManager.Current.NetworkAvailabilityChanged += Current_NetworkAvailabilityChanged;
        }
        
        void Current_NetworkAvailabilityChanged(object sender, Client.Offline.Events.NetworkAvailabilityEventArgs e)
        {
            var logWriter = EnterpriseLibraryContainer.Current.GetInstance<LogWriter>();
            logWriter.Write(
                string.Format(
                    "MenuViewModel.cs (Current_NetworkAvailabilityChanged): Will FilterMenu().  e.IsAvailable = {0}",
                    e.IsAvailable),
                new string[1] { "NETWORK" }, //category
                0, //eventid
                0, //priority
                TraceEventType.Information);

            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                UpdateVisibilityForMainMenuItems(e.IsAvailable);
                FilterMenu();
                RaisePropertyChanged("FilteredMainMenuItemCollection");
            });
        }

        private void UpdateVisibilityForMainMenuItems(bool isOnline)
        {
            //set the visibility of main menu items - if offline - ServerOnly items will not be visible
            MainMenuItems.ForEach(m => { m.Visible = !m.ServerOnly || isOnline; });
        }

        void FilterMenu()
        {
            if (FilteredMainMenuItemCollection.View != null)
            {
                FilteredMainMenuItemCollection.View.Refresh();
            }
        }

        private void ProcessLogoutRequest(Uri source)
        {
            SetSubMenuItems("-1"); //pass invalid, integer menu id - so that sub-menu is cleared
        }

        private int startID;

        int IDGenerater()
        {
            return startID++;
        }

        private void LogURL(MainMenuItem item)
        {
            Logger.Info("MenuViewModel", String.Format("Label:{0}, URL:{1}", item.Label, item.URL));
        }

        private void InitURL(MainMenuItem item)
        {
            if (String.IsNullOrEmpty(item.URL))
            {
                item.URL = (from m in item.SubMenuItems
                    where (String.IsNullOrEmpty(m.URL) == false && WebContext.Current.User.DeltaAdmin)
                    select m.URL).FirstOrDefault();
            }
        }

        public void SetSubMenuItems(string idTag)
        {
            bool ret = true;
            CurrentMainMenuItem = (from m in MainMenuItems
                where m.ID == Int32.Parse(idTag)
                select m).FirstOrDefault();
            if (CurrentMainMenuItem != null)
            {
                if (CurrentMainMenuItem.SubMenuItems.Any())
                {
                    FilteredSubMenuItemCollection = new CollectionViewSource { Source = CurrentMainMenuItem.SubMenuItems };

                    //TODO: filter function on SubMenuItems to process network status and user roles
                    FilteredSubMenuItemCollection.Filter += (s, e) =>
                    {
                        e.Accepted = WebContext.Current.User.DeltaAdmin;
                    };

                    ret = true;
                }
                else if
                    (CurrentMainMenuItem.URL !=
                     null) //keep sub menu available menu choices which won't navigate - such as search???
                {
                    FilteredSubMenuItemCollection = new CollectionViewSource();
                    ret = false;
                }
                else
                {
                    return; //leave DisplaySubMenu alone
                }
            }
            else
            {
                FilteredSubMenuItemCollection = new CollectionViewSource();
                ret = false;
            }

            if (ret)
            {
                CurrentMainMenuItem.URL = string.Empty;
                InitURL(CurrentMainMenuItem);
            }

            DisplaySubMenu = ret;
        }

        public MenuItem GetMenuItem(string idTag)
        {
            if (MainMenuItems == null)
            {
                return null;
            }

            var mainMenu = (from m in MainMenuItems
                where m.ID == Int32.Parse(idTag)
                select m).FirstOrDefault();
            if (mainMenu != null)
            {
                return mainMenu;
            }

            foreach (var mainMenuItem in MainMenuItems)
            {
                var subMenuItem = (from s in mainMenuItem.SubMenuItems
                    where s.ID == Int32.Parse(idTag)
                    select s).FirstOrDefault();
                if (subMenuItem != null)
                {
                    return subMenuItem;
                }
            }

            return null;
        }

        private MainMenuItem _CurrentMainMenuItem;

        public MainMenuItem CurrentMainMenuItem
        {
            get { return _CurrentMainMenuItem; }
            set
            {
                _CurrentMainMenuItem = value;
                RaisePropertyChanged("CurrentMainMenuItem");
            }
        }

        //list of TOP level menu options
        private List<MainMenuItem> _MainMenuItems;

        private List<MainMenuItem> MainMenuItems
        {
            get { return _MainMenuItems; }
            set
            {
                _MainMenuItems = value;
            }
        }

        private CollectionViewSource _FilteredMainMenuItemCollection;

        public CollectionViewSource FilteredMainMenuItemCollection
        {
            get { return _FilteredMainMenuItemCollection; }
            set
            {
                _FilteredMainMenuItemCollection = value;
                RaisePropertyChanged("FilteredMainMenuItemCollection");
            }
        }

        private bool _displaySubMenu;

        public bool DisplaySubMenu
        {
            get { return _displaySubMenu; }
            set
            {
                _displaySubMenu = value;
                RaisePropertyChanged("DisplaySubMenu");
            }
        }

        private CollectionViewSource _FilteredSubMenuItemCollection;

        public CollectionViewSource FilteredSubMenuItemCollection
        {
            get { return _FilteredSubMenuItemCollection; }
            set
            {
                _FilteredSubMenuItemCollection = value;
                RaisePropertyChanged("FilteredSubMenuItemCollection");
            }
        }

        private void OnAlertsChanged()
        {
            RaisePropertyChanged("NumberOfAlerts");
        }

        public int NumberOfAlerts => UserAlertManager.Instance.TotalAlerts;

        private void OnPageOpened(bool _haveApplicationSuite)
        {
            __NumberOfPages = GetActiveApplicationSuiteDataItemCount(_haveApplicationSuite);
            RaisePropertyChanged("NumberOfPages");
        }

        private int __NumberOfPages;
        public int NumberOfPages => __NumberOfPages;

        public static int GetActiveApplicationSuiteDataItemCount(bool HaveApplicationSuite = true)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "----------------------------------------------------------------------------------------------------------------------------");
                System.Diagnostics.Debug.WriteLine("GetActiveApplicationSuiteDataItemCount:BEGIN");
                int count = 0;
                int pageIndex = 0;
                System.Diagnostics.Debug.WriteLine("Total Pages: {0}",
                    Navigation.NonLinearNavigationContentLoader.Current.Pages.Values.Count);
                foreach (var item in Navigation.NonLinearNavigationContentLoader.Current.Pages.Values)
                {
                    var nk =
                        item.GetValue(Navigation.NonLinearNavigationContentLoader.NavigateKeyProperty) as
                            Navigation.NavigateKey;
                    if (nk != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Page[{0}]: {1}", pageIndex++, nk.UriString);

                        if (nk.UriString != null && nk.UriString.EndsWith("OpenWork.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.EndsWith("HomeScreen.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.Contains("component/Views/PatientDashboard.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.EndsWith("AlertManagerWorkList.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.EndsWith("About.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.EndsWith("ToolsMenu.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.EndsWith("OASISList.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.EndsWith("ICDCoderWorkList.xaml"))
                        {
                            continue;
                        }

                        if (nk.UriString != null && nk.UriString.EndsWith("HISList.xaml"))
                        {
                            continue;
                        }

                        System.Diagnostics.Debug.WriteLine("Counting page: {0}", nk.UriString);
                        count++;
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Skipping page: {0}", nk.UriString);
                    }
                }

                System.Diagnostics.Debug.WriteLine("GetActiveApplicationSuiteDataItemCount:END  count={0}", count);
                System.Diagnostics.Debug.WriteLine(
                    "----------------------------------------------------------------------------------------------------------------------------");
                return count;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("GetActiveApplicationSuiteDataItemCount:END  Exception={0}",
                    e.ToString());
                System.Diagnostics.Debug.WriteLine(
                    "----------------------------------------------------------------------------------------------------------------------------");
                return 0;
            }
        }
    }
}