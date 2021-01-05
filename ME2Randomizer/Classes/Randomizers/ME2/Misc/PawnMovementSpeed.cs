using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    public class PawnMovementSpeed
    {
        public void RandomizePlayerMovementSpeed(Random random)
        {
            var femaleFile = MERFileSystem.GetPackageFile("BIOG_Female_Player_C.pcc");
            var maleFile = MERFileSystem.GetPackageFile("BIOG_Male_Player_C.pcc");
            var femalepackage = MEPackageHandler.OpenMEPackage(femaleFile);
            var malepackage = MEPackageHandler.OpenMEPackage(femaleFile);
            SlightlyRandomizeMovementData(femalepackage.GetUExport(2917), random);
            SlightlyRandomizeMovementData(malepackage.GetUExport(2672), random);
            MERFileSystem.SavePackage(femalepackage);
            MERFileSystem.SavePackage(malepackage);
        }

        public void SlightlyRandomizeMovementData(ExportEntry export, Random random)
        {
            var props = export.GetProperties();
            foreach (var prop in props)
            {
                if (prop is FloatProperty fp)
                {
                    fp.Value = random.NextFloat(fp.Value - (fp * .75), fp.Value + (fp * .75));
                }

            }
            export.WriteProperties(props);
        }
    }
}
