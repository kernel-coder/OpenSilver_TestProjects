#region Usings

using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;


#endregion

namespace Virtuoso.Core.Converters
{
    public class LogoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string uri_string = "/Virtuoso;component/Assets/Images/Crescendo_Icon-on-Side_White.png";
#if !OPENSILVER
            if (System.Windows.Application.Current.IsRunningOutOfBrowserOrOpenSilver() == false)
            {
                // NOTE: When SILVERLIGHT and running in browser for the install - no MEF.
                
                BitmapImage bi = new BitmapImage();
                StreamResourceInfo sr = System.Windows.Application.GetResourceStream(new Uri(uri_string, UriKind.Relative));
                bi.SetSource(sr.Stream);
                return bi;
            }
            else
#endif
            {
                //var appFeatures = VirtuosoContainer.Current.GetInstance<IAppFeatures>();
                //return appFeatures.CreateBitmapSource(uri_string);
                return null;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PhotoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                BitmapImage bi = new BitmapImage();
                MemoryStream ms = new MemoryStream((byte[])value);
                bi.SetSource(ms);

                return bi;
            }

            if (parameter != null)
            {
                //var appFeatures = VirtuosoContainer.Current.GetInstance<IAppFeatures>();

                //if (parameter.Equals("User")) //only use the default image on the login page not in user maint
                //{
                //    return appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/icon.png");
                //}

                //if (parameter.Equals("Print")) //The print library needs some kind of image so it doesn't hang.
                //{
                //    return appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/empty.png");
                //}

                return null;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PatientPhotoConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] != null)
            {
                BitmapImage bi = new BitmapImage();
                MemoryStream ms = new MemoryStream((byte[])values[0]);
                bi.SetSource(ms);
                return bi;
            }

            if (values[1] == null) //no patient and no gender
            {
                return null;
            }
            return null;

            //var appFeatures = VirtuosoContainer.Current.GetInstance<IAppFeatures>();

            ////use the default image for a male when a photo hasn't been loaded for the patient
            //if (values[1].ToString().StartsWith("M"))
            //{
            //    return appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/patient-male-generic.jpg");
            //}

            ////use the defualt image for a female when a photo hasn't been loaded for the patient
            //if (values[1].ToString().StartsWith("F"))
            //{
            //    return appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/patient-female-generic.jpg");
            //}
            //else
            //{
            //    return appFeatures.CreateBitmapSource("/Virtuoso;component/Assets/Images/icon.png");
            //}
        }

        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class PhotoMultiBinding : FrameworkElement
    {
        private bool _suppressWriteBack;

        public object Output
        {
            get { return GetValue(OutputProperty); }
            set { SetValue(OutputProperty, value); }
        }

        public static readonly DependencyProperty OutputProperty =
            DependencyProperty.Register("Output", typeof(object), typeof(PhotoMultiBinding),
                new PropertyMetadata(default(object), OutputChanged));

        private static void OutputChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            PhotoMultiBinding instance = (PhotoMultiBinding)source;
            instance.WriteBack();
        }

        public object Photo
        {
            get { return GetValue(PhotoProperty); }
            set { SetValue(PhotoProperty, value); }
        }

        public static readonly DependencyProperty PhotoProperty =
            DependencyProperty.Register("Photo", typeof(object), typeof(PhotoMultiBinding),
                new PropertyMetadata(default(object), PhotoChanged));

        private static void PhotoChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            PhotoMultiBinding instance = (PhotoMultiBinding)source;
            instance.UpdateOutput();
        }

        public object Gender
        {
            get { return GetValue(GenderProperty); }
            set { SetValue(GenderProperty, value); }
        }

        public static readonly DependencyProperty GenderProperty =
            DependencyProperty.Register("Gender", typeof(object), typeof(PhotoMultiBinding),
                new PropertyMetadata(default(object), GenderChanged));

        private static void GenderChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            PhotoMultiBinding instance = (PhotoMultiBinding)source;
            instance.UpdateOutput();
        }

        public IMultiValueConverter Converter
        {
            get { return (IMultiValueConverter)GetValue(ConverterProperty); }
            set { SetValue(ConverterProperty, value); }
        }

        public static readonly DependencyProperty ConverterProperty =
            DependencyProperty.Register("Converter", typeof(IMultiValueConverter), typeof(PhotoMultiBinding),
                new PropertyMetadata(default(IMultiValueConverter), ConverterChanged));

        private static void ConverterChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            PhotoMultiBinding instance = (PhotoMultiBinding)source;
            instance.UpdateOutput();
        }

        public object ConverterParameter
        {
            get { return GetValue(ConverterParameterProperty); }
            set { SetValue(ConverterParameterProperty, value); }
        }

        public static readonly DependencyProperty ConverterParameterProperty =
            DependencyProperty.Register("ConverterParameter", typeof(object), typeof(PhotoMultiBinding),
                new PropertyMetadata(default(object), ConverterParameterChanged));

        private static void ConverterParameterChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            PhotoMultiBinding instance = (PhotoMultiBinding)source;
            instance.UpdateOutput();
        }

        public CultureInfo ConverterCulture
        {
            get { return (CultureInfo)GetValue(ConverterCultureProperty); }
            set { SetValue(ConverterCultureProperty, value); }
        }

        public static readonly DependencyProperty ConverterCultureProperty =
            DependencyProperty.Register("ConverterCulture", typeof(CultureInfo), typeof(PhotoMultiBinding),
                new PropertyMetadata(CultureInfo.InvariantCulture, ConverterCultureChanged));

        private static void ConverterCultureChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
        {
            PhotoMultiBinding instance = (PhotoMultiBinding)source;
            instance.UpdateOutput();
        }

        private void UpdateOutput()
        {
            if (Converter == null)
            {
                throw new InvalidOperationException("The Converter property must be set and cannot be null");
            }

            using (SuppressWriteBack())
            {
                object[] values = new object[2];

                for (int i = 0; i < values.Length; i++) values[i] = GetInput(i);

                Output = Converter.Convert(values, typeof(object), ConverterParameter, CultureInfo.CurrentUICulture);
            }
        }

        private object GetInput(int i)
        {
            switch (i)
            {
                case 0:
                    return Photo;
                case 1:
                    return Gender;
                default:
                    throw new InvalidOperationException("Invalid Input# requested");
            }
        }

        private void SetInput(int i, object value)
        {
            switch (i)
            {
                case 0:
                    Photo = value;
                    break;
                case 1:
                    Gender = value;
                    break;
                default:
                    throw new InvalidOperationException("Invalid Input# requested");
            }
        }

        private void WriteBack()
        {
            if (_suppressWriteBack)
            {
                return;
            }

            if (Converter == null)
            {
                throw new InvalidOperationException("The Converter property must be set and cannot be null");
            }

            object[] inputs = Converter.ConvertBack(Output, null, ConverterParameter, CultureInfo.CurrentUICulture);

            for (int i = 0; i < inputs.Length; i++) SetInput(i, inputs[i]);
        }

        private IDisposable SuppressWriteBack()
        {
            _suppressWriteBack = true;
            return new Disposer(() => _suppressWriteBack = false);
        }
    }

    public interface IMultiValueConverter
    {
        object Convert(object[] values, Type targetType, object parameter, CultureInfo culture);
        object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture);
    }

    public class Disposer : IDisposable
    {
        private readonly Action _dispose;

        public Disposer(Action dispose)
        {
            _dispose = dispose;
        }

        public void Dispose()
        {
            _dispose();
        }
    }

    public class EditableImage
    {
        private int _width;
        private int _height;
        private bool _init;
        private byte[] _buffer;
        private int _rowLength;

        public event EventHandler<EditableImageErrorEventArgs> ImageError;

        public EditableImage(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width
        {
            get { return _width; }
            set
            {
                if (_init)
                {
                    OnImageError("Error: Cannot change Width after the EditableImage has been initialized");
                }
                else if ((value <= 0) || (value > 2047))
                {
                    OnImageError("Error: Width must be between 0 and 2047");
                }
                else
                {
                    _width = value;
                }
            }
        }

        public int Height
        {
            get { return _height; }
            set
            {
                if (_init)
                {
                    OnImageError("Error: Cannot change Height after the EditableImage has been initialized");
                }
                else if ((value <= 0) || (value > 2500))
                {
                    OnImageError("Error: Height must be between 0 and 2047");
                }
                else
                {
                    _height = value;
                }
            }
        }

        public void SetPixel(int col, int row, Color color)
        {
            SetPixel(col, row, color.R, color.G, color.B, color.A);
        }

        public void SetPixel(int col, int row, byte red, byte green, byte blue, byte alpha)
        {
            if (!_init)
            {
                _rowLength = _width * 4 + 1;
                _buffer = new byte[_rowLength * _height];

                // Initialize
                for (int idx = 0; idx < _height; idx++) _buffer[idx * _rowLength] = 0; // Filter bit

                _init = true;
            }

            if ((col > _width) || (col < 0))
            {
                OnImageError("Error: Column must be greater than 0 and less than the Width");
            }
            else if ((row > _height) || (row < 0))
            {
                OnImageError("Error: Row must be greater than 0 and less than the Height");
            }

            // Set the pixel
            int start = _rowLength * row + col * 4 + 1;
            _buffer[start] = red;
            _buffer[start + 1] = green;
            _buffer[start + 2] = blue;
            _buffer[start + 3] = alpha;
        }

        public Color GetPixel(int col, int row)
        {
            if ((col > _width) || (col < 0))
            {
                OnImageError("Error: Column must be greater than 0 and less than the Width");
            }
            else if ((row > _height) || (row < 0))
            {
                OnImageError("Error: Row must be greater than 0 and less than the Height");
            }

            Color color = new Color();
            int _base = _rowLength * row + col + 1;

            color.R = _buffer[_base];
            color.G = _buffer[_base + 1];
            color.B = _buffer[_base + 2];
            color.A = _buffer[_base + 3];

            return color;
        }

        public Stream GetStream()
        {
            Stream stream;

            if (!_init)
            {
                OnImageError("Error: Image has not been initialized");
                stream = null;
            }
            else
            {
                stream = PngEncoder.Encode(_buffer, _width, _height);
            }

            return stream;
        }

        private void OnImageError(string msg)
        {
            if (null != ImageError)
            {
                EditableImageErrorEventArgs args = new EditableImageErrorEventArgs();
                args.ErrorMessage = msg;
                ImageError(this, args);
            }
        }

        public class EditableImageErrorEventArgs : EventArgs
        {
            private string _errorMessage = string.Empty;

            public string ErrorMessage
            {
                get { return _errorMessage; }
                set { _errorMessage = value; }
            }
        }
    }

    public class PngEncoder
    {
        private const int _ADLER32_BASE = 65521;
        private const int _MAXBLOCK = 0xFFFF;
        private static byte[] _HEADER = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        private static byte[] _IHDR = { (byte)'I', (byte)'H', (byte)'D', (byte)'R' };
        private static byte[] _GAMA = { (byte)'g', (byte)'A', (byte)'M', (byte)'A' };
        private static byte[] _IDAT = { (byte)'I', (byte)'D', (byte)'A', (byte)'T' };
        private static byte[] _IEND = { (byte)'I', (byte)'E', (byte)'N', (byte)'D' };
        private static byte[] _4BYTEDATA = { 0, 0, 0, 0 };
        private static byte[] _ARGB = { 0, 0, 0, 0, 0, 0, 0, 0, 8, 6, 0, 0, 0 };


        public static Stream Encode(byte[] data, int width, int height)
        {
            MemoryStream ms = new MemoryStream();
            byte[] size;

            // Write PNG header
            ms.Write(_HEADER, 0, _HEADER.Length);

            // Write IHDR
            //  Width:              4 bytes
            //  Height:             4 bytes
            //  Bit depth:          1 byte
            //  Color type:         1 byte
            //  Compression method: 1 byte
            //  Filter method:      1 byte
            //  Interlace method:   1 byte

            size = BitConverter.GetBytes(width);
            _ARGB[0] = size[3];
            _ARGB[1] = size[2];
            _ARGB[2] = size[1];
            _ARGB[3] = size[0];

            size = BitConverter.GetBytes(height);
            _ARGB[4] = size[3];
            _ARGB[5] = size[2];
            _ARGB[6] = size[1];
            _ARGB[7] = size[0];

            // Write IHDR chunk
            WriteChunk(ms, _IHDR, _ARGB);

            // Set gamma = 1
            size = BitConverter.GetBytes(1 * 100000);
            _4BYTEDATA[0] = size[3];
            _4BYTEDATA[1] = size[2];
            _4BYTEDATA[2] = size[1];
            _4BYTEDATA[3] = size[0];

            // Write gAMA chunk
            WriteChunk(ms, _GAMA, _4BYTEDATA);

            // Write IDAT chunk
            uint widthLength = (uint)(width * 4) + 1;
            uint dcSize = widthLength * (uint)height;

            // First part of ZLIB header is 78 1101 1010 (DA) 0000 00001 (01)
            // ZLIB info
            //
            // CMF Byte: 78
            //  CINFO = 7 (32K window size)
            //  CM = 8 = (deflate compression)
            // FLG Byte: DA
            //  FLEVEL = 3 (bits 6 and 7 - ignored but signifies max compression)
            //  FDICT = 0 (bit 5, 0 - no preset dictionary)
            //  FCHCK = 26 (bits 0-4 - ensure CMF*256+FLG / 31 has no remainder)
            // Compressed data
            //  FLAGS: 0 or 1
            //    00000 00 (no compression) X (X=1 for last block, 0=not the last block)
            //    LEN = length in bytes (equal to ((width*4)+1)*height
            //    NLEN = one's compliment of LEN
            //    Example: 1111 1011 1111 1111 (FB), 0000 0100 0000 0000 (40)
            //    Data for each line: 0 [RGBA] [RGBA] [RGBA] ...
            //    ADLER32

            uint adler = ComputeAdler32(data);
            MemoryStream comp = new MemoryStream();

            // Calculate number of 64K blocks
            uint rowsPerBlock = _MAXBLOCK / widthLength;
            uint blockSize = rowsPerBlock * widthLength;
            uint blockCount;
            ushort length;
            uint remainder = dcSize;

            if ((dcSize % blockSize) == 0)
            {
                blockCount = dcSize / blockSize;
            }
            else
            {
                blockCount = (dcSize / blockSize) + 1;
            }

            // Write headers
            comp.WriteByte(0x78);
            comp.WriteByte(0xDA);

            for (uint blocks = 0; blocks < blockCount; blocks++)
            {
                // Write LEN
                length = (ushort)((remainder < blockSize) ? remainder : blockSize);

                if (length == remainder)
                {
                    comp.WriteByte(0x01);
                }
                else
                {
                    comp.WriteByte(0x00);
                }

                comp.Write(BitConverter.GetBytes(length), 0, 2);

                // Write one's compliment of LEN
                comp.Write(BitConverter.GetBytes((ushort)~length), 0, 2);

                // Write blocks
                comp.Write(data, (int)(blocks * blockSize), length);

                // Next block
                remainder -= blockSize;
            }

            WriteReversedBuffer(comp, BitConverter.GetBytes(adler));
            comp.Seek(0, SeekOrigin.Begin);

            byte[] dat = new byte[comp.Length];
            comp.Read(dat, 0, (int)comp.Length);

            WriteChunk(ms, _IDAT, dat);

            // Write IEND chunk
            WriteChunk(ms, _IEND, new byte[0]);

            // Reset stream
            ms.Seek(0, SeekOrigin.Begin);

            return ms;

            // See http://www.libpng.org/pub/png//spec/1.2/PNG-Chunks.html
            // See http://www.libpng.org/pub/png/book/chapter08.html#png.ch08.div.4
            // See http://www.gzip.org/zlib/rfc-zlib.html (ZLIB format)
            // See ftp://ftp.uu.net/pub/archiving/zip/doc/rfc1951.txt (ZLIB compression format)
        }

        private static void WriteReversedBuffer(Stream stream, byte[] data)
        {
            int size = data.Length;
            byte[] reorder = new byte[size];

            for (int idx = 0; idx < size; idx++) reorder[idx] = data[size - idx - 1];
            stream.Write(reorder, 0, size);
        }

        private static void WriteChunk(Stream stream, byte[] type, byte[] data)
        {
            int idx;
            int size = type.Length;
            byte[] buffer = new byte[type.Length + data.Length];

            // Initialize buffer
            for (idx = 0; idx < type.Length; idx++) buffer[idx] = type[idx];

            for (idx = 0; idx < data.Length; idx++) buffer[idx + size] = data[idx];

            // Write length
            WriteReversedBuffer(stream, BitConverter.GetBytes(data.Length));

            // Write type and data
            stream.Write(buffer, 0, buffer.Length); // Should always be 4 bytes

            // Compute and write the CRC
            WriteReversedBuffer(stream, BitConverter.GetBytes(GetCRC(buffer)));
        }

        private static uint[] _crcTable = new uint[256];
        private static bool _crcTableComputed;

        private static void MakeCRCTable()
        {
            uint c;

            for (int n = 0; n < 256; n++)
            {
                c = (uint)n;
                for (int k = 0; k < 8; k++)
                    if ((c & (0x00000001)) > 0)
                    {
                        c = 0xEDB88320 ^ (c >> 1);
                    }
                    else
                    {
                        c = c >> 1;
                    }

                _crcTable[n] = c;
            }

            _crcTableComputed = true;
        }

        private static uint UpdateCRC(uint crc, byte[] buf, int len)
        {
            uint c = crc;

            if (!_crcTableComputed)
            {
                MakeCRCTable();
            }

            for (int n = 0; n < len; n++) c = _crcTable[(c ^ buf[n]) & 0xFF] ^ (c >> 8);

            return c;
        }

        /* Return the CRC of the bytes buf[0..len-1]. */
        private static uint GetCRC(byte[] buf)
        {
            return UpdateCRC(0xFFFFFFFF, buf, buf.Length) ^ 0xFFFFFFFF;
        }

        private static uint ComputeAdler32(byte[] buf)
        {
            uint s1 = 1;
            uint s2 = 0;
            int length = buf.Length;

            for (int idx = 0; idx < length; idx++)
            {
                s1 = (s1 + buf[idx]) % _ADLER32_BASE;
                s2 = (s2 + s1) % _ADLER32_BASE;
            }

            return (s2 << 16) + s1;
        }
    }
}