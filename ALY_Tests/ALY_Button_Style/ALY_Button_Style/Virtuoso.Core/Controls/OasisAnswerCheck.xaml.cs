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
using System.Windows.Data;

namespace Virtuoso.Core.Controls
{
    public partial class OasisAnswerCheck : UserControl
    {
        public OasisAnswerCheck()
        {
            InitializeComponent();
        }
        #region OasisManagerAnswer dependency property

        public OasisManagerAnswer OasisManagerAnswer
        {
            get { return (OasisManagerAnswer)GetValue(OasisManagerAnswerProperty); }
            set { SetValue(OasisManagerAnswerProperty, value); }
        }

        public static readonly DependencyProperty OasisManagerAnswerProperty =
            DependencyProperty.Register("OasisManagerAnswer",
            typeof(OasisManagerAnswer),
            typeof(OasisAnswerCheck),
            new PropertyMetadata(null, new PropertyChangedCallback(OasisManagerAnswerChanged)));

        private static void OasisManagerAnswerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            OasisAnswerCheck me = sender as OasisAnswerCheck;
            OasisManagerAnswer newValue = e.NewValue as OasisManagerAnswer;
            if ((me != null) && (newValue != null))
            {
                // Binding: CheckBox IsChecked="{Binding OasisManagerAnswer.CheckBoxResponse, Mode=TwoWay}" 
                Binding binding = new Binding();
                binding.Source = newValue;
                binding.Mode = BindingMode.TwoWay;
                binding.Path = new PropertyPath("CheckBoxResponse");
                me.Check.SetBinding(CheckBox.IsCheckedProperty, binding);
                me.Check.IsHitTestVisible = newValue.Protected ? false : true;
                me.Check.IsTabStop = newValue.Protected ? false : true;
                me.CheckContentTextBlock.Text = newValue.OasisAnswer.AnswerLabel + " -";
                me.CheckContentRichTextArea.ParagraphText = newValue.OasisAnswer.AnswerText;
            }
        }

        #endregion
    }
}
