using System;
using LegendaryExplorerCore.Packages;

namespace RandomizerUI.Classes.Randomizers
{
    public interface IExportRandomizer
    {
        public bool RandomizeExport(ExportEntry export, RandomizationOption option, Random random);
    }
}
