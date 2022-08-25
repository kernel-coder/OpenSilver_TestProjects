#region Usings

using System;

#endregion

namespace Virtuoso.Core.Occasional
{
    [Flags]
    public enum OfflineStoreType
    {
        SAVE = 0x01,
        CACHE = 0x02,
        AUTOSAVE = 0x03
    }
}