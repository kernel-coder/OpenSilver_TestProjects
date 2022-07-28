//using System;
//using System.Net;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Documents;
//using System.Windows.Ink;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Animation;
//using System.Windows.Shapes;
//using Wintellect.Sterling;
//using System.ComponentModel;
//using System.Diagnostics;
//using Virtuoso.Core.OccasionallyConnected.ObjectStores;

//namespace Virtuoso.Core.Services
//{
//    public sealed class SterlingService : IApplicationService, IApplicationLifetimeAware, IDisposable
//    {
//        //private SterlingEngine _engine;

//        public static SterlingService Current { get; private set; }

//        public SterlingObjectStore SterlingObjectStore { get; private set; }

//        //public ISterlingDatabaseInstance Database { get; private set; } //SterlingObjectStore contains this

//        //private SterlingDefaultLogger _logger;

//        public void StartService(ApplicationServiceContext context)
//        {
//            if (DesignerProperties.IsInDesignTool) return;
//            //_engine = new SterlingEngine();
//            SterlingObjectStore = new SterlingObjectStore();
//            Current = this;
//        }

//        public void StopService()
//        {
//            return;
//        }

//        public void Starting()
//        {
//            if (DesignerProperties.IsInDesignTool) return;

//            SterlingObjectStore.ActivateEngine();
//        }

//        public void Started()
//        {
//            return;
//        }

//        public void Exiting()
//        {
//            if (DesignerProperties.IsInDesignTool) return;

//            //if (Debugger.IsAttached && _logger != null)
//            //{
//            //    _logger.Detach();
//            //}
//        }

//        public void Exited()
//        {
//            Dispose();
//            return;
//        }

//        public void Dispose()
//        {
//            SterlingObjectStore.DeactivateEngine();
//            GC.SuppressFinalize(this);
//        }
//    }

//}
