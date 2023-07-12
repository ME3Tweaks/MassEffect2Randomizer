using System.IO;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game2.Levels
{
    class ArchangelAcquisition
    {
        /// <summary>
        /// Garrus can kill the player
        /// </summary>
        /// <param name="target"></param>
        private static void MakeGarrusDeadly(GameTarget target)
        {
            var garrusShootSeqFile = MERFileSystem.GetPackageFile(target, @"BioD_OmgGrA_100Leadup.pcc");
            if (garrusShootSeqFile != null && File.Exists(garrusShootSeqFile))
            {
                var garrusSeqP = MERFileSystem.OpenMEPackage(garrusShootSeqFile);

                // Chance to shoot Shepard
                RandSeqVarInt(garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.SeqVar_Int_7"), 60, 100); //60 to 100 percent chance

                // Make garrus shoot faster so he can actually kill the player
                garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.SeqAct_Interp_5").WriteProperty(new FloatProperty(3, "PlayRate"));

                // Lower the damage so its not instant kill
                garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.BioSeqAct_CauseDamage_0").WriteProperty(new FloatProperty(550, "DamageAmount"));
                garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.BioSeqAct_CauseDamage_0").WriteProperty(new FloatProperty(2, "MomentumScale"));

                // Do not reset the chance to shoot shepard again
                SeqTools.ChangeOutlink(garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.BioSeqAct_AttachVisualEffect_2"), 0, 0, 982);

                // Make garrus damage type very deadly
                var garDamage = garrusSeqP.FindExport("SFXGameContent.Default__SFXDamageType_OmgGraFakeSniper");
                var garrusDamageTypeProps = garDamage.GetProperties();
                garrusDamageTypeProps.Clear(); // Remove the old props
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bImmediateDeath"));
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bIgnoreShieldHitLimit"));
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bIgnoreShields"));
                garrusDamageTypeProps.AddOrReplaceProp(new BoolProperty(true, "bIgnoresBleedout"));
                garDamage.WriteProperties(garrusDamageTypeProps);

                // Make shepard more vulnerable
                garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.SeqCond_CompareInt_4").WriteProperty(new IntProperty(60, "ValueB"));


                // Make garrus stand more often
                garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.SeqVar_Int_3").WriteProperty(new IntProperty(60, "IntValue"));

                // Make shoot extra free bullets
                garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.SeqVar_Int_1").WriteProperty(new IntProperty(75, "IntValue"));
                RandSeqVarInt(garrusSeqP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Archangel_Sniping.SeqVar_Int_6"), 1, 4);

                MERFileSystem.SavePackage(garrusSeqP);
            }
        }

        private static void RandSeqVarInt(ExportEntry export, int min, int max)
        {
            export.WriteProperty(new IntProperty(ThreadSafeRandom.Next(min, max), "IntValue"));
        }

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            MakeGarrusDeadly(target);
            return true;
        }
    }
}