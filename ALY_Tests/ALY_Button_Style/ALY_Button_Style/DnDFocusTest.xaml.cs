using ALY_Button_Style.Shared;
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

namespace ALY_Button_Style
{
    public partial class DnDFocusTest : UserControl
    {
        public DnDFocusTest()
        {
            this.InitializeComponent();
            var leftItems = new List<FocusItemVM>();
            leftItems.Add(new FocusItemVM() { InsuranceName = "iName 1", BillingPercent = 100 });
            leftItems.Add(new FocusItemVM() { InsuranceName = "iName 2", BillingPercent = 90 });
            leftItems.Add(new FocusItemVM() { InsuranceName = "iName 3", BillingPercent = 95 });
            this.DataContext = new FocusTestVM()
            {
                IsEdit = true,
                LeftSource = leftItems
            };
        }

        private void CoverageDataTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        bool MouseDown = false;
        public void Ins_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MouseDown = true;
        }
    }

    public class FocusTestVM : NotifyPropertyChanged
    {
        public bool IsEdit { get; set; } = true;

        public List<FocusItemVM> LeftSource { get; set; } 

        public List<FocusItemVM> RightSource { get; set; }

    }

    public class FocusItemVM : NotifyPropertyChanged
    {
        public string InsuranceName
        {
            get; set; 
        }

        public string InsuranceKey
        {
            get; set;
        }


        public System.Nullable<System.Decimal> BillingPercent
        {
            get
            {
                return this._billingPercent;
            }
            set
            {
                Set(ref _billingPercent, value);

            }
        }
        private System.Nullable<System.Decimal> _billingPercent;
    }
}
