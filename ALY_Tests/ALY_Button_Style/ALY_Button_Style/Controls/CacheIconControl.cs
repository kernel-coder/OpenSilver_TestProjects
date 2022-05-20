namespace Virtuoso.Home.V2.Controls
{
    using System.Windows.Controls;
    using Virtuoso.Core.Assets.Icons;

    public class CacheIconControl : ContentControl
    {
        public CacheIconControl()
        {
            MaxHeight = MaxWidth = 19;
            this.Content = new CacheIcon();
        }
    }
}
