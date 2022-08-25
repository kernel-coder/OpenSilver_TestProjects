namespace Virtuoso.Core.Controls
{
    using System.Windows.Controls;
    using Virtuoso.Core.Assets.Icons;

    public class CheckMarkCoderReview : ContentControl
    {
        public CheckMarkCoderReview()
        {
            Content = new CheckMarkFilledIcon();
            VerticalAlignment = System.Windows.VerticalAlignment.Top;
            Width = Height = 16;
            Margin = new System.Windows.Thickness(0);
        }
    }
}
