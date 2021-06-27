using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
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
