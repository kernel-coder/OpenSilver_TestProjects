using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Core.Controls
{
    public partial class BlockRadioButton : RadioButton
    {
        public BlockRadioButton()
        {
            InitializeComponent();
        }

        public static DependencyProperty HeaderProperty =
            DependencyProperty.Register("Header", typeof(string), typeof(BlockRadioButton), new PropertyMetadata(null));

        public object Header
        {
            get
            {
                var s = (GetValue(HeaderProperty)) as string;
                return s;
            }
            set { SetValue(HeaderProperty, value); }
        }

    }
}
