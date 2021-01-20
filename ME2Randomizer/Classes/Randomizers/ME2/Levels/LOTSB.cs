﻿using ME3ExplorerCore.Packages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    class LOTSB
    {
        private static void AddToDossiers()
        {
            // Relay at the end of the DLC
            var lotsbHub = MERFileSystem.GetPackageFile(@"BioD_Exp1Lvl5_100Stronghold.pcc");
            if (lotsbHub != null && File.Exists(lotsbHub))
            {
                var hubP = MEPackageHandler.OpenMEPackage(lotsbHub);

                // Chance to shoot Shepard
                RandSeqVarInt(hubP.GetUExport(1043), 50, 100); //50 to 100 percent chance

                // Make garrus shoot faster so he can actually kill the player
                hubP.GetUExport(974).WriteProperty(new FloatProperty(3, "PlayRate"));

                // Lower the damage so its not instant kill
                hubP.GetUExport(34).WriteProperty(new FloatProperty(300, "DamageAmount"));
                hubP.GetUExport(34).WriteProperty(new FloatProperty(2, "MomentumScale"));

                // Do not reset the chance to shoot shepard again
                SeqTools.ChangeOutlink(hubP.GetUExport(33), 0, 0, 982);

                // Make garrus damage type able to kill
                hubP.GetUExport(8).WriteProperty(new BoolProperty(true, "bHealthDamage"));

                // Make garrus stand more often
                hubP.GetUExport(1039).WriteProperty(new FloatProperty(60, "DamageAmount"));

                // Make shoot extra free bullets
                hubP.GetUExport(1037).WriteProperty(new FloatProperty(75, "DamageAmount"));
                RandSeqVarInt(hubP.GetUExport(1042), 1, 4);

                MERFileSystem.SavePackage(hubP);
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