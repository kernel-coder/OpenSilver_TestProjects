#region Usings

using System;
using Virtuoso.Core.Interface;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class AdmissionCommunication : IDashboardSortable
    {
        public DateTime SortDate => CompletedDateTime.Date;
    }
}