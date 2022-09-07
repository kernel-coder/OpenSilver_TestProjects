using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ALY_Button_Style
{
    public partial class FocusTestPage : Page
    {
        public FocusTestPage()
        {
            this.InitializeComponent();
            //this.TextBlock1.IsTabStop = false;
            //this.textBox1.IsTabStop = false;
            //this.textBox2.IsTabStop = false;
            // Enter construction logic here...
        }

        private void TextBlock1_Click(object sender, RoutedEventArgs e)
        {
           // this.IsTabStop = false;
            //var result = VisualTreeHelper.GetVisualChildren(this);
            //if (result != null) SetTabStop(result, false);
            var childWindow = new CWFocusTest();
            childWindow.Closed += ChildWindow_Closed;
            childWindow.Show();
            //if (result != null) SetTabStop(result, true);
        }

        private void ChildWindow_Closed(object sender, EventArgs e)
        {
            //var result = VisualTreeHelper.GetVisualChildren(this);
            //if (result != null) SetTabStop(result, true);
        }

        private void SetTabStop(IEnumerable<DependencyObject> controls, bool value)
        {
            //foreach(var item in controls)
            //{
            //    if (item.GetType() == typeof(StackPanel) || item.GetType()==typeof(Grid))
            //    {
            //        Panel panel = (Panel)item;
            //        SetTabStop(panel.Children, value);
            //    }
            //    else if (item.GetType() == typeof(Button) || item.GetType()==typeof(TextBox) || item.GetType()==typeof(CheckBox)
            //        || item.GetType()==typeof(RadioButton))
            //    {
            //        var ctrl = item as Control;
            //        ctrl.IsTabStop = value;
            //    }
            //    else if (item.GetType() == typeof(Control))
            //    {
            //        var ctrl = item as Control;
            //        ctrl.IsTabStop = value;
            //    }
            //    else
            //    {

            //    }
            //}
        }
    }
}
