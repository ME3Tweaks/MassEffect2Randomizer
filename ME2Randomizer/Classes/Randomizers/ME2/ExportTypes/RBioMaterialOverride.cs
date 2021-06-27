using System;
using System.Diagnostics;
using ME2Randomizer.Classes.Randomizers.ME2.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioMaterialOverride
    {
        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioMaterialOverride";
        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            PropertyCollection props = export.GetProperties();
            var colorOverrides = props.GetProp<ArrayProperty<StructProperty>>("m_aColorOverrides");
            if (colorOverrides != null)
            {
                foreach (StructProperty colorParameter in colorOverrides)
                {
                    //Debug.WriteLine("Randomizing Color Parameter");
                    RStructs.RandomizeTint(colorParameter.GetProp<StructProperty>("cValue"), false);
                }
            }
            var scalarOverrides = props.GetProp<ArrayProperty<StructProperty>>("m_aScalarOverrides");
            if (scalarOverrides != null)
            {
                foreach (StructProperty scalarParameter in scalarOverrides)
                {
                    var name = scalarParameter.GetProp<NameProperty>("nName");
                    if (name != null)
                    {
                        if (name.Value.Name.Contains("_Frek_") || name.Value.Name.StartsWith("HAIR") || name.Value.Name.StartsWith("HED_Scar"))
                        {

                            var currentValue = scalarParameter.GetProp<FloatProperty>("sValue");
                            if (currentValue != null)
                            {
                                //Debug.WriteLine("Randomizing FREK HAIR HEDSCAR");

                                if (currentValue > 1)
                                {
                                    scalarParameter.GetProp<FloatProperty>("sValue").Value = ThreadSafeRandom.NextFloat(0, currentValue * 1.3);
                                }
                                else
                                {
                                    scalarParameter.GetProp<FloatProperty>("sValue").Value = ThreadSafeRandom.NextFloat(0, 1);
                                }
                            }
                        }

                    }
                }
            }
            export.WriteProperties(props);
            return true;
        }
    }
}
