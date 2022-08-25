#region Usings

using System;
using System.Threading.Tasks;
using Virtuoso.Portable.Database;

#endregion

namespace Virtuoso.Core
{
    public interface IDatabaseWrapper : IDisposable
    {
        //Maybe not a handle to a database (could make it that) - for now, just a place to hold data for what the new 'flat' file implementation might need
        ISimpleStorage
            Storage
        {
            get;
        } //this will replace Siaqodb Current - once the concept is proofed out and the rest converted...

        string Name { get; }
        Task Start();
        void Initialize();
        Task RemovePriorDiskVersions(bool delete_current_version_only);
    }

    public enum VirtuosoDatabase
    {
        Master,
        Allergy,
        ICD,
        ICDCM9,
        ICDCM10,
        ICDPCS9,
        ICDPCS10,
        ICDGEMS9,
        ICDGEMS10,
        ICDCategory,
        Medication,

        AddressMapping
    }

    public interface IVirtuosoDatabaseMetadata
    {
        VirtuosoDatabase DatabaseName { get; }
    }
}