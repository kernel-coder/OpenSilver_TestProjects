using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Virtuoso.Core.Controls;
using Virtuoso.Core.Utility;
using Virtuoso.Server.Data;

namespace ALY_Button_Style
{

    public class ValidationTestUCBase : DetailUserControlBase<ValidationTestUC, ValidationTestEntity>
    {
        public ValidationTestUCBase()
        {           
            SelectedItem = new ValidationTestEntity() { };
            OKVisible = true;
        }
    }
    public partial class ValidationTestUC : ValidationTestUCBase
	{
        public ValidationTestUC()
        {
            this.InitializeComponent();
            this.DataContext = this;
            Loaded += TaskPopup_Loaded;
        }

        void TaskPopup_Loaded(object sender, RoutedEventArgs e)
        {
            //NOTE: setting Format in XAML was raising an error...setting in code-behind seems to work fine...

            if (true)
            {
                TaskTimePicker2.Format = new CustomTimeFormat("HHmm");
            }
            else
            {
                TaskTimePicker2.Format = new ShortTimeFormat();
            }
        }
    }
}
