using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Virtuoso.Core.Controls
{
    public class VirtuosoWeekDayControl : System.Windows.Controls.ComboBox
    {
        public VirtuosoWeekDayControl()
        {
            try 
            {
                try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreComboBoxStyle"]; } catch { }
                this.Items.Add(new ComboBoxItem { Tag = "SUNDAY", Content = "Sunday" });
                this.Items.Add(new ComboBoxItem { Tag= "MONDAY", Content="Monday" });
                this.Items.Add(new ComboBoxItem { Tag = "TUESDAY", Content = "Tuesday" });
                this.Items.Add(new ComboBoxItem { Tag = "WEDNESDAY", Content = "Wednesday" });
                this.Items.Add(new ComboBoxItem { Tag = "THURSDAY", Content = "Thursday" });
                this.Items.Add(new ComboBoxItem { Tag = "FRIDAY", Content = "Friday" });
                this.Items.Add(new ComboBoxItem { Tag = "SATURDAY", Content = "Saturday" });
            }
            catch { }
        }
        
    }
}
