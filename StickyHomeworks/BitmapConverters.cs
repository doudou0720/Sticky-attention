using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ClassIsland;

/// <summary>
/// Source: https://www.cnblogs.com/kybs0/p/15768990.html
/// </summary>
public class BitmapConveters
{
    [DllImport("gdi32")]
    static extern int DeleteObject(IntPtr o);
    
    public static BitmapSource ConvertToBitMapSource(Bitmap bitmap)
    {
        IntPtr hBitmap = bitmap.GetHbitmap();
        try
        {
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            bitmapSource.Freeze(); // 冻结以提高性能并允许跨线程访问
            return bitmapSource;
        }
        finally
        {
            DeleteObject(hBitmap); // 确保始终释放GDI对象
        }
    }
    
    public static BitmapImage ConvertToBitmapImage(Bitmap bitmap, int? w = null, int? h = null)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png);
            stream.Position = 0;
            BitmapImage result = new BitmapImage();
            result.BeginInit();
            result.CacheOption = BitmapCacheOption.OnLoad; // 立即加载数据，这样可以释放源流
            if (h != null && h <= bitmap.Height)
            {
                result.DecodePixelWidth = (int)((double)bitmap.Width / (double)bitmap.Height * (double)h);
                result.DecodePixelHeight = h.Value;
            }
            else if (w != null && w <= bitmap.Width)
            {
                result.DecodePixelHeight = (int)((double)bitmap.Height / (double)bitmap.Width * (double)w);
                result.DecodePixelWidth = w.Value;
            }
            else
            {
                result.DecodePixelWidth = bitmap.Width;
                result.DecodePixelHeight = bitmap.Height;
            }
            result.StreamSource = stream;
            result.EndInit();
            result.Freeze(); // 冻结以提高性能并允许跨线程访问
            return result;
        }
    }
}