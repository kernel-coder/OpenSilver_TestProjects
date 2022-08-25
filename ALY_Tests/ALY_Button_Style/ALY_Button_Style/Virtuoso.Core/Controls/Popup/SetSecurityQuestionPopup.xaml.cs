using GalaSoft.MvvmLight.Messaging;
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
using Virtuoso.Core.Messages;

namespace Virtuoso.Core.Controls
{
    public partial class SetSecurityQuestionPopup : UserControl
    {
        public SetSecurityQuestionPopup()
        {
            InitializeComponent();
            SecurityQuestionComboBox.SelectedIndex = 0;
            Messenger.Default.Register<ResetPasswordMessage>(this, OnResetPassword);
        }

        private void OnResetPassword(ResetPasswordMessage obj)
        {
            SecurityQuestionComboBox.SelectedIndex = 0;
        }
    }
}