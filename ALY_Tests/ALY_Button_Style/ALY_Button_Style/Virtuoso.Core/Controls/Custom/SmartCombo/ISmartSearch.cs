using System;
using System.Collections.Generic;

namespace Virtuoso.Core.Controls
{
  
    public interface ISmartSearch<TEntity> 

    {
        void LoadSmartSearchData(bool includeEmpty, object selectedValue);
        void SaveToMRU(TEntity MRUContents);


        List<TEntity> SmartSearchData { get; set; }
        List<TEntity> SmartSearchDataMRU { get; set; }

    }
}
