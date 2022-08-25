using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Virtuoso.Core.Interface;


/*
    <ricontrols:vDatePicker 
        DateObject="{Binding Path=CurrentItem.EffectiveFrom, Mode=TwoWay, NotifyOnValidationError=True}" 
        SelectedDateFormat="Short"
        valfx:ValidationScope.ValidateBoundProperty="DateObject"
        IsEnabled="{Binding Path=DeltaLocked, Converter={StaticResource OppositeBoolConverter}}"
        Visibility="{Binding CommandMode, Converter={StaticResource OppositeViewModeConverter}}">
    </ricontrols:vDatePicker>
*/
namespace Virtuoso.Core.Controls
{

    public class vDatePicker : System.Windows.Controls.DatePicker, ICustomCtrlContentPresenter, ICleanup
    {
#if OPENRIASERVICES51
        [Client.Shim.VirtuosoShim("Not declared in OpenSliver DatePicker")]
        public event EventHandler<DatePickerDateValidationErrorEventArgs> DateValidationError;
#endif

        public static DependencyProperty IsTabStopCustomProperty =
           DependencyProperty.Register("IsTabStopCustom", typeof(object), typeof(Virtuoso.Core.Controls.vDatePicker), null);
        public bool IsTabStopCustom
        {
            get { return ((bool)(base.GetValue(Virtuoso.Core.Controls.vDatePicker.IsTabStopCustomProperty))); }
            set { base.SetValue(Virtuoso.Core.Controls.vDatePicker.IsTabStopCustomProperty, value); }
        }
        
        public static DependencyProperty DateMaskProperty =
           DependencyProperty.Register("DateMask", typeof(string), typeof(vDatePicker),
           new PropertyMetadata("MM/dd/yyyy", DateMaskChanged));

        private static void DateMaskChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var thisDatePickerControl = ((vDatePicker)sender);
            if (thisDatePickerControl == null) return;
            string dateMask = (string)args.NewValue;
            thisDatePickerControl.FormatUsingDateMask(dateMask);
        }
        private void FormatWaterMarkUsingDateMask(string pDateMask)
        {
            string dateMask = (pDateMask == null) ? DateMask : pDateMask;

            if (datePickerTextBox == null) return;
            if (string.IsNullOrWhiteSpace(dateMask))
            {
                datePickerTextBox.Watermark = "<mm/dd/yyyy>";
                datePickerTextBox.MaxLength = 10;
            }
            else
            {
                datePickerTextBox.Watermark = string.Format("<{0}>", dateMask).ToLower();
                datePickerTextBox.MaxLength = dateMask.Length;
            }
        }
        private void FormatUsingDateMask(string pDateMask)
        {
            string dateMask = (pDateMask == null) ? DateMask : pDateMask;

            if (datePickerTextBox == null) return;
            FormatWaterMarkUsingDateMask(dateMask);

            // Format date if there is one
            if (SelectedDate != null)
            {
                try
                {
                    DateTime dt = (DateTime)SelectedDate;

                    if (string.IsNullOrWhiteSpace(dateMask)) datePickerTextBox.Text = dt.ToShortDateString();
                    else datePickerTextBox.Text = dt.ToString(dateMask);

                }
                catch { }
            }
        }

        public string DateMask
        {
            get { return ((string)(base.GetValue(vDatePicker.DateMaskProperty))); }
            set { base.SetValue(vDatePicker.DateMaskProperty, value);  }
        }


        public static DependencyProperty IsReadOnlyProperty =
            DependencyProperty.Register("IsReadOnly", typeof(bool), typeof(vDatePicker),
            new PropertyMetadata(false, IsReadOnlyChanged));


        
        public bool IsReadOnly
        {
            get { return ((bool)(base.GetValue(vDatePicker.IsReadOnlyProperty))); }
            set { base.SetValue(vDatePicker.IsReadOnlyProperty, value); }
        }


        private static void IsReadOnlyChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            var thisDatePickerControl = ((vDatePicker)sender);
            var datePickerTextBox = thisDatePickerControl.datePickerTextBox;
            var isReadOnlyValue = (bool)args.NewValue;
            if (datePickerTextBox != null)
                datePickerTextBox.IsReadOnly = isReadOnlyValue;
            thisDatePickerControl.IsReadOnly = isReadOnlyValue;
            thisDatePickerControl.IsHitTestVisible = !isReadOnlyValue;
        }

        private static readonly List<Key> _controlKeys = new List<Key> 
        {
            Key.Back, 
            Key.CapsLock, 
            Key.Ctrl, 
            Key.Down, 
            Key.End, 
            Key.Enter, 
            Key.Escape, 
            Key.Home, 
            Key.Insert, 
            Key.Left, 
            Key.PageDown, 
            Key.PageUp, 
            Key.Right, 
            Key.Shift, 
            Key.Tab, 
            Key.Up  
        };
        private static Regex _isNumber = new Regex(@"^\d+$");
        private static bool IsNumeric(Key key)
        {
            // Numeric is 0 thru 9
            bool shiftKey = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            if ((key >= Key.D0) && (key <= Key.D9) && (!shiftKey)) { return true; }
            return ((key >= Key.NumPad0) && (key <= Key.NumPad9));
        }
        private static bool isShift = false;
        private string _invalidText;
        private DatePickerTextBox datePickerTextBox = null;
        public vDatePicker()
        {
            this.DateValidationError += new EventHandler<DatePickerDateValidationErrorEventArgs>(vDatePicker_DateValidationError);
            this.GotFocus += new RoutedEventHandler(vDatePicker_GotFocus);
            this.LostFocus += new RoutedEventHandler(vDatePicker_LostFocus);
            this.CalendarOpened += new RoutedEventHandler(vDatePicker_CalendarOpened);
            this.IsTodayHighlighted = true;
            this.SelectedDateChanged += new EventHandler<SelectionChangedEventArgs>(vDatePicker_SelectedDateChanged);
            this.IsTodayHighlighted = true;
            this.IsTabStopCustom = true;
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreDatePickerStyle"]; }
            catch { }
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            // Triggered when the user hovers over the little calender and a date is set
            // the base class will scroll when the user scrolls, this will prevent that action
            e.Handled = true; // prevent changing date from mouse scroll
            return;
        }

        private void datePickerTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Triggered when the user hovers over the DateTextBox and scrolls with the mousewheel
            // By marking the event as handled the base class will not fire
            e.Handled = true; // prevent changing date from mouse scroll
            return;
        }

        public void Cleanup()
        {
            this.DateValidationError -= vDatePicker_DateValidationError;
            this.GotFocus -= vDatePicker_GotFocus;
            this.LostFocus -= vDatePicker_LostFocus;
            this.CalendarOpened -= vDatePicker_CalendarOpened;
            this.SelectedDateChanged -= vDatePicker_SelectedDateChanged;
            if (datePickerTextBox != null)
            {
                try { datePickerTextBox.KeyDown -= _datePickerTextBoxKeyDown; }
                catch { }
                try { datePickerTextBox.TextChanged -= _datePickerTextChanged; }
                catch { }
                try { datePickerTextBox.MouseWheel -= datePickerTextBox_MouseWheel; }
                catch { }
            }
        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            datePickerTextBox = base.GetTemplateChild("TextBox") as DatePickerTextBox;
            if (datePickerTextBox != null)
            {
#if OPENSILVER
                FormatUsingDateMask(null);
#else
                FormatWaterMarkUsingDateMask(null);
#endif

                try { datePickerTextBox.KeyDown -= _datePickerTextBoxKeyDown; }
                catch { }
                datePickerTextBox.KeyDown += _datePickerTextBoxKeyDown;
                try { datePickerTextBox.TextChanged -= _datePickerTextChanged; }
                catch { }
                datePickerTextBox.TextChanged += _datePickerTextChanged;
                try { datePickerTextBox.MouseWheel -= datePickerTextBox_MouseWheel; }
                catch { }
                datePickerTextBox.MouseWheel += datePickerTextBox_MouseWheel;
                
                datePickerTextBox.IsReadOnly = this.IsReadOnly;
                this.IsReadOnly = this.IsReadOnly;
                this.IsHitTestVisible = !this.IsReadOnly;
            }
        }

        private void _datePickerTextChanged(object sender, TextChangedEventArgs e)
        {
            DatePickerTextBox d = sender as DatePickerTextBox;
            if (d != null)
            {
                FormatWaterMarkUsingDateMask(null);                
            }
        }
        static void _datePickerTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            // Allow numerics and slashes for dates - and T+- numerics for T-logic
            // this sucks but e.Key = Unknown for the slash(191), minusSign(189) and plusSigh (shift 187), so....
            // we need to use the OS specific PlatformKeyCode
            e.Handled = (!(_controlKeys.Contains(e.Key) || IsNumeric(e.Key) || (e.Key == Key.T) || (e.Key == Key.Subtract) || (e.Key == Key.Divide) || ((e.Key == Key.Unknown) && (e.PlatformKeyCode == 189)) || (e.Key == Key.Add) || ((isShift) && (e.Key == Key.Unknown) && (e.PlatformKeyCode == 187)) || ((e.Key == Key.Unknown) && (e.PlatformKeyCode == 191))));
            isShift = (e.Key == Key.Shift);
        }

        public static DependencyProperty DateObjectProperty =
         DependencyProperty.Register("DateObject", typeof(DateTime?), typeof(Virtuoso.Core.Controls.vDatePicker), new PropertyMetadata((o, e) =>
         {
             ((Virtuoso.Core.Controls.vDatePicker)o).SetTextFromDateObject();
         }));

        public DateTime? DateObject
        {
            get { return (DateTime?)base.GetValue(vDatePicker.DateObjectProperty); }
            set { base.SetValue(vDatePicker.DateObjectProperty, value); }
        }
        private void SetTextFromDateObject()
        {
            DateTime? value = DateObject;
            if (value == null)
            {
                SelectedDate = (DateTime?)null;
            }
            else if (value == DateTime.MinValue)
            {
                SelectedDate = (DateTime?)null;
            }
            else
            {
                this.SelectedDate = value;
            }
            FormatUsingDateMask(null);
        }
        private void SetDateObjectFromText()
        {
            if (_invalidText != null)
            {
                DateTime validDate;
                if (!(DateTime.TryParse(_invalidText, out validDate)))
                {
                    if (this.SelectedDate == null)
                    {
                        DateObject = (DateTime?)null;
                        SelectedDate = (DateTime?)null;
                        return;
                    }
                    else
                    {
                        this.DisplayDate = (DateTime)this.SelectedDate;
                    }
                }
                else
                {
                    this.SelectedDate = validDate;
                    this.DisplayDate = validDate;
                }
            }

            if (this.SelectedDate == null)
            {
                DateObject = (DateTime?)null;
                SelectedDate = (DateTime?)null;
                return;
            }
            
            //preserve the time component
            this.DateObject = new DateTime(SelectedDate.Value.Year,
                                                SelectedDate.Value.Month,
                                                SelectedDate.Value.Day,
                                                DateObject == null ? 0 : DateObject.Value.Hour,
                                                DateObject == null ? 0 : DateObject.Value.Minute,
                                                DateObject == null ? 0 : DateObject.Value.Second);
            
            if (this.SelectedDate == DateTime.MinValue)
            {
                DateObject = (DateTime?)null;
                SelectedDate = (DateTime?)null;
            }
            return;
        }

        private void vDatePicker_DateValidationError(object sender, DatePickerDateValidationErrorEventArgs e)
        {
            _invalidText = e.Text;
        }
               
        private void vDatePicker_GotFocus(object sender, RoutedEventArgs e)
        {
            isShift = false; 
            _invalidText = null;
        }
        private void vDatePicker_LostFocus(object sender, RoutedEventArgs e)
        {
            if (datePickerTextBox != null)
            {
                if (_invalidText == null)
                {
                    // Valid date - convert to short (removing leading zeros)
                    if (!string.IsNullOrWhiteSpace(datePickerTextBox.Text))
                    {
                        try
                        {
                            DateTime dt = DateTime.Parse(datePickerTextBox.Text);
                            DisplayDate = dt;
                            SelectedDate = dt;
                          
                            if (string.IsNullOrWhiteSpace(DateMask)) datePickerTextBox.Text = dt.ToShortDateString();
                            else datePickerTextBox.Text = dt.ToString(DateMask);
                          
                        }
                        catch { }
                    }
                }
                else
                {
                    // Invalid date format
                    string d = _invalidText;
                    if (((d.Length == 4) || (d.Length == 6) || (d.Length == 8)) && (_isNumber.Match(d).Success))
                    {
                        // If 4, 6 or 8 numerics - add the slashes and see it if is now a valid date (default year and centry if need be)
                        string dSlash = null;
                        if (d.Length == 4) dSlash = d.Substring(0, 2) + "/" + d.Substring(2, 2) + "/" + DateTime.Today.Year.ToString();
                        else if (d.Length == 6) dSlash = d.Substring(0, 2) + "/" + d.Substring(2, 2) + "/" + (((DateTime.Today.Year - 2000 + 15) < (Int32.Parse(d.Substring(4, 2)))) ? "19" : "20") + d.Substring(4, 2);
                        else dSlash = d.Substring(0, 2) + "/" + d.Substring(2, 2) + "/" + d.Substring(4, 4);
                        try
                        {
                            DateTime dt = DateTime.Parse(dSlash);
                            DisplayDate = dt;
                            SelectedDate = dt;

                            if (string.IsNullOrWhiteSpace(DateMask)) datePickerTextBox.Text = dt.ToShortDateString();
                            else datePickerTextBox.Text = dt.ToString(DateMask);

                            _invalidText = null;
                        }
                        catch { }
                    }
                    else
                    {
                        // T-Logic: eg.  T = today, T7 or T+7 = today plus 7 days, T-1 = yesterday
                        if (d.Substring(0, 1).ToLower() == "t")
                        {
                            int days = 0;
                            try { days = Int32.Parse(d.Substring(1, d.Length - 1)); } catch { }
                            DateTime dt = DateTime.Today;

                            
                            try
                            {
                                dt = dt.AddDays((double)days);
                                DisplayDate = dt;
                                SelectedDate = dt;

                                if (string.IsNullOrWhiteSpace(DateMask)) datePickerTextBox.Text = dt.ToShortDateString();
                                else datePickerTextBox.Text = dt.ToString(DateMask);

                                _invalidText = null;
                            }
                            catch
                            {
                                //ds072914 Task 10245
                                datePickerTextBox.Text = "";
                                DateObject = (DateTime?)null;
                                SelectedDate = (DateTime?)null;
                                _invalidText = null;
                            }

                            
                        }
                    }
                }
            }
            if ((_invalidText != null) && ((this.SelectedDate == null) || (this.SelectedDate == (DateTime?)null) || (this.SelectedDate == DateTime.MinValue)))
            {
                this.ClearValue(DisplayDateProperty);
                DateObject = (DateTime?)null;
                SelectedDate = (DateTime?)null;
                if (datePickerTextBox != null) datePickerTextBox.Text = "";
                this.Text = "";
            }
            else if ((this.SelectedDate == null) || (this.SelectedDate == DateTime.MinValue))
            {
                this.ClearValue(DisplayDateProperty);
                DateObject = (DateTime?)null;
                SelectedDate = (DateTime?)null;
                if (datePickerTextBox != null) datePickerTextBox.Text = "";
                this.Text = "";
            }
            SetDateObjectFromText();
        }
        private void vDatePicker_CalendarOpened(object sender, RoutedEventArgs e)
        {
            if ((this.SelectedDate == null) || (this.SelectedDate == DateTime.MinValue ))
            {
                this.SelectedDate = DateTime.Now.Date;
                this.DisplayDate = DateTime.Now.Date;
                this.Text = DateTime.Now.ToShortDateString();

                if (string.IsNullOrWhiteSpace(DateMask)) this.Text = DateTime.Now.ToShortDateString();
                else this.Text = DateTime.Now.ToString(DateMask);

            }
        }
        private void vDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((this.SelectedDate == null) || (this.SelectedDate == DateTime.MinValue))
            {
                this.ClearValue(DisplayDateProperty);
                if (datePickerTextBox != null) datePickerTextBox.Text = "";
                this.Text = "";
                DateObject = (DateTime?)null;
                SelectedDate = (DateTime?)null;
            }
            else
            {
                this.DisplayDate = (DateTime)SelectedDate;

                //preserve the time component
                this.DateObject = new DateTime(SelectedDate.Value.Year,
                                                SelectedDate.Value.Month,
                                                SelectedDate.Value.Day,
                                                DateObject == null ? 0 : DateObject.Value.Hour,
                                                DateObject == null ? 0 : DateObject.Value.Minute,
                                                DateObject == null ? 0 : DateObject.Value.Second);
            }
            if (datePickerTextBox != null)
            {
                FormatUsingDateMask(null);
                datePickerTextBox.MaxLength = 10;
                try { datePickerTextBox.KeyDown -= _datePickerTextBoxKeyDown; }
                catch { }
                datePickerTextBox.KeyDown += _datePickerTextBoxKeyDown;
            }
        }
    }
}
