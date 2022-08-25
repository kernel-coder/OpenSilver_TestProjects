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
    public class codeLookupTunneling : System.Windows.Controls.ComboBox
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
        private Canvas controlCanvas = null;
        private Button controlCloseButton = null;
        private string CR = char.ToString('\r');

        public codeLookupTunneling()
        {
            this.Loaded += new RoutedEventHandler(codeLookupTunneling_Loaded);
            try { this.Style = (Style)System.Windows.Application.Current.Resources["CoreCodeLookupTunnelingStyle"]; } 
            catch { }
            this.DropDownOpened += new EventHandler(codeLookupTunneling_DropDownOpened);
            this.DropDownClosed += new EventHandler(codeLookupTunneling_DropDownClosed);
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            controlTextBlock = (TextBlock)GetTemplateChild("ControlTextBlock");
            string s = Tunnelings as string;
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

            SetEvents();
        }
        private void SetEvents()
        {
            for (int i = 0; i < 12; i++) { SetTextBoxClockEvents(i, true); }
        }
        private void ClearEvents()
        {
            for (int i = 0; i < 12; i++) { SetTextBoxClockEvents(i, false); }
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
        private void SetTextBoxClockEvents(TextBox textBox)
        {
            if (textBox == null) return;
            try { textBox.GotFocus -= textBoxClock_GotFocus; }
            catch { }
            textBox.GotFocus += new RoutedEventHandler(textBoxClock_GotFocus);
        }

        private void codeLookupTunneling_Loaded(object sender, RoutedEventArgs e)
        {
            if (controlCanvas == null) this.ApplyTemplate();
        }

        public static DependencyProperty TunnelingDescriptionProperty =
         DependencyProperty.Register("TunnelingDescription", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupTunneling),null);

        public object TunnelingDescription
        {
            get 
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupTunneling.TunnelingDescriptionProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set 
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupTunneling.TunnelingDescriptionProperty, value); 
            }
        }
        public static DependencyProperty TunnelingsProperty =
         DependencyProperty.Register("Tunnelings", typeof(object), typeof(Virtuoso.Core.Controls.codeLookupTunneling),
          new PropertyMetadata((o, e) =>
          {
              ((Virtuoso.Core.Controls.codeLookupTunneling)o).SetupTunnelingDescriptionFromTunnelings();
          }));

        public object Tunnelings
        {
            get
            {
                string s = ((string)(base.GetValue(Virtuoso.Core.Controls.codeLookupTunneling.TunnelingsProperty)));
                return string.IsNullOrWhiteSpace(s) ? null : s.Trim();
            }
            set
            {
                base.SetValue(Virtuoso.Core.Controls.codeLookupTunneling.TunnelingsProperty, value);
            }
        }
        private void SetupTunnelingDescriptionFromTunnelings()
        {
            if (controlTextBlock == null) ApplyTemplate();

            string tunnelings = Tunnelings as string;
            string tunnelingDescription = null;
            if (string.IsNullOrWhiteSpace(tunnelings) == false)
            {
                string[] tunnelingsDelimiter = { "|" };
                string[] tunnelingPiecesDelimiter = { "@" };
                string[] tunnelingArray = tunnelings.Split(tunnelingsDelimiter, StringSplitOptions.RemoveEmptyEntries);
                string[] tunnelingPiecesArray = null;
                if (tunnelingArray.Length != 0)
                {
                    foreach (string tunneling in tunnelingArray)
                    {
                        if (!string.IsNullOrEmpty(tunneling))
                        {
                            tunnelingPiecesArray = tunneling.Split(tunnelingPiecesDelimiter, StringSplitOptions.RemoveEmptyEntries);
                            if (tunnelingPiecesArray.Length == 2)
                            {
                                if (tunnelingDescription != null) tunnelingDescription = tunnelingDescription + CR;
                                tunnelingDescription = tunnelingDescription + string.Format("At {0} o'clock with depth of {1} cm", tunnelingPiecesArray[0], tunnelingPiecesArray[1]);
                            }
                        }
                    }
                }
            }
            if (controlTextBlock != null) controlTextBlock.Text = (tunnelingDescription == null) ? "" : tunnelingDescription;
        }
        private void SetupPopupControlsFromTunnelings()
        {
            if (controlTextBlock == null) ApplyTemplate();

            if (textBoxClock12 != null) textBoxClock12.Text = "";
            if (textBoxClock1 != null) textBoxClock1.Text = "";
            if (textBoxClock2 != null) textBoxClock2.Text = "";
            if (textBoxClock3 != null) textBoxClock3.Text = "";
            if (textBoxClock4 != null) textBoxClock4.Text = "";
            if (textBoxClock5 != null) textBoxClock5.Text = "";
            if (textBoxClock6 != null) textBoxClock6.Text = "";
            if (textBoxClock7 != null) textBoxClock7.Text = "";
            if (textBoxClock8 != null) textBoxClock8.Text = "";
            if (textBoxClock9 != null) textBoxClock9.Text = "";
            if (textBoxClock10 != null) textBoxClock10.Text = "";
            if (textBoxClock11 != null) textBoxClock11.Text = "";

            string tunnelings = Tunnelings as string;
            if (string.IsNullOrWhiteSpace(tunnelings)) return;
            string tunnelingDescription = null;
            string[] tunnelingsDelimiter = { "|" };
            string[] tunnelingPiecesDelimiter = { "@" };
            string[] tunnelingArray = tunnelings.Split(tunnelingsDelimiter, StringSplitOptions.RemoveEmptyEntries);
            string[] tunnelingPiecesArray = null;
            if (tunnelingArray.Length != 0)
            {
                foreach (string tunneling in tunnelingArray)
                {
                    if (!string.IsNullOrEmpty(tunneling))
                    {
                        tunnelingPiecesArray = tunneling.Split(tunnelingPiecesDelimiter, StringSplitOptions.RemoveEmptyEntries);
                        if (tunnelingPiecesArray.Length == 2)
                        {
                            if ((tunnelingPiecesArray[0] == "12") && (textBoxClock12 != null)) textBoxClock12.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "1") && (textBoxClock1 != null)) textBoxClock1.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "2") && (textBoxClock2 != null)) textBoxClock2.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "3") && (textBoxClock3 != null)) textBoxClock3.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "4") && (textBoxClock4 != null)) textBoxClock4.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "5") && (textBoxClock5 != null)) textBoxClock5.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "6") && (textBoxClock6 != null)) textBoxClock6.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "7") && (textBoxClock7 != null)) textBoxClock7.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "8") && (textBoxClock8 != null)) textBoxClock8.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "9") && (textBoxClock9 != null)) textBoxClock9.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "10") && (textBoxClock10 != null)) textBoxClock10.Text = tunnelingPiecesArray[1];
                            if ((tunnelingPiecesArray[0] == "11") && (textBoxClock11 != null)) textBoxClock11.Text = tunnelingPiecesArray[1];

                            if (tunnelingDescription != null) tunnelingDescription = tunnelingDescription + CR;
                            tunnelingDescription = tunnelingDescription + string.Format("At {0} o'clock with depth of {1} cm", tunnelingPiecesArray[0], tunnelingPiecesArray[1]);
                        }
                    }
                }
            }
            if (controlTextBlock != null) controlTextBlock.Text = (tunnelingDescription == null) ? "" : tunnelingDescription;
        }
        private string tunnelings = null;
        private string tunnelingDescription = null;
        private void AppendTunneling(TextBox textBlockClock)
        {
            if (string.IsNullOrWhiteSpace(textBlockClock.Text)) return;
            double depth = 0.0;
            try { depth = double.Parse(textBlockClock.Text); }
            catch { }
            if (depth == 0) { textBlockClock.Text = ""; return; }
            string tagOclock = textBlockClock.Tag as string;
            if (tunnelings != null) tunnelings = tunnelings + "|";
            tunnelings = tunnelings + string.Format("{0}@{1}", tagOclock, textBlockClock.Text);
            if (tunnelingDescription != null) tunnelingDescription = tunnelingDescription + CR;
            tunnelingDescription = tunnelingDescription + string.Format("At {0} o'clock with depth of {1} cm", tagOclock, textBlockClock.Text);

        }
        private void SetTunnelingsFromPopupControls()
        {
            tunnelings = null;
            tunnelingDescription = null;
            if (textBoxClock12 != null) AppendTunneling(textBoxClock12);
            if (textBoxClock1 != null) AppendTunneling(textBoxClock1);
            if (textBoxClock2 != null) AppendTunneling(textBoxClock2);
            if (textBoxClock3 != null) AppendTunneling(textBoxClock3);
            if (textBoxClock4 != null) AppendTunneling(textBoxClock4);
            if (textBoxClock5 != null) AppendTunneling(textBoxClock5);
            if (textBoxClock6 != null) AppendTunneling(textBoxClock6);
            if (textBoxClock7 != null) AppendTunneling(textBoxClock7);
            if (textBoxClock8 != null) AppendTunneling(textBoxClock8);
            if (textBoxClock9 != null) AppendTunneling(textBoxClock9);
            if (textBoxClock10 != null) AppendTunneling(textBoxClock10);
            if (textBoxClock11 != null) AppendTunneling(textBoxClock11);

            if (controlTextBlock != null) controlTextBlock.Text = tunnelingDescription;
            Tunnelings = (tunnelings == null) ? "" : tunnelings;
            TunnelingDescription = (tunnelingDescription == null) ? "" : tunnelingDescription;
        }
        private void controlCloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.IsDropDownOpen = false;
        }
        private void codeLookupTunneling_DropDownClosed(object sender, EventArgs e)
        {
            SetTunnelingsFromPopupControls();
        }
        private void codeLookupTunneling_DropDownOpened(object sender, EventArgs e)
        {
            SetupPopupControlsFromTunnelings();
        }

        void textBoxClock_GotFocus(object sender, RoutedEventArgs e)
        {
            ClearEvents();
            Refocus(sender);
            SetTunnelingsFromPopupControls();
            SetEvents();
        }
        private void textBoxClock_KeyUp(object sender, KeyEventArgs e)
        {
            if (e == null) return;
            if (e.Key != Key.Enter) return;
            ClearEvents();
            Refocus(sender);
            SetTunnelingsFromPopupControls();
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

    }
}

