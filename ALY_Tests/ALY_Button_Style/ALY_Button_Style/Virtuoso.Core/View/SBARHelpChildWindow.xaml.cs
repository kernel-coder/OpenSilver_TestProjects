using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Virtuoso.Core.View
{
    public partial class SBARHelpChildWindow : ChildWindow
    {
        public SBARHelpChildWindow()
        {
            InitializeComponent();
        }

        public SBARHelpChildWindow(string letter)
        {
            InitializeComponent();
            Border b = null;
            if (letter == "S") b = borderS;
            else if (letter == "B") b = borderB;
            else if (letter == "A") b = borderA;
            else if (letter == "R") b = borderR;
            if (b != null)
            {
                b.BorderThickness = new Thickness(4);
                try
                {
                    b.BorderBrush = (Brush)System.Windows.Application.Current.Resources["HighlightBrush"];
                }
                catch
                {
                }
            }
        }
    }
}