using System.ComponentModel.Composition;
using System.Windows.Controls;
using Virtuoso.Core.Cache;
using Virtuoso.Core.ViewModel;
using System;
using System.ComponentModel;
using System.Windows;
using Virtuoso.Client.Core;

namespace Virtuoso.Core
{
    public class MenuEventArgs : EventArgs
    {
        // Properties
        public string ID { get; set; }
        public object ViewModel { get; set; }
        public string IconLabel { get; set; }
        public object Object { get; set; }
        public string Label { get; set; }
        public string URL { get; set; }
    }

    public delegate void MenuEventHandler(object sender, MenuEventArgs e);
}

namespace Virtuoso.Core.View
{
    public partial class MenuBar : UserControl
    {
        // Events
        public event MenuEventHandler Click;

        public MenuBar()
        {
            InitializeComponent();

            if (DesignerProperties.IsInDesignTool)
            {
                Loaded += (o, e) =>
                {
                    this.DataContext = new MenuViewModel();
                };
            }
            else
            {
                DataContext = VirtuosoContainer.Current.GetExport<MenuViewModel>().Value;
            }
        }

        //catch internal 'Click' events - to raise external 'Click' event
        private void HyperlinkButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var hyperLinkButton = sender as HyperlinkButton;

            var mi = ((MenuViewModel)(DataContext)).GetMenuItem(hyperLinkButton.Tag.ToString());

            if (mi != null)
            {
                ((MenuViewModel)(DataContext)).SetSubMenuItems(hyperLinkButton.Tag.ToString());

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