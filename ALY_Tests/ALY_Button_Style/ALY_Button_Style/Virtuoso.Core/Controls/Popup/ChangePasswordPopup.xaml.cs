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
    public partial class ChangePasswordPopup : UserControl
    {
        public ChangePasswordPopup()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsDisplayedProperty = DependencyProperty.Register("IsDisplayed",
            typeof(bool), typeof(ChangePasswordPopup),
            new PropertyMetadata(new PropertyChangedCallback(IsDisplayedChanged)));

        public bool IsDisplayed
        {
            get { return (bool)GetValue(IsDisplayedProperty); }
            set { SetValue(IsDisplayedProperty, value); }
        }

        private static void IsDisplayedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChangePasswordPopup self = (ChangePasswordPopup)d;
            if (self.IsDisplayed)
                self.pwdNewPassword.Focus();
        }
    }
}