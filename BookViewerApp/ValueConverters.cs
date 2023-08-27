using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Data;
using System.Diagnostics;

namespace BookViewerApp.ValueConverters
{
    public class RateToPersantageValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (double)value * 100;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (double)value / 100;
        }
    }

    public class BookIdToImageSource : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Windows.UI.Xaml.Media.Imaging.BitmapImage result 
                = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
            try
            {
                if (value != null)
                {
                    ThumbnailManager.SetToImageSourceNoWait(value.ToString(), result);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(
                    "[ex] ThumbnailManager.SetToImageSourceNoWait exception: "
                    + ex.Message);
            }
            
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
            // this is impossible.
        }
    }

    public class BoolToDoubleValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool && targetType == typeof(double))
            {
                return (bool)value ? -1 : 1;
            }
            else if (targetType == typeof(double))
            {
                return 1;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is double && targetType == typeof(bool))
            {
                if ((double)value == 1.0) return false;
                else if ((double)value == -1.0) return true;
            }
            return false;
        }
    }
}
