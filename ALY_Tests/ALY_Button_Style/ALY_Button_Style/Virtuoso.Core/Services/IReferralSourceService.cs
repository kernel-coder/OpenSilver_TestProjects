﻿#region Usings

using GalaSoft.MvvmLight;
using Virtuoso.Core.Framework;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IReferralSourceService : IModelDataService<ReferralSource>, ICleanup
    {
        PagedEntityCollectionView<ReferralSource> ReferralSources { get; }
    }
}