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

namespace Virtuoso.Core.Controls
{
    public partial class ChangeUserPopup : UserControl
    {
        public ChangeUserPopup()
        {
            InitializeComponent();
#if OPENSILVER
         HorizontalAlignment = HorizontalAlignment.Center;
         ScrollViewer.SetVerticalScrollBarVisibility(listBoxUsers, ScrollBarVisibility.Hidden);
#endif
      }
    }
}