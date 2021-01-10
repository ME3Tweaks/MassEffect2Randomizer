using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using Serilog;
using System;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class RBioLookAtTarget
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioLookAtTarget";

        public static bool RandomizeExport(ExportEntry export, Random random, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var location = Location.GetLocation(export);
            if (location != null)
            {
                var locS = location.Value;
                locS.X = random.NextFloat(-100000, 100000);
                locS.Y = random.NextFloat(-100000, 100000);
                locS.Z = random.NextFloat(-100000, 100000);
                Location.SetLocation(export, locS);
                return true;
            }
            return false;
        }
    }

    class RBioLookAtDefinition
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioLookAtDefinition" || export.ClassName == @"Bio_Appr_Character";

        public static bool RandomizeExport(ExportEntry export, Random random, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var boneDefinitions = export.GetProperty<ArrayProperty<StructProperty>>(export.ClassName == @"BioLookAtDefinition" ? "BoneDefinitions" : "m_aLookBoneDefs");
            if (boneDefinitions != null)
            {
                Log.Information($"Randomizing BioLookAtDefinition {export.UIndex}");
                foreach (var item in boneDefinitions)
                {
                    //if (item.GetProp<NameProperty>("m_nBoneName").Value.Name.StartsWith("Eye"))
                    //{
                    //    item.GetProp<FloatProperty>("m_fLimit").Value = random.Next(1, 5);
                    //    item.GetProp<FloatProperty>("m_fUpDownLimit").Value = random.Next(1, 5);
                    //}
                    //else
                    //{
                    item.GetProp<FloatProperty>(@"m_fDelay").Value = random.NextFloat(0, 5);
                    item.GetProp<FloatProperty>("m_fLimit").Value = random.NextFloat(1, 170);
                    item.GetProp<FloatProperty>("m_fUpDownLimit").Value = random.NextFloat(70, 170);
                    //}

                }

                export.WriteProperty(boneDefinitions);
                return true;
            }
            return false;
        }
    }
}
