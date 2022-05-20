namespace Virtuoso.Core.Assets.Icons
{
    using CSHTML5.Native.Html.Controls;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    public class SvgControl : HtmlPresenter
    {
        public string Source
        {
            get { return (string)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(string), typeof(SvgControl), new PropertyMetadata(OnSourceChanged));

        private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if(d is SvgControl control)
            {
                if(e.NewValue is string source && !string.IsNullOrEmpty(source))
                {
                    using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(source))
                    {
                        using(var reader = new StreamReader(stream))
                        {
                            control.Html = reader.ReadToEnd();
                        }
                    }
                }
                else
                {
                    control.Html = "";
                }
            }
        }
    }
}
