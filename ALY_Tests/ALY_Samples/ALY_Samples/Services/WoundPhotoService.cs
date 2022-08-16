#region Usings

using System;
using System.ComponentModel.Composition;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
//for ObservableCollection.ForEach

#endregion

namespace Virtuoso.Core.Services
{
    public interface IWoundPhotoService
    {
        EntitySet<WoundPhoto> EntitySet_WoundPhoto { get; }
        void ClearWoundPhotos();
        void GetWoundPhotoByNumberAsync(int AdmissionKey, int Number);
        event EventHandler<EntityEventArgs<WoundPhoto>> OnGetWoundPhotosLoaded;
    }

    [PartCreationPolicy(CreationPolicy.NonShared)]
    [Export(typeof(IWoundPhotoService))]
    public class WoundPhotoService : PagedModelBase, IWoundPhotoService
    {
        #region PagedModelBase Members

        public override void LoadData()
        {
        }

        #endregion

        private VirtuosoDomainContext context { get; set; }

        public WoundPhotoService()
        {
            context = new VirtuosoDomainContext();
        }

        public EntitySet<WoundPhoto> EntitySet_WoundPhoto => context?.WoundPhotos;

        public void ClearWoundPhotos()
        {
            context.WoundPhotos.Clear();
        }

        public void GetWoundPhotoByNumberAsync(int AdmissionKey, int Number)
        {
            ClearWoundPhotos();

            var query = context.GetWoundPhotoByNumberQuery(AdmissionKey, Number);
            context.Load(
                query,
                LoadBehavior.RefreshCurrent,
                g => HandleEntityResults(g, OnGetWoundPhotosLoaded),
                null);
        }

        public event EventHandler<EntityEventArgs<WoundPhoto>> OnGetWoundPhotosLoaded;
    }
}