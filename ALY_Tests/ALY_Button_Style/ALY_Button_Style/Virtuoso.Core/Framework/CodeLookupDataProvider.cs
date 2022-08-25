#region Usings

using System;
using System.Collections.Generic;
using Virtuoso.Core.Cache;
using Virtuoso.Validation;

#endregion

namespace Virtuoso.Core.Framework
{
    public class CodeLookupDataProvider : ICodeLookupDataProvider
    {
        Server.Data.CodeLookup ICodeLookupDataProvider.GetCodeLookupFromKey(int? key)
        {
            return CodeLookupCache.GetCodeLookupFromKey(key);
        }

        Server.Data.CodeLookup ICodeLookupDataProvider.GetCodeLookupFromCode(string code, int tenantID)
        {
            throw new NotImplementedException();
        }

        string ICodeLookupDataProvider.GetCodeLookupCodeFromKey(int? key)
        {
            return CodeLookupCache.GetCodeFromKey(key);
        }

        string ICodeLookupDataProvider.GetCodeLookupCodeDescriptionFromKey(int? key)
        {
            return CodeLookupCache.GetCodeDescriptionFromKey(key);
        }

        IEnumerable<Server.Data.CodeLookup> ICodeLookupDataProvider.GetCodeLookupsFromType(string codeType)
        {
            return CodeLookupCache.GetCodeLookupsFromType(codeType);
        }
    }
}