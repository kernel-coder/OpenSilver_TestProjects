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
using System.Windows.Data;
using Virtuoso.Core.Services;

namespace Virtuoso.Core.Controls
{
    public partial class OasisAnswerRadioWithBorder : UserControl
    {
        public OasisAnswerRadioWithBorder()
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
            typeof(OasisAnswerRadioWithBorder),
            new PropertyMetadata(null, new PropertyChangedCallback(OasisManagerAnswerChanged)));

        private static void OasisManagerAnswerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            OasisAnswerRadioWithBorder me = sender as OasisAnswerRadioWithBorder;
            OasisManagerAnswer newValue = e.NewValue as OasisManagerAnswer;
            if ((me != null) && (newValue != null))
            {
                me.Radio.GroupName = newValue.OasisAnswer.CachedOasisLayout.CMSField;
                me.Radio.Content = newValue.OasisAnswer.AnswerLabel;
                // Binding: RadioButton IsChecked="{Binding OasisManagerAnswer.RadioResponse, Mode=TwoWay}" 
                Binding binding = new Binding();
                binding.Source = newValue;
                binding.Mode = BindingMode.TwoWay;
                binding.Path = new PropertyPath("RadioResponse");
                me.Radio.SetBinding(RadioButton.IsCheckedProperty, binding);
                me.Radio.IsHitTestVisible = newValue.Protected ? false : true;
                me.Radio.IsTabStop = newValue.Protected ? false : true;
            }
        }

        #endregion
    }
}
