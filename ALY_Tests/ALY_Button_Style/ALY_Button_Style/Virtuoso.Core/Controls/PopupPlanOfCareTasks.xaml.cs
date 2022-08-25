using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Virtuoso.Server.Data;

namespace Virtuoso.Core.Controls
{
    public partial class PopupPlanOfCareTasks : ChildWindow
    {
        public List<string> input { set; get; }
        public string output { set; get; }
        private pocts Codes = new pocts();
  
        public PopupPlanOfCareTasks(string Input)
        {
            InitializeComponent();

            input = Input.Split('|').ToList();           

            Codes.Add(new poct() { code = "A", order = string.Empty});
            Codes.Add(new poct() { code = "B", order = string.Empty });
            Codes.Add(new poct() { code = "C", order = string.Empty });
            Codes.Add(new poct() { code = "D", order = string.Empty });
            Codes.Add(new poct() { code = "E", order = string.Empty });

            int i = 0;
            foreach (string s in input)
            {
                i++;
                var c = Codes.FirstOrDefault(x => x.code == s);
                if (c != null) c.order = i.ToString(); 
             }

             foreach (var c in Codes)
            {
                var me = this.sp_tasks.GetVisualDescendants().OfType<TextBox>().ToList().FirstOrDefault(x => x.Name == "tb_" + c.code);

                Binding binding = new Binding();
                binding.Path = new PropertyPath("order");
                binding.Source = c;
                binding.TargetNullValue="";
                binding.Mode = BindingMode.TwoWay;
                binding.ValidatesOnDataErrors = true;
                binding.ValidatesOnExceptions = true;
                binding.ValidatesOnNotifyDataErrors = true;

                me.SetBinding(TextBox.TextProperty, binding);
               
                me.LostFocus += me_LostFocus;
            }

            this.Loaded += PopupPlanOfCareTasks_Loaded;
 
        }

       
        void PopupPlanOfCareTasks_Loaded(object sender, RoutedEventArgs e)
        {
          
        }

        void me_LostFocus(object sender, RoutedEventArgs e)
        {
            ErrorCheck();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorCheck();

            if (Codes.Any(x => x.HasErrors) || Codes.HasErrors) return;

            List<string> s = new List<string>();
            foreach (var c in Codes.OrderBy(x => x.order))
            {
                if (!string.IsNullOrEmpty(c.order)) s.Add(c.code);
            }
            this.output = string.Join("|", s);

            this.DialogResult = true;

        }      

        private void ErrorCheck()
        {
            resetBoxes();
            this.valSum.Text = string.Empty;

            int i = 0;
            if (Codes.Any(x => x.HasErrors))
            {
                foreach (var err in Codes.Where(x => x.HasErrors))
                {
                    if (i == 0) valSum.Text = err.error;
                    else valSum.Text = valSum.Text + Environment.NewLine + err.error;
                    this.turnBoxRed(err.code);
                    i++;
                }

                //return;
            }
         
            Codes.ValidateCollection();

            if (Codes.HasErrors)
            {      
                foreach (var err in Codes.GetErrors())
                {
                    if (i == 0) valSum.Text = err.message;
                    else valSum.Text = valSum.Text + Environment.NewLine + err.message;
                    this.turnBoxRed(err.code);
                    i++;
                }
            }
        }

        private void resetBoxes()
        {
            foreach (TextBox tb in this.sp_tasks.GetVisualDescendants().OfType<TextBox>())
            {
                tb.BorderBrush = new SolidColorBrush(Colors.LightGray);
            }

        }        

        private void turnBoxRed(string code)
        {
            var tb = this.sp_tasks.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name == "tb_" + code);
            if (tb != null) tb.BorderBrush = new SolidColorBrush(Colors.Red);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            output = string.Empty;
        }

        public class poct 

        {
            public string code { set; get; }

            private bool _hasErrors = false;
            public bool HasErrors
            {
                get { return _hasErrors; }
                set { _hasErrors = value; }
            }

            private string _error = string.Empty;
            public string error
            {
                get { return _error; }
                set { _error = value; }
            }

            private string _order;
            public string order
            {
                get { return _order; }
                set 
                {
                    if (string.IsNullOrEmpty(value))
                    {
                        error = string.Empty;
                        HasErrors = false;
                        _order = value; 
                        return;
                    }
                    
                    if (Regex.IsMatch(value, @"^\d+$"))
                    {
                        int i = Convert.ToInt16(value);

                        if (i < 1 || i > 5)
                        {
                            HasErrors = true;
                            error = "Error: Value not applied - must be between 1 and 5 - in box " + this.code;
                            throw new ValidationException("Error: Value not applied - must be between 1 and 5 - in box " + this.code);
                        }
                        HasErrors = false;
                    }
                    _order = value; 
                }
            }
        }

        public class pocts : ObservableCollection<poct>
        {
            public List<messagewithcode> GetErrors()
            {
                return errors;
            }

            public bool HasErrors
            {
                get
                {
                    if (errors.Any() == true) return true;
                    else return false;
                }
            }

            private List<messagewithcode> errors = new List<messagewithcode>();

            public void AddError(string error, string code)
            {
                errors.Add(new messagewithcode() { code = code,message = error });
            }
           
            public void ClearErrors()
            {
                errors.Clear();
            }

            public void ValidateCollection()
            {
                this.ClearErrors();

                var group = this.GroupBy(x => x.order);
                foreach (var g in group)
                {
                    if (g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    {
                        foreach(var c in g)
                        {
                            this.AddError("Error: Ordering must be unique, there is more than one " + g.Key.ToString() + " including box " + c.code.ToString() + ".", c.code);
                        }
                    }
                }

                foreach (var c in this)
                {
                    if(!string.IsNullOrEmpty(c.order))
                    {
                        if (Convert.ToInt16(c.order) < 1 || Convert.ToInt16(c.order) > 5)
                        {
                            this.AddError("Error: Values must be between 1 and 5, value " + c.order + " is not allowed.", c.code);
                        }
                    }
                }
            } 

        }

        public class messagewithcode
        {
            public string message { set; get; }

            public string code { set; get; }
        }

    }
}

