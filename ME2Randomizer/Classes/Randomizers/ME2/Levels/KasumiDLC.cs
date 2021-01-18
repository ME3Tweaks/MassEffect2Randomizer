using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public static class KasumiDLC
    {
        private static List<(string packageFile, string entryFullPath, string matInstFullPath)> MaleTuxedoReplacements = new()
        {
            ("BioD_CitAsL", "BIOG_DRL_THN_LGT_R.LGTb.DRL_ARM_LGTb_MDL", "BIOG_DRL_THN_LGT_R.LGTb.DRL_ARM_LGTb_MAT_1a"),
        };

        private static List<(string packageFile, string entryFullPath, string matInstFullPath)> FemaleTuxedoReplacements = new()
        {
            ("BioH_Geth", "BIOG_GTH_LEG_NKD_R.GTH_LEG_NKDa_MDL", "BIOG_GTH_LEG_NKD_R.GTH_LEG_NKDa_MAT_1a"),
        };

        private static void RandomizeTuxedoMesh()
        {
            // Oh man dis gun b gud
            var biogame = Coalesced.CoalescedHandler.GetIniFile("BIOGame.ini");
            var casuals = biogame.GetOrAddSection("SFXGame.SFXPawn_Player");

            // Remove the default casual appearance info
            casuals.Entries.Add(new DuplicatingIni.IniEntry("-CasualAppearances", "(Id=95,Type=CustomizableType_Torso,Mesh=(Male=\"BIOG_HMM_SHP_CTH_R.CTHa.HMM_ARM_CTHa_Tux_MDL\",MaleMaterialOverride=\"BIOG_HMM_SHP_CTH_R.CTHa.HMM_ARM_CTHa_Tux_MAT_1a\",Female=\"BIOG_HMF_SHP_CTH_R.CTHa.HMF_ARM_CTHa_Tux_MDL\",FemaleMaterialOverride=\"BIOG_HMF_SHP_CTH_R.CTHa.HMF_ARM_CTHa_Tux_MAT_1a\"),PlotFlag=6709)"));

            var bioengine = CoalescedHandler.GetIniFile("BIOEngine.ini");
            var seekFreePackages = bioengine.GetOrAddSection("Engine.PackagesToAlwaysCook");

            var randomMale = MaleTuxedoReplacements.RandomElement();
            var randomFemale = FemaleTuxedoReplacements.RandomElement();

            seekFreePackages.Entries.Add(new DuplicatingIni.IniEntry("+SeekFreePackage", randomMale.packageFile));
            seekFreePackages.Entries.Add(new DuplicatingIni.IniEntry("+SeekFreePackage", randomFemale.packageFile));
            casuals.Entries.Add(new DuplicatingIni.IniEntry("+CasualAppearances", $"(Id=95,Type=CustomizableType_Torso,Mesh=(Male=\"{randomMale.packageFile}.{randomMale.entryFullPath}\",MaleMaterialOverride=\"{randomMale.packageFile}.{randomMale.matInstFullPath}\",Female=\"{randomFemale.packageFile}.{randomFemale.entryFullPath}\",FemaleMaterialOverride=\"{randomFemale.packageFile}.{randomFemale.matInstFullPath}\"),PlotFlag=6709)"));


        }

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            RandomizeTuxedoMesh();
            return true;
        }
    }
}
