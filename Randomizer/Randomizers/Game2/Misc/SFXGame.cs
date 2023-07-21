using System.Collections.Generic;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.Misc
{
    public class SFXGame
    {
        public static IMEPackage GetSFXGame(GameTarget target)
        {
            var sfxgame = Path.Combine(target.TargetPath, "BioGame", "CookedPCConsole", "SFXGame.pcc");
            if (File.Exists(sfxgame))
            {
                return MEPackageHandler.OpenMEPackage(sfxgame);
            }

            return null;
        }


        public static bool MakeShepardRagdollable(GameTarget target, RandomizationOption option)
        {
            var sfxgame = GetSFXGame(target);

            // Add ragdoll power to shep
            var sfxplayercontrollerDefaults = sfxgame.FindExport(@"Default__SFXPlayerController");
            var cac = sfxplayercontrollerDefaults.GetProperty<ArrayProperty<ObjectProperty>>("CustomActionClasses");
            cac[5].Value = sfxgame.FindExport(@"SFXCustomAction_Ragdoll").UIndex; //SFXCustomAction_Ragdoll in this slot
            sfxplayercontrollerDefaults.WriteProperty(cac);

            // Update power script design and patch out player physics level
            var sd = sfxgame.FindExport(@"BioPowerScriptDesign.GetPhysicsLevel");
            ScriptTools.InstallScriptToExport(sd, "GetPhysicsLevel.uc", false, null);

            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public static bool TurnOnFriendlyFire(GameTarget target, RandomizationOption option)
        {
            // Remove the friendly pawn check
            var sfxgame = ScriptTools.InstallScriptToPackage(target, "SFXGame.pcc", "SFXGame.ModifyDamage", "SFXGame.ModifyDamage.uc", false);
            if (option.HasSubOptionSelected(SUBOPTIONKEY_CARELESSFF))
            {
                // Remove the friendly fire check
                ScriptTools.InstallScriptToPackage(sfxgame, "BioAiController.IsFriendlyBlockingFireLine", "IsFriendlyBlockingFireLine.uc", false);
            }
            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        public const string SUBOPTIONKEY_CARELESSFF = "CarelessMode";

        public static bool RandomizeWwiseEvents(GameTarget target, RandomizationOption option)
        {
            var sfxgame = GetSFXGame(target);
            List<ExportEntry> referencedWwiseEvents = new List<ExportEntry>();

            var f = GetAllProperties(sfxgame.FindExport("BioSFResources.GUI_Sound_Mappings").GetProperties());
            // Get all resolved values
            foreach (var exp in sfxgame.Exports)
            {
                var objProps = GetAllProperties(exp.GetProperties()).OfType<ObjectProperty>();
                foreach (var op in objProps)
                {
                    var resolvedValue = op.ResolveToExport(exp.FileRef);
                    if (resolvedValue != null && resolvedValue.ClassName == @"WwiseEvent")
                    {
                        referencedWwiseEvents.Add(resolvedValue);
                    }
                }
            }

            referencedWwiseEvents.Shuffle();

            // Write them back
            foreach (var exp in sfxgame.Exports)
            {
                var propertyCollection = exp.GetProperties();
                bool modified = false;
                var objProps = GetAllProperties(propertyCollection).OfType<ObjectProperty>();
                foreach (var op in objProps)
                {
                    var resolvedValue = op.ResolveToExport(exp.FileRef);
                    if (resolvedValue != null && resolvedValue.ClassName == @"WwiseEvent")
                    {
                        op.Value = referencedWwiseEvents.PullFirstItem().UIndex;
                        modified = true;
                    }
                }

                if (modified)
                    exp.WriteProperties(propertyCollection);
            }


            MERFileSystem.SavePackage(sfxgame);
            return true;
        }

        /// <summary>
        /// Builds an enumeration of all properties. DO NOT ACCESS .Properties on arrays or structs - as they are also added to this list
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
        private static IEnumerable<Property> GetAllProperties(List<Property> collection)
        {
            List<Property> props = new List<Property>();
            props.AddRange(collection);
            foreach (var subProp in collection)
            {
                if (subProp is ArrayPropertyBase apb)
                {
                    props.AddRange(GetAllProperties(apb.Properties.ToList()));
                }
                else if (subProp is StructProperty sp)
                {
                    props.AddRange(GetAllProperties(sp.Properties.ToList()));
                }
                else
                {
                }
            }

            return props;
        }
    }
}
