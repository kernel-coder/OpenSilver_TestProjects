#region Usings

using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.Services;

#endregion

namespace Virtuoso.Core
{
    public static class ApplicationCoreContext
    {
        private static ServiceLineService serviceLineService;

        public static ServiceLineService ServiceLineService
        {
            get { return serviceLineService; }
            set
            {
                if (value == serviceLineService)
                {
                    return;
                }

                serviceLineService = value;
                Messenger.Default.Send(serviceLineService);
            }
        }
    }
}