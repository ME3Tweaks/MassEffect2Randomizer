using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;

namespace ME2Randomizer.Classes.Randomizers
{
    public interface IExportRandomizer
    {
        public bool RandomizeExport(ExportEntry export, RandomizationOption option, Random random);
    }
}
