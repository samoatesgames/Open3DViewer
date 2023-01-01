using Open3DViewer.Gui.ViewModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Open3DViewer.Gui.Converter
{
    public class ActiveApplicationTabToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ApplicationTabs currentTab))
            {
                return Visibility.Hidden;
            }

            if (!(parameter is string stringParameter))
            {
                return Visibility.Hidden;
            }

            if (!Enum.TryParse(stringParameter, true, out ApplicationTabs applicationTab))
            {
                return Visibility.Hidden;
            }

            return currentTab == applicationTab
                ? Visibility.Visible
                : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
