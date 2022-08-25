using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Virtuoso.Core.Services;
using Virtuoso.Server.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Virtuoso.Core.Cache;
using System.Threading;


namespace Virtuoso.Core.Controls
{
    public class codeLookupUndermining : System.Windows.Controls.ComboBox
    {
        private TextBlock controlTextBlock = null;
        private ScrollViewer scrollViewer = null;
        private TextBox textBoxClock12 = null;
        private TextBox textBoxClock1 = null;
        private TextBox textBoxClock2 = null;
        private TextBox textBoxClock3 = null;
        private TextBox textBoxClock4 = null;
        private TextBox textBoxClock5 = null;
        private TextBox textBoxClock6 = null;
        private TextBox textBoxClock7 = null;
        private TextBox textBoxClock8 = null;
        private TextBox textBoxClock9 = null;
        private TextBox textBoxClock10 = null;
        private TextBox textBoxClock11 = null;
        private CheckBox checkBoxClock12 = null;
        private CheckBox checkBoxClock1 = null;
        private CheckBox checkBoxClock2 = null;
        private CheckBox checkBoxClock3 = null;
        private CheckBox checkBoxClock4 = null;
        private CheckBox checkBoxClock5 = null;
        private CheckBox checkBoxClock6 = null;
        private CheckBox checkBoxClock7 = null;
        private CheckBox checkBoxClock8 = null;
        private CheckBox checkBoxClock9 = null;
        private CheckBox checkBoxClock10 = null;
        private CheckBox checkBoxClock11 = null;
        private Canvas controlCanvas = null;
        private Button controlCloseButton = null;
        private Button controlContinuousButton = null;
        private string CR = char.ToString('\r');

        private string[] underminingsDelimiter = { "|" };
        private string[] underminingPiecesDelimiter = { "@" };

        public codeLookupUndermining()
        {
            this.Loaded += new RoutedEventHandler(codeLookupUndermining_Loaded);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreCodeLookupUnderminingStyle"]; }
            catch { }
            this.DropDownOpened += new EventHandler(codeLookupUndermining_DropDownOpened);
            this.DropDownClosed += new EventHandler(codeLookupUndermining_DropDownClosed);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            controlTextBlock = (TextBlock)GetTemplateChild("ControlTextBlock");
            string s = Underminings as string;
            if (controlTextBlock != null) { controlTextBlock.Text = (s == null) ? "" : s.ToString(); }
            controlCanvas = (Canvas)GetTemplateChild("ControlCanvas");
            controlCloseButton = (Button)GetTemplateChild("ControlCloseButton");
            if (controlCloseButton != null)
            {
                // Set/reset Click event
                try { controlCloseButton.Click -= controlCloseButton_Click; }
                catch { }
                controlCloseButton.Click += new RoutedEventHandler(controlCloseButton_Click);
            }
            controlContinuousButton = (Button)GetTemplateChild("ControlContinuousButton");
            if (controlContinuousButton != null)
            {
                // Set/reset Click event
                try { controlContinuousButton.Click -= controlContinuousButton_Click; }
                catch { }
                controlContinuousButton.Click += new RoutedEventHandler(controlContinuousButton_Click);
            }
            scrollViewer = (ScrollViewer)GetTemplateChild("ScrollViewer");
            textBoxClock12 = (TextBox)GetTemplateChild("textBoxClock12");
            textBoxClock1 = (TextBox)GetTemplateChild("textBoxClock1");
            textBoxClock2 = (TextBox)GetTemplateChild("textBoxClock2");
            textBoxClock3 = (TextBox)GetTemplateChild("textBoxClock3");
            textBoxClock4 = (TextBox)GetTemplateChild("textBoxClock4");
            textBoxClock5 = (TextBox)GetTemplateChild("textBoxClock5");
            textBoxClock6 = (TextBox)GetTemplateChild("textBoxClock6");
            textBoxClock7 = (TextBox)GetTemplateChild("textBoxClock7");
            textBoxClock8 = (TextBox)GetTemplateChild("textBoxClock8");
            textBoxClock9 = (TextBox)GetTemplateChild("textBoxClock9");
            textBoxClock10 = (TextBox)GetTemplateChild("textBoxClock10");
            textBoxClock11 = (TextBox)GetTemplateChild("textBoxClock11");

            checkBoxClock12 = (CheckBox)GetTemplateChild("checkBoxClock12");
            checkBoxClock1 = (CheckBox)GetTemplateChild("checkBoxClock1");
            checkBoxClock2 = (CheckBox)GetTemplateChild("checkBoxClock2");
            checkBoxClock3 = (CheckBox)GetTemplateChild("checkBoxClock3");
            checkBoxClock4 = (CheckBox)GetTemplateChild("checkBoxClock4");
            checkBoxClock5 = (CheckBox)GetTemplateChild("checkBoxClock5");
            checkBoxClock6 = (CheckBox)GetTemplateChild("checkBoxClock6");
            checkBoxClock7 = (CheckBox)GetTemplateChild("checkBoxClock7");
            checkBoxClock8 = (CheckBox)GetTemplateChild("checkBoxClock8");
            checkBoxClock9 = (CheckBox)GetTemplateChild("checkBoxClock9");
            checkBoxClock10 = (CheckBox)GetTemplateChild("checkBoxClock10");
            checkBoxClock11 = (CheckBox)GetTemplateChild("checkBoxClock11");

            SetEvents();
        }
        private void SetEvents()
        {
            for (int i = 0; i < 12; i++) { SetTextBoxClockEvents(i, true); SetCheckBoxClockEvents(i, true); }
        }
        private void ClearEvents()
        {
            for (int i = 0; i < 12; i++) { SetTextBoxClockEvents(i, false); SetCheckBoxClockEvents(i, false); }
        }
        private void SetTextBoxClockEvents(int textBoxNumber, bool set)
        {
            TextBox textBox = GetTextBox(textBoxNumber);
            if (textBox == null) return;
            try { textBox.GotFocus -= textBoxClock_GotFocus; }
            catch { }
            if (set) textBox.GotFocus += new RoutedEventHandler(textBoxClock_GotFocus);
            try { textBox.KeyUp -= textBoxClock_KeyUp; }
            catch { }
            if (set) textBox.KeyUp += new KeyEventHandler(textBoxClock_KeyUp);
        }
        private void SetCheckBoxClockEvents(int checkBoxNumber, bool set)
        {
            CheckBox checkBox = GetCheckBox(checkBoxNumber);
            if (checkBox == null) return;
            try { checkBox.Checked -= checkBoxClock_XChecked; }
            catch { }
            if (set) checkBox.Checked += new RoutedEventHandler(checkBoxClock_XChecked);
            try { checkBox.Unchecked -= checkBoxClock_XChecked; }
            catch { }
            if (set) checkBox.Unchecked += new RoutedEventHandler(checkBoxClock_XChecked);
            try { checkBox.GotFocus -= checkBoxClock_GotFocus; }
            catch { }
            if (set) checkBox.GotFocus += new RoutedEventHandler(checkBoxClock_GotFocus);
        }

        private void codeLookupUndermining_Loaded(object sender, RoutedEventArgs e)
        {
            if (controlCanvas == null) this.ApplyTemplate();
        }

        public static DependencyProperty UnderminingDescriptionProperty =
         DependencyProperty.Register("UnderminingDescription", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupUndermining),null);

        public object UnderminingDescription
        {
            get 
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupUndermining.UnderminingDescriptionProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set 
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupUndermining.UnderminingDescriptionProperty, value); 
            }
        }
        public static DependencyProperty UnderminingsProperty =
         DependencyProperty.Register("Underminings", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupUndermining),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookupUndermining)o).SetupUnderminingDescriptionFromUnderminings();
          }));

        public object Underminings
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupUndermining.UnderminingsProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupUndermining.UnderminingsProperty, value);
            }
        }
        private void SetupUnderminingDescriptionFromUnderminings()
        {
            if (controlTextBlock == null) ApplyTemplate();
            string underminings = Underminings as string;
            string underminingDescription = null;
            if (string.IsNullOrWhiteSpace(underminings) == false)
            {
                string[] underminingArray = underminings.Split(underminingsDelimiter, StringSplitOptions.RemoveEmptyEntries);
                string[] underminingPiecesArray = null;
                if (underminingArray.Length != 0)
                {
                    foreach (string undermining in underminingArray)
                    {
                        if (!string.IsNullOrEmpty(undermining))
                        {
                            underminingPiecesArray = undermining.Split(underminingPiecesDelimiter, StringSplitOptions.RemoveEmptyEntries);
                            if (underminingPiecesArray.Length == 4)
                            {
                                if (underminingDescription != null) underminingDescription = underminingDescription + CR;
                                if (underminingPiecesArray[0] == underminingPiecesArray[1])
                                {
                                    underminingDescription = underminingDescription +
                                       (((underminingPiecesArray[2] == "?") || (underminingPiecesArray[3] == "?")) ?
                                        string.Format("Continuous undermining") :
                                        string.Format("Continuous undermining with deepest {0} cm at {1} o'clock", underminingPiecesArray[2], underminingPiecesArray[3]));
                                }
                                else
                                {
                                    underminingDescription = underminingDescription +
                                        (((underminingPiecesArray[2] == "?") || (underminingPiecesArray[3] == "?")) ?
                                         string.Format("From {0} to {1} o'clock", underminingPiecesArray[0], underminingPiecesArray[1]) :
                                         string.Format("From {0} to {1} o'clock with deepest {2} cm at {3} o'clock", underminingPiecesArray[0], underminingPiecesArray[1], underminingPiecesArray[2], underminingPiecesArray[3]));
                                }
                            }
                        }
                    }
                }
            }
            if (underminingDescription == null) underminingDescription = "";
            if (controlTextBlock != null)
            {
                if (controlTextBlock.Text != underminingDescription) controlTextBlock.Text = underminingDescription;
            } 
        }
        private void SetupPopupControlsFromUnderminings()
        {
            if (controlTextBlock == null) ApplyTemplate();

            for (int i = 0; i < 12; i++) { SetCheckBoxIsChecked(i, false); SetTextBoxIsEnabled(i, false); SetTextBoxText(i, ""); }

            underminings = Underminings as string;
            if (string.IsNullOrWhiteSpace(underminings)) return;
            underminingDescription = null;
            string[] underminingArray = underminings.Split(underminingsDelimiter, StringSplitOptions.RemoveEmptyEntries);
            string[] underminingPiecesArray = null;
            if (underminingArray.Length != 0)
            {
                foreach (string undermining in underminingArray)
                {
                    if (!string.IsNullOrEmpty(undermining))
                    {
                        underminingPiecesArray = undermining.Split(underminingPiecesDelimiter, StringSplitOptions.RemoveEmptyEntries);
                        if (underminingPiecesArray.Length == 4)
                        {
                            int fromOClock = 0;
                            try { fromOClock = Int32.Parse(underminingPiecesArray[0]); }
                            catch { }
                            if (fromOClock == 12) fromOClock = 0;
                            int toOClock = 0;
                            try { toOClock = Int32.Parse(underminingPiecesArray[1]); }
                            catch { }
                            if (toOClock == 12) toOClock = 0;
                            if (fromOClock == toOClock)
                            {
                                for (int i = 0; i < 12; i++) { SetCheckBoxIsChecked(i, true); }
                            }
                            else if (fromOClock < toOClock)
                            {
                                for (int i = fromOClock; i <= toOClock-1; i++) { SetCheckBoxIsChecked(i, true); }
                            }
                            else
                            {
                                for (int i = fromOClock; i < 12; i++) { SetCheckBoxIsChecked(i, true); }
                                for (int i = 0; i <= toOClock - 1; i++) { SetCheckBoxIsChecked(i, true); }
                            }
                            // Setup deepest if it exists
                            if ((underminingPiecesArray[2] != "?") && (underminingPiecesArray[3] != "?"))
                            {
                                int deepestOClock = 0;
                                try { deepestOClock = Int32.Parse(underminingPiecesArray[3]); }
                                catch { }
                                SetTextBoxText(deepestOClock, underminingPiecesArray[2]);
                            }
                            if (underminingDescription != null) underminingDescription = underminingDescription + CR;
                            if (underminingPiecesArray[0] == underminingPiecesArray[1])
                            {
                                underminingDescription = underminingDescription +
                                   (((underminingPiecesArray[2] == "?") || (underminingPiecesArray[3] == "?")) ?
                                    string.Format("Continuous undermining") :
                                    string.Format("Continuous undermining with deepest {0} cm at {1} o'clock", underminingPiecesArray[2], underminingPiecesArray[3]));
                            }
                            else
                            {
                                underminingDescription = underminingDescription +
                                    (((underminingPiecesArray[2] == "?") || (underminingPiecesArray[3] == "?")) ?
                                    string.Format("From {0} to {1} o'clock", underminingPiecesArray[0], underminingPiecesArray[1]) :
                                    string.Format("From {0} to {1} o'clock with deepest {2} cm at {3} o'clock", underminingPiecesArray[0], underminingPiecesArray[1], underminingPiecesArray[2], underminingPiecesArray[3]));
                            }
                        }
                    }
                }
            }
            SetTextBoxEnabled();
            if (underminingDescription == null) underminingDescription = "";
            if (controlTextBlock != null)
            {
                if (controlTextBlock.Text != underminingDescription) controlTextBlock.Text = underminingDescription;
            }
        }
        private string underminings = null;
        private string underminingDescription = null;
        private void AppendUndermining(int? startOClock, int? endOClock)
        {
            if (startOClock == null) return;
            int fromOClock = (int)startOClock;
            int toOClock = (endOClock == null) ? fromOClock : (int)endOClock;
            toOClock++;
            int? deepestOClock = FindDeepest(fromOClock, toOClock);
            if (fromOClock == 0) fromOClock = 12;
            if (underminings != null) underminings = underminings + "|";
            if (underminingDescription != null) underminingDescription = underminingDescription + CR;
            if (deepestOClock == null)
            {
                underminings = underminings + string.Format("{0}@{1}@?@?", fromOClock.ToString(), toOClock.ToString());
                if (fromOClock == toOClock)
                    underminingDescription = underminingDescription + string.Format("Continuous undermining");
                else
                    underminingDescription = underminingDescription + string.Format("From {0} to {1} o'clock", fromOClock.ToString(), toOClock.ToString());
            }
            else
            {
                underminings = underminings + string.Format("{0}@{1}@{2}@{3}", fromOClock.ToString(), toOClock.ToString(), GetTextBoxText((int)deepestOClock), deepestOClock.ToString());
                if (fromOClock == toOClock)
                    underminingDescription = underminingDescription + string.Format("Continuous undermining with deepest {0} cm at {1} o'clock", GetTextBoxText((int)deepestOClock), deepestOClock.ToString());
                else
                    underminingDescription = underminingDescription + string.Format("From {0} to {1} o'clock with deepest {2} cm at {3} o'clock", fromOClock.ToString(), toOClock.ToString(), GetTextBoxText((int)deepestOClock), deepestOClock.ToString());
            }
        }

        int? findDeepestOClock = null;
        double findDeepest = 0;
        private int? FindDeepest(int fromOClock, int toOClock)
        {
            findDeepestOClock = null;
            findDeepest = 0;
            if ((fromOClock == toOClock) || ((fromOClock == 0) && (toOClock == 12)))
            {
                for (int i = 0; i < 12; i++) { AmIDeepest(i); }
            }
            else if (fromOClock < toOClock)
            {
                for (int i = fromOClock; i <= toOClock; i++) { AmIDeepest(i); }
            }
            else
            {
                for (int i = fromOClock; i < 12; i++) { AmIDeepest(i); }
                for (int i = 0; i <= toOClock; i++) { AmIDeepest(i); }
            }
            if (findDeepestOClock == 0) findDeepestOClock = 12;
            return findDeepestOClock;
        }
        private void AmIDeepest(int textBoxNumber)
        {
            double? myDepth = GetTextBoxDouble(textBoxNumber);
            if (myDepth == null) return;
            if (findDeepest < myDepth)
            {
                findDeepest = (double)myDepth;
                if (findDeepestOClock != null) SetTextBoxText((int)findDeepestOClock, "");
                findDeepestOClock = textBoxNumber;
            }
            else
            {
                SetTextBoxText(textBoxNumber, "");
            }
        }

        bool inHere = false;
        private void SetupUnderminingsFromPopupControls(bool setTextBoxEnabled)
        {
            if (inHere) return;
            inHere = true;
            try
            {
                underminings = null;
                underminingDescription = null;
                bool continuous = (IsCheckBoxChecked(0) && IsCheckBoxChecked(1) && IsCheckBoxChecked(2) && IsCheckBoxChecked(3) && IsCheckBoxChecked(4) && IsCheckBoxChecked(5) && IsCheckBoxChecked(6) && IsCheckBoxChecked(7) && IsCheckBoxChecked(8) && IsCheckBoxChecked(9) && IsCheckBoxChecked(10) && IsCheckBoxChecked(11));
                bool wrappingUnderminings = (IsCheckBoxChecked(11) && IsCheckBoxChecked(0));
                int? startOClock = null;
                int? endOClock = null;
                if (continuous)
                {
                    AppendUndermining(0, 11);
                }
                else if (wrappingUnderminings)
                {
                    int lastOClock = 0;
                    for (int i = 1; i < 11; i++)
                    {
                        if (IsCheckBoxChecked(i) == false) break;
                        lastOClock = i;
                    }
                    startOClock = null;
                    endOClock = null;
                    for (int i = lastOClock + 1; i <= 11; i++)
                    {
                        if ((IsCheckBoxChecked(i)) && (startOClock == null))
                        {
                            startOClock = i;
                            endOClock = null;
                        }
                        else if ((IsCheckBoxChecked(i)) && (startOClock != null))
                        {
                            endOClock = i;
                        }
                        else if ((IsCheckBoxChecked(i) == false) && (startOClock != null))
                        {
                            AppendUndermining(startOClock, endOClock);
                            startOClock = null;
                            endOClock = null;
                        }
                    }
                    if (startOClock != null) AppendUndermining(startOClock, lastOClock);
                }
                else
                {
                    startOClock = null;
                    endOClock = null;
                    for (int i = 0; i < 12; i++)
                    {
                        if ((IsCheckBoxChecked(i)) && (startOClock == null))
                        {
                            startOClock = i;
                            endOClock = null;
                        }
                        else if ((IsCheckBoxChecked(i)) && (startOClock != null))
                        {
                            endOClock = i;
                        }
                        else if ((IsCheckBoxChecked(i) == false) && (startOClock != null))
                        {
                            AppendUndermining(startOClock, endOClock);
                            startOClock = null;
                            endOClock = null;
                        }
                    }
                    if (startOClock != null) AppendUndermining(startOClock, endOClock);
                }
                if (underminingDescription == null) underminingDescription = "";
                if (controlTextBlock != null)
                {
                    if (controlTextBlock.Text != underminingDescription) controlTextBlock.Text = underminingDescription;
                }
            }
            catch { }
            if (setTextBoxEnabled) SetTextBoxEnabled();
            inHere = false;
            return;
        }
        private CheckBox GetCheckBox(int checkBoxNumber)
        {
            if (checkBoxNumber == 1) return checkBoxClock1;
            else if (checkBoxNumber == 2) return checkBoxClock2;
            else if (checkBoxNumber == 3) return checkBoxClock3;
            else if (checkBoxNumber == 4) return checkBoxClock4;
            else if (checkBoxNumber == 5) return checkBoxClock5;
            else if (checkBoxNumber == 6) return checkBoxClock6;
            else if (checkBoxNumber == 7) return checkBoxClock7;
            else if (checkBoxNumber == 8) return checkBoxClock8;
            else if (checkBoxNumber == 9) return checkBoxClock9;
            else if (checkBoxNumber == 10) return checkBoxClock10;
            else if (checkBoxNumber == 11) return checkBoxClock11;
            else return checkBoxClock12;
        }
        private TextBox GetTextBox(int textBoxNumber)
        {
            if (textBoxNumber == 1) return textBoxClock1;
            else if (textBoxNumber == 2) return textBoxClock2;
            else if (textBoxNumber == 3) return textBoxClock3;
            else if (textBoxNumber == 4) return textBoxClock4;
            else if (textBoxNumber == 5) return textBoxClock5;
            else if (textBoxNumber == 6) return textBoxClock6;
            else if (textBoxNumber == 7) return textBoxClock7;
            else if (textBoxNumber == 8) return textBoxClock8;
            else if (textBoxNumber == 9) return textBoxClock9;
            else if (textBoxNumber == 10) return textBoxClock10;
            else if (textBoxNumber == 11) return textBoxClock11;
            else return textBoxClock12;
        }
        private bool IsCheckBoxChecked(int checkBoxNumber)
        {
            CheckBox checkBox = GetCheckBox(checkBoxNumber);
            if (checkBox == null) return false;
            return (checkBox.IsChecked == null) ? false : (bool)checkBox.IsChecked;
        }
        private void SetCheckBoxIsChecked(int checkBoxNumber, bool isChecked)
        {
            CheckBox checkBox = GetCheckBox(checkBoxNumber);
            if (checkBox == null) return;
            if (checkBox.IsChecked != isChecked) checkBox.IsChecked = isChecked;
        }
        private void SetTextBoxIsEnabled(int textBoxNumber, bool isEnabled)
        {
            TextBox textBox = GetTextBox(textBoxNumber);
            if (textBox == null) return;
            if (textBox.IsEnabled != isEnabled) textBox.IsEnabled = isEnabled;
        }
        private bool GetTextBoxIsEnabled(int textBoxNumber)
        {
            TextBox textBox = GetTextBox(textBoxNumber);
            if (textBox == null) return false;
            return textBox.IsEnabled;
        }
        private void SetTextBoxText(int textBoxNumber, string text)
        {
            TextBox textBox = GetTextBox(textBoxNumber);
            if (textBox == null) return;
            if (textBox.Text == null) textBox.Text = text;
            else if (textBox.Text != text) textBox.Text = text;
        }
        private string GetTextBoxText(int textBoxNumber)
        {
            TextBox textBox = GetTextBox(textBoxNumber);
            if (textBox == null) return "";
            return (textBox.Text == null) ? "" : textBox.Text;
        }
        private double? GetTextBoxDouble(int textBoxNumber)
        {
            TextBox textBox = GetTextBox(textBoxNumber);
            if (textBox == null) return (double?)null;
            if (string.IsNullOrWhiteSpace(textBox.Text)) return (double?)null; 
            double deepest = 0.0;
            try { deepest = double.Parse(textBox.Text); }
            catch { }
            if (deepest == 0) { textBox.Text = ""; return (double?)null; }
            return deepest;
        }
        private void SetTextBoxEnabled()
        {
            for (int i = 0; i < 12; i++) { SetTextBoxIsEnabled(i, false); }
            for (int i = 0; i < 12; i++)
            {
                if (IsCheckBoxChecked(i)) { SetTextBoxIsEnabled(i, true); SetTextBoxIsEnabled(i+1, true); }
            }
            for (int i = 0; i < 12; i++) { if (GetTextBoxIsEnabled(i) == false) SetTextBoxText(i, ""); }
        }
        private void controlCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsDropDownOpen = false;
        }
        private void controlContinuousButton_Click(object sender, RoutedEventArgs e)
        {
            if (dropDownOpening) return;
            ClearEvents();
            for (int i = 0; i < 12; i++) { SetCheckBoxIsChecked(i, true); }
            SetupUnderminingsFromPopupControls(true);
            SetEvents();
        }
        private void codeLookupUndermining_DropDownClosed(object sender, EventArgs e)
        {
            SetupUnderminingsFromPopupControls(false);
            Underminings = (underminings == null) ? "" : underminings;
            UnderminingDescription = (underminingDescription == null) ? "" : underminingDescription;
        }
        private bool dropDownOpening = false;
        private void codeLookupUndermining_DropDownOpened(object sender, EventArgs e)
        {
            dropDownOpening = true;
            SetupPopupControlsFromUnderminings();
            dropDownOpening = false;
        }
        private void textBoxClock_GotFocus(object sender, RoutedEventArgs e)
        {
            if (dropDownOpening) return;
            ClearEvents();
            Chase(sender);
            Refocus(sender);
            SetupUnderminingsFromPopupControls(false);
            SetEvents();
        }

        private void textBoxClock_KeyUp(object sender, KeyEventArgs e)
        {
            if (e == null) return;
            if (e.Key != Key.Enter) return;
            if (dropDownOpening) return;
            ClearEvents();
            Refocus(sender); 
            SetupUnderminingsFromPopupControls(false);
            SetEvents();
        }
        private void Refocus(object sender)
        {
            TextBox textBox = sender as TextBox;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (textBox != null) textBox.Focus();
                });
            });
        }
        private bool Chase(object sender)
        {
            if (string.IsNullOrWhiteSpace(underminings)) return false;
            TextBox textBox = sender as TextBox;
            if (textBox == null) return false;
            string currentOClockString = textBox.Tag as string;
            int currentOClock = 0;
            try { currentOClock = Int32.Parse(currentOClockString); }
            catch { }
            if (currentOClock == 0) return false;
            if (currentOClock == 12) currentOClock = 0;
            string[] underminingArray = underminings.Split(underminingsDelimiter, StringSplitOptions.RemoveEmptyEntries);
            string[] underminingPiecesArray = null;
            if (underminingArray.Length != 0)
            {
                foreach (string undermining in underminingArray)
                {
                    if (!string.IsNullOrEmpty(undermining))
                    {
                        underminingPiecesArray = undermining.Split(underminingPiecesDelimiter, StringSplitOptions.RemoveEmptyEntries);
                        if (underminingPiecesArray.Length == 4)
                        {
                            int fromOClock = 0;
                            try { fromOClock = Int32.Parse(underminingPiecesArray[0]); }
                            catch { }
                            if (fromOClock == 12) fromOClock = 0;
                            int toOClock = 0;
                            try { toOClock = Int32.Parse(underminingPiecesArray[1]); }
                            catch { }
                            if (toOClock == 12) toOClock = 0;

                            int deepestOClock = 0;
                            try { deepestOClock = Int32.Parse(underminingPiecesArray[3]); }
                            catch { }
                            if (deepestOClock == 12) deepestOClock = 0;

                            if ((underminingPiecesArray[2] == "?") || (underminingPiecesArray[3] == "?")) continue;
                            if (currentOClock == deepestOClock) return false;

                            if (fromOClock == toOClock)
                            {
                                SetTextBoxText (currentOClock, GetTextBoxText(deepestOClock));
                                SetTextBoxText(deepestOClock, "");
                                return true;
                            }
                            else if (fromOClock < toOClock) 
                            {
                                if ((fromOClock <= currentOClock) && (currentOClock <= toOClock))
                                {
                                    SetTextBoxText(currentOClock, GetTextBoxText(deepestOClock));
                                    SetTextBoxText(deepestOClock, "");
                                    return true;
                                }
                            }
                            else if ((fromOClock <= currentOClock) || (currentOClock <= toOClock))
                            {
                                SetTextBoxText(currentOClock, GetTextBoxText(deepestOClock));
                                SetTextBoxText(deepestOClock, "");
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        private void checkBoxClock_XChecked(object sender, RoutedEventArgs e)
        {
            if (dropDownOpening) return;
            ClearEvents();
            SetupUnderminingsFromPopupControls(true);
            SetEvents();
        }
        private void checkBoxClock_GotFocus(object sender, RoutedEventArgs e)
        {
            if (dropDownOpening) return;
            ClearEvents();
            SetupUnderminingsFromPopupControls(false);
            SetEvents();
        }

    }
}

