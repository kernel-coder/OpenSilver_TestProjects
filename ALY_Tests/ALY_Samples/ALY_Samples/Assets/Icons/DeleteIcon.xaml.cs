using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Virtuoso.Core.Assets.Icons
{
    public partial class DeleteIcon : ContentPresenter
    {
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(DeleteIcon), new PropertyMetadata(System.Windows.Application.Current.Resources["HighlightBrush"]));

        public DeleteIcon()
        {
            InitializeComponent();
        }
    }
}
