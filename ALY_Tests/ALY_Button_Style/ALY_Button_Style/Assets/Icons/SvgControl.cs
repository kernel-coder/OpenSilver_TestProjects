namespace Virtuoso.Core.Assets.Icons
{
    using CSHTML5.Native.Html.Controls;
    using System.Diagnostics;
    using System.IO;
    using System.Windows;

    public class SvgControl : HtmlPresenter
    {
        public void SetSize(double wh)
        {
            SetSize(wh, wh);
        }

        public void SetSize(double w, double h)
        {
            MaxWidth = Width = w;
            MaxHeight = Height = h;
        }

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
                    using(var stream = control.GetType().Assembly.GetManifestResourceStream(source))
                    {
                        if(stream is null)
                        {
                            Debug.WriteLine($"Icon {source} is not found");
                            control.Html = "";
                        }
                        else
                        {
                            using(var reader = new StreamReader(stream))
                            {
                                control.Html = reader.ReadToEnd();
                            }
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
