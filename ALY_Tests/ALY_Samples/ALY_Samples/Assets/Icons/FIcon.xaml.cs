namespace Virtuoso.Core.Assets.Icons
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;

    public partial class FIcon : ContentPresenter
    {
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(FIcon), new PropertyMetadata(null));

        public FIcon()
        {
            this.InitializeComponent();
        }
    }
}
