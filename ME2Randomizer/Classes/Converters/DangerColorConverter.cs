using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Brushes = System.Drawing.Brushes;

namespace ME2Randomizer.Classes.Converters
{
    [Localizable(false)]
    public class DangerColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is RandomizationOption.EOptionDangerousness danger)
            {
                switch (danger)
                {
                    case RandomizationOption.EOptionDangerousness.Danger_Safe:
                        return "#a6ffbe".ToBrush();
                    case RandomizationOption.EOptionDangerousness.Danger_Warning:
                        return "#FFf41f".ToBrush();
                    case RandomizationOption.EOptionDangerousness.Danger_Unsafe:
                        return "#ff871f".ToBrush();
                    case RandomizationOption.EOptionDangerousness.Danger_RIP:
                        return "#FF1f1f".ToBrush();
                    case RandomizationOption.EOptionDangerousness.Danger_Normal:
                        return "#FFFFFF".ToBrush();
                    default:
                        return Brushes.White;
                }
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return false; //don't need this
        }
    }
}
