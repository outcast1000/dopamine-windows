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
    public class MultiplyConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double minWidth = (double)parameter;
            double maxWidth = (double)values[0];
            double multiplier = (double)values[1];

            double res = minWidth + multiplier * (maxWidth - minWidth);
            return res > 0 ? res : 0;

            //return (double) parameter + ((double) values[0] - (double) parameter) * (double) values[1];
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new Exception("Not implemented");
        }
    }
}
