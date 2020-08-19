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
        private ICacheService cacheService;
        public KeyToPathConverter()
        {
            this.cacheService = ((Dopamine.App)Application.Current).Container.Resolve<ICacheService>();
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || (!object.ReferenceEquals(value.GetType(), typeof(string))))
            {
                return Binding.DoNothing;
            }

            return cacheService.GetCachedArtworkPath(value.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
