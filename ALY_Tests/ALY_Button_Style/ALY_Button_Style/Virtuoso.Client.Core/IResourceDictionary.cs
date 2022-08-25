#region Usings

using System.Windows;

#endregion

namespace Virtuoso.Core.Services
{
    public interface IResourceDictionary
    {
        ResourceDictionary CurrentResourceDictionary { get; }
    }
}