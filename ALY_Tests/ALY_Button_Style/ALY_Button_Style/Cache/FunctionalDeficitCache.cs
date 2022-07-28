#region Usings

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Services;
using Virtuoso.Portable;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Cache
{
    [ExportMetadata("CacheName", ReferenceTableName.FunctionalDeficit)]
    [Export(typeof(ICache))]
    public class FunctionalDeficitCache : ReferenceCacheBase<FunctionalDeficit>
    {
        public static FunctionalDeficitCache Current { get; private set; }
        protected override EntitySet EntitySet => Context.FunctionalDeficits;

        [ImportingConstructor]
        public FunctionalDeficitCache(ILogger logManager)
            : base(logManager, ReferenceTableName.FunctionalDeficit, "003")
        {
            if (Current == this)
            {
                throw new InvalidOperationException("FunctionalDeficitCache already initialized.");
            }

            Current = this;
        }

        protected override EntityQuery<FunctionalDeficit> GetEntityQuery()
        {
            return Context.GetFunctionalDeficitQuery();
        }

        public static List<FunctionalDeficit> GetFunctionalDeficits(bool includeEmpty = false)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            var ret = (from g in Current.Context.FunctionalDeficits.OrderBy(p => p.Sequence).ThenBy(p => p.Description)
                select g).ToList();
            if (includeEmpty)
            {
                ret.Insert(0,
                    new FunctionalDeficit { FunctionalDeficitKey = 0, Code = " ", Description = " ", Sequence = 0 });
            }

            return ret;
        }

        public static List<FunctionalDeficit> GetFunctionalDeficitsFromQuestionKey(int questionkey)
        {
            Current?.EnsureCacheReady();
            if ((Current == null) || (Current.Context == null))
            {
                return null;
            }

            var ret = (from g in Current.Context.FunctionalDeficits
                    .Where(p => p.QuestionFunctionalDeficit.Where(q => q.QuestionKey == questionkey).Any())
                    .OrderBy(p => p.Sequence).ThenBy(p => p.Description)
                select g).ToList();

            return ret;
        }
    }
}