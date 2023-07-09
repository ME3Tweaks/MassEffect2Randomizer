using System.Windows.Media;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace RandomizerUI.Classes
{

    public static class StringExtensions
    {
        public static SolidColorBrush ToBrush(this string hexColorString)
        {
            return (SolidColorBrush) (new BrushConverter().ConvertFrom(hexColorString));
        }
    }
}
