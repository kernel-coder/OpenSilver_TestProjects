using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Virtuoso.Client.Utils
{
    public class ImageToolsUtility
    {
#if OPENSILVER
        public static async System.Threading.Tasks.Task<WriteableBitmap> CreateThumbnailImage(Stream stream, int width, long file_length)
#else
        public static byte[] CreateThumbnailImage(Stream stream, int width, long file_length)
#endif
        {
            //INFO: original CreateThumbnailImage source = http://www.wintellect.com/devcenter/jprosise/silverlight-s-big-image-problem-and-what-you-can-do-about-it
            //      modified to encode as PNG and return original if reduced image is bigger than original
            BitmapImage bi = new BitmapImage();
            bi.SetSource(stream);

            WriteableBitmap wb0 = new WriteableBitmap(bi); //save in case reduced image is bigger than original
#if OPENSILVER
            await wb0.WaitToInitialize();
#endif

            double cx = width;
            double cy = wb0.PixelHeight * (cx / wb0.PixelWidth);

            Image image = new Image();
            image.Source = bi;

            WriteableBitmap wb1 = new WriteableBitmap((int)cx, (int)cy);
            ScaleTransform transform = new ScaleTransform();
            transform.ScaleX = cx / wb0.PixelWidth;
            transform.ScaleY = cy / wb0.PixelHeight;
            wb1.Render(image, transform);
#if OPENSILVER
            await wb1.WaitToRender();
#endif
            wb1.Invalidate();

            WriteableBitmap wb2 = new WriteableBitmap((int)cx, (int)cy);
            for (int i = 0; i < wb2.Pixels.Length; i++)
                wb2.Pixels[i] = wb1.Pixels[i];
            wb2.Invalidate();

            //using (var str = new MemoryStream())
            //{
            //    var encoder = new ImageTools.IO.Png.PngEncoder();
            //    var img = ImageTools.ImageExtensions.ToImage(wb2);
            //    encoder.Encode(img, str);

            //    var ret = str.ToArray();
            //    var str_length = ret.Length;
            //    var orig_length = 4 * wb0.PixelWidth * wb0.PixelHeight;
            //    if (str_length < orig_length)
            //        return ret;
            //    else
            //    {
            //        str.Seek(0, SeekOrigin.Begin);
            //        img = ImageTools.ImageExtensions.ToImage(wb0);
            //        encoder.Encode(img, str);
            //        return str.ToArray();
            //    }
            //}
            return wb2;
        }
    }
}
