using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Runtime.Serialization;
using System.ComponentModel.DataAnnotations;
using Virtuoso.Server.Data;
using Virtuoso.Core.Utility;
using GalaSoft.MvvmLight;

namespace Virtuoso.Core.Controls
{
    public enum vLabelForceRequired { None, Yes, No };
    public partial class vLabel : UserControl, ICleanup
    {
        private static string STAR = "*";
        public vLabel()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(vLabel_Loaded);
        }
        void vLabel_Loaded(object sender, RoutedEventArgs e)
        {
            vLabel vl = sender as vLabel;
            if (vl == null) return;
            setIndicatorAndLabel(vl);
        }

        private static void setIndicatorAndLabel(vLabel vl)
        {
            // check validity of dependency properties
            if (string.IsNullOrWhiteSpace(vl.Entity)) { MessageBox.Show("vLabel Error: Entity not specified"); return; }
            if (string.IsNullOrWhiteSpace(vl.Property)) { MessageBox.Show("vLabel Error: Property not specified"); return; }
            PropertyInfo property = VirtuosoEntityProperties.GetEntityProperty(vl.Entity, vl.Property);
            if (property == null) { MessageBox.Show(string.Format("vLabel Error: Property {0}.{1} not defined", vl.Entity, vl.Property)); return; }
            // Set the required indicator and label
            try
            {
                vl.Required.Text = vl.GetRequired(property);
            }
            catch
            {
                vl.Required.Text = "";
            }
            try
            {
                vl.Label.Text = vl.GetLabel(property);
            }
            catch
            {
                vl.Label.Text = vl.Property;
            }
        }
        public static DependencyProperty EntityProperty = DependencyProperty.Register("Entity", typeof(String), typeof(Virtuoso.Core.Controls.vLabel), null);

        public String Entity
        {
            get { return ((String)(base.GetValue(vLabel.EntityProperty))); }
            set { base.SetValue(vLabel.EntityProperty, value); }
        }
        public static DependencyProperty PropertyProperty = DependencyProperty.Register("Property", typeof(string), typeof(Virtuoso.Core.Controls.vLabel), null);
        public string Property
        {
            get { return ((string)(base.GetValue(vLabel.PropertyProperty))); }
            set { base.SetValue(vLabel.PropertyProperty, value); }
        }
        
        // Attribute Text Override to force the label to static text.
        public static DependencyProperty TextOverrideProperty = DependencyProperty.Register("TextOverride", typeof(string), typeof(Virtuoso.Core.Controls.vLabel), null);
        public string TextOverride 
        {
            get { return ((string)(base.GetValue(vLabel.TextOverrideProperty))); }
            set { base.SetValue(vLabel.TextOverrideProperty, value); }
        
        }

        //private vLabelForceRequired _ForceRequired = vLabelForceRequired.None;
        public static DependencyProperty ForceRequiredProperty = DependencyProperty.Register("ForceRequired", typeof(vLabelForceRequired), typeof(Virtuoso.Core.Controls.vLabel), new PropertyMetadata(new PropertyChangedCallback(OnForceRequired_Changed)));
        public vLabelForceRequired ForceRequired
        {
            get { return ((vLabelForceRequired)(base.GetValue(vLabel.ForceRequiredProperty))); }
            set { base.SetValue(vLabel.ForceRequiredProperty, value); }
        }
        private static void OnForceRequired_Changed(object sender, DependencyPropertyChangedEventArgs args)
        {
            try
            {
                vLabel vl = sender as vLabel;
                if (vl == null) return;
                setRequired(vl, (vLabelForceRequired) args.NewValue);
            }
            catch (Exception oe)
            {
                MessageBox.Show(String.Format("OnForceRequired_Changed error: {0}", oe.Message));
                throw;
            }
        }
        private static void setRequired(vLabel curLabel, vLabelForceRequired isForceRequired)
        {
            if (isForceRequired == vLabelForceRequired.Yes)
                curLabel.Required.Text = "*";
            else
                curLabel.Required.Text = "";
            
            return;
        }
        private string GetRequired(PropertyInfo property)
        {
            if (ForceRequired == vLabelForceRequired.Yes) return STAR;
            if (ForceRequired == vLabelForceRequired.No) return "";
            RequiredAttribute ra = Attribute.GetCustomAttributes(property, typeof(RequiredAttribute)).FirstOrDefault() as RequiredAttribute;
            if (ra != null) return STAR;
            DataMemberAttribute dma = Attribute.GetCustomAttributes(property, typeof(DataMemberAttribute)).FirstOrDefault() as DataMemberAttribute;
            return (dma == null) ? "" : (dma.IsRequired) ? STAR : "";
        }
        private string GetLabel(PropertyInfo property)
        {
            DisplayAttribute da = Attribute.GetCustomAttributes(property, typeof(DisplayAttribute)).FirstOrDefault() as DisplayAttribute;
            if (da == null) return GetName(property);
            if (!string.IsNullOrWhiteSpace(da.Prompt)) return da.Prompt;
            if (!string.IsNullOrWhiteSpace(da.Name)) return da.Name;
            if (!string.IsNullOrWhiteSpace(da.ShortName)) return da.ShortName;
            if (!string.IsNullOrWhiteSpace(da.Description)) return da.Description;
            return GetName(property);
        }
        private string _name = null;
        private string GetName(PropertyInfo property)
        {
            if(!string.IsNullOrEmpty(TextOverride)) return TextOverride;
            // Default label from column name, adding space between each capatialized word (eg, "MedicationEndDate" becomes "Medication End Date"
            if (string.IsNullOrWhiteSpace(property.Name)) return property.ToString();
            if (_name != null) return _name;
            foreach (char c in property.Name.ToCharArray())
            {
                if ((_name != null) && (c >= 'A') && (c <= 'Z')) _name = _name + " ";
                _name = _name + c.ToString();
            }
            return _name;
        }
        public void Cleanup()
        {
            this.Loaded -= vLabel_Loaded;

            this.ClearValue(EntityProperty);
            this.ClearValue(PropertyProperty);
            this.ClearValue(TextOverrideProperty);
            this.ClearValue(ForceRequiredProperty);

            this.Content = null;
            this.DataContext = null;
        }
    }
}
