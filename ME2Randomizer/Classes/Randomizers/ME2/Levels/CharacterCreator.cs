using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.TLK.ME2ME3;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    /// <summary>
    /// Randomizer for BioP_Char.pcc
    /// </summary>
    public class CharacterCreator
    {

        private void RandomizeCharacterCreator(Random random, List<TalkFile> tlks, IMEPackage biop_char)
        {
            var maleFrontEndData = biop_char.GetUExport(18753);
            randomizeFrontEnd(random, maleFrontEndData);
            //var femaleFrontEndData = biop_char.GetUExport(18754);

            //RandomizeSFXFrontEnd(maleFrontEndData);

            //Copy the final skeleton from female into male.
            var femBase = biop_char.GetUExport(3480);
            var maleBase = biop_char.GetUExport(3481);
            maleBase.WriteProperty(femBase.GetProperty<ArrayProperty<StructProperty>>("m_aFinalSkeleton"));

            foreach (var export in biop_char.Exports)
            {
                if (export.ClassName == "BioMorphFace")
                {
                    RandomizeBioMorphFace(export, random, 10); //.3 default
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
        }
    }
}
