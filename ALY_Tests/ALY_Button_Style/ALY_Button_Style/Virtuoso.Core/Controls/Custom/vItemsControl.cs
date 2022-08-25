using System.Windows.Controls;
using System.Windows;
using System.Linq;
using System;
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.Controls
{
    public class vContentControl : ContentControl
    {
    }

    public class vItemsControl : ItemsControl
    {
        public event EventHandler OnvItemsSourceChanged;
        public static DependencyProperty vItemsSourceProperty =
          DependencyProperty.Register("vItemsSource", typeof(System.Collections.IEnumerable), typeof(Virtuoso.Core.Controls.vItemsControl),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.vItemsControl)o).vItemsSourceChanged();
          }));

        public System.Collections.IEnumerable vItemsSource
        {
            get { return ((System.Collections.IEnumerable)(base.GetValue(Virtuoso.Core.Controls.vItemsControl.vItemsSourceProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.vItemsControl.vItemsSourceProperty, value); }
        }
        private void vItemsSourceChanged()
        {
            var fn = VirtuosoObjectCleanupHelper.FindVisualChildren<Control>(this).ToList();
            foreach (var rc in fn)
            {
                vContentControl vcc = rc as vContentControl;
                if (vcc != null) vcc.Content = null;
            }

            ItemsSource = vItemsSource;
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                DeployvItemsSourceChanged();
            });
        }
        private void DeployvItemsSourceChanged()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (OnvItemsSourceChanged != null) OnvItemsSourceChanged(this, EventArgs.Empty);
            });
        }
    }
}
