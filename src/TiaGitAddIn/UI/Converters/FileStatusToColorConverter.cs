using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using TiaGitAddIn.Models;

namespace TiaGitAddIn.UI.Converters
{
    public sealed class FileStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is FileStatus status)) return Brushes.Transparent;

            switch (status)
            {
                case FileStatus.Modified: return Brushes.RoyalBlue;
                case FileStatus.Added: return Brushes.ForestGreen;
                case FileStatus.Deleted: return Brushes.Firebrick;
                case FileStatus.Renamed: return Brushes.DarkOrange;
                case FileStatus.Copied: return Brushes.DeepPink;
                case FileStatus.Untracked: return Brushes.Gray;
                case FileStatus.Conflicted: return Brushes.Red;
                case FileStatus.Ignored: return Brushes.Silver;
                default: return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
