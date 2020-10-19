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
            switch ((HistoryActionEnum)value)
            {
                case HistoryActionEnum.Executed:
                    return "\uE8B0";
                case HistoryActionEnum.Played:
                    return "\uE768";
                case HistoryActionEnum.Skipped:
                    return "\uE711";
                case HistoryActionEnum.Loved:
                    return "\uEB51";
                case HistoryActionEnum.Rated:
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
