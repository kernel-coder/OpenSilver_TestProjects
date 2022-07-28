using ALY_Button_Style.Shared;
using System;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Virtuoso.Core.Converters;

namespace ALY_Button_Style
{

    public partial class MainPage : UserControl
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = new TimePickerViewModel();
        }


        private Stroke LastStroke;

        //A new stroke object named MyStroke is created. MyStroke is added to the StrokeCollection of the InkPresenter named MyIP
        private void OnIP_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            InkPad.CaptureMouse();
            StylusPointCollection MyStylusPointCollection = new StylusPointCollection();
#if OPENSILVER
            var point = e.GetPosition(InkPad);
            MyStylusPointCollection.Add(new StylusPoint() { X = point.X, Y = point.Y });
#else
                    MyStylusPointCollection.Add(e.StylusDevice.GetStylusPoints(MyIP));
#endif
            LastStroke = new Stroke(MyStylusPointCollection);
            InkPad.Strokes.Add(LastStroke);
        }

        //StylusPoint objects are collected from the MouseEventArgs and added to MyStroke. 
        private void OnIP_MouseMove(object sender, MouseEventArgs e)
        {
            if (LastStroke != null && InkPad.IsMouseCaptured)
            {
#if OPENSILVER
                var point = e.GetPosition(InkPad);
                LastStroke.StylusPoints.Add(new StylusPoint() { X = point.X, Y = point.Y });
#else
                        NewStroke.StylusPoints.Add(e.StylusDevice.GetStylusPoints(MyIP));
#endif
            }

        }

        //MyStroke is completed
        private async void OnIP_LostMouseCapture(object sender, MouseEventArgs e)
        {
            //WriteableBitmap wbBitmap = new WriteableBitmap(imageView.Source as BitmapImage);
            WriteableBitmap wbBitmap = new WriteableBitmap(InkPad, null);
            //await wbBitmap.WaitToInitialize();
            //EditableImage eiImage = new EditableImage(wbBitmap.PixelWidth, wbBitmap.PixelHeight);

            try
            {
                //for (int y = 0; y < wbBitmap.PixelHeight; ++y)
                //{
                //    for (int x = 0; x < wbBitmap.PixelWidth; ++x)
                //    {
                //        int pixel = wbBitmap.Pixels[wbBitmap.PixelWidth * y + x];
                //        var rgba = BitConverter.GetBytes(pixel);
                //        rgba[0] = (byte)((pixel >> 16) & 0xFF);
                //        rgba[1] = (byte)((pixel >> 8) & 0xFF);
                //        rgba[2] = (byte)((pixel >> 0) & 0xFF);
                //        rgba[3] = (byte)((pixel >> 24) & 0xFF);
                //        wbBitmap.Pixels[wbBitmap.PixelWidth * y + x] = BitConverter.ToInt32(rgba, 0);
                //        //eiImage.SetPixel(x, y,
                //        //(byte)((pixel >> 16) & 0xFF),
                //        //(byte)((pixel >> 8) & 0xFF),
                //        //(byte)(pixel & 0xFF), (byte)((pixel >> 24) & 0xFF)
                //        //);
                //    }
                //}
            }
            catch (System.Security.SecurityException)
            {
                throw new Exception("Cannot print images from other domains");
            }

            wbBitmap.Render(imageView, null);
            //await wbBitmap.WaitToRender();

            wbBitmap.Invalidate();
            //var bitmap = new BitmapImage();
            //bitmap.SetSource(eiImage.GetStream());
            imgView.Source = wbBitmap;
        }

        private void OnClearInkPad(object sender, System.Windows.RoutedEventArgs e)
        {
            LastStroke = null;
            InkPad.Strokes.Clear();
        }

        private void OnUndoLastStroke(object sender, System.Windows.RoutedEventArgs e)
        {
            if (InkPad.Strokes.Contains(LastStroke))
            {
                InkPad.Strokes.Remove(LastStroke);
            }
        }

        private void OnRedoLastStroke(object sender, System.Windows.RoutedEventArgs e)
        {
            if (!InkPad.Strokes.Contains(LastStroke))
            {
                InkPad.Strokes.Add(LastStroke);
            }
        }
    }

    public class TimePickerViewModel : NotifyPropertyChanged
    {
        private DateTimeOffset _selectedDate;

        public DateTimeOffset SelectedDate
        {
            get { return _selectedDate; }
            set { Set(ref _selectedDate, value); }
        }        
    }
}
