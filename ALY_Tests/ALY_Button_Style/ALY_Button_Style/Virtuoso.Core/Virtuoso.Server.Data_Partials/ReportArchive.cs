namespace Virtuoso.Server.Data
{
    public partial class ReportArchive
    {
        private bool _SelectedToPrint;

        public bool SelectedToPrint
        {
            get { return _SelectedToPrint; }
            set
            {
                _SelectedToPrint = value;
                RaisePropertyChanged("SelectedToPrint");
            }
        }
    }
}