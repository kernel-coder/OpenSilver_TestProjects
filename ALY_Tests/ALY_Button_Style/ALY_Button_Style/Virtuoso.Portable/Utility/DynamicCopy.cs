using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;

namespace Virtuoso.Portable.Utility
{
    public static class DynamicCopy
    {
        public static void CopyProperties<S, T>(S a, T b)
        {
            Type typeB = b.GetType();
            foreach (PropertyInfo property in a.GetType().GetProperties())
            {
                if (!property.CanRead || (property.GetIndexParameters().Length > 0))
                    continue;

                PropertyInfo other = typeB.GetProperty(property.Name);
                if ((other != null) && (other.CanWrite))
                    other.SetValue(b, property.GetValue(a, null), null);
            }
        }

        public static void CopyDataMemberProperties<S, T>(S source, T destination, string[] exclusionList = null)
        {
            Type typeB = destination.GetType();

            foreach (PropertyInfo property in source.GetType().GetProperties())
            {
                if (!property.CanRead || (property.GetIndexParameters().Length > 0))
                    continue;

                if (exclusionList != null && exclusionList.Contains(property.Name))
                    continue;

                object[] readOnly = property.GetCustomAttributes(typeof(System.ComponentModel.ReadOnlyAttribute), true);
                object[] dataMember = property.GetCustomAttributes(typeof(System.Runtime.Serialization.DataMemberAttribute), true);

                PropertyInfo other = typeB.GetProperty(property.Name);
                if ((other != null) && (other.CanWrite) && (readOnly.Length == 0) && (dataMember.Length > 0))
                {       
                    other.SetValue(destination, property.GetValue(source, null), null);
                }
            }
        }

    }

    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ////http://stackoverflow.com/questions/3319240/can-one-copy-the-contents-of-one-object-to-another-dynamically-if-they-have-the
    //// Usage
    ////Monkey monkey = new Monkey() { numberOfEyes = 7, name = "Henry" };
    ////Dog dog = new Dog();
    ////DynamicCopy.CopyProperties<IAnimal>(monkey, dog);
    ////Debug.Assert(dog.numberOfEyes == monkey.numberOfEyes);
    ////...    
    ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //public static class DynamicCopy
    //{
    //    public static void CopyProperties<T,S>(T source, S target)
    //    {
    //        Helper<T,S>.CopyProperties(source, target);
    //    }

    //    private static class Helper<T,S>
    //    {
    //        private static readonly Action<T, S>[] _copyProps = Prepare();

    //        private static Action<T, S>[] Prepare()
    //        {
    //            Type type = typeof(T);
    //            ParameterExpression source = Expression.Parameter(type, "source");
    //            ParameterExpression target = Expression.Parameter(type, "target");

    //            var copyProps = from prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
    //                            where prop.CanRead && prop.CanWrite
    //                            let getExpr = Expression.Property(source, prop)
    //                            let setExpr = Expression.Call(target, prop.GetSetMethod(true), getExpr)
    //                            select Expression.Lambda<Action<T, S>>(setExpr, source, target).Compile();

    //            return copyProps.ToArray();
    //        }

    //        public static void CopyProperties(T source, S target)
    //        {
    //            foreach (Action<T, S> copyProp in _copyProps)
    //                copyProp(source, target);
    //        }
    //    }
    //}

    ///// <summary>
    ///// Generic class which copies to its target type from a source
    ///// type specified in the Copy method. The types are specified
    ///// separately to take advantage of type inference on generic
    ///// method arguments.
    ///// </summary>
    //public static class PropertyCopy<TTarget> where TTarget : class, new()
    //{
    //    /// <summary>
    //    /// Copies all readable properties from the source to a new instance
    //    /// of TTarget.
    //    /// </summary>
    //    public static TTarget CopyFrom<TSource>(TSource source) where TSource : class
    //    {
    //        return PropertyCopier<TSource>.Copy(source);
    //    }

    //    /// <summary>
    //    /// Static class to efficiently store the compiled delegate which can
    //    /// do the copying. We need a bit of work to ensure that exceptions are
    //    /// appropriately propagated, as the exception is generated at type initialization
    //    /// time, but we wish it to be thrown as an ArgumentException.
    //    /// </summary>
    //    private static class PropertyCopier<TSource> where TSource : class
    //    {
    //        private static readonly Func<TSource, TTarget> copier;
    //        private static readonly Exception initializationException;

    //        internal static TTarget Copy(TSource source)
    //        {
    //            if (initializationException != null)
    //            {
    //                throw initializationException;
    //            }
    //            if (source == null)
    //            {
    //                throw new ArgumentNullException("source");
    //            }
    //            return copier(source);
    //        }

    //        static PropertyCopier()
    //        {
    //            try
    //            {
    //                copier = BuildCopier();
    //                initializationException = null;
    //            }
    //            catch (Exception e)
    //            {
    //                copier = null;
    //                initializationException = e;
    //            }
    //        }

    //        private static Func<TSource, TTarget> BuildCopier()
    //        {
    //            ParameterExpression sourceParameter = Expression.Parameter(typeof(TSource), "source");
    //            var bindings = new List<MemberBinding>();
    //            foreach (PropertyInfo sourceProperty in typeof(TSource).GetProperties())
    //            {
    //                if (!sourceProperty.CanRead)
    //                {
    //                    continue;
    //                }
    //                PropertyInfo targetProperty = typeof(TTarget).GetProperty(sourceProperty.Name);
    //                if (targetProperty == null)
    //                {
    //                    throw new ArgumentException("Property " + sourceProperty.Name + " is not present and accessible in " + typeof(TTarget).FullName);
    //                }
    //                if (!targetProperty.CanWrite)
    //                {
    //                    throw new ArgumentException("Property " + sourceProperty.Name + " is not writable in " + typeof(TTarget).FullName);
    //                }
    //                if (!targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
    //                {
    //                    throw new ArgumentException("Property " + sourceProperty.Name + " has an incompatible type in " + typeof(TTarget).FullName);
    //                }
    //                bindings.Add(Expression.Bind(targetProperty, Expression.Property(sourceParameter, sourceProperty)));
    //            }
    //            Expression initializer = Expression.MemberInit(Expression.New(typeof(TTarget)), bindings);
    //            return Expression.Lambda<Func<TSource, TTarget>>(initializer, sourceParameter).Compile();
    //        }
    //    }
    //}

}