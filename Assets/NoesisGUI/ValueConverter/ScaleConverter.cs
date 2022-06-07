using System;
#if UNITY_5_3_OR_NEWER
using Noesis;
using UnityEngine;
#else
using System.Windows.Data;
#endif

namespace DemoCollection
{
	public class ScaleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var baseValue = System.Convert.ToInt32(value);
            var scale = System.Convert.ToInt32(parameter);

            return baseValue * scale;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
