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
    public partial class OasisAnswerRadioWithDate : UserControl
    {
        public OasisAnswerRadioWithDate()
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
            typeof(OasisAnswerRadioWithDate),
            new PropertyMetadata(null, new PropertyChangedCallback(OasisManagerAnswerChanged)));

        private static void OasisManagerAnswerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            OasisAnswerRadioWithDate me = sender as OasisAnswerRadioWithDate;
            OasisManagerAnswer newValue = e.NewValue as OasisManagerAnswer;
            if ((me != null) && (newValue != null))
            {
                me.Radio.GroupName = newValue.OasisAnswer.CachedOasisLayout.CMSField;
                // Binding: RadioButton IsChecked="{Binding OasisManagerAnswer.RadioResponse, Mode=TwoWay}" 
                Binding binding = new Binding();
                binding.Source = newValue;
                binding.Mode = BindingMode.TwoWay;
                binding.Path = new PropertyPath("RadioResponse");
                me.Radio.SetBinding(RadioButton.IsCheckedProperty, binding);
                me.Radio.IsHitTestVisible = newValue.Protected ? false : true;
                me.Radio.IsTabStop = newValue.Protected ? false : true;
                me.RadioContentTextBlock.Text = newValue.OasisAnswer.AnswerLabel + " -";
                me.RadioContentRichTextArea.ParagraphText = newValue.OasisAnswer.AnswerText;
                // Binding: vDatePicker DateObject="{Binding OasisManagerAnswerChildDate.DateResponse, Mode=TwoWay}"/>
                if (newValue.OasisManagerAnswerChildDate != null)
                {
                    me.RadioDatePicker.Visibility = Visibility.Visible;
                    me.RadioDatePicker.IsHitTestVisible = newValue.Protected ? false : true;
                    me.RadioDatePicker.IsTabStop = newValue.Protected ? false : true;
                    Binding bindingDO = new Binding();
                    bindingDO.Source = newValue.OasisManagerAnswerChildDate;
                    bindingDO.Mode = BindingMode.TwoWay;
                    bindingDO.Path = new PropertyPath("DateResponse");
                    me.RadioDatePicker.SetBinding(vDatePicker.DateObjectProperty, bindingDO);
                }
            }
        }

        #endregion
    }
}
