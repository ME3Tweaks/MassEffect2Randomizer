using System.Linq;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game2.Misc
{
    class PawnAI
    {
        /// <summary>
        /// The list of AIs that can be swapped out.
        /// </summary>
        private static string[] AllowedAIClasses =
        {
            //"SFXAI_Aggressive", // Not used in ME2
            //"SFXAI_Defensive" // Not used in ME2
            "SFXAI_Humanoid",
            "SFXAI_Vanguard",
        };

        private static bool CanRandomize(ExportEntry export, out EAIFuncType funcType)
        {
            // C# weird restrictions
            var calcfuncType = EAIFuncType.FUNCTYPE_NOTALLOWED;
            var cr = !export.IsDefaultObject && CanRandomizeAI(export, out calcfuncType);
            funcType = calcfuncType;
            return cr;
        }

        /// <summary>
        /// Calculated randomization function
        /// </summary>
        private enum EAIFuncType
        {
            FUNCTYPE_NOTALLOWED,
            /// <summary>
            /// Swapping AIController
            /// </summary>
            FUNCTYPE_AICLASSSWAP,
            /// <summary>
            /// Patching it to randomly nuke YMIR mech
            /// </summary>
            FUNCTYPE_HEAVYMECH_NUKEPOWER
        }

        private static bool CanRandomizeAI(ExportEntry export, out EAIFuncType aiFuncType)
        {
            aiFuncType = EAIFuncType.FUNCTYPE_NOTALLOWED;
            if (export.ClassName == "BioPawnChallengeScaledType")
            {
                if (export.ObjectName.Name.Contains("hench"))
                    return false;
                aiFuncType = EAIFuncType.FUNCTYPE_AICLASSSWAP;
                return true; // This pawn can have it's AI class changed.
            }
            if (export.FullPath == "SFXGamePawns.SFXAI_HeavyMech.ChooseDeathPower")
            {
                aiFuncType = EAIFuncType.FUNCTYPE_HEAVYMECH_NUKEPOWER;
                return true; // We will randomize the death explosion token.
            }
            return false;
        }

        public static bool RandomizeExport(ExportEntry exp, RandomizationOption option)
        {
            if (!CanRandomize(exp, out var randFuncType)) return false;

            if (randFuncType == EAIFuncType.FUNCTYPE_AICLASSSWAP)
            {
                var currentAi = exp.GetProperty<ObjectProperty>("AIController");
                if (currentAi != null && currentAi.Value < 0)
                {
                    // it's an import
                    var aiImp = currentAi.ResolveToEntry(exp.FileRef) as ImportEntry;
                    if (AllowedAIClasses.Contains(aiImp.ObjectName.Name))
                    {
                        // It uses a basic AI, we can change it
                        // We should not change customized AI cause other parts depend on it (e.g. collector)
                        var newAi = AllowedAIClasses.RandomElement();
                        if (newAi != aiImp.ObjectName.Name)
                        {
                            // AI is changing.
                            var sfxgame = NonSharedPackageCache.Cache.GetCachedPackage("SFXGame.pcc");
                            var newAIImp = EntryImporter.GetOrAddCrossImportOrPackageFromGlobalFile(newAi, sfxgame, exp.FileRef);
                            currentAi.Value = newAIImp.UIndex;
                            exp.WriteProperty(currentAi);
                            MERLog.Information($@"AI Changing: {aiImp.FullPath} => {newAi}");
                            return true;
                        }
                    }
                }
            }
            else if (randFuncType == EAIFuncType.FUNCTYPE_HEAVYMECH_NUKEPOWER)
            {
                // Export is ChooseDeathPower. Assuming that the power has not been modified in other ways.
                if (exp.DataSize != 259)
                    return false; // Do not operate on this function if it's modified as we will probably break it.

                var expData = exp.Data;
                if (ThreadSafeRandom.Next(2) == 0)
                {
                    // It's boom time.
                    var nameIdx = expData.Slice(0x9D, 4); //HeavyMech_DeathExplosion
                    expData.OverwriteRange(0xE3, nameIdx);
                    exp.Data = expData;

                    if (ThreadSafeRandom.Next(2) == 0)
                    {
                        // Make it DEADLY
                        var hmnDT = exp.FileRef.FindExport("SFXGameContent_Inventory.Default__SFXDamageType_HeavyMechNuke");
                        if (hmnDT != null)
                        {
                            var props = hmnDT.GetProperties();
                            props.AddOrReplaceProp(new FloatProperty(1, "fChanceOfRagdoll"));
                            props.AddOrReplaceProp(new BoolProperty(true, "bIgnoreShields"));
                            props.AddOrReplaceProp(new BoolProperty(true, "bCausesRagdoll"));
                            props.AddOrReplaceProp(new BoolProperty(true, "bImmediateDeath"));
                            hmnDT.WriteProperties(props);
                        }
                    }
                }
            }

            return false;
        }
    }
}
