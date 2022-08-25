﻿#region Usings

using System;
using System.Collections.Generic;
using System.Linq;

#endregion

namespace Virtuoso.Linq
{
    public static class VirtuosoExtensions
    {
        public static IEnumerable<TSource> WhereIf<TSource>(this IEnumerable<TSource> source, bool condition,
            Func<TSource, bool> predicate)
        {
            if (condition)
            {
                return source.Where(predicate);
            }

            return source;
        }
    }
}