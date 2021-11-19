using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Navigation;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using ME3TweaksCore.Targets;

namespace Randomizer.Randomizers.Game1._2DA
{
    class RCharacter2DA
    {
        internal const string RANDSETTING_CHARACTER_HENCH_ARCHETYPES = "RANDSETTING_CHARACTER_HENCH_ARCHETYPES";
        internal const string RANDSETTING_CHARACTER_INVENTORY = "RANDSETTING_CHARACTER_INVENTORY";
       
        private static bool CanRandomize(ExportEntry exp) => !exp.IsDefaultObject && exp.ClassName == "Bio2DA" && exp.ObjectName.Name.StartsWith("Characters_Character");
        public static bool RandomizeExport(GameTarget target, ExportEntry export, RandomizationOption option)
        {
            bool hasChanges = false;
            int[] humanLightArmorManufacturers = { 373, 374, 375, 379, 383, 451 };
            int[] bioampManufacturers = { 341, 342, 343, 345, 410, 496, 497, 498, 526 };
            int[] omnitoolManufacturers = { 362, 363, 364, 366, 411, 499, 500, 501, 527 };
            List<string> actorTypes = new List<string>();
            actorTypes.Add("BIOG_HumanFemale_Hench_C.hench_humanFemale");
            actorTypes.Add("BIOG_HumanMale_Hench_C.hench_humanmale");
            actorTypes.Add("BIOG_Asari_Hench_C.hench_asari");
            actorTypes.Add("BIOG_Krogan_Hench_C.hench_krogan");
            actorTypes.Add("BIOG_Turian_Hench_C.hench_turian");
            actorTypes.Add("BIOG_Quarian_Hench_C.hench_quarian");
            //actorTypes.Add("BIOG_Jenkins_Hench_C.hench_jenkins");

            Bio2DA character2da = new Bio2DA(export);
            for (int row = 0; row < character2da.RowNames.Count(); row++)
            {
                //Console.WriteLine("[" + row + "][" + colsToRandomize[i] + "] value is " + BitConverter.ToSingle(cluster2da[row, colsToRandomize[i]].Data, 0));


                if (option.HasSubOptionSelected(RANDSETTING_CHARACTER_HENCH_ARCHETYPES))
                {
                    if (character2da[row, 0].DisplayableValue.StartsWith("hench") && !character2da[row, 0].DisplayableValue.Contains("jenkins"))
                    {
                        //Henchman
                        int indexToChoose = ThreadSafeRandom.Next(actorTypes.Count);
                        var actorNameVal = actorTypes[indexToChoose];
                        actorTypes.RemoveAt(indexToChoose);
                        Console.WriteLine("Character Randomizer HENCH ARCHETYPE [" + row + "][2] value is now " + actorNameVal);
                        character2da[row, 2].NameValue = new NameReference(actorNameVal);
                        hasChanges = true;
                    }
                }

                if (option.HasSubOptionSelected(RANDSETTING_CHARACTER_INVENTORY))
                {
                    int randvalue = ThreadSafeRandom.Next(humanLightArmorManufacturers.Length);
                    int manf = humanLightArmorManufacturers[randvalue];
                    Console.WriteLine("Character Randomizer ARMOR [" + row + "][21] value is now " + manf);
                    character2da[row, 21].IntValue = manf;

                    if (character2da[row, 24] != null)
                    {
                        randvalue = ThreadSafeRandom.Next(bioampManufacturers.Length);
                        manf = bioampManufacturers[randvalue];
                        Console.WriteLine("Character Randomizer BIOAMP [" + row + "][24] value is now " + manf);
                        character2da[row, 24].IntValue = manf;
                        hasChanges = true;
                    }

                    if (character2da[row, 29] != null)
                    {
                        randvalue = ThreadSafeRandom.Next(omnitoolManufacturers.Length);
                        manf = omnitoolManufacturers[randvalue];
                        Console.WriteLine("Character Randomizer OMNITOOL [" + row + "][29] value is now " + manf);
                        character2da[row, 29].IntValue = manf;
                        hasChanges = true;
                    }
                }
            }

            if (character2da.IsModified)
            {
                Debug.WriteLine("Writing Character_Character to export");
                character2da.Write2DAToExport(export);
            }

            return true;
        }
    }
}
