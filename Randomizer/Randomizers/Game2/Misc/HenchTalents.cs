using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using PropertyChanged;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.Talents;
using Randomizer.Randomizers.Handlers;
using Randomizer.Randomizers.Utility;

namespace Randomizer.Randomizers.Game2.Misc
{
    public class HenchTalents
    {
        public const string SUBOPTION_HENCHPOWERS_REMOVEGATING = "SUBOPTION_HENCHPOWERS_REMOVEGATING";

        public static bool ShuffleSquadmateAbilitiesLE2(GameTarget target, RandomizationOption option)
        {
            bool removeRank2Gating = option.HasSubOptionSelected(SUBOPTION_HENCHPOWERS_REMOVEGATING);

            option.CurrentOperation = "Building new henchmen loadouts";
            option.ProgressValue = 0;
            option.ProgressIndeterminate = true;

            // Load the startup override package from assets for filling out
            var loadoutPackage = MEREmbedded.GetEmbeddedPackage(MEGame.LE2, @"Powers.Startup_LE2R_HenchLoadouts.pcc");
            using var loadoutPackageP =
                MEPackageHandler.OpenMEPackageFromStream(loadoutPackage, @"Startup_LE2R_HenchLoadouts.pcc");

            var allPowers = MEREmbedded.GetEmbeddedPackage(MEGame.LE2, @"AllPowers.pcc");
            using var allPowersP = MEPackageHandler.OpenMEPackageFromStream(allPowers, @"AllPowers.pcc");

            var henchBasePowerIFPs = MEREmbedded.GetEmbeddedTextAsset(@"Powers.BasePowerIFPs.txt").SplitToLines()
                .Where(x => !x.StartsWith(@"//")).ToList();

            List<HTalent> talentPoolMaster = new List<HTalent>(); // The base powers list (should be half the size of evolvedTalentPoolMaster)
            List<HTalent> evolvedTalentPoolMaster = new List<HTalent>(); // The master list (DO NOT EDIT) of all rank 4 powers
            ConcurrentBag<string> passiveStrs = new ConcurrentBag<string>(); // The list of passive strings, resolved via TLK

            foreach (var hb in henchBasePowerIFPs)
            {
                var htalent = new HTalent(allPowersP.FindExport(hb));
                talentPoolMaster.Add(htalent);
                if (htalent.HasEvolution())
                {
                    evolvedTalentPoolMaster.AddRange(htalent.GetEvolutions());
                }

                if (htalent.IsPassive)
                {
                    passiveStrs.Add(htalent.PassiveDescriptionString);
                    passiveStrs.Add(htalent.PassiveTalentDescriptionString);
                    passiveStrs.Add(htalent.PassiveRankDescriptionString);
                }
            }

            var allPS = passiveStrs.Distinct().ToList();
            foreach (var s in allPS)
            {
                Debug.WriteLine(s);
            }

            // Step 3. Precalculate talent sets that will be assigned
#if DEBUG
            ThreadSafeRandom.SetSeed(132512);
#endif
            #region Find compatible power sets

            // Populate the hench loadouts
            option.ProgressIndeterminate = true;
            var henchLoadouts = new List<HenchLoadoutInfo>(); // The resulting loadouts
            foreach (var loadout in loadoutPackageP.Exports.Where(x => x.ClassName == @"SFXPlayerSquadLoadoutData"))
            {
                henchLoadouts.Add(new HenchLoadoutInfo() { LoadoutIFP = loadout.InstancedFullPath });
            }

            int baseAttempt = 0;
            bool powersConfigured = false; //if a solution was found
            while (!powersConfigured)
            {
                // Reset
                Debug.WriteLine($"Assigning hench base talent sets, attempt #{baseAttempt++}");
                var basePowerPool = talentPoolMaster.ToList();
                basePowerPool.Shuffle();

                // Old code reset the henchtalentset to null

                // Attempt to build a list of compatible base powers
                bool powerSetsValid = true;
                foreach (var loadout in henchLoadouts)
                {
                    if (!loadout.BuildTalentSet(basePowerPool) || loadout.HenchTalentSet == null ||
                        !loadout.HenchTalentSet.IsBaseValid)
                    {
                        powerSetsValid = false;
                        break; // Try again, this is not a valid solution to the problem
                    }
                }

                if (!powerSetsValid)
                {
                    continue; // try again
                }


                // We have configured a set of compatible base powers
                // There should be a configuration of evolutions that works,
                // but we will only attempt 5 ways
                bool configuredEvolutions = false;
                int evolutionAttempt = 0;
                while (!configuredEvolutions)
                {
                    // Reset
                    Debug.WriteLine($"Assigning evolution talent sets, attempt #{baseAttempt - 1}-{evolutionAttempt++}");
                    var assignmentEvolutions = evolvedTalentPoolMaster.ToList();
                    foreach (var hpi in henchLoadouts)
                    {
                        // hpi.ResetTalents();
                        hpi.ResetEvolutions();
                    }

                    // Attempt solution
                    assignmentEvolutions.Shuffle(); // reshuffle for new attempt
                    bool foundSolution = true;
                    foreach (var loadout in henchLoadouts)
                    {
                        foundSolution &= loadout.BuildEvolvedTalentSet(assignmentEvolutions);
                        if (!foundSolution)
                            break; //short circuit search for solution
                    }

                    if (!foundSolution)
                    {
                        if (evolutionAttempt > 4)
                            break; // Force full retry
                        continue; // Try again, a solution was not found
                    }

                    configuredEvolutions = true;
                }

                if (!configuredEvolutions)
                    continue; // Do a full retry
                powersConfigured = true;

                // Print results
                MERLog.Information(
                    $@"Found a power solution for basegame powers on henchmen in {baseAttempt} attempts:");
                foreach (var loadout in henchLoadouts)
                {
                    MERLog.Information($"{loadout.LoadoutIFP}-----");
                    foreach (var pow in loadout.HenchTalentSet.Powers)
                    {
                        MERLog.Information($" - BP {pow.PowerName} ({pow.BaseName})");
                    }

                    foreach (var pow in loadout.HenchTalentSet.EvolvedPowers)
                    {
                        MERLog.Information($" - EP {pow.PowerName} ({pow.BaseName})");
                    }
                }
            }

            #endregion

            // Step 4. Assign and map powers
            foreach (var loadout in henchLoadouts)
            {
                loadout.OrderBasePowers();
            }

            #region Install powers and evolutions

            option.CurrentOperation = "Installing randomized henchmen powers";
            option.ProgressValue = 0;
            option.ProgressIndeterminate = false;
            option.ProgressMax = henchLoadouts.Sum(x => x.NumPowersToAssign);
            foreach (var henchInfo in henchLoadouts)
            {
                object tlkSync = new object();
                MERLog.Information($"Installing talent set for {henchInfo.HenchUIName}");
                option.CurrentOperation = $"Installing randomized powers for {henchInfo.HenchUIName}";
                // henchInfo.SetPowersToUniqueNames();
                // We force a large GC here cause this loop can make like 7GB, I'll take the timing hit 
                // to reduce memory usage
                //GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                //GC.Collect();

                ////foreach (var loadout in assets)
                ////{
                //// We must load a new copy of the package file into memory, one for reading, one for modifying,
                //// otherwise it will be concurrent modification
                ////if (!package.FilePath.Contains("Thief"))
                ////      return;
                //if (package == null)
                //    Debugger.Break();
                var loadout = loadoutPackageP.FindExport(henchInfo.LoadoutIFP);
                if (loadout != null)
                {
                    List<MappedPower> configuredPowers = new List<MappedPower>();

                    var loadoutProps = loadout.GetProperties();
                    var powersList = loadoutProps.GetProp<ArrayProperty<ObjectProperty>>("Powers");

                    //var originalCount = powersList.Count;
                    //var powersToKeep = powersList.Where(x => !CanBeShuffled(x.ResolveToEntry(package) as ExportEntry, out _)).ToList();
                    //powersList.Clear();

                    var talentSet = henchInfo.HenchTalentSet;

                    // Assign base powers
                    int powIndex = 0;
                    // Lists for updating the class description in the character creator
                    foreach (var talentSetBasePower in talentSet.Powers)
                    {
                        var savedProperties = talentSetBasePower.PowerExport.GetDefaults().GetProperties();
                        var newProperties = talentSetBasePower.PowerExport.GetDefaults().GetProperties();
                        newProperties.RemoveNamedProperty(@"EvolvedPowerClass1"); // Do not bring over evos in this round
                        newProperties.RemoveNamedProperty(@"EvolvedPowerClass2"); // Do not bring over evos in this round
                        var portedPower = PackageTools.PortExportIntoPackage(target, loadout.FileRef,
                            talentSetBasePower.PowerExport);
                        powersList.Add(new ObjectProperty(portedPower.UIndex));

                        talentSetBasePower.PowerExport.GetDefaults().WriteProperties(savedProperties); // restore properties
                        option.IncrementProgressValue();
                        // For each power, change the evolutions
                        var defaults = portedPower.GetDefaults();
                        var props = defaults.GetProperties();
                        var evolution1 = talentSet.EvolvedPowers[powIndex * 2];
                        var evolution2 = talentSet.EvolvedPowers[(powIndex * 2) + 1];
                        configuredPowers.Add(new MappedPower()
                        {
                            BaseTalent = talentSetBasePower,
                            EvolvedTalent1 = evolution1,
                            EvolvedTalent2 = evolution2
                        });
                        if (portedPower.FileRef.FindExport(evolution1.PowerExport.InstancedFullPath) != null)
                        {
                            evolution1.SetUniqueName(henchInfo.HenchUIName);
                        }

                        if (portedPower.FileRef.FindExport(evolution2.PowerExport.InstancedFullPath) != null)
                        {
                            evolution2.SetUniqueName(henchInfo.HenchUIName);
                        }
                        var evo1 = PackageTools.PortExportIntoPackage(target, portedPower.FileRef,
                            evolution1.PowerExport);
                        var evo2 = PackageTools.PortExportIntoPackage(target, portedPower.FileRef,
                            evolution2.PowerExport);

                        evolution1.ResetSourcePowerName();
                        evolution2.ResetSourcePowerName();

                        evo1.GetDefaults().WriteProperty(new ArrayProperty<StructProperty>("UnlockRequirements"));
                        evo2.GetDefaults().WriteProperty(new ArrayProperty<StructProperty>("UnlockRequirements"));
                        props.AddOrReplaceProp(new ObjectProperty(evo1, "EvolvedPowerClass1"));
                        props.AddOrReplaceProp(new ObjectProperty(evo2, "EvolvedPowerClass2"));

                        // Update the evolution text via override
                        var ranksSource = talentSetBasePower.CondensedProperties.GetProp<ArrayProperty<StructProperty>>("Ranks");
                        if (ranksSource == null)
                        {
                            Debugger.Break();
                            // Projectile powers are subclassed for player
                            //ranksSource = (basePower.BasePower.SuperClass as ExportEntry).GetDefaults().GetProperty<ArrayProperty<StructProperty>>("Ranks");
                        }

                        #region Ranks text change

                        lock (tlkSync)
                        {
                            // Update passive strings for ranks 1/2/3
                            if (talentSetBasePower.IsPassive)
                            {
                                int i = 0;
                                var newStringID = TLKBuilder.GetNewTLKID();
                                string rankDescription = null;
                                while (i <= 2)
                                {
                                    var rankDescriptionProp = ranksSource[i].Properties.GetProp<StringRefProperty>("Description");
                                    // UnlockBlurb is not used here.
                                    if (rankDescription == null)
                                    {
                                        rankDescription = henchInfo.GenderizeString(talentSetBasePower.PassiveRankDescriptionString);
                                        TLKBuilder.ReplaceString(newStringID, rankDescription);
                                    }

                                    rankDescriptionProp.Value = newStringID; // written below by addreplaceRanksSource
                                    //rankUnlockBlurb.Value = newStringID;
                                    i++;
                                }
                            }

                            // Update Rank 4 unevolved text
                            {
                                var evolveRank = ranksSource[3];
                                var descriptionProp = evolveRank.Properties.GetProp<StringRefProperty>("Description");
                                var rankUnlockBlurb = evolveRank.Properties.GetProp<StringRefProperty>("UnlockBlurb");

                                if (!TLKBuilder.IsAssignedMERString(descriptionProp.Value))
                                {
                                    var description =
                                        TLKBuilder.TLKLookupByLang(descriptionProp.Value, MELocalization.INT);
                                    var descriptionLines = description.Split('\n');
                                    descriptionLines[2] = $"1) {henchInfo.GenderizeString(evolution1.EvolvedBlurb)}";
                                    descriptionLines[4] = $"2) {henchInfo.GenderizeString(evolution2.EvolvedBlurb)}";
                                    var newStringID = TLKBuilder.GetNewTLKID();
                                    TLKBuilder.ReplaceString(newStringID, string.Join('\n', descriptionLines));
                                    descriptionProp.Value = newStringID; // written below by addreplaceRanksSource
                                    rankUnlockBlurb.Value = newStringID; // written below by addreplaceRanksSource
                                }
                            }
                        }

                        props.AddOrReplaceProp(
                            ranksSource); // copy the source rank info into our power with the modification

                        #endregion

                        #region Passives text changes (non-ranks)

                        if (talentSetBasePower.IsPassive)
                        {
                            lock (tlkSync)
                            {
                                // Talent Description
                                var talentDescriptionProp =
                                    talentSetBasePower.CondensedProperties.GetProp<StringRefProperty>(
                                        "TalentDescription");
                                if (!TLKBuilder.IsAssignedMERString(talentDescriptionProp.Value))
                                {
                                    var newStringID = TLKBuilder.GetNewTLKID();
                                    TLKBuilder.ReplaceString(newStringID,
                                        henchInfo.GenderizeString(talentSetBasePower.PassiveTalentDescriptionString));
                                    talentDescriptionProp.Value = newStringID;
                                }

                                props.AddOrReplaceProp(talentDescriptionProp);

                                var descriptionProp =
                                    talentSetBasePower.CondensedProperties.GetProp<StringRefProperty>("Description");
                                if (!TLKBuilder.IsAssignedMERString(descriptionProp.Value))
                                {
                                    var newStringID = TLKBuilder.GetNewTLKID();
                                    TLKBuilder.ReplaceString(newStringID,
                                        henchInfo.GenderizeString(talentSetBasePower.PassiveDescriptionString));
                                    descriptionProp.Value = newStringID;
                                }

                                props.AddOrReplaceProp(descriptionProp);
                            }
                        }

                        #endregion

                        defaults.WriteProperties(props);
                        powIndex++;
                    }

                    // Add passives back
                    //List<ObjectProperty> appendAtEndItems = new List<ObjectProperty>();
                    //foreach (var ptk in powersToKeep)
                    //{
                    //    var export = ptk.ResolveToEntry(package) as ExportEntry;
                    //    var talent = new HTalent(export, false, isFixedPower: true);
                    //    if (talent.ShowInCR)
                    //    {
                    //        powersList.Add(ptk);
                    //    }
                    //    else
                    //    {
                    //        appendAtEndItems.Add(ptk);
                    //    }
                    //}

                    //powersList.AddRange(appendAtEndItems);

                    // Order the powers
                    // Write loadout data
                    //if (powersList.Count != originalCount)
                    //{
                    //    Debugger.Break();
                    //}

                    loadoutProps.AddOrReplaceProp(powersList);

                    // Build the autoranks
                    //foreach (var fixedPower in .FixedPowers.Where(x => x.ShowInCR))
                    //{
                    //    // Add fixed powers evo information
                    //    var mp = new MappedPower() { BaseTalent = fixedPower };
                    //    var evo1c = fixedPower.CondensedProperties.GetProp<ObjectProperty>("EvolvedPowerClass1")
                    //        .ResolveToEntry(fixedPower.PowerExport.FileRef) as ExportEntry;
                    //    var evo2c = fixedPower.CondensedProperties.GetProp<ObjectProperty>("EvolvedPowerClass2")
                    //        .ResolveToEntry(fixedPower.PowerExport.FileRef) as ExportEntry;
                    //    mp.EvolvedTalent1 = new HTalent(evo1c, true);
                    //    mp.EvolvedTalent2 = new HTalent(evo2c, true);
                    //    configuredPowers.Add(mp);
                    //}

                    loadoutProps.AddOrReplaceProp(BuildAutoRankList(loadout, configuredPowers));

                    // Finalize loadout export
                    loadout.WriteProperties(loadoutProps);

                    // Correct the unlock criteria to ensure every power can be unlocked. Special stuff for Kenson and Liara. Don't care about Wilson, sorry dude.
                    for (int i = 0; i < talentSet.Powers.Count; i++)
                    {
                        // i = Power Index (0 indexed)

                        // Power dependency map:
                        // Power 1: Rank 1
                        // Power 2: Depends on Power 1 Rank 1 UNLESS Miranda or Jacob who have this power at rank 1 already
                        // Power 3: Rank 0
                        // Power 4: Locked by Loyalty

                        var powerExport = loadout.FileRef.FindExport(talentSet.Powers[i].PowerExport.InstancedFullPath);
                        var defaults = powerExport.GetDefaults();

                        var properties = defaults.GetProperties();
                        properties.RemoveNamedProperty("Rank");
                        if (properties.RemoveNamedProperty("UnlockRequirements"))
                        {
                            Debug.WriteLine("T");
                        }

                        // All squadmates have a piont in Slot 0 by default.
                        // Miranda and Jacob have a point in slot 1
                        if (henchInfo.HenchUIName == "Kenson")
                        {
                            // Since you can't power up kenson we'll just give her points in all her powers
                            properties.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.Next(2) + 1, "Rank"));
                        }
                        // VANILLA HENCH CODE
                        else if (i == 0 || (i == 1 && henchInfo.HenchUIName == "Miranda" || henchInfo.HenchUIName == "Jacob"))
                        {
                            properties.Add(new FloatProperty(1, "Rank"));
                        }
                        else if (i == 1)
                        {
                            if (removeRank2Gating)
                            {
                                // Rank 0
                                properties.AddOrReplaceProp(new FloatProperty(0, "Rank"));
                            }
                            else
                            {
                                // Has unlock dependency on the prior slotted item
                                var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                                dependencies.AddRange(GetUnlockRequirementsForPower(
                                    loadout.FileRef.FindExport(talentSet.Powers[i - 1].PowerExport.InstancedFullPath),
                                    false));
                                properties.AddOrReplaceProp(dependencies);
                            }
                        }
                        else if (i == 3 && henchInfo.HenchUIName != "Liara")
                        {
                            // Has unlock dependency on loyalty power
                            // Has unlock dependency on the prior slotted item
                            var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                            dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport("SFXGameContent_Powers.SFXPower_LoyaltyRequirement"), true));
                            properties.AddOrReplaceProp(dependencies);
                        }

                        defaults.WriteProperties(properties);
                    }
                }
                henchInfo.ResetSourcePowerNames();

            }

            #endregion

            // Save the new startup package
            MERFileSystem.SavePackage(loadoutPackageP);

            // Add the loadouts as a startup package to force overrides
            ThreadSafeDLCStartupPackage.AddStartupPackage(@"Startup_LE2R_HenchLoadouts");

            // Patch the initialize function for henchmen to refund any lost points
            ScriptTools.InstallScriptToPackage(target, @"SFXGame.pcc", "SFXPawn_Henchman.InitializeHenchman",
                "InitializeHenchman.uc", false, true);

            // Patch the game tutorial to prevent softlock
            PatchOutTutorials(target);

            return true;
        }

        // Old code for ME2R
        public static bool ShuffleSquadmateAbilities(GameTarget target, RandomizationOption option)
        {
#if OLDCODE
            bool removeRank2Gating = option.HasSubOptionSelected(SUBOPTION_HENCHPOWERS_REMOVEGATING);

            PatchOutTutorials(target);

            var henchCache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true);

            // We can have up to 5 UI powers assigned. However, kits will only have 4 powers total assigned as each pawn only has 4 in vanilla gameplay.
            var squadmatePackageMap = new CaseInsensitiveConcurrentDictionary<HenchInfo>();

            #region Build squadmate sets
            option.CurrentOperation = "Inventorying henchmen powers";
            var henchFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioH_") && !x.Contains("_LOC_") && !x.Contains("END") && x != "BioH_SelectGUI" && (x.Contains("00")) || x.Contains("BioH_Wilson") || NonBioHHenchmenFiles.Contains(x)).ToList();


            int numDone = 0;
            option.ProgressMax = henchFiles.Count;

#if DEBUG
            ThreadSafeRandom.SetSeed(132512);
#endif
            Parallel.ForEach(henchFiles, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, h =>
                {
                    //foreach (var h in henchFiles)
                    //{

                    // Debug only
#if DEBUG
                    if (true
                        && !h.Contains("mystic", StringComparison.InvariantCultureIgnoreCase)
                        && !h.Contains("assassin", StringComparison.InvariantCultureIgnoreCase)
                        && !h.Contains("liara", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return;
                    }
#endif

                    HenchInfo hpi = new HenchInfo();

                    string internalName;
                    if (h == "BioH_Wilson.pcc")
                    {
                        hpi.HenchInternalName = "wilson";
                    }
                    else if (h == "BioD_ArvLvl1.pcc")
                    {
                        hpi.LoadoutHint = "hench_Kenson";
                        hpi.HenchInternalName = "kenson";
                    }
                    else if (h.StartsWith("BioH_"))
                    {
                        internalName = h.Substring(5); // Remove BioH_
                        internalName = internalName.Substring(0, internalName.IndexOf("_", StringComparison.InvariantCultureIgnoreCase)); // "Assassin"
                        hpi.HenchInternalName = internalName;

                        // These pawns appear embedded in existing files
                        if (h.Contains("leading", StringComparison.InvariantCultureIgnoreCase))
                        {
                            hpi.LoadoutHint = "hench_Jacob";
                        }
                        else if (h.Contains("leading", StringComparison.InvariantCultureIgnoreCase))
                        {
                            hpi.LoadoutHint = "hench_Miranda";
                        }
                    }
                    else
                    {
                        // Custom other
                    }

                    LoadSquadmatePackages(hpi, h, squadmatePackageMap, henchCache);

                    Interlocked.Increment(ref numDone);
                    option.ProgressValue = numDone;
                    option.ProgressIndeterminate = false;

                    //}
                });
            #endregion

            #region Build power pool
            List<HTalent> talentPoolMaster = new List<HTalent>();
            List<HTalent> evolvedTalentPoolMaster = new List<HTalent>();
            numDone = 0;
            option.ProgressValue = 0;
            option.CurrentOperation = "Building henchmen power sets";
            ConcurrentBag<string> passiveStrs = new ConcurrentBag<string>();
            Parallel.ForEach(squadmatePackageMap, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, henchInfo =>
                {
                    var henchP = henchInfo.Value.Packages[0];
                    var loadouts = henchP.Exports.Where(x => x.ClassName == "SFXPlayerSquadLoadoutData").ToList();
                    foreach (var loadout in loadouts)
                    {
                        if (henchInfo.Value.LoadoutHint != null)
                        {
                            // HenchInfo has hint to indicate only select this named loadout
                            if (loadout.ObjectName != henchInfo.Value.LoadoutHint)
                                return; // Don't operate on this one
                        }


                        HenchhenchInfo hli = new HenchLoadoutInfo() { LoadoutIFP = loadout.InstancedFullPath };
                        henchInfo.Value.PackageLoadouts.Add(hli);
                        if (henchInfo.Key == "wilson")
                        {
                            // Wilson only has 1 power.
                            hli.NumPowersToAssign = 1;
                        }

                        // Inventory the powers
                        var powers = loadout.GetProperty<ArrayProperty<ObjectProperty>>("Powers").Select(x => x.ResolveToEntry(loadout.FileRef) as ExportEntry).ToList();
                        int numContributed = 0;
                        foreach (var p in powers)
                        {
                            if (CanBeShuffled(p, out var usi))
                            {
                                if (hli.LoadoutIFP == "BioChar_Loadouts.Henchmen.hench_Morinth")
                                {
                                    p.CondenseArchetypes(); // makes porting work better by removing Samara's parent power which can be used
                                }

                                var htalent = new HTalent(p);
                                if (htalent.IsPassive)
                                {
                                    passiveStrs.Add(htalent.PassiveDescriptionString);
                                    passiveStrs.Add(htalent.PassiveTalentDescriptionString);
                                    passiveStrs.Add(htalent.PassiveRankDescriptionString);
                                }

                                talentPoolMaster.Add(htalent);

                                var evolutions = htalent.GetEvolutions();
                                evolvedTalentPoolMaster.AddRange(evolutions);
                                numContributed++;
                            }
                            else if (usi.Pawn != null && usi.Pawn.Equals(henchInfo.Key, StringComparison.InvariantCultureIgnoreCase))
                            {
                                hli.FixedPowers.Add(new HTalent(p, isFixedPower: true));

                                if (usi.CountsTowardsTalentCount)
                                {
                                    hli.NumPowersToAssign--;
                                }
                            }
                        }

                        Debug.WriteLine($"{hli.LoadoutIFP} contributed {numContributed} powers");
                        Interlocked.Increment(ref numDone);
                        option.ProgressValue = numDone;
                        //}
                    }
                });
            #endregion

            var allPS = passiveStrs.Distinct().ToList();
            foreach (var s in allPS)
            {
                Debug.WriteLine(s);
            }

            // Step 3. Precalculate talent sets that will be assigned
            #region Find compatible power sets
            int baseAttempt = 0;
            bool powersConfigured = false; //if a solution was found
            while (!powersConfigured)
            {
                // Reset
                Debug.WriteLine($"Assigning hench base talent sets, attempt #{baseAttempt++}");
                var basePowerPool = talentPoolMaster.ToList();
                basePowerPool.Shuffle();

                foreach (var hpi in squadmatePackageMap.Values)
                {
                    hpi.ResetTalents();
                }

                // Attempt to build a list of compatible base powers
                bool powerSetsValid = true;
                foreach (var hpi in squadmatePackageMap.Values)
                {
                    foreach (var loadout in hpi.PackageLoadouts)
                    {
                        if (!loadout.BuildTalentSet(basePowerPool) || loadout.HenchTalentSet == null || !loadout.HenchTalentSet.IsBaseValid)
                        {
                            powerSetsValid = false;
                            break; // Try again, this is not a valid solution to the problem
                        }
                    }

                    if (!powerSetsValid)
                    {
                        break;
                    }
                    // Valid set found
                    //Debug.WriteLine($@"Assigned powers for {hpi.HenchInternalName}. Powers remaining: {basePowerPool.Count}");
                }

                if (!powerSetsValid)
                {
                    continue; // try again
                }


                // We have configured a set of compatible base powers
                // There should be a configuration of evolutions that works,
                // but we will only attempt 5 ways
                bool configuredEvolutions = false;
                int evolutionAttempt = 0;
                while (!configuredEvolutions)
                {
                    // Reset
                    Debug.WriteLine($"Assigning evolution talent sets, attempt #{baseAttempt - 1}-{evolutionAttempt++}");
                    var assignmentEvolutions = evolvedTalentPoolMaster.ToList();
                    foreach (var hpi in squadmatePackageMap)
                    {
                        hpi.Value.ResetEvolutions();
                    }

                    // Attempt solution
                    assignmentEvolutions.Shuffle(); // reshuffle for new attempt
                    bool foundSolution = true;
                    foreach (var hpi in squadmatePackageMap.Values)
                    {
                        foreach (var loadout in hpi.PackageLoadouts)
                        {
                            foundSolution &= loadout.BuildEvolvedTalentSet(assignmentEvolutions);
                            if (!foundSolution)
                                break; //short circuit search for solution
                        }
                        if (!foundSolution)
                            break; //short circuit search for solution
                    }

                    if (!foundSolution)
                    {
                        if (evolutionAttempt > 4)
                            break; // Force full retry
                        continue; // Try again, a solution was not found
                    }

                    configuredEvolutions = true;
                }

                if (!configuredEvolutions)
                    continue; // Do a full retry
                powersConfigured = true;

                // Print results
                MERLog.Information($@"Found a power solution for basegame powers on henchmen in {baseAttempt} attempts:");
                foreach (var hpi in squadmatePackageMap.Values)
                {
                    foreach (var loadout in hpi.PackageLoadouts)
                    {
                        MERLog.Information($"{loadout.LoadoutIFP}-----");
                        foreach (var pow in loadout.HenchTalentSet.Powers)
                        {
                            MERLog.Information($" - BP {pow.PowerName} ({pow.BaseName})");
                        }

                        foreach (var pow in loadout.HenchTalentSet.EvolvedPowers)
                        {
                            MERLog.Information($" - EP {pow.PowerName} ({pow.BaseName})");
                        }
                    }
                }
            }
            #endregion

            // Step 4. Assign and map powers
            foreach (var pow in squadmatePackageMap.Values)
            {
                foreach (var loadout in pow.PackageLoadouts)
                {
                    loadout.OrderBasePowers();
                }
            }

            #region Install powers and evolutions
            option.CurrentOperation = "Installing randomized henchmen powers";
            option.ProgressValue = 0;
            option.ProgressMax = squadmatePackageMap.Values.Sum(x => x.Packages.Count);
            numDone = 0;
            foreach (var hpi in squadmatePackageMap.Values)
            {
                object tlkSync = new object();
                MERLog.Information($"Installing talent set for {hpi.HenchInternalName}");
                option.CurrentOperation = $"Installing randomized powers for {hpi.PackageLoadouts[0].HenchUIName}";
                // We force a large GC here cause this loop can make like 7GB, I'll take the timing hit 
                // to reduce memory usage
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect();

                Parallel.ForEach(hpi.Packages, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, package =>
                  {
                      //foreach (var loadout in assets)
                      //{
                      // We must load a new copy of the package file into memory, one for reading, one for modifying,
                      // otherwise it will be concurrent modification
                      //if (!package.FilePath.Contains("Thief"))
                      //      return;
                      if (package == null)
                          Debugger.Break();
                      foreach (var loadoutInfo in hpi.PackageLoadouts)
                      {
                          var loadout = package.FindExport(loadoutInfo.LoadoutIFP);

                          if (loadout != null)
                          {
                              List<MappedPower> configuredPowers = new List<MappedPower>();

                              var loadoutProps = loadout.GetProperties();
                              var powersList = loadoutProps.GetProp<ArrayProperty<ObjectProperty>>("Powers");

                              var originalCount = powersList.Count;
                              var powersToKeep = powersList.Where(x => !CanBeShuffled(x.ResolveToEntry(package) as ExportEntry, out _)).ToList();
                              powersList.Clear();

                              var talentSet = loadoutInfo.HenchTalentSet;

                              // Assign base powers
                              int powIndex = 0;
                              // Lists for updating the class description in the character creator
                              foreach (var talentSetBasePower in talentSet.Powers)
                              {
                                  if (loadoutInfo.FixedPowers.Contains(talentSetBasePower))
                                      continue; // Do not modify this
                                  var portedPower = PackageTools.PortExportIntoPackage(target, loadout.FileRef, talentSetBasePower.PowerExport);
                                  powersList.Add(new ObjectProperty(portedPower.UIndex));

                                  // For each power, change the evolutions
                                  var defaults = portedPower.GetDefaults();
                                  var props = defaults.GetProperties();
                                  var evolution1 = talentSet.EvolvedPowers[powIndex * 2];
                                  var evolution2 = talentSet.EvolvedPowers[(powIndex * 2) + 1];
                                  configuredPowers.Add(new MappedPower() { BaseTalent = talentSetBasePower, EvolvedTalent1 = evolution1, EvolvedTalent2 = evolution2 });
                                  var evo1 = PackageTools.PortExportIntoPackage(target, portedPower.FileRef, evolution1.PowerExport);
                                  var evo2 = PackageTools.PortExportIntoPackage(target, portedPower.FileRef, evolution2.PowerExport);
                                  evo1.WriteProperty(new ArrayProperty<StructProperty>("UnlockRequirements"));
                                  evo2.WriteProperty(new ArrayProperty<StructProperty>("UnlockRequirements"));
                                  props.AddOrReplaceProp(new ObjectProperty(evo1, "EvolvedPowerClass1"));
                                  props.AddOrReplaceProp(new ObjectProperty(evo2, "EvolvedPowerClass2"));

                                  // Update the evolution text via override
                                  var ranksSource = talentSetBasePower.CondensedProperties.GetProp<ArrayProperty<StructProperty>>("Ranks");
                                  if (ranksSource == null)
                                  {
                                      Debugger.Break();
                                      // Projectile powers are subclassed for player
                                      //ranksSource = (basePower.BasePower.SuperClass as ExportEntry).GetDefaults().GetProperty<ArrayProperty<StructProperty>>("Ranks");
                                  }

                                  #region Ranks text change
                                  lock (tlkSync)
                                  {
                                      // Update passive strings for ranks 1/2/3
                                      if (talentSetBasePower.IsPassive)
                                      {
                                          int i = 0;
                                          var newStringID = TLKBuilder.GetNewTLKID();
                                          string rankDescription = null;
                                          while (i <= 2)
                                          {
                                              var rankDescriptionProp = ranksSource[i].Properties.GetProp<StringRefProperty>("Description");
                                              if (rankDescription == null)
                                              {
                                                  rankDescription = loadoutInfo.GenderizeString(talentSetBasePower.PassiveRankDescriptionString);
                                                  TLKBuilder.ReplaceString(newStringID, rankDescription);
                                              }
                                              rankDescriptionProp.Value = newStringID; // written below by addreplaceRanksSource
                                              i++;
                                          }
                                      }

                                      // Update Rank 4 unevolved text
                                      {
                                          var evolveRank = ranksSource[3];
                                          var descriptionProp = evolveRank.Properties.GetProp<StringRefProperty>("Description");
                                          if (!TLKBuilder.IsAssignedMERString(descriptionProp.Value))
                                          {
                                              var description = TLKBuilder.TLKLookupByLang(descriptionProp.Value, MELocalization.INT);
                                              var descriptionLines = description.Split('\n');
                                              descriptionLines[2] = $"1) {loadoutInfo.GenderizeString(evolution1.EvolvedBlurb)}";
                                              descriptionLines[4] = $"2) {loadoutInfo.GenderizeString(evolution2.EvolvedBlurb)}";
                                              var newStringID = TLKBuilder.GetNewTLKID();
                                              TLKBuilder.ReplaceString(newStringID, string.Join('\n', descriptionLines));
                                              descriptionProp.Value = newStringID; // written below by addreplaceRanksSource
                                          }
                                      }
                                  }
                                  props.AddOrReplaceProp(ranksSource); // copy the source rank info into our power with the modification
                                  #endregion

                                  #region Passives text changes (non-ranks)
                                  if (talentSetBasePower.IsPassive)
                                  {
                                      lock (tlkSync)
                                      {
                                          // Talent Description
                                          var talentDescriptionProp = talentSetBasePower.CondensedProperties.GetProp<StringRefProperty>("TalentDescription");
                                          if (!TLKBuilder.IsAssignedMERString(talentDescriptionProp.Value))
                                          {
                                              var newStringID = TLKBuilder.GetNewTLKID();
                                              TLKBuilder.ReplaceString(newStringID, loadoutInfo.GenderizeString(talentSetBasePower.PassiveTalentDescriptionString));
                                              talentDescriptionProp.Value = newStringID;
                                          }
                                          props.AddOrReplaceProp(talentDescriptionProp);

                                          var descriptionProp = talentSetBasePower.CondensedProperties.GetProp<StringRefProperty>("Description");
                                          if (!TLKBuilder.IsAssignedMERString(descriptionProp.Value))
                                          {
                                              var newStringID = TLKBuilder.GetNewTLKID();
                                              TLKBuilder.ReplaceString(newStringID, loadoutInfo.GenderizeString(talentSetBasePower.PassiveDescriptionString));
                                              descriptionProp.Value = newStringID;
                                          }
                                          props.AddOrReplaceProp(descriptionProp);
                                      }
                                  }
                                  #endregion

                                  defaults.WriteProperties(props);
                                  powIndex++;
                              }

                              // Add passives back
                              List<ObjectProperty> appendAtEndItems = new List<ObjectProperty>();
                              foreach (var ptk in powersToKeep)
                              {
                                  var export = ptk.ResolveToEntry(package) as ExportEntry;
                                  var talent = new HTalent(export, false, isFixedPower: true);
                                  if (talent.ShowInCR)
                                  {
                                      powersList.Add(ptk);
                                  }
                                  else
                                  {
                                      appendAtEndItems.Add(ptk);
                                  }
                              }

                              powersList.AddRange(appendAtEndItems);

                              // Order the powers
                              // Write loadout data
                              if (powersList.Count != originalCount)
                              {
                                  Debugger.Break();
                              }

                              loadoutProps.AddOrReplaceProp(powersList);

                              // Build the autoranks
                              foreach (var fixedPower in loadoutInfo.FixedPowers.Where(x => x.ShowInCR))
                              {
                                  // Add fixed powers evo information
                                  var mp = new MappedPower() { BaseTalent = fixedPower };
                                  var evo1c = fixedPower.CondensedProperties.GetProp<ObjectProperty>("EvolvedPowerClass1").ResolveToEntry(fixedPower.PowerExport.FileRef) as ExportEntry;
                                  var evo2c = fixedPower.CondensedProperties.GetProp<ObjectProperty>("EvolvedPowerClass2").ResolveToEntry(fixedPower.PowerExport.FileRef) as ExportEntry;
                                  mp.EvolvedTalent1 = new HTalent(evo1c, true);
                                  mp.EvolvedTalent2 = new HTalent(evo2c, true);
                                  configuredPowers.Add(mp);
                              }

                              loadoutProps.AddOrReplaceProp(BuildAutoRankList(loadout, configuredPowers));

                              // Finalize loadout export
                              loadout.WriteProperties(loadoutProps);

                              // Correct the unlock criteria to ensure every power can be unlocked. Special stuff for Kenson and Liara. Don't care about Wilson, sorry dude.
                              for (int i = 0; i < talentSet.Powers.Count; i++)
                              {
                                  // i = Power Index (0 indexed)

                                  // Power dependency map:
                                  // Power 1: Rank 1
                                  // Power 2: Depends on Power 1 Rank 1 UNLESS Miranda or Jacob who have this power at rank 1 already
                                  // Power 3: Rank 0
                                  // Power 4: Locked by Loyalty

                                  var powerExport = loadout.FileRef.FindExport(talentSet.Powers[i].PowerExport.InstancedFullPath);
                                  var defaults = powerExport.GetDefaults();

                                  var properties = defaults.GetProperties();
                                  properties.RemoveNamedProperty("Rank");
                                  if (properties.RemoveNamedProperty("UnlockRequirements"))
                                  {
                                      Debug.WriteLine("T");
                                  }

                                  // All squadmates have a piont in Slot 0 by default.
                                  // Miranda and Jacob have a point in slot 1
                                  if (hpi.HenchInternalName == "kenson")
                                  {
                                      // Since you can't power up kenson we'll just give her points in all her powers
                                      properties.AddOrReplaceProp(new FloatProperty(ThreadSafeRandom.Next(2) + 1, "Rank"));
                                  }
                                  // VANILLA HENCH CODE
                                  else if (i == 0 || (i == 1 && hpi.HenchInternalName == "vixen" || hpi.HenchInternalName == "leading"))
                                  {
                                      properties.Add(new FloatProperty(1, "Rank"));
                                  }
                                  else if (i == 1)
                                  {
                                      if (removeRank2Gating)
                                      {
                                          // Rank 0
                                          properties.AddOrReplaceProp(new FloatProperty(0, "Rank"));
                                      }
                                      else
                                      {
                                          // Has unlock dependency on the prior slotted item
                                          var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                                          dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport(talentSet.Powers[i - 1].PowerExport.InstancedFullPath), false));
                                          properties.AddOrReplaceProp(dependencies);
                                      }
                                  }
                                  else if (i == 3 && hpi.HenchInternalName != "liara")
                                  {
                                      // Has unlock dependency on loyalty power
                                      // Has unlock dependency on the prior slotted item
                                      var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                                      dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport("SFXGameContent_Powers.SFXPower_LoyaltyRequirement"), true));
                                      properties.AddOrReplaceProp(dependencies);
                                  }

                                  defaults.WriteProperties(properties);
                              }
                          }
                      }

                      Interlocked.Increment(ref numDone);
                      option.ProgressValue = numDone;
                  });
            }

            #endregion

            #region Finalize packages
            numDone = 0;
            option.CurrentOperation = "Saving squadmate packages";
            option.ProgressValue = 0;
            GC.Collect();
            foreach (var hpi in squadmatePackageMap.Values)
            {
                foreach (var package in hpi.Packages)
                {
                    foreach (var loadoutInfo in hpi.PackageLoadouts)
                    {
                        var loadout = package.FindExport(loadoutInfo.LoadoutIFP);
                        if (loadout != null)
                        {
                            var powersList = loadout.GetProperty<ArrayProperty<ObjectProperty>>("Powers");
                            foreach (var powO in powersList)
                            {
                                // Memory-unique powers so squadmates with same powers don't conflict. This has to be done after all the other changes are done or it will not be able to use FindExport()
                                var entry = powO.ResolveToEntry(package);
                                if (CanBeShuffled(entry as ExportEntry, out _))
                                {
                                    entry = package.FindExport(entry.InstancedFullPath);
                                    entry.ObjectName = entry.ObjectName.Name + $"_MER{hpi.HenchInternalName}";
                                    var defaults = (entry as ExportEntry).GetDefaults();
                                    defaults.ObjectName = defaults.ObjectName.Name + $"_MER{hpi.HenchInternalName}";
                                }
                            }
                        }
                    }

                    MERFileSystem.SavePackage(package);
                    Interlocked.Increment(ref numDone);
                    option.ProgressValue = numDone;
                }
            }
            #endregion

            GC.Collect(); // Dump lots of memory out
            return true;
#endif
            return true;
        }

        private static void PatchOutTutorials(GameTarget target)
        {
            // This is LE2 specific

            // Patch out Warp tutorial
            var catwalkF = MERFileSystem.GetPackageFile(target, "BioD_ProCer_200Catwalk.pcc");
            if (catwalkF != null)
            {
                var catwalkP = MEPackageHandler.OpenMEPackage(catwalkF);

                // They're falling back -> Stop respawns
                KismetHelper.CreateNewOutputLink(catwalkP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS65_PullTuto_Spawn_Wave_2.BioSeqAct_FaceOnlyVO_9"),
                    "Done", catwalkP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS65_PullTuto_Spawn_Wave_2.BioSeqAct_CombatController_0"), 2); // index 2: Stop spawns

                // Remove outputs from Delay 1s
                KismetHelper.RemoveOutputLinks(catwalkP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS65_PullTuto_Spawn_Wave_2.SeqAct_Delay_4"));

                // Set Delay 1s to 'They're falling back'
                // LE2: This goes to a gate in?
                KismetHelper.CreateOutputLink(catwalkP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS65_PullTuto_Spawn_Wave_2.SeqAct_Delay_4"), "Finished",
                    catwalkP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS65_PullTuto_Spawn_Wave_2.SeqAct_Gate_0"), 0);

                // Set 5 second delay instead of tutorial for when enemies stop spawning
                catwalkP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS65_PullTuto_Spawn_Wave_2.SeqVar_Float_4").WriteProperty(new FloatProperty(5, "Duration"));

                MERFileSystem.SavePackage(catwalkP);
            }

            // Patch out Overload tutorial
            var controlRoomF = MERFileSystem.GetPackageFile(target, "BioD_ProCer_250ControlRoom.pcc");
            if (controlRoomF != null)
            {
                var controlRoomP = MEPackageHandler.OpenMEPackage(controlRoomF);



                // Fastest way to the shuttle ->ForceCrateExplode
                KismetHelper.CreateNewOutputLink(controlRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS87_OverloadTutorial.BioSeqAct_FaceOnlyVO_6"),
                    "Done", controlRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS87_OverloadTutorial.SeqAct_ActivateRemoteEvent_0"),
                    0);

                // The fastest way to the shuttle -> Destroy crate object
                KismetHelper.CreateOutputLink(controlRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS87_OverloadTutorial.BioSeqAct_FaceOnlyVO_6"), "Done", 
                    controlRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS87_OverloadTutorial.SeqAct_Destroy_0"),
                    0);

                var outboundToHint = SeqTools.GetOutboundLinksOfNode(controlRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS87_OverloadTutorial.BioSeqAct_FaceOnlyVO_7"));
                if (outboundToHint.Count == 4)
                {
                    // Done to ShowHint remove
                    // Total hack, but it works, maybe
                    if (outboundToHint[0].Count == 3)
                    {
                        outboundToHint[0].RemoveAt(0);
                    }

                    // Failed to ShowHint remove
                    if (outboundToHint[1].Count == 2)
                    {
                        outboundToHint[1].RemoveAt(0);
                    }

                    SeqTools.WriteOutboundLinksToNode(controlRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS87_OverloadTutorial.BioSeqAct_FaceOnlyVO_7"), outboundToHint);
                }

                controlRoomP.FindExport("TheWorld.PersistentLevel.Main_Sequence.LS87_OverloadTutorial.SeqVar_Float_2").WriteProperty(new FloatProperty(1, "FloatValue"));

                MERFileSystem.SavePackage(controlRoomP);
            }
        }

        private static void LoadSquadmatePackages(HenchInfo hpi, string vanillaPackagePath, CaseInsensitiveConcurrentDictionary<HenchInfo> squadmatePackageMap, MERPackageCache henchCache)
        {

            var sqm = henchCache.GetCachedPackage(vanillaPackagePath);
            hpi.Packages.Add(sqm);
            var casualHubsVer = henchCache.GetCachedPackage($"BioH_{hpi.HenchInternalName}_00_NC.pcc", true);
            if (casualHubsVer != null)
            {
                // Liara doesn't have endgm file
                hpi.Packages.Add(casualHubsVer);
            }

            var endGmFile = henchCache.GetCachedPackage($"BioH_END_{hpi.HenchInternalName}_00.pcc", true);
            if (endGmFile != null)
            {
                // Liara doesn't have endgm file
                hpi.Packages.Add(endGmFile);
            }

            squadmatePackageMap[hpi.HenchInternalName] = hpi;

            // Build out the rest of the list for this pawn
            int sqmIndex = 0;
            while (true)
            {
                sqmIndex++;
                var fName = $"BioH_{hpi.HenchInternalName}_0{sqmIndex}.pcc";
                var newPackageF = henchCache.GetCachedPackage(fName, true);
                if (newPackageF != null)
                {
                    hpi.Packages.Add(newPackageF);
                    hpi.Packages.Add(henchCache.GetCachedPackage($"BioH_END_{hpi.HenchInternalName}_0{sqmIndex}.pcc", true));
                    var casHubV = henchCache.GetCachedPackage($"BioH_{hpi.HenchInternalName}_0{sqmIndex}_NC.pcc", true);
                    if (casHubV != null)
                        hpi.Packages.Add(casHubV);
                }
                else
                {
                    // No more squadmates in this set
                    break;
                }
            }

            // Add specific files for specific squadmates
            switch (hpi.HenchInternalName.ToLower())
            {
                case "leading": // Jacob
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioP_ProCer.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_350BriefRoom.pcc", true));
                    break;
                case "vixen": // Miranda
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioP_ProCer.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_300ShuttleBay.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_350BriefRoom.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioA_ZyaVtl_100.pcc", true));
                    break;
                case "kenson":
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ArvLvl1_710Shuttle.pcc", true));
                    break;
            }
        }

        class UnshuffleablePower
        {
            public string PowerName { get; init; }
            public string Pawn { get; init; }
            public bool CountsTowardsTalentCount { get; init; } = true;
        }

        private static UnshuffleablePower[] UnshuffleablePowers = new[]
        {
            new UnshuffleablePower() {PowerName = "SFXPower_LoyaltyRequirement"},
            new UnshuffleablePower() {PowerName = "SFXPower_StasisNew_Liara", Pawn = "liara"},

            new UnshuffleablePower() {PowerName = "SFXPower_KasumiCloakTeleport", Pawn = "thief"}, // Will need a slight rework to swap AI over?
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiUnique", Pawn = "thief"}, // Flashbang grenade
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiAssassinate", Pawn = "thief", CountsTowardsTalentCount = false},
            new UnshuffleablePower() {PowerName = "SFXPower_ZaeedUnique", Pawn = "veteran"}, // Inferno grenade
        };

        /// <summary>
        /// If the power can be shuffled/reassigned
        /// </summary>
        /// <param name="exportEntry"></param>
        /// <returns></returns>
        private static bool CanBeShuffled(ExportEntry exportEntry, out UnshuffleablePower unshuffablePower)
        {
            if (exportEntry == null)
            {
                unshuffablePower = new UnshuffleablePower() { PowerName = "UNKNOWN POWER!!!" };
                return false;
            }

            unshuffablePower = UnshuffleablePowers.FirstOrDefault(x => x.PowerName.Equals(exportEntry.ObjectName.Name, StringComparison.InvariantCultureIgnoreCase));
            return unshuffablePower == null;
        }

        /// <summary>
        /// Generates the structs for
        /// <param name="powerClass">The kit-power to depend on.</param>
        /// <returns>< UnlockRequirements that are used to setup a dependency on this power
        /// </summary>/returns>
        private static List<StructProperty> GetUnlockRequirementsForPower(ExportEntry powerClass, bool isLoyaltyPower, bool blankUnlock = false)
        {
            void PopulateProps(PropertyCollection props, ExportEntry export, float rank)
            {
                props.Add(new ObjectProperty(export.UIndex, "PowerClass")); // The power that will be checked
                props.Add(new FloatProperty(rank, "Rank")); // Required rank. 1 means at least one point in it and is used for evolved powers which area always at 4.
                props.Add(new StringRefProperty(isLoyaltyPower ? 339163 : 0, "CustomUnlockText")); // "Locked: squad member is not loyal"
            }

            List<StructProperty> powerRequirements = new();
            if (blankUnlock)
                return powerRequirements;

            // Unevolved power
            PropertyCollection props = new PropertyCollection();
            PopulateProps(props, powerClass, isLoyaltyPower ? 1 : 2); // Loyalty is rank 1. Others are rank 2
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            // EVOLVED POWER
            if (!isLoyaltyPower)
            {
                // Find base power
                var baseClass = powerClass;
                ExportEntry baseDefaults = baseClass.GetDefaults();
                var evolvedPower1 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass1");
                while (evolvedPower1 == null || evolvedPower1.Value == 0)
                {
                    baseClass = (ExportEntry)baseClass.SuperClass;
                    baseDefaults = baseClass.GetDefaults();
                    evolvedPower1 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass1");
                }


                // Loyalty power has no unlock requirement beyond SFXPower_Loyalty

                // Evolved power 1
                props = new PropertyCollection();
                PopulateProps(props, evolvedPower1.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
                powerRequirements.Add(new StructProperty("UnlockRequirement", props));

                // Evolved power 2
                var evolvedPower2 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass2");
                props = new PropertyCollection();
                PopulateProps(props, evolvedPower2.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
                powerRequirements.Add(new StructProperty("UnlockRequirement", props));
            }

            return powerRequirements;
        }

        private static ArrayProperty<StructProperty> BuildAutoRankList(ExportEntry loadout, List<MappedPower> mappedPowers)
        {
            ArrayProperty<StructProperty> alui = new ArrayProperty<StructProperty>("PowerLevelUpInfo");
            Dictionary<MappedPower, int> rankMap = mappedPowers.ToDictionary(x => x, x => 1);
            while (rankMap.Any(x => x.Value != 4))
            {
                // Add ranks
                var mp = mappedPowers.RandomElement();
                while (rankMap[mp] >= 4)
                {
                    mp = mappedPowers.RandomElement(); // Repick
                }

                var newRank = rankMap[mp] + 1;

                PropertyCollection props = new PropertyCollection();
                props.Add(new ObjectProperty(loadout.FileRef.FindExport(mp.BaseTalent.PowerExport.InstancedFullPath).UIndex, "PowerClass"));
                props.Add(new FloatProperty(newRank, "Rank"));
                if (newRank < 4)
                {
                    props.Add(new ObjectProperty(0, "EvolvedPowerClass"));
                }
                else
                {
                    var evo = ThreadSafeRandom.Next(2) == 0 ? mp.EvolvedTalent1 : mp.EvolvedTalent2;
                    props.Add(new ObjectProperty(loadout.FileRef.FindExport(evo.PowerExport.InstancedFullPath).UIndex, "EvolvedPowerClass"));
                }
                props.Add(new NoneProperty());
                alui.Add(new StructProperty("PowerLevelUp", props));
                rankMap[mp] = newRank;
            }

            return alui;
        }

        public static void ResetTalents(RandomizationOption obj)
        {
            //todo: fix this
            //TalentReset.ResetTalents(true);
        }
    }
}
