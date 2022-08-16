using System.Windows.Controls;
using System.Windows;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class EncounterStatusMark : UserControl
    {
        #region StatusMark dependency property

        public object StatusMark
        {
            get { return (object)GetValue(StatusMarkProperty); }
            set { SetValue(StatusMarkProperty, value); }
        }

        public static readonly DependencyProperty StatusMarkProperty =
            DependencyProperty.Register("StatusMark",
            typeof(object),
            typeof(EncounterStatusMark),
            new PropertyMetadata(null, new PropertyChangedCallback(StatusMarkChanged)));

        private static void StatusMarkChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            EncounterStatusMark me = sender as EncounterStatusMark;
            if (me == null) return;
            int statusMark = (int)EncounterStatusType.None;
            try
            {
                statusMark = System.Convert.ToInt32(me.StatusMark);
            }
            catch
            {
                statusMark = (int)EncounterStatusType.None;
            }
            me.completeCheckMark.Visibility = (statusMark == (int)EncounterStatusType.Completed) ? Visibility.Visible : Visibility.Collapsed;
            me.reviewCoder.Visibility = ((statusMark == (int)EncounterStatusType.CoderReview) || (statusMark == (int)EncounterStatusType.CoderReviewEdit)) ? Visibility.Visible : Visibility.Collapsed;
            me.reviewOASIS.Visibility = ((statusMark == (int)EncounterStatusType.OASISReview) || (statusMark == (int)EncounterStatusType.OASISReviewEdit) || (statusMark == (int)EncounterStatusType.OASISReviewEditRR)) ? Visibility.Visible : Visibility.Collapsed;
            me.reviewHIS.Visibility = ((statusMark == (int)EncounterStatusType.HISReview) || (statusMark == (int)EncounterStatusType.HISReviewEdit) || (statusMark == (int)EncounterStatusType.HISReviewEditRR)) ? Visibility.Visible : Visibility.Collapsed;
            me.reviewPOCOrder.Visibility = (statusMark == (int)EncounterStatusType.POCOrderReview) ? Visibility.Visible : Visibility.Collapsed;
            me.reviewCheckMark.Visibility = ((statusMark == (int)EncounterStatusType.CoderReviewEdit) || (statusMark == (int)EncounterStatusType.OASISReviewEdit) || (statusMark == (int)EncounterStatusType.OASISReviewEditRR) || (statusMark == (int)EncounterStatusType.HISReviewEdit) || (statusMark == (int)EncounterStatusType.HISReviewEditRR)) ? Visibility.Visible : Visibility.Collapsed;
            me.toolTip.Text =
                ((statusMark == (int)EncounterStatusType.Completed) ? "Completed/Signed" :
                ((statusMark == (int)EncounterStatusType.CoderReview) ? "Awaiting diagnosis review" :
                ((statusMark == (int)EncounterStatusType.CoderReviewEdit) ? "Diagnosis review complete, awaiting clinical input" :
                ((statusMark == (int)EncounterStatusType.POCOrderReview) ? "Awaiting POC order review" :
                ((statusMark == (int)EncounterStatusType.OASISReview) ? "Awaiting OASIS coordinator review" :
                ((statusMark == (int)EncounterStatusType.OASISReviewEdit) ? "Failed OASIS coordinator review, awaiting clinical input" :
                ((statusMark == (int)EncounterStatusType.OASISReviewEditRR) ? "Failed OASIS coordinator review, awaiting clinical input" :
                ((statusMark == (int)EncounterStatusType.HISReview) ? "Awaiting HIS coordinator review" :
                ((statusMark == (int)EncounterStatusType.HISReviewEdit) ? "Failed HIS coordinator review, awaiting clinical input" :
                ((statusMark == (int)EncounterStatusType.HISReviewEditRR) ? "Failed HIS coordinator review, awaiting clinical input" : ""))))))))));
        }

        #endregion

        public EncounterStatusMark()
        {
            InitializeComponent();
        }
    }
}
