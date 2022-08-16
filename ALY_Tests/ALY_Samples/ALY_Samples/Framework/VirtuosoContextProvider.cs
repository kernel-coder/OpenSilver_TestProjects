#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    //A client-side/Silverlight, proxy around DomainContext, for accessing EntitySets in client-side custom validators
    public class VirtuosoContextProvider : IVirtuosoContextProvider
    {
        WeakReference _weakRef;
        VirtuosoDomainContext Context => _weakRef.Target as VirtuosoDomainContext;

        public VirtuosoContextProvider(VirtuosoDomainContext context)
        {
            _weakRef = new WeakReference(context);
        }

        public IEnumerable<AdmissionDiscipline> AdmissionDisciplinesByAdmissionKey(int AdmissionKey)
        {
            return Context.AdmissionDisciplines.Where(ad => ad.AdmissionKey == AdmissionKey);
        }
    }
}