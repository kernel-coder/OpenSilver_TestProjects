﻿using System;
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

namespace Virtuoso.Core.Controls
{
    public partial class MedicationAdministrationListPopup : UserControl
    {
        public MedicationAdministrationListPopup()
        {
            InitializeComponent();
        }

        void UserControl_ItemSelected(object sender, EventArgs e)
        {
            this.DetailAreaScrollViewer.ScrollToVerticalOffset(0);
        }
    }
}