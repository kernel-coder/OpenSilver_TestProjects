#region Usings

using System;
using System.Linq;

#endregion

namespace Virtuoso.Server.Data
{
    public partial class TeleMonitorResult
    {
        public DateTime? ResultTimePart =>
            ResultDateTimeOffset == null ? DateTime.Today.Date : ResultDateTimeOffset.Value.DateTime;

        public string ResultValueAndUnits => TruncatedResultValue + " " + ResultUnits;

        public string TruncatedResultValue
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ResultValue))
                {
                    return ResultValue;
                }

                if (ResultValue.Contains(".") == false)
                {
                    return ResultValue;
                }

                double Num;
                var isNum = double.TryParse(ResultValue, out Num);
                if (isNum == false)
                {
                    return ResultValue;
                }

                return string.Format("{0:0.00}", Num);
            }
        }

        public string ResultMonitorFieldPlusComments
        {
            get
            {
                string comments = null;
                var CR = char.ToString('\r');
                if (TeleMonitorBatch != null && TeleMonitorBatch.TeleMonitorComment != null)
                {
                    foreach (var tmc in TeleMonitorBatch.TeleMonitorComment
                                 .Where(c => c.TeleMonitorResultKey == TeleMonitorResultKey)
                                 .OrderBy(c => c.SequenceNumber))
                        comments = string.IsNullOrWhiteSpace(comments)
                            ? tmc.CommentText
                            : comments + CR + tmc.CommentText;
                }

                return string.IsNullOrWhiteSpace(comments)
                    ? "    " + ResultDescription
                    : "    " + ResultDescription + CR + comments;
            }
        }
    }
}