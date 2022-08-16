#region Usings

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

#endregion

namespace Virtuoso.Core.Utility
{
    public static class EnumerableExtensions
    {
        //http://stackoverflow.com/questions/489258/linq-distinct-on-a-particular-property
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
            (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> coll)
        {
            var c = new ObservableCollection<T>();
            foreach (var e in coll)
                c.Add(e);
            return c;
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var e in enumerable) action(e);
        }

        /// <summary> 
        /// Invokes a transform function on each element of a sequence and returns the minimum Double value  
        /// if the sequence is not empty; otherwise returns the specified default value.  
        /// </summary> 
        /// <typeparam name="TSource">The type of the elements of source.</typeparam> 
        /// <param name="source">A sequence of values to determine the minimum value of.</param> 
        /// <param name="selector">A transform function to apply to each element.</param> 
        /// <param name="defaultValue">The default value.</param> 
        /// <returns>The minimum value in the sequence or default value if sequence is empty.</returns> 
        public static double MinOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector,
            double defaultValue)
        {
            if (source.Count() == 0)
            {
                return defaultValue;
            }

            return source.Min(selector);
        }

        public static int MinOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector,
            int defaultValue)
        {
            if (source.Count() == 0)
            {
                return defaultValue;
            }

            return source.Min(selector);
        }

        /// <summary> 
        /// Invokes a transform function on each element of a sequence and returns the maximum Double value  
        /// if the sequence is not empty; otherwise returns the specified default value.  
        /// </summary> 
        /// <typeparam name="TSource">The type of the elements of source.</typeparam> 
        /// <param name="source">A sequence of values to determine the maximum value of.</param> 
        /// <param name="selector">A transform function to apply to each element.</param> 
        /// <param name="defaultValue">The default value.</param> 
        /// <returns>The maximum value in the sequence or default value if sequence is empty.</returns> 
        public static double MaxOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector,
            double defaultValue)
        {
            if (source.Count() == 0)
            {
                return defaultValue;
            }

            return source.Max(selector);
        }

        public static int MaxOrDefault<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector,
            int defaultValue)
        {
            if (source.Count() == 0)
            {
                return defaultValue;
            }

            return source.Max(selector);
        }

        public static IEnumerable<t> DistinctBy<t>(this IEnumerable<t> list, Func<t, object> propertySelector)
        {
            return list.GroupBy(propertySelector).Select(x => x.First());
        }
    }
}