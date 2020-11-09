using Dopamine.Data;
using Dopamine.Services.Cache;
using DryIoc;
using Prism.Ioc;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Dopamine.Converters
{
    public class TwoDatesToDateRangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return false;
            string d1 = Date2String((DateTime)values[0]);
            string d2 = Date2String((DateTime)values[0]);
            if (d1.Equals(d2))
                return d1;
            return String.Format($"{d1} - {d2}");
        }

        private string Date2String(DateTime date)
        {
            if (DateTime.Now.Subtract(date).TotalDays < 365)
                return date.ToString("MMM. yyy");
            else
                return date.ToString("yyy");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TwoYearsToYearRangeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 2)
                return false;
            if (values[0] == null || values[1] == null)
                return false;
            long y1 = (long)values[0];
            long y2 = (long)values[1];
            if (y1 == y2)
                return y1.ToString();
            return String.Format($"{y1} - {y2}");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
