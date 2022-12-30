using System;
using System.Collections;
using System.Globalization;
using System.Windows.Data;

namespace Open3DViewer.Gui.Converter
{
    internal class DoesCollectionContainItemsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ICollection collection))
            {
                return false;
            }

            return collection.Count > 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
