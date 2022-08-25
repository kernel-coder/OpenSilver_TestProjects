#region Usings

using System.Collections.Generic;
using GalaSoft.MvvmLight;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Model
{
    public class ComEntryLine : ICleanup
    {
        public int PrintItem { get; set; }
        public AdmissionCommunication ComEntry { get; set; }
        public List<AdmissionCommunication> AdmitComList { get; set; }

        public void Cleanup()
        {
            ComEntry = null;
            if (AdmitComList != null)
            {
                foreach (var ac in AdmitComList)
                {
                    if (ac.Admission != null)
                    {
                        ac.Admission.Cleanup();
                    }

                    ac.Cleanup();
                    VirtuosoObjectCleanupHelper.CleanupAll(ac);
                }
            }
        }
    }
}