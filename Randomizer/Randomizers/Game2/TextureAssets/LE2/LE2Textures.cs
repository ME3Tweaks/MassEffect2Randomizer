using System;
using System.Collections.Generic;
using System.IO;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using ME3TweaksCore.Targets;
using Newtonsoft.Json;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.TextureAssets.LE2
{
#if DEBUG

    public class SourceTexture
    {
        private string FindFile()
        {
            var fPath = @"G:\My Drive\Mass Effect Legendary Modding\LERandomizer\LE2\Images";
            foreach (var f in Directory.GetFiles(fPath, @"*.*", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(f).CaseInsensitiveEquals(Filename))
                {
                    return f;
                }
            }

            return null;
        }

        /// <summary>
        /// The filename on disk. Will be enumerated for, names must be unique
        /// </summary>
        [JsonIgnore]
        public string Filename { get; set; }

        /// <summary>
        /// The ID of the texture
        /// </summary>
        [JsonProperty(@"id")]
        public string Id { get; set; }

        /// <summary>
        /// Tee name of a package that contains the desired destination texture - doesn't have to be exact, but must match dest type (DXT, etc)
        /// </summary>
        [JsonIgnore]
        public string ContainingPackageName { get; set; }

        /// <summary>
        /// IFP in ContainingPackageName that can be used to trigger the texture replacement
        /// </summary>
        [JsonIgnore]
        public string IFPToBuildOff { get; set; }

        public void StoreTexture(IMEPackage premadePackage)
        {
            var sourceFile = FindFile();
            if (sourceFile == null)
            {
                throw new Exception($"Source file not found: {Filename}");
            }

            using var sourceFileData = File.OpenRead(sourceFile);
            var loadedFiles = MELoadedFiles.GetFilesLoadedInGame(MERFileSystem.Game);
            var packageF = loadedFiles[ContainingPackageName];
            using var package = MEPackageHandler.OpenMEPackage(packageF);
            var sourceTex = package.FindExport(IFPToBuildOff);
            TextureTools.ReplaceTexture(sourceTex, sourceFileData, false, out var loadedImage);
            EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.AddSingularAsChild, sourceTex,
                premadePackage, null, true, new RelinkerOptionsPackage(), out var newEntry);
            newEntry.ObjectName = Id;
        }
    }
#endif

    public static class LE2Textures
    {

        public static void BuildPremadeTFC()
        {
#if DEBUG

            var loadedFiles = MELoadedFiles.GetFilesLoadedInGame(MEGame.LE2, includeTFCs: true, forceReload: true);
            if (loadedFiles.TryGetValue($"{PremadeTFCName}.tfc", out var premadeExisting))
            {
                File.Delete(premadeExisting);
            }

            MELoadedFiles.GetFilesLoadedInGame(MEGame.LE2, forceReload: true); // Force reload to remove file
            List<SourceTexture> builderInfo = getShippedTextures();

            var premadePackageF = @"B:\UserProfile\source\repos\ME2Randomizer\Randomizer\Randomizers\Game2\Assets\Binary\Packages\LE2\Textures\PremadeImages.pcc";
            using var premadePackage = MEPackageHandler.CreateAndOpenPackage(premadePackageF, MEGame.LE2);

            var premadeTfcPath = $@"B:\UserProfile\source\repos\ME2Randomizer\Randomizer\Randomizers\Game2\Assets\Binary\Textures\{PremadeTFCName}.tfc";
            foreach (var bfi in builderInfo)
            {
                bfi.StoreTexture(premadePackage);
            }

            loadedFiles = MELoadedFiles.GetFilesLoadedInGame(MEGame.LE2, includeTFCs: true, forceReload: true);
            File.Copy(loadedFiles[PremadeTFCName + ".tfc"], premadeTfcPath, true);

            premadePackage.Save();
#endif
        }

        private static List<SourceTexture> getShippedTextures()
        {
            return new List<SourceTexture>()
            {
                new SourceTexture()
                {
                    Filename = "H_Graphs_mybrandamazon.png",
                    ContainingPackageName = "BioA_N7Mmnt1.pcc",
                    IFPToBuildOff = "BioVFX_Env_Hologram.Textures.H_Graphs",
                    Id = "HoloScreensAmazon"
                },
                new SourceTexture()
                {
                    Filename = "H_Graphs_pizzaonion.png",
                    ContainingPackageName = "BioA_N7Mmnt1.pcc",
                    IFPToBuildOff = "BioVFX_Env_Hologram.Textures.H_Graphs",
                    Id = "HoloScreensPizza"
                },
                new SourceTexture()
                {
                    Filename = "map.png",
                    ContainingPackageName = "BioD_CitAsL.pcc",
                    IFPToBuildOff = "BioApl_Dec_DataPad01.Materials.Datapad01_Screen",
                    Id = "DatapadMap"
                },
                new SourceTexture()
                {
                    Filename = "monsterplan.png",
                    ContainingPackageName = "BioD_CitAsL.pcc",
                    IFPToBuildOff = "BioApl_Dec_DataPad01.Materials.Datapad01_Screen",
                    Id = "DatapadMonsterPlan"
                },
                new SourceTexture()
                {
                    Filename = "sizebounty.png",
                    ContainingPackageName = "BioD_CitAsL.pcc",
                    IFPToBuildOff = "BioApl_Dec_DataPad01.Materials.Datapad01_Screen",
                    Id = "DatapadSizeBounty"
                },
                new SourceTexture()
                {
                    Filename = "thisisfine.png",
                    ContainingPackageName = "BioD_CitAsL.pcc",
                    IFPToBuildOff = "BioApl_Dec_DataPad01.Materials.Datapad01_Screen",
                    Id = "DatapadThisIsFine"
                },
            };
        }

        /// <summary>
        /// DO NOT CHANGE
        /// </summary>
        public const string PremadeTFCName = @"Textures_DLC_MOD_LE2Randomizer_PM";

        public static void SetupLE2Textures(GameTarget target)
        {
            var options = new List<RTexture2D>
            {
                    new RTexture2D
                    {
                        // The orange datapad you see everyone holding
                        TextureInstancedFullPath = "BioApl_Dec_DataPad01.Materials.Datapad01_Screen",
                        AllowedTextureIds = new List<string>
                        {
                            "DatapadMap",
                            "DatapadMonsterPlan",
                            "DatapadSizeBounty",
                            "DatapadThisIsFine",
                        }
                    },
                //new RTexture2D
                //{
                //    // The end-of-game datapad texture
                //    TextureInstancedFullPath = "BioVFX_Env_End.Textures.Reaper_Display",
                //    "audemus_fishdog_food_shack.png",
                //    AllowedTextureIds = new List<string>
                //    {
                //        "DatapadScreens.Reaper_Display.fishdog_foodshack.bin",
                //    }
                //},
            
                new RTexture2D
                {
                    // The graphs that scroll by (H_Graphs)
                    TextureInstancedFullPath = "BioVFX_Env_Hologram.Textures.H_Graphs",
                    AllowedTextureIds = new List<string>
                    {
                        "HoloScreensPizza",
                        "HoloScreensAmazon",
                    },
                    // PreMountTexture = true
                },
            //    new RTexture2D
            //    {
            //        // The graphs that scroll by, line and bar rchargs (H_Graphs3_5)
            //        TextureInstancedFullPath = "BioVFX_Env_Hologram.Textures.H_Graphs2",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "HoloScreens.H_Graphs2.neonbolt.bin",
            //        },
            //        PreMountTexture = false // Texture isn't used anywhere before DLC mount
            //    },
            //    new RTexture2D
            //    {
            //        // The graphs that scroll by, line and bar rchargs (H_Graphs3_5)
            //        TextureInstancedFullPath = "BioVFX_Env_Hologram.Textures.H_Graphs3_5",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "HoloScreens.H_Graphs3_5.sonicburger.bin",
            //        },
            //        PreMountTexture = false // Texture isn't used anywhere before DLC mount
            //    },
            //    new RTexture2D
            //    {
            //        // Vertically scrolling text (1)
            //        TextureInstancedFullPath = "BioVFX_Env_Hologram.Textures.H_texts",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "HoloScreens.H_texts.sourdough.bin",
            //            "HoloScreens.H_texts.newspaper.bin",
            //        },
            //        PreMountTexture = true
            //    },
            //    new RTexture2D
            //    {
            //        // Vertically scrolling text (2)
            //        TextureInstancedFullPath = "BioVFX_Env_Hologram.Textures.H_texts_2",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "HoloScreens.H_texts_2.vim.bin",
            //        },
            //        PreMountTexture = true
            //    },
            //    new RTexture2D
            //    {
            //        // Vertically scrolling text (2)
            //        TextureInstancedFullPath = "BioVFX_Env_Hologram.Textures.H_texts_3",
            //        // purposely lower res than others.
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "HoloScreens.H_texts_3.visualstudio.bin",
            //        },
            //        PreMountTexture = false
            //    },
            //    new RTexture2D
            //    {
            //        // The picture frame that archer holds up in Overlord DLC act 2 start
            //        TextureInstancedFullPath = "BioVFX_Env_UNC_Pack01.Textures.archer_photograph",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "PictureFrames.scarymiranda.bin",
            //            "PictureFrames.creepyshep.bin",
            //            "PictureFrames.hungryillusiveman.bin",
            //            "PictureFrames.monkaanderson.bin",
            //            "PictureFrames.longfaceudina.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        // The picture shown on the screen in the beginning of overlord DLC
            //        TextureInstancedFullPath = "BioVFX_Env_UNC_Pack01.Textures.UNC_1_Dish_Display",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_Promotional", 0),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "Overlord.satimg1.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        // Liara Love Interest Pic (Only if no romance is chosen)
            //        TextureInstancedFullPath = "BioVFX_Env_Hologram.Textures.Liara_1",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "PictureFrames.LoveInterests.steak.bin",
            //        }
            //    },
            //    #region LOTSB
            //    new RTexture2D()
            //    {
            //        // Liara Diploma
            //        TextureInstancedFullPath = "BioS_Exp1Apt.APT_LIARADIP",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_VFX", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "LOTSB.LiaraDiploma.diploma.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        // ILOS painting
            //        TextureInstancedFullPath = "BioS_Exp1Apt.APT_ILOSPAINTING",
            //        AllowedTextureIds = new List<string>
            //        {
            //            "LOTSB.Painting.edge.bin",
            //            "LOTSB.Painting.me1stylesp.bin",
            //            "LOTSB.Painting.outsidethecity.bin",
            //            "LOTSB.Painting.thesource.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        // What picture frame shep picks up turns into
            //        TextureInstancedFullPath = "BioS_Exp1Apt.APT_PROTHDIG",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_Environment", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "LOTSB.PictureFrame.digsite.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        // Picture frame shep picks up
            //        TextureInstancedFullPath = "BioS_Exp1Apt.NRM_SR1",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_Environment", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "LOTSB.PictureFrame.garage.bin",
            //        }
            //    },
            //    #endregion
            //    #region Burger
            //    new RTexture2D()
            //    {
            //        TextureInstancedFullPath = "Edmonton_Burger_Delux2go.Textures.Burger_Norm",
            //        AllowedTextureIds = new List<string>
            //        {
            //            "Burger.Norm.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        TextureInstancedFullPath = "Edmonton_Burger_Delux2go.Textures.Burger_Spec",
            //        AllowedTextureIds = new List<string>
            //        {
            //            "Burger.Spec.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        TextureInstancedFullPath = "Edmonton_Burger_Delux2go.Textures.Burger_Diff",
            //        AllowedTextureIds = new List<string>
            //        {
            //            "Burger.Diff.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        TextureInstancedFullPath = "BioAPL_Dec_PlatesCup_Ceramic.Materials.Plates_NotUgly_Norm",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_APL", 513),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "Burger.Plates_Norm.bin",
            //        }
            //    },
            //    new RTexture2D()
            //    {
            //        TextureInstancedFullPath = "BioAPL_Dec_PlatesCup_Ceramic.Materials.Plates_NotUgly_Diff",
            //        LODGroup = new EnumProperty(new NameReference("TEXTUREGROUP_APL", 1025),"TextureGroup", MEGame.ME2, "LODGroup"), // A bit higher quality
            //        AllowedTextureIds = new List<string>
            //        {
            //            "Burger.Plates_Diff.bin",
            //        }
            //    },
            //    #endregion
            };

            TextureHandler.StartHandler(target, options);
        }
    }
}