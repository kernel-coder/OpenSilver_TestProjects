#region Usings

using System.Threading.Tasks;

#endregion

namespace Virtuoso.Client.Core.Storage
{
    public interface IPersistentCareTaker
    {
#if OPENSILVER
        Task Save();
        Task Load();
#else
        void Save();
        void Load();
#endif
    }

    public interface IStore
    {
        Task Save(string data);
        Task<string> Load();

#if !OPENSILVER
        /// <summary>
        /// *** DO NOT REFERENCE THIS FUNCTION ***
        ///
        /// This is only used by offline authentication and will eventually go away
        /// when Silverlight is deprecated. We don't want to abstract this since
        /// it will not be used in Blazor / OpenSilver.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        void SaveSync(string data);

        /// <summary>
        /// *** DO NOT REFERENCE THIS FUNCTION ***
        ///
        /// This is only used by offline authentication and will eventually go away
        /// when Silverlight is deprecated. We don't want to abstract this since
        /// it will not be used in Blazor / OpenSilver.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        string LoadSync();
#endif
    }
}