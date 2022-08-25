namespace Virtuoso.Core.Interface
{
    public interface IAdminViewModel
    {
        bool IsBusy { get; set; }
        bool NavigatingFromDueToFormOpen { get; set; }
    }
}