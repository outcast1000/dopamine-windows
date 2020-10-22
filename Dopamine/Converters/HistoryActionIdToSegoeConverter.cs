using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Dopamine.Converters
{
    public class HistoryActionIdToSegoeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            switch ((HistoryActionType)value)
            {
                case HistoryActionType.Executed:
                    return "\uE8B0";
                case HistoryActionType.Played:
                    return "\uE768";
                case HistoryActionType.Skipped:
                    return "\uE711";
                case HistoryActionType.Loved:
                    return "\uEB51";
                case HistoryActionType.Rated:
                    return "\uF0E8";
                default:
                    Debug.Assert(false, "Out of bounds - Should not happen");
                    break;
            }
            return "\uE7BA";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
