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
using Virtuoso.Core.Utility;

namespace Virtuoso.Core.Controls
{
    public partial class OKCancelStackPanelAlwaysVisible : UserControl
    {
        public OKCancelStackPanelAlwaysVisible()
        {
            InitializeComponent();

            
        }

         public static readonly DependencyProperty OkButtonTextProperty =
        DependencyProperty.Register("OkButtonText", typeof(string), typeof(OKCancelStackPanelAlwaysVisible), null);

         public string OkButtonText
         {
             get
             {
                 return (string)GetValue(OkButtonTextProperty);
             }
             set
             {
                 SetValue(OkButtonTextProperty, value);
                 OkButton.Content = value;
             }
         
         }

    }
}
