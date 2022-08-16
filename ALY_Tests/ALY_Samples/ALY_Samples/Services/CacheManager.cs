#region Usings

#if OPENSILVER
using Autofac.Features.Metadata;
#endif
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using Microsoft.Practices.EnterpriseLibrary.Logging.Diagnostics;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Client.Offline;
using Virtuoso.Core.Cache;
using Virtuoso.Services.Configuration;
using Virtuoso.Services.Framework;

#endregion

namespace Virtuoso.Core.Services
{
    //TODO: add a LoadedFrom property to CacheLoader - allowable values = DATABASE or FILESYSTEM/CACHE...
    //      add timestamps for loading to/from database and to/from disk
    public class CacheLoader : ViewModelBase
    {
        public DateTime? LastUpdatedDate { get; set; }

        public Action<CacheLoader> Callback;
        public ICache Cache { get; set; }

        public CacheLoader(string name, ICache cache, Action<CacheLoader> loaderAction)
        {
            Errors = new List<Exception>();
            Cache = cache;
            CacheName = name;
            Callback = loaderAction;
            IsCacheBusy = false;
            IsCacheLoaded = false;
        }

        private bool _IsCacheBusy;

        public bool IsCacheBusy
        {
            get { return _IsCacheBusy; }
            set
            {
                _IsCacheBusy = value;
                RaisePropertyChanged("IsCacheBusy");
            }
        }

        private bool _IsCacheLoaded;

        public bool IsCacheLoaded
        {
            get { return _IsCacheLoaded; }
            set
            {
                _IsCacheLoaded = value;
                RaisePropertyChanged("IsCacheLoaded");
            }
        }

        private string _CacheName;

        public string CacheName
        {
            get { return _CacheName; }
            set
            {
                _CacheName = value;
                RaisePropertyChanged("CacheName");
            }
        }

        private DateTime? _StartDateTime;

        public DateTime? StartDateTime
        {
            get { return _StartDateTime; }
            set
            {
                _StartDateTime = value;
                RaisePropertyChanged("StartDateTime");
            }
        }

        private DateTime? _EndDateTime;

        public DateTime? EndDateTime
        {
            get { return _EndDateTime; }
            set
            {
                _EndDateTime = value;
                RaisePropertyChanged("EndDateTime");
            }
        }

        private List<Exception> _Errors;

        public List<Exception> Errors
        {
            get { return _Errors; }
            set
            {
                _Errors = value;
                RaisePropertyChanged("Errors");
            }
        }

        public async System.Threading.Tasks.Task Exec(bool isOnline)
        {
            StartDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
            IsCacheBusy = true;

            //TODO: return TPL Task, use ContinueWith to callback
            await Cache.Load(LastUpdatedDate, isOnline, () =>
            {
                IsCacheBusy = false;
                IsCacheLoaded = true;
                EndDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);
                Callback(this);
            });
        }
    }

    public class CacheManager : ICleanup
    {
        Action<ICollection<Exception>> ClientCallback;

        public EntityManager EntityManager => EntityManager.Current;

        ReferenceContext Context = new ReferenceContext();

        ObservableCollection<CacheLoader> CacheLoaders { get; set; }

#if OPENSILVER
        List<Meta<Lazy<ICache>>> CacheImplementations { get; set; }
#else
        List<Lazy<ICache, IVirtuosoCacheMetadata>> CacheImplementations { get; set; }
#endif
        readonly ILogger _logManager; //ordinarily this will belong to someone else - possibly an MEF singleton...

        //NOTE: clientCallback is used for the hosting XAML/View Model to know when each cache is finished.
        public CacheManager(ILogger logManager, Action<ICollection<Exception>> clientCallback)
        {
            ClientCallback = clientCallback;

            _logManager = logManager;

            CacheLoaders = new ObservableCollection<CacheLoader>();

#if OPENSILVER
            CacheImplementations = Virtuoso.Client.Core.VirtuosoContainer.Current.GetExports<ICache>()
                // These take the longest on the server, so kick these off first for better performance
                .OrderBy(s => s.Metadata["CacheName"].Equals("User") ? 0 : 1)
                .ThenBy(s => s.Metadata["CacheName"].Equals("Form") ? 0 : 1)
                .ThenBy(s => s.Metadata["CacheName"].Equals("Physician") ? 0 : 1)
                .ToList();
#else
            CacheImplementations = Virtuoso.Client.Core.VirtuosoContainer.Current.GetExports<ICache, IVirtuosoCacheMetadata>().ToList();
#endif

            foreach (var _cache in CacheImplementations)
            {
#if OPENSILVER
                CacheLoaders.Add(new CacheLoader(
                    _cache.Metadata["CacheName"].ToString(), //ReferenceTableNameHelper.Convert(ReferenceTableName.TenantSetting), //"Settings",
                    _cache.Value.Value,                      //create the singleton, NOTE: it hasn't 'started' loading yet...
                    cl => clientCallback(cl.Errors)
                ));
#else
            CacheLoaders.Add(new CacheLoader(
                    _cache.Metadata.CacheName, //ReferenceTableNameHelper.Convert(ReferenceTableName.TenantSetting), //"Settings",
                    _cache.Value,              //create the singleton, NOTE: it hasn't 'started' loading yet...
                    cl => clientCallback(cl.Errors)
                ));
#endif
            }
        }

        public async System.Threading.Tasks.Task BuildCaches(bool isOnline)
        {
            if (isOnline)
            {
                Context.EntityContainer.Clear();

                LoadOperation<ReferenceDataInfo> lo = Context.Load(Context.GetReferenceDataInfoQuery());
                lo.Completed += lo_Completed;
            }
            else
            {
                await ExecuteLoaders();
            }
        }

        async void lo_Completed(object sender, EventArgs e)
        {
            var loadOperation = sender as LoadOperation<ReferenceDataInfo>;
            loadOperation.Completed -= lo_Completed;
            foreach (var refDataInfo in loadOperation.Entities)
            {
                var loader = CacheLoaders.Where(c => c.CacheName == refDataInfo.Name).FirstOrDefault();
                if (loader != null)
                {
                    loader.LastUpdatedDate = refDataInfo.LastUpdatedDate;
                }
            }

            await ExecuteLoaders();
        }

        private async System.Threading.Tasks.Task ExecuteLoaders()
        {
            var _isOnline = EntityManager.IsOnline;
            foreach (var loader in CacheLoaders)
                try
                {
                    await loader.Exec(_isOnline);
                }
                catch (Exception e)
                {
                    //NO - this let's me into the application!!!! - need to show error and stop!
                    loader.IsCacheBusy = false;
                    loader.IsCacheLoaded = true;
                    loader.EndDateTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified);

                    Log(
                        String.Format("Failure in ExecuteLoaders().  Examine exception for error details.  Loader: {0}",
                            loader.CacheName), string.Empty, 0, 0, TraceEventType.Critical, e);

                    ClientCallback(new[] { e });
                }
        }

        public bool AllLoaded
        {
            get
            {
                var ret = CacheLoaders.All(c => c.IsCacheLoaded);
                return ret;
            }
        }

        public void Log(string message, string category, int priority, int eventId, TraceEventType severity, Exception exception)
        {
            _logManager.Log(
                severity,
                category,
                String.Format("{0} - {1}",
                    message,
                    exception.ToString()));

            //FYI: 
            //The default implementation of ToString obtains the name of the class that threw the current 
            //exception, the message (my emphasis), the result of calling ToString on the inner exception, 
            //and the result of calling Environment.StackTrace. If any of these members is Nothing, its value 
            //is not included in the returned string.
        }

        public void Cleanup()
        {
            if (CacheImplementations != null)
            {
                foreach (var cache in CacheImplementations)
                {
#if OPENSILVER
                    Client.Core.VirtuosoContainer.Current.ReleaseExport(cache.Value);
#else
                    Client.Core.VirtuosoContainer.Current.ReleaseExport(cache);
#endif
                }
                CacheImplementations.Clear();
            }
        }
    }
}