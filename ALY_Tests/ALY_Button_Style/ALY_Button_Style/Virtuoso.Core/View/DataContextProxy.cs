using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Virtuoso.Core.View
{
    //http://weblogs.asp.net/dwahlin/archive/2009/08/20/creating-a-silverlight-datacontext-proxy-to-simplify-data-binding-in-nested-controls.aspx
    public class DataContextProxy : FrameworkElement
    {
        public DataContextProxy()
        {
#if OPENSILVER // Loaded event never happens because this element is stored in Resources and not added to visual tree
            this.DataContextChanged += (s, e) => SetMyBindings();
#else
            this.Loaded += new RoutedEventHandler(DataContextProxy_Loaded);
#endif
            //Needed these (especially LayoutUpdated) BEFORE change PatientMedicationUserControl to use relative source binding
            //this.GotFocus += new RoutedEventHandler(DataContextProxy_GotFocus);
            //this.LayoutUpdated += new EventHandler(DataContextProxy_LayoutUpdated);
            //this.DataContextChanged += new DependencyPropertyChangedEventHandler(DataContextProxy_DependencyPropertyChangedEventHandler);
        }

        void DataContextProxy_Loaded(object sender, RoutedEventArgs e)
        {
            SetMyBindings();
        }

        //void DataContextProxy_DependencyPropertyChangedEventHandler(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    SetMyBindings();
        //}
        //void DataContextProxy_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    SetMyBindings();
        //}
        //void DataContextProxy_LayoutUpdated(object sender, EventArgs e)
        //{
        //    SetMyBindings();
        //}
        private void SetMyBindings()
        {
            if (this.DataContext == null) return;
            if (this.GetBindingExpression(DataContextProxy.DataSourceProperty) != null) return;
            Binding binding = new Binding();
            if (!String.IsNullOrEmpty(BindingPropertyName))
            {
                binding.Path = new PropertyPath(BindingPropertyName);
            }

            binding.Source = this.DataContext;
            binding.Mode = BindingMode;
            this.SetBinding(DataContextProxy.DataSourceProperty, binding);
        }

        public Object DataSource
        {
            get { return (Object)GetValue(DataSourceProperty); }
            set { SetValue(DataSourceProperty, value); }
        }

        public static readonly DependencyProperty DataSourceProperty =
            DependencyProperty.Register("DataSource", typeof(Object), typeof(DataContextProxy), null);


        public string BindingPropertyName { get; set; }

        public BindingMode BindingMode { get; set; }
    }
}