using System;
using System.Globalization;
using System.Windows.Data;

namespace Open3DViewer.Gui.Converter
{
    internal class BoolToInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolValue))
            {
                return false;
            }
            return !boolValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
