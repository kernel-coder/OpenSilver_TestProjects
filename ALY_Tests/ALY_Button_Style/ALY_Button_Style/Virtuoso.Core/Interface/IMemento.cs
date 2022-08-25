#region Usings

using Virtuoso.Core.Occasional;

#endregion

namespace Virtuoso.Core.Interface
{
    public interface IOfflineIDGeneratorMemento
    {
        IDGeneratorMemento GetMemento();
        void SetMemento(IDGeneratorMemento memento);
    }

    public interface IAlertManagerMemento
    {
        AlertManagerMemento GetMemento();
        void SetMemento(AlertManagerMemento memento);
    }
}