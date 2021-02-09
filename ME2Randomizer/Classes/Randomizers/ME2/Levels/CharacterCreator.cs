using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    /// <summary>
    /// Randomizer for BioP_Char.pcc
    /// </summary>
    public class CharacterCreator
    {
        private static RandomizationOption SuperRandomOption = new RandomizationOption() { SliderValue = 10 };

        public static bool RandomizeIconicFemShep(RandomizationOption option)
        {
            var femF = MERFileSystem.GetPackageFile("BIOG_Female_Player_C.pcc");
            if (femF != null && File.Exists(femF))
            {
                var femP = MEPackageHandler.OpenMEPackage(femF);
                var femMorphFace = femP.GetUExport(682);
                RBioMorphFace.RandomizeExport(femMorphFace, option);
                var matSetup = femP.GetUExport(681);
                RBioMaterialOverride.RandomizeExport(matSetup, option);

                // Copy this data into BioP_Char so you get accurate results
                var biop_charF = MERFileSystem.GetPackageFile(@"BioP_Char.pcc");
                var biop_char = MEPackageHandler.OpenMEPackage(biop_charF);
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, femMorphFace, biop_char, biop_char.GetUExport(3482), true, out IEntry _);
                EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, matSetup, biop_char, biop_char.GetUExport(3472), true, out IEntry _);
                //biop_char.GetUExport(3482).WriteProperties(femMorphFace.GetProperties()); // Copy the morph face
                //biop_char.GetUExport(3472).WriteProperties(matSetup.GetProperties()); // Copy the material setups
                MERFileSystem.SavePackage(biop_char);
                MERFileSystem.SavePackage(femP);
            }
            return true;
        }
        public static bool RandomizeCharacterCreator(RandomizationOption option)
        {

            if (true /*|| option.HasSubOptionSelected(CharacterCreator.MESS_UP_ICONIC_MALESHEP)*/)
            {
                var sfxgame = MERFileSystem.GetPackageFile("SFXGame.pcc");
                if (sfxgame != null && File.Exists(sfxgame))
                {
                    var sfxgameP = MEPackageHandler.OpenMEPackage(sfxgame);
                    var shepMDL = sfxgameP.GetUExport(42539);
                    RSkeletalMesh.FuzzSkeleton(shepMDL, option);
                    MERFileSystem.SavePackage(sfxgameP);
                }
            }

            var bgr = CoalescedHandler.GetIniFile("BIOGuiResources.ini");
            var charCreatorS = bgr.GetOrAddSection("SFXGame.BioSFHandler_PCNewCharacter");

            charCreatorS.SetSingleEntry("!MalePregeneratedHeadCodes", "CLEAR");
            charCreatorS.SetSingleEntry("!FemalePregeneratedHeadCodes", "CLEAR");
            int numToMake = 20;
            int i = 0;

            // Male: 34 chars
            while (i < numToMake)
            {
                i++;
                charCreatorS.Entries.Add(GenerateHeadCode(false));
            }

            // Female: 36 chars
            i = 0;
            while (i < numToMake)
            {
                i++;
                charCreatorS.Entries.Add(GenerateHeadCode(true));
            }

            var biop_charF = MERFileSystem.GetPackageFile(@"BioP_Char.pcc");
            var biop_char = MEPackageHandler.OpenMEPackage(biop_charF);
            var maleFrontEndData = biop_char.GetUExport(18753);
            var femaleFrontEndData = biop_char.GetUExport(18754);
            randomizeFrontEnd(maleFrontEndData);
            randomizeFrontEnd(femaleFrontEndData);

            //Copy the final skeleton from female into male.
            var femBase = biop_char.GetUExport(3480);
            var maleBase = biop_char.GetUExport(3481);
            maleBase.WriteProperty(femBase.GetProperty<ArrayProperty<StructProperty>>("m_aFinalSkeleton"));

            foreach (var export in biop_char.Exports)
            {
                if (export.ClassName == "BioMorphFace" && !export.ObjectName.Name.Contains("Iconic"))
                {
                    RBioMorphFace.RandomizeExport(export, SuperRandomOption); //.3 default
                }
                else if (export.ClassName == "MorphTarget")
                {
                    if (
                         export.ObjectName.Name.StartsWith("jaw") || export.ObjectName.Name.StartsWith("mouth")
                                                                  || export.ObjectName.Name.StartsWith("eye")
                                                                  || export.ObjectName.Name.StartsWith("cheek")
                                                                  || export.ObjectName.Name.StartsWith("nose")
                                                                  || export.ObjectName.Name.StartsWith("teeth")
                        )
                    {
                        RMorphTarget.RandomizeExport(export, option);
                    }
                }
                else if (export.ClassName == "BioMorphFaceFESliderColour")
                {
                    var colors = export.GetProperty<ArrayProperty<StructProperty>>("m_acColours");
                    foreach (var color in colors)
                    {
                        RStructs.RandomizeColor(color, true);
                    }
                    export.WriteProperty(colors);
                }
                else if (export.ClassName == "BioMorphFaceFESliderMorph")
                {
                    // These don't work becuase of the limits in the morph system
                    // So all this does is change how much the values change, not the max/min
                }
                else if (export.ClassName == "BioMorphFaceFESliderScalar" || export.ClassName == "BioMorphFaceFESliderSetMorph")
                {
                    //no idea how to randomize this lol
                    var floats = export.GetProperty<ArrayProperty<FloatProperty>>("m_afValues");
                    var minfloat = floats.Min();
                    var maxfloat = floats.Max();
                    if (minfloat == maxfloat)
                    {
                        if (minfloat == 0)
                        {
                            maxfloat = 1;
                        }
                        else
                        {
                            var vari = minfloat / 2;
                            maxfloat = ThreadSafeRandom.NextFloat(-vari, vari) + minfloat; //+/- 50%
                        }

                    }
                    foreach (var floatval in floats)
                    {
                        floatval.Value = ThreadSafeRandom.NextFloat(minfloat, maxfloat);
                    }
                    export.WriteProperty(floats);
                }
                else if (export.ClassName == "BioMorphFaceFESliderTexture")
                {

                }
            }
            MERFileSystem.SavePackage(biop_char);
            return true;
        }

        private static DuplicatingIni.IniEntry GenerateHeadCode(bool female)
        {
            // Doubt this will actually work but whatevers.
            return new DuplicatingIni.IniEntry(female ? "+FemalePregeneratedHeadCodes" : "+MalePregeneratedHeadCodes", RandomString(female ? 36 : 34));
        }

        private static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[ThreadSafeRandom.Next(s.Length)]).ToArray());
        }

        private static void randomizeFrontEnd(ExportEntry frontEnd)
        {
            var props = frontEnd.GetProperties();

            //read categories
            var morphCategories = props.GetProp<ArrayProperty<StructProperty>>("MorphCategories");
            var sliders = new Dictionary<string, StructProperty>();
            foreach (var cat in morphCategories)
            {
                var catSliders = cat.GetProp<ArrayProperty<StructProperty>>("m_aoSliders");
                foreach (var cSlider in catSliders)
                {
                    var name = cSlider.GetProp<StrProperty>("m_sName");
                    sliders[name.Value] = cSlider;
                }
            }

            //Default Settings
            var defaultSettings = props.GetProp<ArrayProperty<StructProperty>>("m_aDefaultSettings");
            foreach (var basehead in defaultSettings)
            {
                randomizeBaseHead(basehead, frontEnd, sliders);
            }

            //randomize base heads ?
            var baseHeads = props.GetProp<ArrayProperty<StructProperty>>("m_aBaseHeads");
            foreach (var basehead in baseHeads)
            {
                randomizeBaseHead(basehead, frontEnd, sliders);
            }


            frontEnd.WriteProperties(props);

        }

        private static void randomizeBaseHead(StructProperty basehead, ExportEntry frontEnd, Dictionary<string, StructProperty> sliders)
        {
            var bhSettings = basehead.GetProp<ArrayProperty<StructProperty>>("m_fBaseHeadSettings");
            foreach (var baseSlider in bhSettings)
            {
                var sliderName = baseSlider.GetProp<StrProperty>("m_sSliderName");
                //is slider stepped?
                if (sliderName.Value == "Scar")
                {
                    baseSlider.GetProp<FloatProperty>("m_fValue").Value = 1;
                    continue;
                }
                var slider = sliders[sliderName.Value];
                var notched = slider.GetProp<BoolProperty>("m_bNotched");
                var val = baseSlider.GetProp<FloatProperty>("m_fValue");

                if (notched)
                {
                    //it's indexed
                    var maxIndex = slider.GetProp<IntProperty>("m_iSteps");
                    val.Value = ThreadSafeRandom.Next(maxIndex); //will have to see if isteps is inclusive or not.
                }
                else
                {
                    //it's variable, we have to look up the m_fRange in the SliderMorph.
                    var sliderDatas = slider.GetProp<ArrayProperty<ObjectProperty>>("m_aoSliderData");
                    if (sliderDatas.Count == 1)
                    {
                        var slDataExport = frontEnd.FileRef.GetUExport(sliderDatas[0].Value);
                        var range = slDataExport.GetProperty<FloatProperty>("m_fRange");
                        val.Value = ThreadSafeRandom.NextFloat(0, range * 100);
                    }
                    else
                    {
                        Debug.WriteLine("wrong count of slider datas!");
                    }
                }
            }
        }
    }
}
