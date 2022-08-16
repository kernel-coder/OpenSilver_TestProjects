#region Usings

using System;

#endregion

namespace Virtuoso.Core.Services
{
    internal class ConcurrencyConflict
    {
        public ConcurrencyConflict(object key)
        {
            Key = key;
        }

        public object Key { get; }

        public int KeyAsInt
        {
            get
            {
                if (Key != null)
                {
                    int _key = 0;
                    if (Int32.TryParse(Key.ToString(), out _key))
                    {
                        return _key;
                    }
                }

                return 0;
            }
        }
    }

    internal class NullConcurrencyConflict : ConcurrencyConflict
    {
        public NullConcurrencyConflict() : base(0)
        {
        }
    }
}