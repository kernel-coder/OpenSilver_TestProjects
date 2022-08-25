using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.ServiceModel.DomainServices.Client;
using System.Windows.Threading;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Events;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;
using Virtuoso.Services.Web;

namespace Virtuoso.Core.Services
{
    public static class SaveVersionService
    {
        public static void Save(string version)
        {
            VirtuosoDomainContext Context = new VirtuosoDomainContext();

            try { Context.SaveVersion(version);} catch (Exception) { }
        }
       
    }
}
