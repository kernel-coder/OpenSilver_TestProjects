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
    public partial class ForgotPasswordPopup : UserControl
    {
        public ForgotPasswordPopup()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsDisplayedProperty = DependencyProperty.Register("IsDisplayed",
            typeof(bool), typeof(ForgotPasswordPopup),
            new PropertyMetadata(new PropertyChangedCallback(IsDisplayedChanged)));

        public bool IsDisplayed
        {
            get { return (bool)GetValue(IsDisplayedProperty); }
            set { SetValue(IsDisplayedProperty, value); }
        }

        private static void IsDisplayedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ForgotPasswordPopup self = (ForgotPasswordPopup)d;
            if (self.IsDisplayed)
                self.txtSecurityAnswer.Focus();
        }
    }
}