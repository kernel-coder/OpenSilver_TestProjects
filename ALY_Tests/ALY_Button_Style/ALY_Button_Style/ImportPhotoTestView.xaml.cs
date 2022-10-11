using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Virtuoso.Client.Utils;

namespace ALY_Button_Style
{
    public partial class ImportPhotoTestView : UserControl
    {
        public ImportPhotoTestView()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //var stream = GetType().Assembly.GetManifestResourceStream("/ALY_Button_Style;component/Assets/Images/test.png");
            //{
            //    long numBytes = stream.Length;
            //    imgView.Source = await ImageToolsUtility.CreateThumbnailImage(stream, 360 * 2, numBytes);
            //    GC.Collect();
            //}

            var ofd = new FileDialogs.OpenFileDialog();
            ofd.Multiselect = false;
            // SelectedItem could be NULL if adding new 1st Wound to Encounter/Admission
            //INFO: Only allow JPG and PNG - DO NOT allow GIF, Silverlight does not support GIF.
            //      Using GIF bytes will GPF the application when used as 'source' to image controls.
            ofd.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png|JPEG file|*.jpg|PNG file|*.png";
            ofd.ResultKind = FileDialogs.ResultKind.DataURL;
            if (await ofd.ShowDialog() == true)
            {
                var ff = ofd.File;
                if (ff != null)
                {
                    using (Stream stream = new MemoryStream(ff.Buffer))
                    {
                        long numBytes = ff.Buffer.Length;
                        imgView.Source = await ImageToolsUtility.CreateThumbnailImage(stream, 360 * 2, numBytes);
                        GC.Collect();
                    }
                }
            }

            //var ofd = new OpenSilver.Controls.OpenFileDialog();
            //ofd.Multiselect = false;
            //// SelectedItem could be NULL if adding new 1st Wound to Encounter/Admission
            ////INFO: Only allow JPG and PNG - DO NOT allow GIF, Silverlight does not support GIF.
            ////      Using GIF bytes will GPF the application when used as 'source' to image controls.
            //ofd.Filter = "Image files (*.jpg;*.png)|*.jpg;*.png|JPEG file|*.jpg|PNG file|*.png";
            ////ofd.ResultKind = FileDialogs.ResultKind.DataURL;
            //if (await ofd.ShowDialogAsync() == true)
            //{
            //    var ff = ofd.File;
            //    if (ff != null)
            //    {
            //        using (Stream strm = ofd.File.OpenRead())
            //        {
            //            byte[] buffer = new byte[strm.Length];
            //            strm.Read(buffer, 0, (int)strm.Length);

            //            imgView.Source = await ImageToolsUtility.CreateThumbnailImage(strm, 360 * 2, strm.Length);

            //        }
            //    }
            //}
        }
    }
}
