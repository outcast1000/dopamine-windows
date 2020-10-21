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
    public class KeyToPathConverter : IValueConverter
    {
        private IFileStorage fileStorage = new FileStorage();
        public KeyToPathConverter()
        {
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || !(object.ReferenceEquals(value.GetType(), typeof(string)) || object.ReferenceEquals(value.GetType(), typeof(Byte[]))))
            {
                return parameter;
            }
            if (object.ReferenceEquals(value.GetType(), typeof(Byte[])))
                return value;
            return fileStorage.GetRealPath(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
