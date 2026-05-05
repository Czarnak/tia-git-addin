using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TiaGitAddIn.Models.Sact;

namespace TiaGitAddIn.UI.Converters
{
    public class CompareStateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CompareState state)
            {
                switch (state)
                {
                    case CompareState.Changed:
                        return new SolidColorBrush(Color.FromRgb(255, 255, 200)); // light yellow
                    case CompareState.MissingOnLeft:
                        return new SolidColorBrush(Color.FromRgb(200, 255, 200)); // light green
                    case CompareState.MissingOnRight:
                        return new SolidColorBrush(Color.FromRgb(255, 200, 200)); // light red
                    case CompareState.Equal:
                    default:
                        return Brushes.Transparent;
                }
            }
            
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}