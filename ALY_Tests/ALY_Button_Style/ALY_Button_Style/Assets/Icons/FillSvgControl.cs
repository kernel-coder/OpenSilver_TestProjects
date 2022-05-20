namespace Virtuoso.Core.Assets.Icons
{
    using OpenSilver;
    using System.Windows;
    using System.Windows.Media;

    public abstract class FillSvgControl : SvgControl
    {
        protected string FillElementId = "fillElement";

        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(FillSvgControl), new PropertyMetadata(OnFillChanged));

        private static void OnFillChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is FillSvgControl icon)
            {
                if(icon.IsLoaded)
                {
                    icon.SetFillAttribute();
                }
            }
        }

        public FillSvgControl()
        {
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetFillAttribute();
        }

        private void SetFillAttribute()
        {
            if(Fill is null)
            {
                return;
            }

            // converting C# ARGB to JS RGBA color
            var hexARGB = Fill.ToString(); // #AARRGGBB
            var jsColor = $"#{hexARGB.Substring(3, 6)}{hexARGB.Substring(1, 2)}";

            Interop.ExecuteJavaScriptAsync($"$0.getElementById('{FillElementId}').setAttribute('fill','{jsColor}')", DomElement);
        }
    }
}
