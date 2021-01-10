using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    /// <summary>
    /// Randomizer for BioP_Char.pcc
    /// </summary>
    public class CharacterCreator
    {
        private static RandomizationOption SuperRandomOption = new RandomizationOption() {SliderValue = 10};

        public static bool RandomizeCharacterCreator(Random random, RandomizationOption option)
        {
            var biop_charF = MERFileSystem.GetPackageFile(@"BioP_Char.pcc");
            var biop_char = MEPackageHandler.OpenMEPackage(biop_charF);
            var maleFrontEndData = biop_char.GetUExport(18753);
            var femaleFrontEndData = biop_char.GetUExport(18754);
            randomizeFrontEnd(random, maleFrontEndData);
            randomizeFrontEnd(random, femaleFrontEndData);

            //Copy the final skeleton from female into male.
            var femBase = biop_char.GetUExport(3480);
            var maleBase = biop_char.GetUExport(3481);
            maleBase.WriteProperty(femBase.GetProperty<ArrayProperty<StructProperty>>("m_aFinalSkeleton"));

            foreach (var export in biop_char.Exports)
            {
                if (export.ClassName == "BioMorphFace")
                {
                    RBioMorphFace.RandomizeExport(export, SuperRandomOption, random); //.3 default
                }
                else if (export.ClassName == "MorphTarget")
                {
                    if (export.ObjectName.Name.StartsWith("jaw")
                        || export.ObjectName.Name.StartsWith("mouth")
                        || export.ObjectName.Name.StartsWith("eye")
                        || export.ObjectName.Name.StartsWith("cheek")
                        || export.ObjectName.Name.StartsWith("nose")
                        || export.ObjectName.Name.StartsWith("teeth"))
                    {
                        //randomizeMorphTarget(random, export);
                    }
                }
                else if (export.ClassName == "BioMorphFaceFESliderColour")
                {
                    var colors = export.GetProperty<ArrayProperty<StructProperty>>("m_acColours");
                    foreach (var color in colors)
                    {
                        RStructs.RandomizeColor(random, color, true);
                    }
                    export.WriteProperty(colors);
                }
                else if (export.ClassName == "BioMorphFaceFESliderMorph")
                {
                    //not sure if this one actually works due to how face morphs are limited
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
                            maxfloat = random.NextFloat(-vari, vari) + minfloat; //+/- 50%
                        }

                    }
                    foreach (var floatval in floats)
                    {
                        floatval.Value = random.NextFloat(minfloat, maxfloat);
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

        private static void randomizeFrontEnd(Random random, ExportEntry frontEnd)
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
                randomizeBaseHead(random, basehead, frontEnd, sliders);
            }

            //randomize base heads ?
            var baseHeads = props.GetProp<ArrayProperty<StructProperty>>("m_aBaseHeads");
            foreach (var basehead in baseHeads)
            {
                randomizeBaseHead(random, basehead, frontEnd, sliders);
            }


            frontEnd.WriteProperties(props);

        }

        private static void randomizeBaseHead(Random random, StructProperty basehead, ExportEntry frontEnd, Dictionary<string, StructProperty> sliders)
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
                    val.Value = random.Next(maxIndex); //will have to see if isteps is inclusive or not.
                }
                else
                {
                    //it's variable, we have to look up the m_fRange in the SliderMorph.
                    var sliderDatas = slider.GetProp<ArrayProperty<ObjectProperty>>("m_aoSliderData");
                    if (sliderDatas.Count == 1)
                    {
                        var slDataExport = frontEnd.FileRef.GetUExport(sliderDatas[0].Value);
                        var range = slDataExport.GetProperty<FloatProperty>("m_fRange");
                        val.Value = random.NextFloat(0, range * 100);
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
