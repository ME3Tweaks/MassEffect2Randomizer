using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using Randomizer.Randomizers.Game2.Misc;

namespace Randomizer.Randomizers.Game2.Levels
{
    public class Normandy
    {
        public static bool PerformRandomization(RandomizationOption option)
        {
            AddBurgersToCookingQuest();
            RandomizeNormandyHolo();
            RandomizeWrongWashroomSFX();
            return true;
        }

        /// <summary>
        /// Going into male room as female
        /// </summary>
        private static List<(string packageName, int uindex)> MaleWashroomAudioSources = new List<(string packageName, int uindex)>()
        {
            ( "BioD_Nor_CR3_200_LOC_INT.pcc", 609 ), // SHEPARD, SUBMIT NOW
            ( "BioD_Nor_CR3_200_LOC_INT.pcc", 603 ), // shitshitshitshitshit
            ( "BioD_Nor_250Henchmen_LOC_INT.pcc", 4227 ), // More food less ass
        };

        /// <summary>
        /// Going into female room as male
        /// </summary>
        private static List<(string packageName, int uindex)> FemaleWashroomAudioSources = new List<(string packageName, int uindex)>()
        {
            ( "BioD_Nor_CR3_200_LOC_INT.pcc", 609 ), // SHEPARD, SUBMIT NOW
            ( "BioD_Nor_CR3_200_LOC_INT.pcc", 597 ), // AUGH!
        };

        private static void RandomizeWrongWashroomSFX()
        {
            // Yeah I went to canada
            // they call them washrooms
            var henchmenLOCInt250 = MERFileSystem.GetPackageFile("BioD_Nor_250Henchmen_LOC_INT.pcc");
            if (henchmenLOCInt250 != null && File.Exists(henchmenLOCInt250))
            {
                var washroomP = MEPackageHandler.OpenMEPackage(henchmenLOCInt250);
                MERPackageCache pc = new MERPackageCache();
                var randomMale = MaleWashroomAudioSources.RandomElement();
                var randomFemale = FemaleWashroomAudioSources.RandomElement();

                var mPackage = pc.GetCachedPackage(randomMale.packageName);
                var fPackage = pc.GetCachedPackage(randomFemale.packageName);
                WwiseTools.RepointWwiseStream(mPackage.GetUExport(randomMale.uindex), washroomP.GetUExport(4195)); //male into female
                WwiseTools.RepointWwiseStream(fPackage.GetUExport(randomFemale.uindex), washroomP.GetUExport(4196)); //female into male

                MERFileSystem.SavePackage(washroomP);
            }
        }

        private static (Vector3 loc, CIVector3 rot)[] BurgerLocations = new[]
        {
            (new Vector3(-517,2498,-459), new CIVector3(0,ThreadSafeRandom.Next(65535),0)), // The one near the cook

            // On the tables
            (new Vector3(79,3265,-479), new CIVector3(0,ThreadSafeRandom.Next(65535),0)),
            (new Vector3(-94,3225,-479), new CIVector3(0,ThreadSafeRandom.Next(65535),0)),
            (new Vector3(160,3823,-479), new CIVector3(0,ThreadSafeRandom.Next(65535),0)),
        };

        private static void AddBurgersToCookingQuest()
        {
            var cookingAreaF = MERFileSystem.GetPackageFile("BioD_Nor_250Henchmen.pcc");
            if (cookingAreaF != null && File.Exists(cookingAreaF))
            {
                var nor250Henchmen = MEPackageHandler.OpenMEPackage(cookingAreaF);

                var packageBin = MERUtilities.GetEmbeddedStaticFilesBinaryFile("Delux2go_Edmonton_Burger.pcc");
                var burgerPackage = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(packageBin));

                List<ExportEntry> addedBurgers = new List<ExportEntry>();

                // 1. Add the burger package by porting in the first skeletal mesh.
                var world = nor250Henchmen.FindExport("TheWorld.PersistentLevel");
                var firstBurgerSKM = PackageTools.PortExportIntoPackage(nor250Henchmen, burgerPackage.FindExport("BurgerSKMA"), world.UIndex, false);

                // Setup the object for cloning, add to list of new actors
                firstBurgerSKM.WriteProperty(new BoolProperty(true, "bHidden")); // Make burger hidden by default. It's unhidden in kismet
                addedBurgers.Add(firstBurgerSKM);

                // 1.5 Shrink the burger
                // Look, everyone likes a thicc burger but this thing is the size
                // of a barbie 4 wheeler
                // I think it could use a small taking down a notch
                var ds = firstBurgerSKM.GetProperty<FloatProperty>("DrawScale");
                ds.Value = 0.0005f;
                firstBurgerSKM.WriteProperty(ds);

                // 2. Link up the textures
                TFCBuilder.RandomizeExport(nor250Henchmen.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Diff"), null);
                TFCBuilder.RandomizeExport(nor250Henchmen.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Norm"), null);
                TFCBuilder.RandomizeExport(nor250Henchmen.FindExport("Edmonton_Burger_Delux2go.Textures.Burger_Spec"), null);

                // 3. Clone 3 more burgers
                addedBurgers.Add(EntryCloner.CloneTree(firstBurgerSKM));
                addedBurgers.Add(EntryCloner.CloneTree(firstBurgerSKM));
                addedBurgers.Add(EntryCloner.CloneTree(firstBurgerSKM));

                // 4. Port in plates for the ones people are eating to sit on
                var plateFile = MERFileSystem.GetPackageFile("BioA_OmgHub800_Marketplace.pcc");
                var platePackage = MEPackageHandler.OpenMEPackage(plateFile);
                List<ExportEntry> plateExports = new List<ExportEntry>();
                var plateSMA1 = PackageTools.PortExportIntoPackage(nor250Henchmen, platePackage.GetUExport(3280), world.UIndex, false);
                plateSMA1.WriteProperty(new BoolProperty(true, "bHidden")); //make hidden by default

                // Update the textures to remove that god awful original design and use something less ugly
                var pNorm = nor250Henchmen.FindExport("BioAPL_Dec_PlatesCup_Ceramic.Materials.Plates_Norm");
                var pDiff = nor250Henchmen.FindExport("BioAPL_Dec_PlatesCup_Ceramic.Materials.Plates_Diff");
                pNorm.ObjectName = "Plates_NotUgly_Norm";
                pDiff.ObjectName = "Plates_NotUgly_Diff";
                TFCBuilder.RandomizeExport(pDiff, null);
                TFCBuilder.RandomizeExport(pNorm, null);


                // We need to make dynamic lit - values are copied from another SMA in BioA_Nor_200. I don't know what the channels mean
                var skmc = plateSMA1.GetProperty<ObjectProperty>("StaticMeshComponent").ResolveToEntry(world.FileRef) as ExportEntry;
                var lc = skmc.GetProperty<StructProperty>("LightingChannels");
                lc.Properties.Clear();
                lc.Properties.Add(new BoolProperty(true, "bInitialized"));
                lc.Properties.Add(new BoolProperty(true, new NameReference("Unnamed", 5))); //??
                lc.Properties.Add(new BoolProperty(true, new NameReference("Unnamed", 7))); //??
                skmc.WriteProperty(lc);
                skmc.WriteProperty(new BoolProperty(false, "bForceDirectLightMap"));

                plateExports.Add(plateSMA1);
                plateExports.Add(EntryCloner.CloneTree(plateSMA1));
                plateExports.Add(EntryCloner.CloneTree(plateSMA1));

                // 4. Setup the locations and rotations, setup the sequence object info
                var toggleHiddenUnhide = nor250Henchmen.GetUExport(5734);
                for (int i = 0; i < addedBurgers.Count; i++)
                {
                    var burger = addedBurgers[i];
                    var lp = BurgerLocations[i];
                    burger.WriteProperty(lp.loc.ToVectorStructProperty("Location"));
                    burger.WriteProperty(lp.rot.ToRotatorStructProperty("Rotation"));

                    // Add burger object to kismet unhide
                    var clonedSeqObj = MERSeqTools.CloneBasicSequenceObject(nor250Henchmen.GetUExport(5970));
                    clonedSeqObj.WriteProperty(new ObjectProperty(burger.UIndex, "ObjValue"));
                    KismetHelper.CreateVariableLink(toggleHiddenUnhide, "Target", clonedSeqObj);

                    if (i >= 1)
                    {
                        var plate = plateExports[i - 1];
                        // Put the burger on the plate
                        var pLoc = lp.loc;
                        pLoc.Z -= 15; // Plate under burger
                        plate.WriteProperty(lp.loc.ToVectorStructProperty("Location"));

                        // Add plate object to kismet unhide
                        var clonedSeqObjPlate = MERSeqTools.CloneBasicSequenceObject(nor250Henchmen.GetUExport(5970));
                        clonedSeqObjPlate.WriteProperty(new ObjectProperty(plate.UIndex, "ObjValue"));
                        KismetHelper.CreateVariableLink(toggleHiddenUnhide, "Target", clonedSeqObjPlate);
                    }
                }

                // Add burgers to level
                addedBurgers.AddRange(plateExports);
                nor250Henchmen.AddToLevelActorsIfNotThere(addedBurgers.ToArray());

                MERFileSystem.SavePackage(nor250Henchmen);
            }
        }

        private static void RandomizeTrashCompactor()
        {
            var packageF = MERFileSystem.GetPackageFile("BioA_Nor_310.pcc");
            if (packageF != null && File.Exists(packageF))
            {
                var package = MEPackageHandler.OpenMEPackage(packageF);
                if (package.TryGetUExport(2176, out var junkCube) && junkCube.ClassName == "SkeletalMesh")
                {
                    // We had to check the class name of the object cause it might be the vanilla version and not the HEN_VT one


                    MERFileSystem.SavePackage(package);
                }
            }

        }

        private static void RandomizeNormandyHolo()
        {
            string[] packages = { "BioD_Nor_104Comm.pcc", "BioA_Nor_110.pcc" };
            foreach (var packagef in packages)
            {
                var package = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(packagef));

                //WIREFRAME COLOR
                var wireframeMaterial = package.Exports.First(x => x.ObjectName == "Wireframe_mat_Master");
                var data = wireframeMaterial.Data;

                var wireColorR = ThreadSafeRandom.NextFloat(0.01, 2);
                var wireColorG = ThreadSafeRandom.NextFloat(0.01, 2);
                var wireColorB = ThreadSafeRandom.NextFloat(0.01, 2);

                List<float> allColors = new List<float>();
                allColors.Add(wireColorR);
                allColors.Add(wireColorG);
                allColors.Add(wireColorB);

                data.OverwriteRange(0x33C, BitConverter.GetBytes(wireColorR)); //R
                data.OverwriteRange(0x340, BitConverter.GetBytes(wireColorG)); //G
                data.OverwriteRange(0x344, BitConverter.GetBytes(wireColorB)); //B
                wireframeMaterial.Data = data;

                //INTERNAL HOLO
                var norHoloLargeMat = package.Exports.First(x => x.ObjectName == "Nor_Hologram_Large");
                data = norHoloLargeMat.Data;

                float holoR = 0, holoG = 0, holoB = 0;
                holoR = wireColorR * 5;
                holoG = wireColorG * 5;
                holoB = wireColorB * 5;

                data.OverwriteRange(0x314, BitConverter.GetBytes(holoR)); //R
                data.OverwriteRange(0x318, BitConverter.GetBytes(holoG)); //G
                data.OverwriteRange(0x31C, BitConverter.GetBytes(holoB)); //B
                norHoloLargeMat.Data = data;

                if (packagef == "BioA_Nor_110.pcc")
                {
                    //need to also adjust the glow under the CIC. It's controlled by a interp apparently
                    var lightColorInterp = package.GetUExport(300);
                    var vectorTrack = lightColorInterp.GetProperty<StructProperty>("VectorTrack");
                    var blueToOrangePoints = vectorTrack.GetProp<ArrayProperty<StructProperty>>("Points");
                    //var maxColor = allColors.Max();
                    blueToOrangePoints[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("X").Value = wireColorR;
                    blueToOrangePoints[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Y").Value = wireColorG;
                    blueToOrangePoints[1].GetProp<StructProperty>("OutVal").GetProp<FloatProperty>("Z").Value = wireColorB;
                    lightColorInterp.WriteProperty(vectorTrack);
                }
                MERFileSystem.SavePackage(package);
            }
        }
    }
}
