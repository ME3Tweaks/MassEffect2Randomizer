using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using ME3TweaksCore.Targets;
using Randomizer.Randomizers.Utility;

namespace Randomizer.MER
{
    internal class MEREasyPorts
    {
        /// <summary>
        /// Ports an export out of EasyPorts.pcc
        /// </summary>
        /// <param name="target"></param>
        /// <param name="objectName"></param>
        /// <param name="destPackage"></param>
        /// <returns></returns>
        public static ExportEntry PortExportIntoPackage(GameTarget target, string objectName, IMEPackage destPackage)
        {
            var packageBin = MEREmbedded.GetEmbeddedPackage(target.Game, "EasyPorts.pcc");
            var easyP = MEPackageHandler.OpenMEPackageFromStream(packageBin);
            return PackageTools.PortExportIntoPackage(target, destPackage, easyP.FindExport(objectName), 0, false, true);
        }
    }
}
