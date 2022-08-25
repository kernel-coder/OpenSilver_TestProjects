#region Usings

using System;
using System.Collections.Generic;
using System.Linq;
using Virtuoso.Core.Cache;
using Virtuoso.Core.Model;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services.MAR
{
    public class MARMedResult
    {
        public List<MARMed> PRN { get; private set; }
        public List<MARMed> STD { get; private set; }

        internal MARMedResult(List<MARMed> prn, List<MARMed> std)
        {
            PRN = prn;
            STD = std;
        }
    }
}
