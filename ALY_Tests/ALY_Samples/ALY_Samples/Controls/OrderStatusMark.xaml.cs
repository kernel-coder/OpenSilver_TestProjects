using System.Windows.Controls;
using System.Windows;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class OrderStatusMark : UserControl
    {
        #region OrderStatus dependency property

        public object OrderStatus
        {
            get { return (object)GetValue(OrderStatusProperty); }
            set { SetValue(OrderStatusProperty, value); }
        }

        public static readonly DependencyProperty OrderStatusProperty =
            DependencyProperty.Register("OrderStatus",
            typeof(object),
            typeof(OrderStatusMark),
            new PropertyMetadata(null, new PropertyChangedCallback(OrderStatusChanged)));

        private static void OrderStatusChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            OrderStatusMark me = sender as OrderStatusMark;
            if (me == null) return;
            int orderStatus = 0;
            try
            {
                orderStatus = System.Convert.ToInt32(me.OrderStatus);
            }
            catch
            {
                orderStatus = 0;
            }
            me.completeCheckMark.Visibility = ((orderStatus == (int)OrderStatusType.Completed) || (orderStatus == (int)OrderStatusType.SigningPhysicianVerified)) ? Visibility.Visible : Visibility.Collapsed;
            me.signingPhysicianVerifiedCheckMark.Visibility = (orderStatus == (int)OrderStatusType.SigningPhysicianVerified) ? Visibility.Visible : Visibility.Collapsed;
            me.reviewOrder.Visibility = ((orderStatus == (int)OrderStatusType.OrderEntryReview)) ? Visibility.Visible : Visibility.Collapsed;
            me.voidOrder.Visibility = ((orderStatus == (int)OrderStatusType.Voided)) ? Visibility.Visible : Visibility.Collapsed;

            me.toolTip.Text =
                ((orderStatus == (int)OrderStatusType.InProcess) ? "Order in-process/unsigned" :
                ((orderStatus == (int)OrderStatusType.OrderEntryReview) ? "Awaiting order entry review" :
                ((orderStatus == (int)OrderStatusType.Completed) ? "Order completed, awaiting physician signature" :
                ((orderStatus == (int)OrderStatusType.SigningPhysicianVerified) ? "Order completed, verified physician signature" :
                ((orderStatus == (int)OrderStatusType.Voided) ? "Order voided" : "")))));
        }

        #endregion

        public OrderStatusMark()
        {
            InitializeComponent();
        }
    }
}

