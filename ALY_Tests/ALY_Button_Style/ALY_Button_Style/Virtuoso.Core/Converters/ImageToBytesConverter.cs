#region Usings

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

#endregion

namespace Virtuoso.Core.Converters
{
    public class ImageToBytesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return value;
            }

            WriteableBitmap bitmap = new WriteableBitmap(value as BitmapSource);
            int[] p = bitmap.Pixels;
            int len = p.Length << 2;
            byte[] result = new byte[len];
            Buffer.BlockCopy(p, 0, result, 0, len);
            return result;
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}