using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Globalization; // Для CultureInfo
using System.IO; // Для MemoryStream
using System.Windows.Data; // Для IValueConverter
using System.Windows.Media.Imaging; // Для BitmapImage, BitmapCacheOption

namespace AptekaIS.Converters
{
    public class ByteArrayToImageConverters : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte[] bytes && bytes.Length > 0)
            {
                using (var stream = new MemoryStream(bytes))
                {
                    var image = new BitmapImage();

                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();

                    image.Freeze();

                    return image;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
