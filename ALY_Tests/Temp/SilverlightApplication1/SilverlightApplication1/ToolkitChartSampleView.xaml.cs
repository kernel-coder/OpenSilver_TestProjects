using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ALY_Button_Style
{
    // Class to store sales data
    public class SalesInfo
    {
        public DateTime SaleDate { get; set; }
        public int Sales { get; set; }
    }

    // Collection of sales data
    public class SalesInfoCollection : IEnumerable<SalesInfo>
    {
        public IEnumerator<SalesInfo> GetEnumerator()
        {
            yield return new SalesInfo { SaleDate = DateTime.Parse("12/27/2011", CultureInfo.InvariantCulture), Sales = 2041 };

            yield return new SalesInfo { SaleDate = DateTime.Parse("12/28/2011", CultureInfo.InvariantCulture), Sales = 1991 };

            yield return new SalesInfo { SaleDate = DateTime.Parse("01/01/2010", CultureInfo.InvariantCulture), Sales = 1033 };

            yield return new SalesInfo { SaleDate = DateTime.Parse("01/01/2010", CultureInfo.InvariantCulture), Sales = 1167 };
            yield return new SalesInfo { SaleDate = DateTime.Parse("04/01/2010", CultureInfo.InvariantCulture), Sales = 5815 };
            yield return new SalesInfo { SaleDate = DateTime.Parse("05/01/2010", CultureInfo.InvariantCulture), Sales = 5586 };
            yield return new SalesInfo { SaleDate = DateTime.Parse("03/01/2010", CultureInfo.InvariantCulture), Sales = 5064 };
            yield return new SalesInfo { SaleDate = DateTime.Parse("01/17/2010", CultureInfo.InvariantCulture), Sales = 2268 };
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<SalesInfo>)this).GetEnumerator();
        }
    }

    public partial class ToolkitChartSampleView : UserControl
    {
        public ToolkitChartSampleView()
        {
            InitializeComponent();

            var binding = new Binding()
            {
                Source = chartLine.Series[0],
                Path = new PropertyPath("Background")
            };


        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //SaveFileDialog filter = new SaveFileDialog();
            //filter.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
            //filter.FilterIndex = 1;
            //filter.ShowDialog();
        }
    }

}
