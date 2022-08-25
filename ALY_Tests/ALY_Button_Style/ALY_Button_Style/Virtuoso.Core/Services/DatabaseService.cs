#region Usings

#if OPENSILVER
using Autofac.Features.Metadata;
#endif
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Virtuoso.Client.Core;
using Virtuoso.Client.Utils;
using Virtuoso.Services.Model;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IDatabaseLocator
    {
        IDatabaseWrapper DatabaseFor(VirtuosoDatabase databaseName);
    }

    public class DatabaseServiceImpl : IDatabaseLocator
    {
        public static DatabaseServiceImpl
            Current
        {
            get;
            private set;
        } //Used so that Cache can access it's SIAQODB database instance via - DatabaseWrapper = DatabaseService.Current.DatabaseFor(VirtuosoDatabase.Reference);

        protected string _localName = "Virtuoso DatabaseService";
        protected bool _ServiceStarted;

        protected LogManager _logger;

#if OPENSILVER
        public IList<Meta<Lazy<IDatabaseWrapper>>> _loadedDatabases =
            new List<Meta<Lazy<IDatabaseWrapper>>>();
#else
        public IList<Lazy<IDatabaseWrapper, IVirtuosoDatabaseMetadata>> _loadedDatabases =
            new List<Lazy<IDatabaseWrapper, IVirtuosoDatabaseMetadata>>();
#endif

        public void Start()
        {
            if (DesignerProperties.IsInDesignTool)
            {
                return;
            }

            _logger = VirtuosoFactory.GetLogger();

            try
            {
                _logger.Log(VirtuosoLogLevel.TRACE, DateTime.Now, "[Database Service] BEGIN StartService()", _localName,
                    null);

                ////////////////////////////////////////////////////////////////////////////
                //Only allow single instance of application to run at a time
                //LocalMessageReceiver incomingMessage = new LocalMessageReceiver(String.Format("{0}-{1}-{2}", _localName, Host, AppName));
                //Start listening       
                //incomingMessage.Listen();
                ////////////////////////////////////////////////////////////////////////////

                _ServiceStarted = true;
#if OPENSILVER
                _loadedDatabases = VirtuosoContainer.Current.GetExports<IDatabaseWrapper>().ToList();
#else
                _loadedDatabases = VirtuosoContainer.Current.GetExports<IDatabaseWrapper, IVirtuosoDatabaseMetadata>().ToList();
#endif
                // NOTE: 'Starting' each DatabaseService wrapper moved to respecitve cache
                Current = this;
            }
            catch (Exception e)
            {
                _logger.Log(VirtuosoLogLevel.FATAL, DateTime.Now, "[Database Service] failed", "DatabaseServiceImpl",
                    e);
                _ServiceStarted = false;
            }

            _logger.Log(VirtuosoLogLevel.TRACE, DateTime.Now, "[Database Service] END StartService()", _localName,
                null);
        }

        public void Stop()
        {

        }

        public void Dispose()
        {
            foreach (var op in _loadedDatabases)
            {
#if OPENSILVER
                _logger.Log(VirtuosoLogLevel.DEBUG, DateTime.Now, String.Format("Found database: {0}", op.Value.Value.Name), _localName, null);
                op.Value.Value.Dispose();
#else
                _logger.Log(VirtuosoLogLevel.DEBUG, DateTime.Now, String.Format("Found database: {0}", op.Value.Name), _localName, null);
                op.Value.Dispose();
#endif
            }

            GC.SuppressFinalize(this);
        }

        public IDatabaseWrapper DatabaseFor(VirtuosoDatabase databaseName)
        {
#if OPENSILVER
            var ret = (from db in _loadedDatabases where db.Metadata["DatabaseName"].ToString() == databaseName.ToString() select db.Value.Value).First();
            return ret;
#else
            var ret = (from db in _loadedDatabases where db.Metadata.DatabaseName == databaseName select db.Value).First();
            return ret;
#endif
        }
    }

    public class DatabaseService : DatabaseServiceImpl, IApplicationService, IApplicationLifetimeAware
    {
        public void StartService(ApplicationServiceContext context)
        {
            // NOTE: instead of running code in StartService, caller must await Initialize();
        }

        public void Initialize()
        {
            Start();
        }

        public void StopService()
        {
            Stop();
        }

        public void Starting()
        {
        }

        public void Started()
        {
        }

        public void Exiting()
        {
            if (DesignerProperties.IsInDesignTool)
            {
            }
        }

        public void Exited()
        {
            if (_ServiceStarted)
            {
                Dispose();
            }
        }
    }
}