using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ME2Randomizer.Classes.Converters
{
    [Localizable(false)]
    public class ImplementedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RandomizationOption ro)
                return (ro.PerformRandomizationOnExportDelegate == null && ro.PerformSpecificRandomizationDelegate == null) ? Brushes.Red : Brushes.Transparent;
            return Brushes.Maroon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false; //don't need this
        }
    }
}