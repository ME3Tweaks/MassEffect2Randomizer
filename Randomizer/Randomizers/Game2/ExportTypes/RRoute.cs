using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game2.ExportTypes
{
    class RRoute
    {
        public static bool RandomizeExport(ExportEntry exp,RandomizationOption option)
        {
            if (!CanRandomize(exp)) return false;
            var props = exp.GetProperties();

            var navs = props.GetProp<ArrayProperty<StructProperty>>("NavList").Select(x=>x.Properties.GetProp<ObjectProperty>("Nav")).ToList();

            var destNavs = exp.FileRef.Exports.Where(x => x.IsA("NavigationPoint")).ToList();

            destNavs.Shuffle();
            foreach (var n in navs)
            {
                n.Value = destNavs[0].UIndex;
                destNavs.RemoveAt(0);
            }

            //foreach (var nav in navs)
            //{
            //    var entry = nav.ResolveToEntry(exp.FileRef) as ExportEntry;
            //}

            exp.WriteProperties(props);
            return true;
        }

        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ClassName == "Route";
    }
}
