#region Usings

using System;
using Virtuoso.Core.Model;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IFaxService
    {
        void FaxDocument(EncounterFaxParameters parameters, Action successCallBack, Action finallyCallback);
        void FaxInterimOrderBatchDocuments(string interimOrderBatchKeys, Action successCallBack, Action finallyCallback);
    }
}