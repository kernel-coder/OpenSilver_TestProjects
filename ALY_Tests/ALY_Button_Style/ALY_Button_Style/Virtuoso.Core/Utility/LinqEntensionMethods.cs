#region Usings

using System.Collections.Generic;
using System.Collections.ObjectModel;

#endregion

namespace Virtuoso.Core
{
    public static class LinqEntensionMethods
    {
        public static ObservableCollection<TSource> ToObservableCollection<TSource>(this IEnumerable<TSource> source)
        {
            var Output = new ObservableCollection<TSource>();
            foreach (var Item in source)
                Output.Add(Item);
            return Output;
        }
    }
}