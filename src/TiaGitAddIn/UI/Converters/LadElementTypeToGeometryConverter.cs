using System;
using System.Globalization;
using System.Windows.Data;
using TiaGitAddIn.Models.Lad;

namespace TiaGitAddIn.UI.Converters
{
    public class LadElementTypeToGeometryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LadElementType elementType)
            {
                switch (elementType)
                {
                    case LadElementType.Powerrail:
                        return "||";
                    case LadElementType.Contact:
                        return "--| |--";
                    case LadElementType.NegatedContact:
                        return "--|/|--";
                    case LadElementType.PEdgeContact:
                        return "--|P|--";
                    case LadElementType.NEdgeContact:
                        return "--|N|--";
                    case LadElementType.Coil:
                        return "--( )--";
                    case LadElementType.NegatedCoil:
                        return "--(/)--";
                    case LadElementType.ComparatorBox:
                        return "[CMP]";
                    case LadElementType.OrBranch:
                        return "+";
                    case LadElementType.TemplatedContact:
                        return "--|T|--";
                    case LadElementType.TemplatedCoil:
                        return "--(T)--";
                    default:
                        return string.Empty;
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}