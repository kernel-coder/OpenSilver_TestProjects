#region Usings

using System;
using System.Collections.Generic;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;

#endregion

namespace Virtuoso.Core.Events
{
    public class EntityEventArgs<T> : ErrorEventArgs
    {
        readonly IEnumerable<T> _results;
        readonly int _TotalEntityCount;

        public EntityEventArgs(IEnumerable<T> results, int totalEntityCount = 0,
            DataLoadType loadType = DataLoadType.SERVER)
            : base(null)
        {
            _results = results;
            _TotalEntityCount = totalEntityCount;
        }

        public EntityEventArgs(Exception ex, DataLoadType loadType = DataLoadType.SERVER)
            : base(ex)
        {
        }

        public int TotalEntityCount => _TotalEntityCount;

        public IEnumerable<T> Results => _results;
    }

    public class EntityEventArgs : ErrorEventArgs
    {
        public EntityEventArgs(DataLoadType loadType = DataLoadType.SERVER)
            : base(null, loadType)
        {
        }

        public EntityEventArgs(Exception ex, DataLoadType loadType = DataLoadType.SERVER)
            : base(ex, loadType)
        {
        }
    }

    public class BatchEventArgs : ErrorEventArgs
    {
        public BatchEventArgs(DomainContextLoadBatch batch, DataLoadType loadType = DataLoadType.SERVER)
            : base(null, loadType)
        {
            Batch = batch;
        }

        public BatchEventArgs(DomainContextLoadBatch batch, Exception ex, DataLoadType loadType = DataLoadType.SERVER)
            : base(ex, loadType)
        {
            Batch = batch;
        }

        public DomainContextLoadBatch Batch { get; set; }
    }

    public class ADFResponseEventArgs : EventArgs
    {
        public bool IgnoreEmptyResults { get; internal set; }

        public ADFResponseEventArgs()
        {
            IgnoreEmptyResults = false;
        }

        public ADFResponseEventArgs(bool ignoreEmptyResults)
        {
            //NOTE: will be true if we were not able to determine the vendor response count
            IgnoreEmptyResults = ignoreEmptyResults;
        }

        public List<ADFVendorResponse> adfVendorResponses { set; get; }
    }

    public class SHPAlertsRequestArgs : EventArgs
    {
        public string Response { set; get; }
    }
}