#region Usings

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

#endregion

namespace Virtuoso.Core.Behaviors
{
    public static class TextBoxFilters
    {
        private static readonly string DASH = "dash";

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

        private static readonly Key _dashKey = Key.Subtract;

        private static bool IsNumeric(Key key)
        {
            // Numeric is 0 thru 9
            bool shiftKey = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
            if ((key >= Key.D0) && (key <= Key.D9) && (!shiftKey))
            {
                return true;
            }

            return ((key >= Key.NumPad0) && (key <= Key.NumPad9));
        }

        public static bool GetIsNumericFilter(DependencyObject source)
        {
            return (bool)source.GetValue(IsNumericFilterProperty);
        }

        public static void SetIsNumericFilter(DependencyObject source, bool value)
        {
            source.SetValue(IsNumericFilterProperty, value);
        }

        public static DependencyProperty IsNumericFilterProperty = DependencyProperty.RegisterAttached
        (
            "IsNumericFilter", typeof(bool), typeof(TextBoxFilters),
            new PropertyMetadata(false, IsNumericFilterChanged)
        );

        public static void IsNumericFilterChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            if (source != null && source is TextBox)
            {
                TextBox textBox = source as TextBox;

                if ((bool)args.NewValue)
                {
                    try
                    {
                        textBox.KeyDown -= _TextBoxNumericKeyDown;
                    }
                    catch
                    {
                    }

                    textBox.KeyDown += _TextBoxNumericKeyDown;
                }
            }
        }

        static void _TextBoxNumericKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = (_controlKeys.Contains(e.Key) || IsNumeric(e.Key));
        }

        public static string GetNumericFormat(DependencyObject source)
        {
            return (string)source.GetValue(NumericFormatProperty);
        }

        public static void SetNumericFormat(DependencyObject source, string value)
        {
            source.SetValue(NumericFormatProperty, value);
        }

        public static DependencyProperty NumericFormatProperty = DependencyProperty.RegisterAttached
        (
            "NumericFormat", typeof(string), typeof(TextBoxFilters),
            new PropertyMetadata("", NumericFormatChanged)
        );

        private static string GetPreviousText(DependencyObject source)
        {
            return (string)source.GetValue(PreviousTextProperty);
        }

        private static void SetPreviousText(DependencyObject source, string value)
        {
            source.SetValue(PreviousTextProperty, value);
        }

        private static DependencyProperty PreviousTextProperty = DependencyProperty.RegisterAttached
        (
            "PreviousText", typeof(string), typeof(TextBoxFilters),
            new PropertyMetadata("", null)
        );

        public static void NumericFormatChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            if (source != null && source is TextBox)
            {
                TextBox textBox = source as TextBox;
                string numericFormat = (string)args.NewValue;
                if (string.IsNullOrWhiteSpace(numericFormat) == false)
                {
                    numericFormat = numericFormat.Trim().ToLower();
                }

                // Capture key press to filter numerics (and decimal points if need be)
                textBox.KeyDown += _TextBoxNumericFormatKeyDown;

                // SetMaxLength and TextChanged to insure input conforms to the NumericFormat
                if (numericFormat == "phone")
                {
                    // 999.9999 or 999.999.9999
                    textBox.MaxLength = 12;
                    try
                    {
                        textBox.TextChanged -= _TextBoxNumericFormatPhoneTextChanged;
                    }
                    catch
                    {
                    }

                    textBox.TextChanged += _TextBoxNumericFormatPhoneTextChanged;
                }
                else if (numericFormat == "ndc")
                {
                    // 99999-9999-99
                    textBox.MaxLength = 13;
                    try
                    {
                        textBox.TextChanged -= _TextBoxNumericFormatNdcTextChanged;
                    }
                    catch
                    {
                    }

                    textBox.TextChanged += _TextBoxNumericFormatNdcTextChanged;
                }
                else if (numericFormat == "zip")
                {
                    // 99999 or 99999-9999
                    textBox.MaxLength = 10;
                    try
                    {
                        textBox.TextChanged -= _TextBoxNumericFormatZipCodeTextChanged;
                    }
                    catch
                    {
                    }

                    textBox.TextChanged += _TextBoxNumericFormatZipCodeTextChanged;
                }
                else if (numericFormat == "ssn")
                {
                    // 999-99-9999
                    textBox.MaxLength = 11;
                    try
                    {
                        textBox.TextChanged -= _TextBoxNumericFormatSSNTextChanged;
                    }
                    catch
                    {
                    }

                    textBox.TextChanged += _TextBoxNumericFormatSSNTextChanged;
                }
                else if (numericFormat == "federaltaxid")
                {
                    // 99-9999999
                    textBox.MaxLength = 10;
                    try
                    {
                        textBox.TextChanged -= _TextBoxNumericFormatFederalTaxIDTextChanged;
                    }
                    catch
                    {
                    }

                    textBox.TextChanged += _TextBoxNumericFormatFederalTaxIDTextChanged;
                }
                else if (numericFormat.EndsWith(DASH))
                {
                    // Assume numeric / decimal - capture TextChanged to handle decimal point if need be
                    // and allow DASH-only
                    numericFormat = numericFormat.Replace(DASH, "");
                    textBox.MaxLength = numericFormat.Length;
                    // for now - don't support decimal points with dash
                }
                else
                {
                    textBox.MaxLength = numericFormat.Length;
                    // Assume numeric / decimal - capture TextChanged to handle decimal point if need be
                    string[] formatSplit = numericFormat.Split('.');
                    if ((formatSplit.Length == 2))
                    {
                        try
                        {
                            textBox.TextChanged -= _TextBoxNumericFormatDecimalTextChanged;
                        }
                        catch
                        {
                        }

                        textBox.TextChanged += _TextBoxNumericFormatDecimalTextChanged;
                        if (numericFormat.Contains("$"))
                        {
                            try
                            {
                                textBox.LostFocus -= _TextBoxNumericFormatMoneyTextLostFocus;
                            }
                            catch
                            {
                            }

                            textBox.LostFocus += _TextBoxNumericFormatMoneyTextLostFocus;
                        }
                        else
                        {
                            try
                            {
                                textBox.LostFocus -= _TextBoxNumericFormatDecimalLostFocus;
                            }
                            catch
                            {
                            }

                            textBox.LostFocus += _TextBoxNumericFormatDecimalLostFocus;
                        }
                    }
                }
            }
        }

        static void _TextBoxNumericFormatMoneyTextLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            string s = textBox.Text;
            if (string.IsNullOrEmpty(s))
            {
                return;
            }

            // remove ALLL leading and trailing whiteSpace (spaces, CRs...) and add leading zero if need be
            s = s.Trim();
            if (s.StartsWith("."))
            {
                s = "0" + s;
            }

            if (s.EndsWith("."))
            {
                s = s + "0";
            }

            try
            {
                double d = Convert.ToDouble(s.TrimStart(Convert.ToChar("$")));
                textBox.Text = (d).ToString("C");
            }
            catch
            {
                textBox.Text = s;
            }
        }

        static void _TextBoxNumericFormatDecimalLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox == null)
            {
                return;
            }

            string __strDecimal = textBox.Text;

            if (string.IsNullOrEmpty(__strDecimal))
            {
                //__strDecimal = "0";
            }
            else
            {
                //remove ALL leading and trailing whiteSpace (spaces, CRs...) and add leading zero if need be
                __strDecimal = __strDecimal.Trim();
                if (__strDecimal.StartsWith("."))
                {
                    __strDecimal = "0" + __strDecimal;
                }

                if (__strDecimal.EndsWith("."))
                {
                    __strDecimal = __strDecimal + "0";
                }
            }

            BindingExpression bindingExpression = textBox.GetBindingExpression(TextBox.TextProperty);
            if (null != bindingExpression)
            {
                //<!--Text="{Binding AuthInstanceSelectedItem.AuthorizationAmount,  Mode=TwoWay, NotifyOnValidationError=True, StringFormat=\{0:N3\}, Converter={StaticResource DecimalNullableValueConverter}}"-->
                //<!--Text="{Binding AuthInstanceSelectedItem.AuthorizationAmount,  Mode=TwoWay, NotifyOnValidationError=True, StringFormat='{}{0:#:#0.00}', FallbackValue='0.00', Converter={StaticResource DecimalNullableValueConverter}}"-->
                //<!--Text="{Binding AuthInstanceSelectedItem.AuthorizationAmount,  Mode=TwoWay, NotifyOnValidationError=True, StringFormat='{}{0:0.00}', FallbackValue='0.00', Converter={StaticResource DecimalNullableValueConverter}}"-->
                var strFormatSpecifierFromBindingExpression = bindingExpression.ParentBinding.StringFormat;
                //E.G. Given following binding:
                //     Text="{Binding AuthInstanceSelectedItem.AuthorizationAmount,  Mode=TwoWay, NotifyOnValidationError=True, StringFormat='{}{0:0.00}', FallbackValue='0.00', TargetNullValue='0.00'}"
                //     This code will extract the format specifier as - {0:n2}
                if (string.IsNullOrEmpty(strFormatSpecifierFromBindingExpression)) //have no StringFormat
                {
                    textBox.Text = __strDecimal;
                }
                else
                {
                    decimal __retDecimal;
                    if (decimal.TryParse(__strDecimal, out __retDecimal))
                    {
                        var formattedText = string.Format(strFormatSpecifierFromBindingExpression, __retDecimal);
                        var customFormattedText = formattedText.Replace(",", ""); //ensure no commas in output
                        textBox.Text = customFormattedText;
                    }
                    else
                    {
                        textBox.Text = __strDecimal;
                    }
                }

                bindingExpression.UpdateSource(); //Should force validation after setting .Text
            }
            else
            {
                textBox.Text = __strDecimal;
            }
        }

        static void _TextBoxNumericFormatKeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            string format = (string)textBox.GetValue(NumericFormatProperty);
            if (string.IsNullOrWhiteSpace(format) == false)
            {
                format = format.Trim().ToLower();
            }

            if ((format != null) && (format.ToLower().EndsWith(DASH)))
            {
                _TextBoxNumericFormatKeyDownWithDash(sender, e);
                return;
            }

            // Remember the previous value so we can back it off if need be
            string previousText = textBox.Text;
            textBox.SetValue(PreviousTextProperty, previousText);
            if ((_controlKeys.Contains(e.Key)) || (IsNumeric(e.Key)))
            {
                // Allow control keys and numerics
                e.Handled = false;
            }
            else
            {
                // All non control keys and non numerics fall into here
                // We  are only interested in whether to allow decimal points and negative signs
                string numericFormat = (string)textBox.GetValue(NumericFormatProperty);
                // skim off the negative sign if need be - we need to use the OS specific PlatformKeyCode AND the Subtract key
                if ((e.Key == _dashKey) || (e.PlatformKeyCode == 189))
                {
                    if (StartsWithNegative(numericFormat) == false)
                    {
                        // Negative sign is not allowed
                        e.Handled = true;
                        return;
                    }

                    if (AllowNegativeAndIsNegative(numericFormat, textBox.Text))
                    {
                        // Only one negative sign
                        e.Handled = true;
                        return;
                    }

                    if (StartsWithNegative(numericFormat) && (textBox.SelectionStart != 0))
                    {
                        // Only one negative sign and must be the first character
                        e.Handled = true;
                        return;
                    }

                    e.Handled = false;
                    return;
                }

                // skim off the decimal point
                string[] formatSplit = numericFormat.Split('.');
                if (formatSplit.Length == 2)
                {
                    // Format is decimal allows one and only one decimal point
                    // this sucks but e.Key = Unknown for the non-keypad decimal point, so....
                    // we need to use the OS specific PlatformKeyCode AND the decimal point
                    bool shiftKey = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                    e.Handled = (!(((e.Key == Key.Decimal) || (e.PlatformKeyCode == 190)) && (!shiftKey)));
                    if (!e.Handled)
                    {
                        // The KeyDown is a decimal point - allow one and only one
                        e.Handled = (textBox.Text.Contains("."));
                    }
                }
                else
                {
                    // Format does not allow decimal point
                    e.Handled = true;
                }
            }
        }

        static void _TextBoxNumericFormatKeyDownWithDash(object sender, KeyEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            // Remember the previous value so we can back it off if need be
            textBox.SetValue(PreviousTextProperty, textBox.Text);
            // this sucks but e.Key = Unknown for the non-keypad subtract/dash key, so....
            // we need to use the OS specific PlatformKeyCode AND the Subtract key
            if (string.IsNullOrWhiteSpace(textBox.Text) && (_controlKeys.Contains(e.Key) || IsNumeric(e.Key) ||
                                                            (e.Key == _dashKey) || (e.PlatformKeyCode == 189)))
            {
                // textBox is currently empty = allow control keys and numerics and dash
                e.Handled = false;
            }
            else if ((string.IsNullOrWhiteSpace(textBox.Text) == false) && (textBox.Text.Equals("-")))
            {
                // textBox is currently just a dash - thats all you can have, nothing else
                e.Handled = (_controlKeys.Contains(e.Key)) ? false : true;
            }
            else if ((_controlKeys.Contains(e.Key)) || (IsNumeric(e.Key)))
            {
                // textBox is currently a number of sorts - allow control keys and numerics
                e.Handled = false;
            }
            else
            {
                // All non control keys and non numerics and non dashes fall into here
                // We are only interested in whether to allow decimal points 
                // but dash Formats do not allow decimal point
                e.Handled = true;
            }
        }

        static bool StartsWithNegative(string numericFormat)
        {
            if (string.IsNullOrWhiteSpace(numericFormat))
            {
                return false;
            }

            return numericFormat.StartsWith("-") ? true : false;
        }

        static bool AllowNegativeAndIsNegative(string numericFormat, string text)
        {
            if (StartsWithNegative(numericFormat) == false)
            {
                return false;
            }

            if (StartsWithNegative(text) == false)
            {
                return false;
            }

            return true;
        }

        static void _TextBoxNumericFormatDecimalTextChanged(object sender, EventArgs e)
        {
            // For decimal numbers - force the user to enter the decimal point and honor its location
            TextBox textBox = sender as TextBox;

            // If no data to check - return
            if (string.IsNullOrEmpty((string)textBox.GetValue(NumericFormatProperty)))
            {
                return;
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            // Split the format specifier and the textbox data for integral/fractional part length checks
            string[] formatSplit = ((string)textBox.GetValue(NumericFormatProperty)).Trim(Convert.ToChar("$"))
                .Trim(Convert.ToChar("-")).Split('.');
            string[] textSplit = textBox.Text.Split('.');
            string[] textSplitNegative = textBox.Text.Split('-');

            // Check that integral part is not too many digits
            if ((AllowNegativeAndIsNegative((string)textBox.GetValue(NumericFormatProperty), textBox.Text) == false) &&
                (textSplit[0].Length > formatSplit[0].Length) && (!string.IsNullOrWhiteSpace(GetPreviousText(textBox))))
            {
                // Too large (without negative sign) - reset to previous (valid) value
                textBox.Text = GetPreviousText(textBox);
                textBox.SelectionStart = textBox.Text.Length;
                return;
            }

            if (AllowNegativeAndIsNegative((string)textBox.GetValue(NumericFormatProperty), textBox.Text) &&
                (textSplit[0].Length > formatSplit[0].Length + 1) &&
                (!string.IsNullOrWhiteSpace(GetPreviousText(textBox))))
            {
                // Too large (with negative sign) - reset to previous (valid) value
                textBox.Text = GetPreviousText(textBox);
                textBox.SelectionStart = textBox.Text.Length;
                return;
            }

            if (AllowNegativeAndIsNegative((string)textBox.GetValue(NumericFormatProperty), textBox.Text) &&
                ((textSplitNegative.GetLength(0) > 2) || (textSplitNegative[0].Length != 0)))
            {
                // Multiple negative signs or negative sign is not the first character
                textBox.Text = GetPreviousText(textBox);
                textBox.SelectionStart = textBox.Text.Length;
                return;
            }

            // Check that fractional part is not too many digits
            if (((textSplit.GetLength(0) > 1) && (textSplit[1].Length > formatSplit[1].Length)) &&
                (!string.IsNullOrWhiteSpace(GetPreviousText(textBox))))
            {
                // Too large - reset to previous (valid) value
                textBox.Text = GetPreviousText(textBox);
                textBox.SelectionStart = textBox.Text.Length;
            }

            if (((textSplit.GetLength(0) > 1) && (textSplit[1].Length > formatSplit[1].Length)) &&
                (string.IsNullOrWhiteSpace(GetPreviousText(textBox))))
            {
                // This code gets hit if data does not match format during initial binding
                textBox.Text = textSplit[0] + "." + textSplit[1].Substring(0, formatSplit[1].Length);
                textBox.SelectionStart = textBox.Text.Length;
            }
        }

        static void _TextBoxNumericFormatZipCodeTextChanged(object sender, EventArgs e)
        {
            // For ZipCodes - input the dash as appropriate - don't let the user type it
            TextBox textBox = sender as TextBox;

            // If no data to check - return
            if (string.IsNullOrEmpty((string)textBox.GetValue(NumericFormatProperty)))
            {
                return;
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            // Add the dash as appropriate depending on the zipcode length
            string text = textBox.Text.Replace("-", "");
            int selectionStart = (textBox.SelectionStart == textBox.Text.Length)
                ? textBox.Text.Length + 1
                : textBox.SelectionStart;
            if (text.Length <= 5)
            {
                // 99999
                textBox.Text = text;
            }
            else
            {
                // 99999 - 9999
                textBox.Text = text.Substring(0, 5) + '-' + text.Substring(5, (text.Length - 5));
            }

            textBox.SelectionStart = selectionStart;
        }

        static void _TextBoxNumericFormatSSNTextChanged(object sender, EventArgs e)
        {
            // For SSN - input the dash as appropriate - don't let the user type it
            TextBox textBox = sender as TextBox;
            // If no data to check - return
            if (string.IsNullOrEmpty((string)textBox.GetValue(NumericFormatProperty)))
            {
                return;
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            // Add the dash as appropriate depending on the ssn length
            string text = textBox.Text.Replace("-", "");
            int selectionStart = (textBox.SelectionStart == textBox.Text.Length)
                ? textBox.Text.Length + 1
                : textBox.SelectionStart;
            if (text.Length <= 3)
            {
                // 999
                textBox.Text = text;
            }
            else if (text.Length <= 5)
            {
                // 999-99
                textBox.Text = text.Substring(0, 3) + '-' + text.Substring(3, (text.Length - 3));
            }
            else
            {
                // 999-99-9999
                textBox.Text = text.Substring(0, 3) + '-' + text.Substring(3, 2) + '-' +
                               text.Substring(5, (text.Length - 5));
            }

            textBox.SelectionStart = selectionStart;
        }

        static void _TextBoxNumericFormatFederalTaxIDTextChanged(object sender, EventArgs e)
        {
            // For FederalTaxID - input the dash as appropriate - don't let the user type it
            TextBox textBox = sender as TextBox;

            // If no data to check - return
            if (string.IsNullOrEmpty((string)textBox.GetValue(NumericFormatProperty)))
            {
                return;
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            // Add the dash as appropriate depending on the FederalTaxID length
            string text = textBox.Text.Replace("-", "");
            int selectionStart = (textBox.SelectionStart == textBox.Text.Length)
                ? textBox.Text.Length + 1
                : textBox.SelectionStart;
            if (text.Length <= 2)
            {
                // 99
                textBox.Text = text;
            }
            else
            {
                // 99-9999999
                textBox.Text = text.Substring(0, 2) + '-' + text.Substring(2, (text.Length - 2));
            }

            textBox.SelectionStart = selectionStart;
        }

        static void _TextBoxNumericFormatPhoneTextChanged(object sender, EventArgs e)
        {
            // For Phones - input the period(s) as appropriate - don't let the user type then
            TextBox textBox = sender as TextBox;

            // If no data to check - return
            if (string.IsNullOrEmpty((string)textBox.GetValue(NumericFormatProperty)))
            {
                return;
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            // Add the period(s) as appropriate depending on the phone number length
            string text = textBox.Text.Replace(".", "");
            int selectionStart = (textBox.SelectionStart == textBox.Text.Length)
                ? textBox.Text.Length + 1
                : textBox.SelectionStart;
            string resultText = FormatPhoneText(text);
            textBox.Text = resultText;
            textBox.SelectionStart = selectionStart;
        }

        public static string FormatPhoneText(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var resultText = string.Empty;
            if (text.Length <= 3)
            {
                // 999
                resultText = text;
            }
            else if (text.Length <= 7)
            {
                // 999.9999
                resultText = text.Substring(0, 3) + '.' + text.Substring(3, (text.Length - 3));
            }
            else
            {
                // 999.999.9999
                resultText = text.Substring(0, 3) + '.' + text.Substring(3, 3) + '.' +
                             text.Substring(6, (text.Length - 6));
            }

            return resultText;
        }

        static void _TextBoxNumericFormatNdcTextChanged(object sender, EventArgs e)
        {
            // For Phones - input the dashes(s) as appropriate - don't let the user type then
            TextBox textBox = sender as TextBox;

            // If no data to check - return
            if (string.IsNullOrEmpty((string)textBox.GetValue(NumericFormatProperty)))
            {
                return;
            }

            if (string.IsNullOrEmpty(textBox.Text))
            {
                return;
            }

            // Add the dashes(s) as appropriate depending on the phone number length
            string text = textBox.Text.Replace("-", "");
            int selectionStart = (textBox.SelectionStart == textBox.Text.Length)
                ? textBox.Text.Length + 1
                : textBox.SelectionStart;
            string resultText = FormatNDCText(text);
            textBox.Text = resultText;
            textBox.SelectionStart = selectionStart;
        }

        public static string FormatNDCText(string text)
        {
            if (String.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var resultText = string.Empty;
            if (text.Length <= 5)
            {
                // 99999
                resultText = text;
            }
            else if (text.Length <= 9)
            {
                // 99999-9999
                resultText = text.Substring(0, 5) + '-' + text.Substring(5, (text.Length - 5));
            }
            else
            {
                // 99999-9999-99
                resultText = text.Substring(0, 5) + '-' + text.Substring(5, 4) + '-' +
                             text.Substring(9, (text.Length - 9));
            }

            return resultText;
        }
    }
}