#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Data;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Navigation;
using Virtuoso.Core.Services;
using Virtuoso.Core.Utility;
using Virtuoso.Core.View;
using Virtuoso.Server.Data;
using Virtuoso.Services.Authentication;

#endregion

namespace Virtuoso.Core.ViewModel
{
    [Export(typeof(ContextSensitiveMenuViewModel))]
    public sealed class ContextSensitiveMenuViewModel : GenericBase
    {
        bool IsOnline { get; set; }
        private bool InDynamicForm;

        public int MenuItemCount => MainMenuItems?.Count() ?? 0;

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
        public ContextSensitiveMenuViewModel()
        {
            MainMenuItems = new List<MainMenuItem>();
            RaisePropertyChanged("MenuItemCount");
        }

        [ImportingConstructor]
        public ContextSensitiveMenuViewModel(VirtuosoApplicationConfiguration config, ILogger logger)
        {
            IsOnline = EntityManager.IsOnlineCached;

            Messenger.Default.Register<ContextSensitiveArgs>(this, "SetContextSensitiveMenu", SetContextSensitiveMenu);
            Messenger.Default.Register<ContextSensitiveArgs>(this, "RemoveFromContextSensitiveMenu", RemoveFromContextSensitiveMenu);

            Configuration = config;
            Logger = logger;
            
            try
            {
                MainMenuItems = new List<MainMenuItem>();
                RaisePropertyChanged("MenuItemCount");
            }
            catch (Exception e)
            {
                Logger.Error("ContextSensitiveMenuViewModel", e);
            }

            //If URL = "", then initialize to first URL of SubMenuItems - Reporting menu is like this...
            MainMenuItems.ForEach(InitURL);

            MainMenuItems.ForEach(LogURL);

            FilteredMainMenuItemCollection = new CollectionViewSource
            {
                Source = MainMenuItems
            };
            FilteredMainMenuItemCollection.Filter += MainMenuItemFilter;

            FilterMenu();

            Messenger.Default.Register<bool>(this, Constants.Messaging.NetworkAvailability,
                IsAvailable =>
                {
                    IsOnline = IsAvailable;
                    FilterMenu();
                });
        }

        private void MainMenuItemFilter(object sender, FilterEventArgs e)
        {
            MainMenuItem item = e.Item as MainMenuItem;

            //TODO: when we implement roles - check that the currently logged in end user should see the menu item - based on that items roles...                

            if ((item.ServerOnly) && (IsOnline == false))
            {
                e.Accepted = false;
                item.Visible = false;
            }
            else
            {
                e.Accepted = true;
            }

            // Dither buttons if InDynamicForm and we are offline (minus the exceptions) - Noye the IsEnabled default is always true
            if ((InDynamicForm) && (item.IsMenuItemEnabledInDynamicFormOffline == false))
            {
                item.IsEnabled = IsOnline;
            }
        }

        void FilterMenu()
        {
            if (FilteredMainMenuItemCollection.View != null)
            {
                FilteredMainMenuItemCollection.View.Refresh();
            }
        }

        private int startID;

        int IDGenerater()
        {
            return startID++;
        }

        private void LogURL(MainMenuItem item)
        {
            Logger.Info("ContextSensitiveMenuViewModel", String.Format("Label:{0}, URL:{1}", item.Label, item.URL));
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

        private void SetContextSensitiveMenu(ContextSensitiveArgs e)
        {
            RemoveFromContextSensitiveMenu(e);
            ViewModelBase vm = e.ViewModel as ViewModelBase;
            IPatientMenuSupport iPMS = e.ViewModel as IPatientMenuSupport;
            IPatientMaintenanceMenuSupport iPMMS = e.ViewModel as IPatientMaintenanceMenuSupport;
            IPatientAdmissionMaintenanceMenuSupport iPAMMS = e.ViewModel as IPatientAdmissionMaintenanceMenuSupport;
            if (iPMS != null)
            {
                SetContextSensitiveMenu2(e.ViewModel, iPMS.CurrentPatient, true, true);
            }
            else if (iPMMS != null)
            {
                SetContextSensitiveMenu2(e.ViewModel, iPMMS.SelectedPatient, false, true, true);
            }
            else if (iPAMMS != null)
            {
                SetContextSensitiveMenu2(e.ViewModel, iPAMMS.SelectedPatient, true);
            }
            else
            {
                SetContextSensitiveMenu2(vm);
            }
        }

        private void RemoveFromContextSensitiveMenu(ContextSensitiveArgs e)
        {
            if (CurrentMainMenuItem != null)
            {
                CurrentMainMenuItem.Cleanup();
                CurrentMainMenuItem = null;
            }

            if (MainMenuItems == null)
            {
                return;
            }

            foreach (MainMenuItem mmi in MainMenuItems.AsEnumerable().Reverse())
            {
                mmi.ViewModel = null;
                mmi.Object = null;
                mmi.SubMenuItems = null;
                mmi.Cleanup();
                MainMenuItems.Remove(mmi);
            }

            MainMenuItems = null;
        }

        private void RemoveFromContextSensitiveMenu2(ContextSensitiveArgs e)
        {
            var item = MainMenuItems?.Where(m => m.ViewModel == e.ViewModel).FirstOrDefault();
            if (item != null)
            {
                if (CurrentMainMenuItem != null && CurrentMainMenuItem.ViewModel == item.ViewModel)
                {
                    CurrentMainMenuItem.Cleanup();
                    CurrentMainMenuItem = null;
                }

                item.Cleanup();
                foreach (MainMenuItem mmi in MainMenuItems.AsEnumerable().Reverse())
                {
                    mmi.ViewModel = null;
                    mmi.Object = null;
                    mmi.SubMenuItems = null;
                    MainMenuItems.Remove(mmi);
                }

                MainMenuItems = null;
                item = null;
                e.ViewModel = null; //remove reference to the passed ViewModel - e.g. AdmissionViewModel
            }
        }

        private void SetContextSensitiveMenu2(object viewModel, Patient patient = null,
            bool showPatientMaintenance = false, bool showAdmissionMaintenance = false, bool showAllAdmissions = false)
        {
            MainMenuItems = new List<MainMenuItem>();
            string uri = null;
            GenericBase gb = viewModel as GenericBase;
            if (gb != null)
            {
                if (patient != null)
                {
                    if (patient.ActivePatientMessages != null)
                    {
                        MainMenuItems.Add(new MainMenuItem
                        {
                            ViewModel = viewModel, ID = 10, Object = patient, Label = "Patient Messages",
                            IconLabel = "PatientMessage", URL = null, ServerOnly = false,
                            SubMenuItems = new List<SubMenuItem>()
                        });
                    }

                    if (patient.ActiveDNRs != null)
                    {
                        MainMenuItems.Add(new MainMenuItem
                        {
                            ViewModel = viewModel, ID = 1, Object = patient, Label = "Do Not Resuscitate Orders",
                            IconLabel = "DNR", URL = null, ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                        });
                    }

                    if (showPatientMaintenance && RoleAccessHelper.CheckPermission(RoleAccess.Patient, false))
                    {
                        uri = NavigationUriBuilder.Instance.GetPatientMaintenanceURI(0, patient.PatientKey);
                        MainMenuItems.Add(new MainMenuItem
                        {
                            ID = 2, Label = "Patient " + patient.FullNameInformal, IconLabel = "Patient", URL = uri,
                            ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                        });
                        
                        if (patient.Translator)
                        {
                            MainMenuItems.Add(new MainMenuItem
                            {
                                ViewModel = viewModel, ID = 7, Object = patient,
                                Label = patient.FullNameInformal + " requires a translator", IconLabel = "Translator",
                                URL = null, ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                            });
                        }
                        
                        if (patient.RequiresGuard)
                        {
                            MainMenuItems.Add(new MainMenuItem
                            {
                                ViewModel = viewModel, ID = 8, Object = patient,
                                Label = patient.FullNameInformal + " requires a Guard", IconLabel = "Guard",
                                URL = string.Empty, ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                            });
                        }
                        
                        if (patient.RequiresEscort)
                        {
                            MainMenuItems.Add(new MainMenuItem
                            {
                                ViewModel = viewModel, ID = 9, Object = patient,
                                Label = patient.FullNameInformal + " requires an Escort", IconLabel = "Escort",
                                URL = string.Empty, ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                            });
                        }
                    }
                    
                    if (showAdmissionMaintenance && RoleAccessHelper.CheckPermission(RoleAccess.Patient, false))
                    {
                        if (showAllAdmissions)
                        {
                            if (patient.PatientKey > 0)
                            {
                                if (patient.Translator)
                                {
                                    MainMenuItems.Add(new MainMenuItem
                                    {
                                        ViewModel = viewModel, ID = 7, Object = patient,
                                        Label = patient.FullNameInformal + " requires a translator",
                                        IconLabel = "Translator", URL = string.Empty, ServerOnly = false,
                                        SubMenuItems = new List<SubMenuItem>()
                                    });
                                }
                                
                                if (patient.RequiresGuard)
                                {
                                    MainMenuItems.Add(new MainMenuItem
                                    {
                                        ViewModel = viewModel, ID = 8, Object = patient,
                                        Label = patient.FullNameInformal + " requires a Guard", IconLabel = "Guard",
                                        URL = string.Empty, ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                                    });
                                }
                                
                                if (patient.RequiresEscort)
                                {
                                    MainMenuItems.Add(new MainMenuItem
                                    {
                                        ViewModel = viewModel, ID = 9, Object = patient,
                                        Label = patient.FullNameInformal + " requires an Escort", IconLabel = "Escort",
                                        URL = string.Empty, ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                                    });
                                }


                                bool openAdmission = false;
                                List<ServiceLineGrouping> slgList =
                                    ServiceLineCache.GetAllActiveUserServiceLineGroupingPlusMe(null);
                                List<int> slList = (from slg in slgList
                                        select slg.ServiceLineKey
                                    ).Distinct().ToList();


                                List<Admission> l = patient.Admission.Where(q => ((slList != null)
                                        && slList.Any(sl => sl == q.ServiceLineKey)
                                    )
                                    && (q.HistoryKey == null)).OrderByDescending(q => q.ReferDateTime).ToList();

                                if (l != null)
                                {
                                    if (l.Any() == false)
                                    {
                                        l = null;
                                    }
                                    else
                                    {
                                        openAdmission = l.Any(q =>
                                            (q.AdmissionStatusCode == "A") || (q.AdmissionStatusCode == "R") ||
                                            (q.AdmissionStatusCode == "M"));
                                    }
                                }

                                // if we don't yet have an Admission for the Patient, lets create a new one
                                if (l == null)
                                {
                                    //Uri="/MaintenanceAdmission/{tab}/{patient}/{admission}"
                                    //Tab 1 (SelectedIndex = 0) – Details (Patient)
                                    //Tab 2 – Admission/Referral
                                    //Tab 3 – Physicians
                                    //Tab 4 – Services
                                    //Tab 5 – Authorizations
                                    //Tab 6 – Order Entry
                                    //Tab 7 – Communications
                                    //Tab 8 – Documentation
                                    //Tab 9 – OASIS
                                    uri = NavigationUriBuilder.Instance.GetAdmissionMaintenanceURI(1,
                                        patient.PatientKey, 0);
                                    MainMenuItems.Add(new MainMenuItem
                                    {
                                        ID = 3, Object = patient,
                                        Label = "New Admission/Referral for " + patient.FullNameInformal,
                                        IconLabel = "Admission", URL = uri, ServerOnly = false,
                                        SubMenuItems = new List<SubMenuItem>()
                                    });
                                }
                                // if there's only one, we only have access to one ServiceLine, and the Admission is open, edit the Admission
                                else if ((l.Count == 1) && (slList.Count == 1) && openAdmission)
                                {
                                    Admission a = l.FirstOrDefault();
                                    if (a != null)
                                    {
                                        //Uri="/MaintenanceAdmission/{tab}/{patient}/{admission}"
                                        //Tab 1 (SelectedIndex = 0) – Details (Patient)
                                        //Tab 2 – Admission/Referral
                                        //Tab 3 – Physicians
                                        //Tab 4 – Services
                                        //Tab 5 – Authorizations
                                        //Tab 6 – Order Entry
                                        //Tab 7 – Communications
                                        //Tab 8 – Documentation
                                        //Tab 9 – OASIS
                                        uri = NavigationUriBuilder.Instance.GetAdmissionMaintenanceURI(1,
                                            patient.PatientKey, a.AdmissionKey);
                                        MainMenuItems.Add(new MainMenuItem
                                        {
                                            ID = 3, Object = patient,
                                            Label = "Admission for " + patient.FullNameInformal + " " +
                                                    a.AdmissionStatusText,
                                            IconLabel = "Admission", URL = uri, ServerOnly = false,
                                            SubMenuItems = new List<SubMenuItem>()
                                        });
                                    }
                                }
                                // otherwise, throw up a window and make the user choose
                                else
                                {
                                    MainMenuItems.Add(new MainMenuItem
                                    {
                                        ID = 3, Object = patient, Label = "Admissions for " + patient.FullNameInformal,
                                        IconLabel = "Admissions", URL = null, ServerOnly = false,
                                        SubMenuItems = new List<SubMenuItem>()
                                    });
                                }
                            }
                        }
                        else
                        {
                            Admission a = patient.Admission.FirstOrDefault();
                            if (a != null)
                            {
                                //Uri="/MaintenanceAdmission/{tab}/{patient}/{admission}"
                                //Tab 1 (SelectedIndex = 0) – Details (Patient)
                                //Tab 2 – Admission/Referral
                                //Tab 3 – Physicians
                                //Tab 4 – Services
                                //Tab 5 – Authorizations
                                //Tab 6 – Order Entry
                                //Tab 7 – Communications
                                //Tab 8 – Documentation
                                //Tab 9 – OASIS
                                uri = NavigationUriBuilder.Instance.GetAdmissionMaintenanceURI(1, patient.PatientKey,
                                    a.AdmissionKey);
                                MainMenuItems.Add(new MainMenuItem
                                {
                                    ID = 4,
                                    Label = "Admission for " + patient.FullNameInformal + " " + a.AdmissionStatusText,
                                    IconLabel = "Admission", URL = uri, ServerOnly = false,
                                    SubMenuItems = new List<SubMenuItem>()
                                });
                            }
                        }
                    }

                    List<NavigateKey> list = GetOpenDynamicFormsForPatient(gb.NavigateKey, patient.PatientKey);
                    if (list.Count == 1)
                    {
                        foreach (NavigateKey nKey in list)
                        {
                            uri = GetUriStringFromNavigateKey(nKey, patient.PatientKey);
                            MainMenuItems.Add(new MainMenuItem
                            {
                                ID = 5, Label = "Return to open " + nKey.ApplicationSuite + " for " + nKey.Title,
                                IconLabel = "Documentation", URL = uri, ServerOnly = false,
                                SubMenuItems = new List<SubMenuItem>()
                            });
                        }
                    }
                }
            }

            INavigateClose inc = viewModel as INavigateClose;

            InDynamicForm = inc is DynamicFormViewModel;
            if (inc != null && !InDynamicForm)
            {
                MainMenuItems.Add(new MainMenuItem
                {
                    ViewModel = viewModel, ID = 6, Object = patient, Label = "Close", IconLabel = "Close", URL = null,
                    ServerOnly = false, SubMenuItems = new List<SubMenuItem>()
                });
            }

            FilteredMainMenuItemCollection = new CollectionViewSource { Source = MainMenuItems };
            FilteredMainMenuItemCollection.Filter += MainMenuItemFilter;

            RaisePropertyChanged("MenuItemCount");
        }

        private List<NavigateKey> GetOpenDynamicFormsForPatient(NavigateKey navigateKey, int patientKey)
        {
            var list = new List<NavigateKey>();
            if (navigateKey != null)
            {
                // do not return a list if we are a dynamic form 
                if (navigateKey.UriString.ToLower().Contains("component/views/dynamicform.xaml?patient=") == false)
                {
                    foreach (var item in navigateKey.ActivePages.Pages.Values)
                    {
                        var nk = item.GetValue(NonLinearNavigationContentLoader.NavigateKeyProperty) as NavigateKey;

                        if (nk != null)
                        {
                            if (GetUriStringFromNavigateKey(nk, patientKey) != null)
                            {
                                list.Add(nk);
                            }
                        }
                    }
                }
            }

            return list;
        }

        public static string GetUriStringFromNavigateKey(NavigateKey Key, int patientKey)
        {
            if (Key.UriString.ToLower()
                .Contains(string.Format("component/views/dynamicform.xaml?patient={0}", patientKey.ToString())))
            {
                return Key.UriString;
            }

            return null;
        }

        public override void Cleanup()
        {
            if (FilteredMainMenuItemCollection != null)
            {
                FilteredMainMenuItemCollection.Filter -= MainMenuItemFilter;
            }

            base.Cleanup();
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
                        SubMenuItem item = e.Item as SubMenuItem;
                        e.Accepted = true;
                        if (item.Resource != null)
                        {
                            e.Accepted = WebContext.Current.User.DeltaAdmin;
                        }
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
    }

    public class ContextSensitiveArgs
    {
        private object _ViewModel;

        public object ViewModel
        {
            get { return _ViewModel; }
            set { _ViewModel = value; }
        }
    }
}