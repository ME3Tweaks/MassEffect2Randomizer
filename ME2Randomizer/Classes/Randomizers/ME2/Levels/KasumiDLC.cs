using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.TextureAssets;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.Classes;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{

    class KasumiArtGalleryFile
    {
        public string PackageName { get; set; }
        public List<int> TallTextureUIndexes { get; set; }
        public List<int> WideTextureUIndexes { get; set; }
        public List<int> MaterialUIndexes { get; set; }
        public bool RenameMemoryInstances { get; set; }
    }

    public static class KasumiDLC
    {

        // These have to be shepard's outfits.
        // Cause they have to have right number of bones
        // shep is the only char with this many bones
        // so it must be shep outfits -_-
        private static List<(string packageFile, string entryFullPath, string matInstFullPath)> MaleTuxedoReplacements = new()
        {
            ("BioD_CitAsL", "BIOG_DRL_THN_LGT_R.LGTb.DRL_ARM_LGTb_MDL", "BIOG_DRL_THN_LGT_R.LGTb.DRL_ARM_LGTb_MAT_1a"),
        };

        private static List<(string packageFile, string entryFullPath, string matInstFullPath)> FemaleTuxedoReplacements = new()
        {
            ("BioH_Geth", "BIOG_GTH_LEG_NKD_R.GTH_LEG_NKDa_MDL", "BIOG_GTH_LEG_NKD_R.GTH_LEG_NKDa_MAT_1a"),
        };

        private static KasumiArtGalleryFile[] GetKasumiArtGallerySetup()
        {
            // Method so we can return new copies as they have lists.
            return new KasumiArtGalleryFile[]
            {
                new KasumiArtGalleryFile()
                {
                    PackageName = "BioA_PtyMtl_100Party.pcc",
                    TallTextureUIndexes = new List<int>(new[] {6952, 6954, 6956, 6957, 6958}),
                    WideTextureUIndexes = new List<int>(new[]{ 6953, 6955, 6959, 6960, 6961, 6962, 6963, 6964 }),
                },
                new KasumiArtGalleryFile()
                {
                    PackageName = "BioA_PtyMtl_105LandingCutscene.pcc",
                    TallTextureUIndexes = new List<int>(new [] {9118}),
                    RenameMemoryInstances = true
                },
                new KasumiArtGalleryFile()
                {
                    PackageName = "BioA_PtyMtl_110Bedroom.pcc",
                    TallTextureUIndexes = new List<int>(new[] {4671, 2240}),
                    RenameMemoryInstances = true
                },
                new KasumiArtGalleryFile()
                {
                    PackageName = "BioA_PtyMtl_120SecurityOffice.pcc",
                    TallTextureUIndexes = new List<int>(new[] {2239, 2240}),
                    MaterialUIndexes = new List<int>(new[] {339, 340}),
                    RenameMemoryInstances = true,
                },
                new KasumiArtGalleryFile()
                {
                    PackageName = "BioA_PtyMtl_130BedroomHall.pcc",
                    TallTextureUIndexes = new List<int>(new[] {4910, 2240}),
                    RenameMemoryInstances = true
                },
            };
        }

        private static void ChangeSecurityTV()
        {
            // 2 frames
            //BioA_PtyMtl_120SecurityOffice 2242
            //BioA_PtyMtl_120SecurityOffice 2243
        }

        private static void ChangeNormandyPaintings()
        {
            //BioA_Nor_230
            // BioT_NorHenMT Painting01_Diff
            // BioT_NorHenMT Painting02_Diff
            // Both square
            var nor230F = MERFileSystem.GetPackageFile("BioA_Nor_230.pcc");
            if (nor230F != null && File.Exists(nor230F))
            {
                var nor230P = MEPackageHandler.OpenMEPackage(nor230F);

                var painting1 = nor230P.FindExport("BioT_NorHenMT.Painting01_Diff");
                var painting2 = nor230P.FindExport("BioT_NorHenMT.Painting02_Diff");
                if (painting1 != null && painting2 != null)
                {
                    var assets = TFCBuilder.ListTextureAssets("Kasumi.NormandyPaintings").Select(x => $"Kasumi.NormandyPaintings.{x}").ToList();
                    assets.Shuffle();

                    RTexture2D r2d = new RTexture2D()
                    {
                        AllowedTextureAssetNames = assets,
                        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_Environment", 1025), "TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
                    };

                    TFCBuilder.InstallTexture(r2d, painting1, assets.PullFirstItem());
                    TFCBuilder.InstallTexture(r2d, painting2, assets.PullFirstItem());

                    MERFileSystem.SavePackage(nor230P);
                }
            }
        }

        private static void RandomizeArtGallery()
        {
            var wideAssets = TFCBuilder.ListTextureAssets("Kasumi.ArtGallery.wide").Select(x => $"Kasumi.ArtGallery.wide.{x}").ToList();
            var tallAssets = TFCBuilder.ListTextureAssets("Kasumi.ArtGallery.tall").Select(x => $"Kasumi.ArtGallery.tall.{x}").ToList();
            wideAssets.Shuffle();
            tallAssets.Shuffle();

            var artyGallery = GetKasumiArtGallerySetup();
            foreach (var kagf in artyGallery)
            {
                var artGalleryF = MERFileSystem.GetPackageFile(kagf.PackageName);
                if (artGalleryF != null && File.Exists(artGalleryF))
                {
                    var artGalleryP = MEPackageHandler.OpenMEPackage(artGalleryF);

                    // Rename instances so they're memory unique so we have a few more paintings
                    if (kagf.RenameMemoryInstances)
                    {
                        if (kagf.TallTextureUIndexes != null)
                        {
                            foreach (var uindex in kagf.TallTextureUIndexes)
                            {
                                artGalleryP.GetUExport(uindex).ObjectName = $"ME2R_T_KASUMIPAINTING{ThreadSafeRandom.Next(15000)}";
                            }
                        }

                        // Rename mats so they're also unique
                        if (kagf.WideTextureUIndexes != null)
                        {
                            foreach (var uindex in kagf.WideTextureUIndexes)
                            {
                                artGalleryP.GetUExport(uindex).ObjectName = $"ME2R_W_KASUMIPAINTING{ThreadSafeRandom.Next(15000)}";
                            }
                        }

                        if (kagf.MaterialUIndexes != null)
                        {
                            foreach (var uindex in kagf.MaterialUIndexes)
                            {
                                artGalleryP.GetUExport(uindex).ObjectName = $"ME2R_PAINTMAT_KASUMI{ThreadSafeRandom.Next(15000)}";
                            }
                        }
                    }

                    InstallARArtTextures(kagf.WideTextureUIndexes, wideAssets, artGalleryP, "Wide");
                    InstallARArtTextures(kagf.TallTextureUIndexes, tallAssets, artGalleryP, "Tall");

                    MERFileSystem.SavePackage(artGalleryP);
                }
            }
        }

        private static void InstallARArtTextures(List<int> uindexes, List<string> assets, IMEPackage artGalleryP, string loggingName)
        {
            if (uindexes == null) return; // This set is not installed
            while (uindexes.Any())
            {
                if (!assets.Any())
                {
                    Debug.WriteLine($"Not enough assets for {loggingName}! Need {uindexes.Count} more!");
                    return;
                }
                var uIndex = uindexes.PullFirstItem();
                var asset = assets.PullFirstItem();
                var texExport = artGalleryP.GetUExport(uIndex);
                RTexture2D r2d = new RTexture2D()
                {
                    LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_Environment", 1025), "TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
                    AllowedTextureAssetNames = new List<string>() { asset },
                    TextureInstancedFullPath = texExport.InstancedFullPath // don't think this is used but just leave it here anyways.
                };
                TFCBuilder.InstallTexture(r2d, texExport, asset);
            }
        }

        private static void RandomizeTuxedoMesh()
        {
            // Won't implement: too many restrictions on shep mesh

            // Oh man dis gun b gud
            //var biogame = Coalesced.CoalescedHandler.GetIniFile("BIOGame.ini");
            //var casuals = biogame.GetOrAddSection("SFXGame.SFXPawn_Player");

            //// Remove the default casual appearance info
            //casuals.Entries.Add(new DuplicatingIni.IniEntry("-CasualAppearances", "(Id=95,Type=CustomizableType_Torso,Mesh=(Male=\"BIOG_HMM_SHP_CTH_R.CTHa.HMM_ARM_CTHa_Tux_MDL\",MaleMaterialOverride=\"BIOG_HMM_SHP_CTH_R.CTHa.HMM_ARM_CTHa_Tux_MAT_1a\",Female=\"BIOG_HMF_SHP_CTH_R.CTHa.HMF_ARM_CTHa_Tux_MDL\",FemaleMaterialOverride=\"BIOG_HMF_SHP_CTH_R.CTHa.HMF_ARM_CTHa_Tux_MAT_1a\"),PlotFlag=6709)"));

            ////var bioengine = CoalescedHandler.GetIniFile("BIOEngine.ini");
            ////var seekFreePackages = bioengine.GetOrAddSection("Engine.PackagesToAlwaysCook");

            //var randomMale = MaleTuxedoReplacements.RandomElement();
            //var randomFemale = FemaleTuxedoReplacements.RandomElement();

            ////seekFreePackages.Entries.Add(new DuplicatingIni.IniEntry("+SeekFreePackage", randomMale.packageFile));
            ////seekFreePackages.Entries.Add(new DuplicatingIni.IniEntry("+SeekFreePackage", randomFemale.packageFile));
            //casuals.Entries.Add(new DuplicatingIni.IniEntry("+CasualAppearances", $"(Id=95,Type=CustomizableType_Torso,Mesh=(Male=\"{randomMale.packageFile}.{randomMale.entryFullPath}\",MaleMaterialOverride=\"{randomMale.packageFile}.{randomMale.matInstFullPath}\",Female=\"{randomFemale.packageFile}.{randomFemale.entryFullPath}\",FemaleMaterialOverride=\"{randomFemale.packageFile}.{randomFemale.matInstFullPath}\"),PlotFlag=6709)"));
        }

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            ChangeNormandyPaintings();
            //RandomizeTuxedoMesh();
            RandomizeArtGallery();
            return true;
        }
    }
}
