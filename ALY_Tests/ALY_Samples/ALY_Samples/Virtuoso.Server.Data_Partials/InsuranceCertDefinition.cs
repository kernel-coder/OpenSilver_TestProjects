namespace Virtuoso.Server.Data
{
    public partial class InsuranceCertDefinition
    {
        public bool WrapNOEFlag
        {
            get { return IsNoticeOfElectionRequired; }
            set
            {
                IsNoticeOfElectionRequired = value;
                if (Insurance != null)
                {
                    Insurance.SignalCertChildChange();
                }
            }
        }
    }
}