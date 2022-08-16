using System.Windows;
using System.Windows.Controls;

namespace Virtuoso.Core.Controls
{
    public class vButton : Button
    {
        public vButton()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreButtonStyle"]; }
            catch { }
        }
        public void vButtonOnClick()
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>{ OnClick(); });
        }
    }

}
