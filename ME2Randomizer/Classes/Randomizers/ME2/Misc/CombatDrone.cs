using System;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    /// <summary>
    /// Randomizes the color of combat drones
    /// </summary>
    public class CombatDrone
    {
        private static bool CanRandomize(ExportEntry export) => export.IsDefaultObject && (export.Archetype?.ObjectName.Name == "Default__SFXPawn_EngineerCombatDrone" || export.ClassName == "SFXPawn_EngineerCombatDrone" || export.Archetype?.ObjectName.Name == "Default__SFXPower_CombatDrone");
        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            var props = export.GetProperties();
            if (export.ObjectName.Name.Contains("SFXPower"))
            {

                props.AddOrReplaceProp(new BoolProperty(true, "bCustomDroneColor"));
                props.AddOrReplaceProp(new BoolProperty(true, "bCustomDroneColor2"));
            }
            else
            {
                //sfxpawn
                props.AddOrReplaceProp(new BoolProperty(true, "bCustomColor"));
                props.AddOrReplaceProp(new BoolProperty(true, "bCustomColor2"));
            }

            PropertyCollection randColors = new PropertyCollection();
            randColors.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.NextFloat(0, 60), "X"));
            randColors.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.NextFloat(0, 60), "Y"));
            randColors.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.NextFloat(0, 60), "Z"));

            PropertyCollection randColors2 = new PropertyCollection();
            randColors2.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.NextFloat(0, 128), "X"));
            randColors2.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.NextFloat(0, 128), "Y"));
            randColors2.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.NextFloat(0, 128), "Z"));

            if (export.ObjectName.Name.Contains("SFXPower"))
            {
                props.AddOrReplaceProp(new StructProperty("Vector", randColors, "CustomDroneColor", true));
                props.AddOrReplaceProp(new StructProperty("Vector", randColors2, "CustomDroneColor2", true));
            }
            else
            {
                //sfxpawn
                props.AddOrReplaceProp(new StructProperty("Vector", randColors, "DroneColor", true));
                props.AddOrReplaceProp(new StructProperty("Vector", randColors2, "DroneColor2", true));
            }

            export.WriteProperties(props);
            return true;
        }
    }
}
