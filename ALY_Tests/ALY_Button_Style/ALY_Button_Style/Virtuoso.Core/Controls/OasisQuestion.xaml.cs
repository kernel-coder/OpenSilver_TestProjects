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
using Virtuoso.Core.Services;
using Virtuoso.Core.View;

namespace Virtuoso.Core.Controls
{
    public partial class OasisQuestion : UserControl
    {
        public OasisQuestion()
        {
            InitializeComponent();
        }
        #region OasisManagerQuestion dependency property

        public OasisManagerQuestion OasisManagerQuestion
        {
            get { return (OasisManagerQuestion)GetValue(OasisManagerQuestionProperty); }
            set { SetValue(OasisManagerQuestionProperty, value); }
        }

        public static readonly DependencyProperty OasisManagerQuestionProperty =
            DependencyProperty.Register("OasisManagerQuestion",
            typeof(OasisManagerQuestion),
            typeof(OasisQuestion),
            new PropertyMetadata(null, new PropertyChangedCallback(OasisManagerQuestionChanged)));

        private static void OasisManagerQuestionChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            OasisQuestion me = sender as OasisQuestion;
            OasisManagerQuestion newValue = e.NewValue as OasisManagerQuestion;
            if ((me != null) && (newValue != null))
            {
                me.OasisFinancialIndicator.Visibility = (newValue.OasisQuestion.FinancialIndicator) ? Visibility.Visible : Visibility.Collapsed;
                me.questionHyperlinkButton.Content = string.Format("({0})", newValue.OasisQuestion.Question);
                me.questionHyperlinkButtonRed.Content = string.Format("({0})", newValue.OasisQuestion.Question);
                me.questionTextBlock.Text = string.Format("({0})", newValue.OasisQuestion.Question);
                me.questionTextBlockRed.Text = string.Format("({0})", newValue.OasisQuestion.Question);
                me.questionStackPanelHyperlinkButton.Visibility = IsQuestionHIS(newValue) ? Visibility.Collapsed : Visibility.Visible;
                me.questionStackPanelTextBlock.Visibility = IsQuestionHIS(newValue) ? Visibility.Visible : Visibility.Collapsed;
                me.OasisHelpButton.Visibility = IsQuestionHIS(newValue) ? Visibility.Collapsed : Visibility.Visible;
                me.QuestionText.ParagraphText = newValue.OasisQuestion.QuestionText;
                me.OasisLookbackButton.Visibility = (newValue.IsLookbackQuestion) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
        private static bool IsQuestionHIS(OasisManagerQuestion q)
        {
            if ((q == null) || (q.OasisManager == null) || (q.OasisManager.CurrentEncounter == null)) return false;
            return q.OasisManager.CurrentEncounter.SYS_CDIsHospice;
        }
        #endregion

        private void OasisHelpButton_Click(object sender, RoutedEventArgs e)
        {
            if (OasisManagerQuestion == null) return;
            OasisHelpChildWindow cw = new OasisHelpChildWindow(OasisManagerQuestion);
            cw.Show();
        }
        private void OasisLookbackButton_Click(object sender, RoutedEventArgs e)
        {
            if (OasisManagerQuestion == null) return;
            if (OasisManagerQuestion.OasisManager == null) return;
            if (OasisManagerQuestion.OasisQuestion == null) return;
            OasisManagerQuestion.OasisManager.LookbackShowPopup(OasisManagerQuestion.OasisQuestion.Question);
        }
    }
}
