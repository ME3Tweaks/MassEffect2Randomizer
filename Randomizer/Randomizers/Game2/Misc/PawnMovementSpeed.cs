using System.IO;
using Randomizer.Randomizers.Game2.Coalesced;

namespace Randomizer.Randomizers.Game2.Misc
{
    public class PawnMovementSpeed
    {
        public static bool RandomizePlayerMovementSpeed(RandomizationOption option)
        {
            var femaleFile = MERFileSystem.GetPackageFile("BIOG_Female_Player_C.pcc");
            var maleFile = MERFileSystem.GetPackageFile("BIOG_Male_Player_C.pcc");
            var femalepackage = MEPackageHandler.OpenMEPackage(femaleFile);
            var malepackage = MEPackageHandler.OpenMEPackage(maleFile);
            SlightlyRandomizeMovementData(femalepackage.GetUExport(2917));
            SlightlyRandomizeMovementData(malepackage.GetUExport(2672));
            MERFileSystem.SavePackage(femalepackage);
            MERFileSystem.SavePackage(malepackage);

            var biogame = CoalescedHandler.GetIniFile("BIOGame.ini");
            var sfxgame = biogame.GetOrAddSection("SFXGame.SFXGame");
            sfxgame.SetSingleEntry("StormStamina", ThreadSafeRandom.NextFloat(1.5f, 12));
            sfxgame.SetSingleEntry("StormRegen", ThreadSafeRandom.NextFloat(0.3f, 1.5f));
            sfxgame.SetSingleEntry("StormStaminaNonCombat", ThreadSafeRandom.NextFloat(1.5f, 8));
            sfxgame.SetSingleEntry("StormRegenNonCombat", ThreadSafeRandom.NextFloat(0.1f, 0.8f));
            return true;
        }

        private static void SlightlyRandomizeMovementData(ExportEntry export)
        {
            var props = export.GetProperties();
            foreach (var prop in props)
            {
                if (prop is FloatProperty fp)
                {
                    // We try to make sure it weights more towards faster not slower
                    fp.Value = ThreadSafeRandom.NextFloat(fp.Value - (fp * .35), fp.Value + (fp * .75));
                }

            }
            export.WriteProperties(props);
        }


        private static bool CanRandomizeNPCSpeed(ExportEntry export)
        {
            // No defaults, must be appr character class
            if (export.IsDefaultObject || export.ClassName != "Bio_Appr_Character") return false;

            // Do not randomize joker as this may lead to softlock in NorCR3
            if (export.ObjectName.Name == "JKR_Movement2DA") return false;

            // Cannot randomize files in BIOG and cannot randomize ProNor (which has player speed)
            var filename = Path.GetFileName(export.FileRef.FilePath);
            if (filename.StartsWith("BIOG_") || filename == "BioD_ProNor.pcc") return false;

            return true;
        }

        public static bool RandomizeMovementSpeed(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomizeNPCSpeed(export)) return false;
            var movementInfo = export.GetProperty<ObjectProperty>("MovementInfo");
            if (movementInfo != null)
            {
                // Generate a newized movement speed. This will result in some duplicate data (since some may already exist in the local file) 
                // But it will be way less complicated to just add new ones

                //MERLog.Information($@"Randomizing movement speed for {export.UIndex}");
                movementInfo.Value = AddNewRandomizedMovementSpeed(export);
                export.WriteProperty(movementInfo);
            }
            return true;
        }

        private static int AddNewRandomizedMovementSpeed(ExportEntry bio_appr_character)
        {
            ImportEntry sfxMovementData = bio_appr_character.FileRef.FindImport("SFXGame.SFXMovementData");
            if (sfxMovementData == null)
            {
                // Import needs added

                // ME2 SPECIFIC!
                sfxMovementData = EntryImporter.GetOrAddCrossImportOrPackageFromGlobalFile("SFXMovementData", MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile("SFXGame.pcc")), bio_appr_character.FileRef) as ImportEntry;
            }

            PropertyCollection props = new PropertyCollection();
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(70, 210) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "WalkSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(200, 700) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "GroundSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(50, 900) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "TurnSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(100, 320) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "CombatWalkSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(10, 470) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "CombatGroundSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(100, 380) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "CoverGroundSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(60, 180) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "CoverCrouchGroundSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(300, 900) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "StormSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(25, 75) + (ThreadSafeRandom.Next(10) == 0 ? 20 : 0), "StormTurnSpeed"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(250, 1250) + (ThreadSafeRandom.Next(10) == 0 ? 100 : 0), "AccelRate"));

            var export = new ExportEntry(bio_appr_character.FileRef, null, new NameReference("ME2RMovementData", ThreadSafeRandom.Next(200000)), properties: props)
            {
                Class = sfxMovementData,
                idxLink = bio_appr_character.FileRef.Exports.First(x => x.ClassName == "Package").UIndex,
            };

            bio_appr_character.FileRef.AddExport(export);
            return export.UIndex;
        }
    }
}
