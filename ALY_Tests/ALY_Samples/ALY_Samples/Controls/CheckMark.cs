namespace Virtuoso.Core.Controls
{
    using System.Windows.Controls;
    using Virtuoso.Core.Assets.Icons;

    public class CheckMark : ContentControl
    {
        public CheckMark()
        {
            //Content = new CheckMarkIcon();
            Width = Height = 25;
            Margin = new System.Windows.Thickness(0);
        }
    }
}
