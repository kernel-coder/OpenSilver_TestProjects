#region Usings

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Windows.Media.Imaging;

#endregion

namespace Virtuoso.Core.Converters
{
    public class FileUploadConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string key = (string)parameter;

            if (key == "Override")
            {
                return value;
            }

            if (value != null)
            {
                BitmapImage img = new BitmapImage();
                using (MemoryStream memStream = new MemoryStream(value as byte[]))
                {
                    img.SetSource(memStream);
                }

                return img;
            }

            return value;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                byte[] data12 = Encoding.Unicode.GetBytes(value.ToString());

                MemoryStream ms1 = new MemoryStream(data12);

                BinaryReader binaryReader12 = new BinaryReader(ms1);
                byte[] currentImageInBytes12 = binaryReader12.ReadBytes((int)ms1.Length);

                return currentImageInBytes12;
            }

            return value;
        }
    }
}