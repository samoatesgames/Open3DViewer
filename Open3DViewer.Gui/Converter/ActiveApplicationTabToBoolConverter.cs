using System;
using System.Globalization;
using System.Windows.Data;
using Open3DViewer.Gui.ViewModel;

namespace Open3DViewer.Gui.Converter
{
    public class ActiveApplicationTabToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is ApplicationTabs currentTab))
            {
                return false;
            }

            if (!(parameter is string stringParameter))
            {
                return false;
            }

            if (!Enum.TryParse(stringParameter, true, out ApplicationTabs applicationTab))
            {
                return false;
            }

            return currentTab == applicationTab;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
