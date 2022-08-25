using System.ComponentModel.Composition;
using System.Windows.Controls;
using Virtuoso.Core.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;
using Virtuoso.Client.Core;

namespace Virtuoso.Core.View
{
    public partial class ContextSensitiveMenuBar : UserControl
    {
        // Events
        public event MenuEventHandler Click;

        public ContextSensitiveMenuBar()
        {
            InitializeComponent();

            if (DesignerProperties.IsInDesignTool)
            {
                Loaded += (o, e) =>
                {
                    this.DataContext = new ContextSensitiveMenuViewModel();
                };
            }
            else
            {
                this.DataContext = VirtuosoContainer.Current.GetExport<ContextSensitiveMenuViewModel>().Value; ;
            }

            ContextSensitiveMenuViewModel = this.DataContext as ContextSensitiveMenuViewModel;
        }

        public ContextSensitiveMenuViewModel ContextSensitiveMenuViewModel
        {
            get { return (ContextSensitiveMenuViewModel)GetValue(ContextSensitiveMenuViewModelProperty); }
            set { SetValue(ContextSensitiveMenuViewModelProperty, value); }
        }

        public static readonly DependencyProperty ContextSensitiveMenuViewModelProperty =
            DependencyProperty.Register("ContextSensitiveMenuViewModel", typeof(ContextSensitiveMenuViewModel),
                typeof(ContextSensitiveMenuBar), null);

        //catch internal 'Click' events - to raise external 'Click' event
        private void HyperlinkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var hyperLinkButton = sender as HyperlinkButton;

            var mi = ((ContextSensitiveMenuViewModel)(DataContext)).GetMenuItem(hyperLinkButton.Tag.ToString());

            if (mi != null)
            {
                ((ContextSensitiveMenuViewModel)(DataContext)).SetSubMenuItems(hyperLinkButton.Tag.ToString());

                if (Click != null)
                {
                    Click(this, new MenuEventArgs()
                    {
                        ViewModel = mi.ViewModel,
                        ID = hyperLinkButton.Tag.ToString(),
                        Object = mi.Object,
                        Label = mi.Label, //(hyperLinkButton.Content == null) ? "" : hyperLinkButton.Content.ToString(),
                        IconLabel = mi
                            .IconLabel, //(hyperLinkButton.Content == null) ? "" : hyperLinkButton.Content.ToString(),
                        URL = mi.URL //(hyperLinkButton.Tag == null) ? "" : hyperLinkButton.Tag.ToString()
                    });
                }
            }
        }
    }
}