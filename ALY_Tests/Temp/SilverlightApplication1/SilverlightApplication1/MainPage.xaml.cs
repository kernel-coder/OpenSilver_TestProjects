using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Controls.DataVisualization.Charting.Primitives;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace SilverlightApplication1
{
    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            InitializeComponent();
            //Line
            //Polyline
            // DataPointSeriesDragDropTarget
     
        }


        Stroke NewStroke;

        //A new stroke object named MyStroke is created. MyStroke is added to the StrokeCollection of the InkPresenter named MyIP
        private void MyIP_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            MyIP.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
            MyStylusPointCollection.Add(e.StylusDevice.GetStylusPoints(MyIP));
            NewStroke = new Stroke(MyStylusPointCollection);
            MyIP.Strokes.Add(NewStroke);
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void MyIP_MouseMove(object sender, MouseEventArgs e)
        {
            if (NewStroke != null)
                NewStroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(MyIP));
        }

        //MyStroke is completed
        private void MyIP_LostMouseCapture(object sender, MouseEventArgs e)
        {
            NewStroke = null;

        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            var dlg = new ChildWindow1();
            dlg.Show();
        }

        private void ButtonBase_OnClick2(object sender, RoutedEventArgs e)
        {
            var dlg = new ChildWindow2();
            dlg.Show();
        }
    }
}
