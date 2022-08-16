using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using OpenRiaServices.DomainServices.Client;
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.Controls
{
    public partial class OKCancelEditStackPanel : UserControl
    {
        public OKCancelEditStackPanel()
        {
            InitializeComponent();
        }
        #region EditWhat dependency property
        public string EditWhat
        {
            get { return (string)GetValue(EditWhatProperty); }
            set { SetValue(EditWhatProperty, value); }
        }
        public static readonly DependencyProperty EditWhatProperty =
            DependencyProperty.Register("EditWhat", typeof(string), typeof(OKCancelEditStackPanel),
            new PropertyMetadata(null, new PropertyChangedCallback(EditWhatChanged)));
        private static void EditWhatChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            OKCancelEditStackPanel me = sender as OKCancelEditStackPanel;
            if (me == null) return;
            me.editHyperlinkButton.Content = "Edit " + me.EditWhat;
        }
        #endregion
    }
}
