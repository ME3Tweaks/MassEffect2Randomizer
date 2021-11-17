using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Randomizer.Randomizers;

namespace RandomizerUI.Classes.Converters
{
    [Localizable(false)]
    public class ImplementedConverter : IValueConverter
    {
        private static SolidColorBrush SelectedBrush = new SolidColorBrush(Color.FromArgb(128, 0, 192, 0));
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RandomizationOption ro)
            {
                if (ro.OptionIsSelected)
                {
                    return SelectedBrush;
                }

                return (!ro.IsOptionOnly && ro.PerformRandomizationOnExportDelegate == null && ro.PerformSpecificRandomizationDelegate == null && ro.PerformFileSpecificRandomization == null) ? Brushes.Red : Brushes.Transparent;
            }
            return Brushes.Maroon;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false; //don't need this
        }
    }
}