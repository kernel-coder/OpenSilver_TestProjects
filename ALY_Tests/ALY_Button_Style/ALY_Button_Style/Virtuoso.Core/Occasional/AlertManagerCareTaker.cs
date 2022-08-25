#region Usings

using System.Threading.Tasks;
using Virtuoso.Client.Core;
using Virtuoso.Client.Core.Storage;

#endregion

namespace Virtuoso.Core.Occasional
{
    public class AlertManagerCareTaker
    {
        public AlertManagerMemento Memento { get; set; }
    }

    public class PersistentAlertManagerCareTaker : AlertManagerCareTaker, IPersistentCareTaker
    {
        IStore Storage { get; set; } //helper to save data somewhere

        public PersistentAlertManagerCareTaker(IStore storage)
        {
            Storage = storage;
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
            Memento = JSONSerializer.Deserialize<AlertManagerMemento>(data);
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
            Memento = JSONSerializer.Deserialize<AlertManagerMemento>(data);
        }
#endif
    }
}