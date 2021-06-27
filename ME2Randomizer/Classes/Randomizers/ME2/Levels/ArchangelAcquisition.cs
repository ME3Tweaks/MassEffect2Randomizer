using LegendaryExplorerCore.Packages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class ArchangelAcquisition
    {
        private static void MakeGarrusDeadly()
        {
            // Relay at the end of the DLC
            var garrusShootSeqFile = MERFileSystem.GetPackageFile(@"BioD_OmgGrA_100Leadup.pcc");
            if (garrusShootSeqFile != null && File.Exists(garrusShootSeqFile))
            {
                var garrusSeqP = MEPackageHandler.OpenMEPackage(garrusShootSeqFile);

                // Chance to shoot Shepard
                RandSeqVarInt(garrusSeqP.GetUExport(1043), 60, 100); //60 to 100 percent chance

                // Make garrus shoot faster so he can actually kill the player
                garrusSeqP.GetUExport(974).WriteProperty(new FloatProperty(3, "PlayRate"));

                // Lower the damage so its not instant kill
                garrusSeqP.GetUExport(34).WriteProperty(new FloatProperty(550, "DamageAmount"));
                garrusSeqP.GetUExport(34).WriteProperty(new FloatProperty(2, "MomentumScale"));

                // Do not reset the chance to shoot shepard again
                SeqTools.ChangeOutlink(garrusSeqP.GetUExport(33), 0, 0, 982);

                // Make garrus damage type very deadly
                var garrusDamageTypeProps = garrusSeqP.GetUExport(8).GetProperties();
                garrusDamageTypeProps.Clear(); // Remove the old props
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bImmediateDeath"));
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bIgnoreShieldHitLimit"));
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bIgnoreShields"));
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bIgnoresBleedout"));
                garrusSeqP.GetUExport(8).WriteProperties(garrusDamageTypeProps);

                // Make shepard more vulnerable
                garrusSeqP.GetUExport(1005).WriteProperty(new IntProperty(60, "ValueB"));


                // Make garrus stand more often
                garrusSeqP.GetUExport(1039).WriteProperty(new IntProperty(60, "IntValue"));

                // Make shoot extra free bullets
                garrusSeqP.GetUExport(1037).WriteProperty(new IntProperty(75, "IntValue"));
                RandSeqVarInt(garrusSeqP.GetUExport(1042), 1, 4);

                MERFileSystem.SavePackage(garrusSeqP);
            }
        }

        private static void RandSeqVarInt(ExportEntry export, int min, int max)
        {
            export.WriteProperty(new IntProperty(ThreadSafeRandom.Next(min, max), "IntValue"));
        }

        public static bool PerformRandomization(RandomizationOption option)
        {
            MakeGarrusDeadly();
            return true;
        }
    }
}