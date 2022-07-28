#region Usings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Virtuoso.Core.Model;

#endregion

namespace Virtuoso.Core.Cache
{
    public interface IAddressMap
    {
        Task<IEnumerable> Search(string zip, string state, DateTime? effectiveFrom, DateTime? effectiveTo,
            int take = 300);

        Task<IEnumerable<ZIPCode>> GetZIPCodes();
        Task<IEnumerable<COUNTYCode>> GetCOUNTYCodes();
        List<USAState> USAStates { get; }
    }
}