using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes;
using LegendaryExplorerCore.ME1.Unreal.UnhoodBytecode;
using LegendaryExplorerCore.Packages;

namespace ME2Randomizer.DebugTools
{
    class DebugRandomizer
    {
        public static bool RandomizeExport(ExportEntry arg1, RandomizationOption arg2)
        {
            // Write debug code here
            //var hi = arg1.InstancedFullPath;
            if (arg1.ClassName == "Function")
            {
                var unFunc = UE3FunctionReader.ReadFunction(arg1);
                TextBuilder tb = new TextBuilder();
                unFunc.Decompile(tb, false, false);
            }
            return true;
        }
    }
}
