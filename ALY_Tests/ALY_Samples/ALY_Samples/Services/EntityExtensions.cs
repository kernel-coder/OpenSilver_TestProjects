#region Usings

using System;
using OpenRiaServices.DomainServices.Client;

#endregion

namespace Virtuoso.Core.Services
{
    public static class EntityExtensions
    {
        public static T IfNotNull<R, T>(this R r, Func<R, T> selector) where R : Entity
        {
            if (r != null)
            {
                return selector(r);
            }

            return default(T);
        }

        public static T IfNotNull<R, T>(this object r, Func<R, T> selector) where R : Entity
        {
            if (r != null)
            {
                try
                {
                    return selector((R)r);
                }
                catch (InvalidCastException)
                {
                    return default(T);
                }
            }

            return default(T);
        }

        //I know, dumb, but I need to cast and test the code when doing first pass refactor to get typed Validate methods - no time to refactor SubTypeItem to 
        //anything better.  This way the code is better typed, but still with the checks - and I don't have to test every doc type because of this refactor.
        public static T As<T>(this Entity entity, bool defaultObject = false) where T : Entity
        {
            if (entity.GetType() == typeof(T))
            {
                return (T)entity;
            }

            return (defaultObject) ? default(T) : null;
        }
    }
}