using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace Virtuoso.Core.Controls
{
    public class VirtuosoBusyIndicator : BusyIndicator
    {
        public VirtuosoBusyIndicator()
        {
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreBusyIndicatorStyleDefault"]; }
            catch { }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetBusyAnimationStoryboardFromIsBusy();
        }

        protected override void OnIsBusyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsBusyChanged(e);
            SetBusyAnimationStoryboardFromIsBusy();
        }
        private void SetBusyAnimationStoryboardFromIsBusy()
        {
            Storyboard sb = (Storyboard)GetTemplateChild("BusyAnimation");
            if (sb == null) return;
            if (this.IsBusy)
                sb.Begin();
            else
                sb.Stop();
        }
    }
}
