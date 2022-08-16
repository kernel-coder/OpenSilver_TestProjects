using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Virtuoso.Core.Assets.Icons
{
    public partial class PencilIcon : ContentPresenter
    {
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(PencilIcon), new PropertyMetadata(null));

        public PencilIcon()
        {
            InitializeComponent();
        }
    }
}
