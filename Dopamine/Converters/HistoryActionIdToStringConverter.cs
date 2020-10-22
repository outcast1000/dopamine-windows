using Digimezzo.Foundation.Core.Utils;
using Dopamine.Data.Entities;
using Dopamine.Data.Repositories;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Dopamine.Converters
{
    public class HistoryActionIdToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((HistoryActionType)value)
            {
                case HistoryActionType.Executed:
                    return ResourceUtils.GetString("Language_Executed");
                case HistoryActionType.Played:
                    return ResourceUtils.GetString("Language_Played");
                case HistoryActionType.Skipped:
                    return ResourceUtils.GetString("Language_Skipped");
                case HistoryActionType.Loved:
                    return ResourceUtils.GetString("Language_Loved");
                case HistoryActionType.Rated:
                    return ResourceUtils.GetString("Language_Rated");
                default:
                    Debug.Assert(false, "Out of bounds - Should not happen");
                    break;
            }
            return String.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
