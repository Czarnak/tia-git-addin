using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TiaGitAddIn.Models;

namespace TiaGitAddIn.UI.Converters
{
    public sealed class DiffLineTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is DiffLineType type)) return Brushes.Transparent;

            switch (type)
            {
                case DiffLineType.Added: return new SolidColorBrush(Color.FromRgb(230, 255, 230)); // Light Green
                case DiffLineType.Deleted: return new SolidColorBrush(Color.FromRgb(255, 230, 230)); // Light Red
                case DiffLineType.Header: return new SolidColorBrush(Color.FromRgb(240, 240, 255)); // Light Blue
                case DiffLineType.Context:
                default: return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
