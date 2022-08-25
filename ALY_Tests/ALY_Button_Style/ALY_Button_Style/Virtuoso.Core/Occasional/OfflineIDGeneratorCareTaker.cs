#region Usings

using System.IO;
using System.Threading.Tasks;
using Virtuoso.Client.Core;
using Virtuoso.Client.Core.Storage;

#endregion

namespace Virtuoso.Core.Occasional
{
    public class OfflineIDGeneratorCareTaker
    {
        public IDGeneratorMemento Memento { get; set; }
    }

    public class PersistentOfflineIDGeneratorCareTaker : OfflineIDGeneratorCareTaker, IPersistentCareTaker
    {
        IStore Storage { get; set; } //helper to save data somewhere

        public PersistentOfflineIDGeneratorCareTaker(IStore storage)
        {
            Storage = storage;
        }

        public static string GetFileName()
        {
            return Path.Combine(ApplicationStoreInfo.GetUserStoreForApplication(Constants.PRIVATE_APPDATA_FOLDER),
                Constants.IDGEN_SAVE_FILENAME);
        }

#if OPENSILVER
        public async Task Save()
        {
            var data = JSONSerializer.SerializeToJsonString(Memento);
            await Storage.Save(data);
        }

        public async Task Load()
        {
            var data = await Storage.Load();
            Memento = JSONSerializer.Deserialize<IDGeneratorMemento>(data);
        }
#else
        public void Save()
        {
            var data = JSONSerializer.SerializeToJsonString(Memento);
            Storage.SaveSync(data);
        }

        public void Load()
        {
            var data = Storage.LoadSync();
            Memento = JSONSerializer.Deserialize<IDGeneratorMemento>(data);
        }
#endif
    }
}