using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using GalaSoft.MvvmLight.Messaging;
using Virtuoso.Core.ViewModel;

namespace Virtuoso.Core.View
{
    public partial class ToolsMenu : UserControl
    {
        bool _hasNavigated;

        public ToolsMenu()
        {
            InitializeComponent();
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            var hyperLinkButton = sender as HyperlinkButton;
            Uri uri = null;

            if (hyperLinkButton != null)
            {
                uri = new Uri(hyperLinkButton.Tag.ToString(), UriKind.Relative);
            }

            _hasNavigated = true;

            Messenger.Default.Send<Uri>(uri, "NavigationRequest");
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            if (!_hasNavigated)
            {
                Messenger.Default.Send<Uri>(new Uri("/Home", UriKind.Relative), "NavigationRequest");
            }

            _hasNavigated = false;
        }
    }
}