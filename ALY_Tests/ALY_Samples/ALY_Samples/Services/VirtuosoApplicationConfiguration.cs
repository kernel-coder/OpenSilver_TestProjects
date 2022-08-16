#region Usings

using System;
using Virtuoso.Client.Infrastructure.Storage;
using Virtuoso.Core.Cache;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public class VirtuosoApplicationConfiguration : GalaSoft.MvvmLight.ViewModelBase, IVirtuosoApplicationConfiguration
    {
        public string SessionID { get; }

        public VirtuosoApplicationConfiguration(bool initialized)
        {
            ApplicationInitialized = initialized;
            SessionID = Guid.NewGuid().ToString();
        }

        public TenantSetting Setting => TenantSettingsCache.Current.TenantSetting;

        public String LastLogin
        {
            //NOTE: the LastLogin property is unlike all other properties in that it doesn't originate from initparms
            //      this property is a wrapper around ISO application settings...
            get { return VirtuosoStorageContext.LocalSettings.Get<string>("LastLogin"); }
            set
            {
                VirtuosoStorageContext.LocalSettings.Put("LastLogin", value);
                RaisePropertyChanged("LastLogin");
            }
        }

        static readonly object _applicationInitializedLock = new object();
        private bool _ApplicationInitialized;

        public bool ApplicationInitialized
        {
            get
            {
                lock (_applicationInitializedLock)
                {
                    return _ApplicationInitialized;
                }
            }
            set
            {
                lock (_applicationInitializedLock)
                {
                    _ApplicationInitialized = value;
                    RaisePropertyChanged("ApplicationInitialized");
                }
            }
        }

        private string _VirtuosoVersion = "01.01";

        public string VirtuosoVersion
        {
            get { return _VirtuosoVersion; }
            set
            {
                _VirtuosoVersion = value;
                RaisePropertyChanged("VirtuosoVersion");
            }
        }

        public string HomeScreenView { get; set; }
    }
}