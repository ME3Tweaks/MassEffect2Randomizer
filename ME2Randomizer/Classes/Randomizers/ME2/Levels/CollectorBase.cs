using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class CollectorBase
    {
        public static bool PerformRandomization(Random random, RandomizationOption option)
        {
            RandomizeTheLongWalk(random, option);

            return true;
        }

        private static void RandomizeTheLongWalk(Random random, RandomizationOption option)
        {
            //randomize long walk lengths.
            var endwalkexportmap = new Dictionary<string, int>()
            {
                {"BioD_EndGm2_300Walk01", 40},
                {"BioD_EndGm2_300Walk02", 5344},
                {"BioD_EndGm2_300Walk03", 8884},
                {"BioD_EndGm2_300Walk04", 6370},
                {"BioD_EndGm2_300Walk05", 3190}
            };

            foreach (var map in endwalkexportmap)
            {
                var file = MERFileSystem.GetPackageFile(map.Key + ".pcc");
                if (file != null)
                {
                    var package = MEPackageHandler.OpenMEPackage(file);
                    var export = package.GetUExport(map.Value);
                    export.WriteProperty(new FloatProperty(random.NextFloat(.5, 2.5), "PlayRate"));
                    MERFileSystem.SavePackage(package);
                }
            }

            /*foreach (var f in files)
            {
                var package = MEPackageHandler.OpenMEPackage(f);
                var animExports = package.Exports.Where(x => x.ClassName == "InterpTrackAnimControl");
                foreach (var anim in animExports)
                {
                    var animseqs = anim.GetProperty<ArrayProperty<StructProperty>>("AnimSeqs");
                    if (animseqs != null)
                    {
                        foreach (var animseq in animseqs)
                        {
                            var seqname = animseq.GetProp<NameProperty>("AnimSeqName").Value.Name;
                            if (seqname.StartsWith("Walk_"))
                            {
                                var playrate = animseq.GetProp<FloatProperty>("AnimPlayRate");
                                var oldrate = playrate.Value;
                                if (oldrate != 1) Debugger.Break();
                                playrate.Value = random.NextFloat(.2, 6);
                                var data = anim.Parent.Parent as ExportEntry;
                                var len = data.GetProperty<FloatProperty>("InterpLength");
                                len.Value = len.Value * playrate; //this might need to be changed if its not 1
                                data.WriteProperty(len);
                            }
                        }
                    }
                    anim.WriteProperty(animseqs);
                }
                SavePackage(package);
            }*/
        }
    }
}
