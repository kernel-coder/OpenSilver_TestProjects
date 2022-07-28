#region Usings

using System.Linq;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class InsuranceVerifyHistoryDetail
    {
        public bool CanMoveUp
        {
            get
            {
                var canMoveUp = false;

                if (InsuranceVerifyHistory != null)
                {
                    if (InsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Any(ivhd =>
                            ivhd.SequenceNumber < SequenceNumber
                            && !ivhd.Inactive
                        )
                       )
                    {
                        canMoveUp = true;
                    }
                }

                return canMoveUp;
            }
        }

        public bool CanMoveDown
        {
            get
            {
                var canMoveDown = false;

                if (InsuranceVerifyHistory != null)
                {
                    if (InsuranceVerifyHistory.InsuranceVerifyHistoryDetail.Any(ivhd =>
                            ivhd.SequenceNumber > SequenceNumber
                            && !ivhd.Inactive
                        )
                       )
                    {
                        canMoveDown = true;
                    }
                }

                return canMoveDown;
            }
        }
    }
}