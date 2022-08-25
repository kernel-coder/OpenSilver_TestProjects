using GalaSoft.MvvmLight.Messaging;
using System;
using System.Collections;
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

namespace Virtuoso.Core.View
{
    public partial class AuthDistributionView : UserControl
    {
        public AuthDistributionView()
        {
            InitializeComponent();
            //this.Loaded += AuthDistributionView_Loaded;
            //this.Unloaded += AuthDistributionView_Unloaded;
        }

        //void AuthDistributionView_Loaded(object sender, RoutedEventArgs e)
        //{
        //    Messenger.Default.Register<AuthorizationDistributionCreated>(this, (evt) =>
        //    {
        //        Deployment.Current.Dispatcher.BeginInvoke(() =>
        //        {
        //            DistributionGrid.Focus();
        //        });

        //        try
        //        {

        //            var enumerator = DistributionGrid.ItemsSource.GetEnumerator();
        //            enumerator.MoveNext();
        //            if (enumerator.Current != null)
        //            {
        //                var row = enumerator.Current as Virtuoso.Core.ViewModel.AuthDistributionViewModel.Distribution;
        //                //DistributionGrid.ScrollIntoView(row, DistributionGrid.Columns.First());
        //                FrameworkElement el = DistributionGrid.Columns.First().GetCellContent(row);
        //                Deployment.Current.Dispatcher.BeginInvoke(() =>
        //                {
        //                    try
        //                    {
        //                        ((TextBox)el).Focus();
        //                        ((TextBox)el).SelectAll();
        //                    }
        //                    catch (Exception)
        //                    {
        //                        //ignore exception - do not crash application if we couldn't set focus
        //                    }
        //                });
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            //ignore exception - do not crash application if we couldn't set focus
        //        }
        //    });
        //}

        void AuthDistributionView_Unloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister(this);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(((System.Windows.Controls.TextBox)(sender)).Text.Trim()))
            {
                ((System.Windows.Controls.TextBox)(sender)).Text = 0.ToString();
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                ((TextBox)sender).SelectAll();
            }
            catch
            {
            }
        }
    }
}