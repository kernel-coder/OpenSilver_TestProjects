#region Usings

using System.ComponentModel.Composition;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IBackgroundService
    {
        bool IsBackground { get; set; }
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IBackgroundService))]
    public class BackgroundService : IBackgroundService
    {
        public bool IsBackground { get; set; }

        public BackgroundService()
        {
            IsBackground = false;
        }
    }
}