﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ME2Randomizer.Classes;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Kismet;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class HenchTalents
    {
        [DebuggerDisplay("HTalent - {PowerExport.ObjectName}, Base {BasePower.ObjectName}, IsPassive: {IsPassive}")]
        class HTalent
        {
            protected bool Equals(HTalent other)
            {
                return Equals(PowerExport.InstancedFullPath, other.PowerExport.InstancedFullPath);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return PowerExport.InstancedFullPath.Equals(((HTalent)obj).PowerExport.InstancedFullPath);
            }

            public override int GetHashCode()
            {
                return (PowerExport != null ? PowerExport.GetHashCode() : 0);
            }
            /// <summary>
            /// If power is shown in the Character Record (squad) screen
            /// </summary>
            public bool ShowInCR { get; set; } = true;

            public PropertyCollection CondensedProperties { get; private set; }
            /// <summary>
            /// Builds a property collection going up the export tree, keeping bottom properties and populating missing ones available in higher classes
            /// </summary>
            /// <param name="export"></param>
            private void CondenseTalentProperties(ExportEntry export)
            {
                CondensedProperties = export.GetDefaults().GetProperties();
                IEntry superEntry = export.SuperClass;
                while (superEntry is ExportEntry superC)
                {
                    var archProps = superC.GetDefaults().GetProperties();
                    foreach (Property prop in archProps)
                    {
                        if (!CondensedProperties.ContainsNamedProp(prop.Name))
                        {
                            CondensedProperties.AddOrReplaceProp(prop);
                        }
                    }

                    superEntry = superC.SuperClass;
                }

            }


            public HTalent(ExportEntry powerClass, bool isEvolution = false, bool isFixedPower = false)
            {
                PowerExport = powerClass;
                IsEvolution = isEvolution;
                CondenseTalentProperties(powerClass);
                var displayName = CondensedProperties.GetProp<StringRefProperty>("DisplayName");
                if (displayName != null)
                {
                    PowerName = TLKHandler.TLKLookupByLang(displayName.Value, "INT");
                }

                ShowInCR = CondensedProperties.GetProp<BoolProperty>("DisplayInCharacterRecord")?.Value ?? true;

                if (isFixedPower)
                {
                    BasePower = PowerExport;
                    if (PowerName == null)
                    {
                        PowerName = powerClass.ObjectName.Name;
                    }
                    return;
                }

                var baseClass = powerClass;
                var baseClassObj = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
                while (baseClass.SuperClass is ExportEntry bcExp && (baseClassObj == null || baseClassObj.Value == 0))
                {
                    baseClass = bcExp;
                    baseClassObj = baseClass.GetDefaults().GetProperty<ObjectProperty>("EvolvedPowerClass1");
                }

                BasePower = baseClass; // BasePower is used to prevent duplicates... I think

                if (BasePower.ObjectName.Name.Contains("Passive"))
                {
                    IsPassive = true;
                }
                else
                {
                    var baseName = baseClass.GetDefaults().GetProperty<NameProperty>("BaseName");
                    while (baseName == null)
                    {
                        baseClass = (ExportEntry)baseClass.SuperClass;
                        baseName = baseClass.GetDefaults().GetProperty<NameProperty>("BaseName");
                    }

                    BaseName = baseName.Value.Name;
                }

                // Setup name
                //var superDefaults = PowerExport.GetDefaults();
                //var displayNameProps = superDefaults.GetProperties();
                //var superProps = displayNameProps;

                //TalentDescriptionProp = superProps.GetProp<StringRefProperty>("TalentDescription");
                //while (displayName == null)
                //{
                //    superDefaults = ((superDefaults.Class as ExportEntry).SuperClass as ExportEntry).GetDefaults();
                //    superProps = superDefaults.GetProperties();
                //    superProps.GetProp<StringRefProperty>("DisplayName");
                //    displayName = superProps.GetProp<StringRefProperty>("DisplayName");
                //    TalentDescriptionProp ??= superProps.GetProp<StringRefProperty>("TalentDescription");
                //}

                if (IsEvolution)
                {
                    // Setup the blurb
                    var blurbDesc = TLKHandler.TLKLookupByLang(CondensedProperties.GetProp<StringRefProperty>("TalentDescription").Value, "INT").Split('\n')[0];
                    EvolvedBlurb = $"{PowerName}: {blurbDesc}";
                }

                IsAmmoPower = PowerName.Contains("Ammo");
                IsCombatPower = !IsAmmoPower && !IsPassive;
            }


            /// <summary>
            /// Is this a combat power? (Like Warp, Throw, Tech Armor)
            /// </summary>
            public bool IsCombatPower { get; set; }
            /// <summary>
            /// Is this an ammo power? (Like Disruptor, Cryo)
            /// </summary>
            public bool IsAmmoPower { get; set; }

            /// <summary>
            /// Blurb text to use when changing the evolution
            /// </summary>
            public string EvolvedBlurb { get; set; }

            /// <summary>
            /// If this is a passive power
            /// </summary>
            public bool IsPassive { get; set; }

            /// <summary>
            /// If this is an evolved power
            /// </summary>
            public bool IsEvolution { get; set; }
            /// <summary>
            /// The base class of the power - player version
            /// </summary>
            public ExportEntry BasePower { get; }
            /// <summary>
            /// The usable power export
            /// </summary>
            public ExportEntry PowerExport { get; }
            /// <summary>
            /// The base name of the power that is used for mapping in config
            /// </summary>
            public string BaseName { get; }

            /// <summary>
            /// The name of the power (localized)
            /// </summary>
            public string PowerName { get; }

            public bool HasEvolution()
            {
                return !IsEvolution;
            }

            public IEnumerable<HTalent> GetEvolutions()
            {
                var baseProps = BasePower.GetDefaults().GetProperties();
                HTalent evo1 = new HTalent(baseProps.GetProp<ObjectProperty>("EvolvedPowerClass1").ResolveToEntry(BasePower.FileRef) as ExportEntry, true);
                HTalent evo2 = new HTalent(baseProps.GetProp<ObjectProperty>("EvolvedPowerClass2").ResolveToEntry(BasePower.FileRef) as ExportEntry, true);
                return new[] { evo1, evo2 };
            }
        }

        class TalentSet
        {
            /// <summary>
            /// If we were able to find a solution of compatible powers for this set
            /// </summary>
            public bool IsBaseValid { get; }
            /// <summary>
            /// Creates a talent set from the list of all powers that are available for use. The list WILL be modified so ensure it is a clone
            /// </summary>
            /// <param name="allPowers"></param>
            public TalentSet(HenchLoadoutInfo hpi, List<HTalent> allPowers)
            {
                int numPassives = 0;

                int numPowersToAssign = hpi.NumPowersToAssign;
                for (int i = 0; i < numPowersToAssign; i++)
                {
                    var talent = allPowers.PullFirstItem();
                    int retry = allPowers.Count;
                    while (retry > 0 && Powers.Any(x => x.BaseName == talent.BaseName))
                    {
                        allPowers.Add(talent);
                        talent = allPowers.PullFirstItem();
                        retry--;
                        if (retry <= 0)
                        {
                            IsBaseValid = false;
                            return;
                        }
                    }

                    if (talent.BasePower.ObjectName.Name.Contains("Passive"))
                    {
                        numPassives++;
                        if (numPassives > 3)
                        {
                            // We must ensure there is not a class full of passives
                            // or the evolutions will not have a solution
                            // only doing half allows us to give the evolution solution finder
                            // a better chance at a quick solution
                            IsBaseValid = false;
                            return;
                        }
                    }
                    Powers.Add(talent);
                }

                // Add in the fixed powers
                if (hpi.FixedPowers.Any())
                {
                    Powers.AddRange(hpi.FixedPowers);
                    Powers.Shuffle();
                }

                IsBaseValid = true;
            }

            public bool SetEvolutions(HenchLoadoutInfo hpi, List<HTalent> availableEvolutions)
            {
                // 1. Calculate the number of required bonus evolutions
                var numToPick = (Powers.Count(x => x.HasEvolution()) - hpi.FixedPowers.Count) * 2;
                while (numToPick > 0)
                {
                    var numAttempts = availableEvolutions.Count;
                    var evolutionToCheck = availableEvolutions.PullFirstItem();
                    while (numAttempts > 0 && EvolvedPowers.Any(x => x.PowerExport.InstancedFullPath == evolutionToCheck.PowerExport.InstancedFullPath)) // Ensure there are no duplicate power exports
                    {
                        // Repick
                        availableEvolutions.Add(evolutionToCheck);
                        evolutionToCheck = availableEvolutions.PullFirstItem();
                        numAttempts--;
                        if (numAttempts == 0)
                        {
                            Debug.WriteLine("Could not find suitable evolution for talentset!");
                            return false; // There is no viable solution
                        }
                    }

                    EvolvedPowers.Add(evolutionToCheck);
                    numToPick--;
                }

                return true;
            }

            /// <summary>
            /// The base power set of the kit
            /// </summary>
            public List<HTalent> Powers { get; } = new();

            /// <summary>
            /// The evolution power pool. Should not contain any items that are the same as an item in the Powers list
            /// </summary>
            public List<HTalent> EvolvedPowers { get; } = new();
        }

        class MappedPower
        {
            public int StartingRank { get; set; }
            public HTalent BaseTalent { get; set; }
            public HTalent EvolvedTalent1 { get; set; }
            public HTalent EvolvedTalent2 { get; set; }
        }

        class HenchLoadoutInfo
        {
            /// <summary>
            /// The list of talents that will be installed to this henchman
            /// </summary>
            public TalentSet HenchTalentSet { get; private set; }
            /// <summary>
            /// Powers that cannot be removed from this squadmate
            /// </summary>
            public List<HTalent> FixedPowers { get; } = new List<HTalent>(3);

            /// <summary>
            /// Number of powers to try to assign when building a base talent set
            /// </summary>
            public int NumPowersToAssign { get; set; } = 4;
            /// <summary>
            /// Instanced full path for this loadout object
            /// </summary>
            public string LoadoutIFP { get; set; }


            /// <summary>
            /// Builds a valid base talentset for this henchman from the list of available base talents. If none can be built this method returns false
            /// </summary>
            /// <param name="baseTalents"></param>
            /// <returns></returns>
            public bool BuildTalentSet(List<HTalent> baseTalentPool)
            {
                HenchTalentSet = new TalentSet(this, baseTalentPool);
                return HenchTalentSet.IsBaseValid;
            }

            /// <summary>
            /// Builds a valid evolved talentset for this henchmen from the list of available evolved talents. If none can be built, this method returns false
            /// </summary>
            /// <param name="evolvedTalentPool"></param>
            /// <returns></returns>
            public bool BuildEvolvedTalentSet(List<HTalent> evolvedTalentPool)
            {
                return HenchTalentSet.SetEvolutions(this, evolvedTalentPool);
            }

            /// <summary>
            /// Puts base powers in order, with HUD powers first and non-HUD powers at the bottom. This ensure the unlock requirements are done properly
            /// </summary>
            public void OrderBasePowers()
            {
                var powers = HenchTalentSet.Powers.ToList();
                HenchTalentSet.Powers.Clear();

                HenchTalentSet.Powers.AddRange(powers.Where(x => x.ShowInCR));
                HenchTalentSet.Powers.AddRange(powers.Where(x => !x.ShowInCR));
            }

            /// <summary>
            /// Completely resets the talentset object
            /// </summary>
            public void ResetTalents()
            {
                HenchTalentSet = null;
            }

            /// <summary>
            /// Clears the existing talentset's evolutions
            /// </summary>
            public void ResetEvolutions()
            {
                HenchTalentSet?.EvolvedPowers.Clear();
            }
        }

        class HenchInfo
        {
            /// <summary>
            /// The internal name for this henchman
            /// </summary>
            public string HenchInternalName { get; }
            /// <summary>
            /// The list of packages that contain this henchmen
            /// </summary>
            public List<IMEPackage> Packages { get; } = new List<IMEPackage>();

            /// <summary>
            /// Loadout InstancedFullPaths. Samara has multiple loadouts
            /// </summary>
            public List<HenchLoadoutInfo> PackageLoadouts { get; } = new List<HenchLoadoutInfo>();

            public HenchInfo(string internalName)
            {
                HenchInternalName = internalName;
            }

            public void ResetTalents()
            {
                foreach (var l in PackageLoadouts)
                {
                    l.ResetTalents();
                }
            }

            public void ResetEvolutions()
            {
                foreach (var l in PackageLoadouts)
                {
                    l.ResetEvolutions();
                }
            }
        }

        public static bool ShuffleSquadmateAbilities(RandomizationOption option)
        {
            PatchOutTutorials();

            var henchCache = new MERPackageCache();

            // We can have up to 5 UI powers assigned. However, kits will only have 4 powers total assigned as each pawn only has 4 in vanilla gameplay.
            var squadmatePackageMap = new CaseInsensitiveConcurrentDictionary<HenchInfo>();

            #region Build squadmate sets
            var henchFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioH_") && !x.Contains("_LOC_") && !x.Contains("END") && x != "BioH_SelectGUI" && (x.Contains("00")) || x.Contains("BioH_Wilson")).ToList();
            option.CurrentOperation = "Inventorying henchmen powers";
            int numDone = 0;
            option.ProgressMax = henchFiles.Count;
            Parallel.ForEach(henchFiles, h =>
            {
                //foreach (var h in henchFiles)
                //{

                string internalName = null;
                if (h == "BioH_Wilson.pcc")
                {
                    internalName = "wilson";
                }
                else
                {
                    internalName = h.Substring(5); // Remove BioH_
                    internalName = internalName.Substring(0, internalName.IndexOf("_", StringComparison.InvariantCultureIgnoreCase)); // "Assassin"
                }
                HenchInfo hpi = new HenchInfo(internalName);
                InventorySquadmate(hpi, h, squadmatePackageMap, henchCache);

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
            Parallel.ForEach(squadmatePackageMap, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, henchInfo =>
                {
                    //foreach (var henchInfo in squadmatePackageMap)
                    //{
                    

                    var henchP = henchInfo.Value.Packages[0];
                    var loadouts = henchP.Exports.Where(x => x.ClassName == "SFXPlayerSquadLoadoutData" && x.ObjectName.Name.StartsWith("hench_")).ToList();
                    foreach (var loadout in loadouts)
                    {
                        HenchLoadoutInfo hli = new HenchLoadoutInfo() { LoadoutIFP = loadout.InstancedFullPath };
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
                                var htalent = new HTalent(p);
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

                Parallel.ForEach(hpi.Packages, new ParallelOptions() { MaxDegreeOfParallelism = 1 }, package =>
                  {
                      //foreach (var loadout in assets)
                      //{
                      // We must load a new copy of the package file into memory, one for reading, one for modifying,
                      // otherwise it will be concurrent modification
                      //if (!package.FilePath.Contains("Thief"))
                      //      return;
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
                              foreach (var basePower in talentSet.Powers)
                              {
                                  if (loadoutInfo.FixedPowers.Contains(basePower))
                                      continue; // Do not modify this
                                  var portedPower = PackageTools.PortExportIntoPackage(loadout.FileRef, basePower.PowerExport);
                                  powersList.Add(new ObjectProperty(portedPower.UIndex));

                                  // For each power, change the evolutions
                                  var defaults = portedPower.GetDefaults();
                                  var props = defaults.GetProperties();
                                  var evolution1 = talentSet.EvolvedPowers[powIndex * 2];
                                  var evolution2 = talentSet.EvolvedPowers[(powIndex * 2) + 1];
                                  configuredPowers.Add(new MappedPower() { BaseTalent = basePower, EvolvedTalent1 = evolution1, EvolvedTalent2 = evolution2 });
                                  props.AddOrReplaceProp(new ObjectProperty(PackageTools.PortExportIntoPackage(portedPower.FileRef, evolution1.PowerExport), "EvolvedPowerClass1"));
                                  props.AddOrReplaceProp(new ObjectProperty(PackageTools.PortExportIntoPackage(portedPower.FileRef, evolution2.PowerExport), "EvolvedPowerClass2"));

                                  // Update the evolution text via override
                                  var ranksSource = basePower.CondensedProperties.GetProp<ArrayProperty<StructProperty>>("Ranks");
                                  if (ranksSource == null)
                                  {
                                      Debugger.Break();
                                      // Projectile powers are subclassed for player
                                      //ranksSource = (basePower.BasePower.SuperClass as ExportEntry).GetDefaults().GetProperty<ArrayProperty<StructProperty>>("Ranks");
                                  }

                                  lock (tlkSync)
                                  {
                                      var evolveRank = ranksSource[3];
                                      var descriptionProp = evolveRank.Properties.GetProp<StringRefProperty>("Description");
                                      if (!TLKHandler.IsAssignedMERString(descriptionProp.Value))
                                      {
                                          var description = TLKHandler.TLKLookupByLang(descriptionProp.Value, "INT");
                                          var descriptionLines = description.Split('\n');
                                          descriptionLines[2] = $"1) {evolution1.EvolvedBlurb}";
                                          descriptionLines[4] = $"2) {evolution2.EvolvedBlurb}";
                                          var newStringID = TLKHandler.GetNewTLKID();
                                          TLKHandler.ReplaceString(newStringID, string.Join('\n', descriptionLines));
                                          descriptionProp.Value = newStringID;
                                      }
                                  }

                                  props.AddOrReplaceProp(ranksSource); // copy the source rank info into our power with the modification

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

                              // Correct the unlock criteria to ensure every power can be unlocked.
                              for (int i = 0; i < talentSet.Powers.Count; i++)
                              {
                                  // i = Power Index (0 indexed)

                                  // Power dependency map:
                                  // Power 1: Rank 1
                                  // Power 2: Depends on Power 1 Rank 2
                                  // Power 3: Rank 1
                                  // Power 4: Depends on Power 3 Rank 2
                                  // Power 5: Depends on Power 4 Rank 2
                                  // Power 6: No requirements, but no points assigned

                                  var talent = talentSet.Powers[i];
                                  var powerExport = loadout.FileRef.FindExport(talentSet.Powers[i].PowerExport.InstancedFullPath);
                                  var defaults = powerExport.GetDefaults();

                                  var properties = defaults.GetProperties();
                                  properties.RemoveNamedProperty("Rank");
                                  properties.RemoveNamedProperty("UnlockRequirements");

                                  // All squadmates have a piont in Slot 0 by default.
                                  // Miranda and Jacob have a point in slot 1
                                  if (i == 0 || (i == 1 && hpi.HenchInternalName == "vixen" || hpi.HenchInternalName == "leading"))
                                  {
                                      properties.Add(new FloatProperty(1, "Rank"));
                                  }
                                  else if (i == 1)
                                  {
                                      // Has unlock dependency on the prior slotted item
                                      var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                                      dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport(talentSet.Powers[i - 1].PowerExport.InstancedFullPath), false));
                                      properties.Add(dependencies);
                                  }
                                  else if (i == 3)
                                  {
                                      // Has unlock dependency on loyalty power
                                      // Has unlock dependency on the prior slotted item
                                      var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                                      dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport(talentSet.Powers[i - 1].PowerExport.InstancedFullPath), true));
                                      properties.Add(dependencies);
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
        }

        private static void PatchOutTutorials()
        {
            // Patch out Warp tutorial
            var catwalkF = MERFileSystem.GetPackageFile("BioD_ProCer_200Catwalk.pcc");
            if (catwalkF != null)
            {
                var catwalkP = MEPackageHandler.OpenMEPackage(catwalkF);

                // They're falling back -> Stop respawns
                KismetHelper.CreateOutputLink(catwalkP.GetUExport(386), "Done", catwalkP.GetUExport(376), 2);

                // Remove outputs from Delay 1s
                KismetHelper.RemoveOutputLinks(catwalkP.GetUExport(2231));

                // Set Delay 1s to 'They're falling back'
                KismetHelper.CreateOutputLink(catwalkP.GetUExport(2231), "Finished", catwalkP.GetUExport(2247), 0);

                catwalkP.GetUExport(2231).WriteProperty(new FloatProperty(5, "Duration"));

                MERFileSystem.SavePackage(catwalkP);
            }

            // Patch out Warp tutorial
            var controlRoomF = MERFileSystem.GetPackageFile("BioD_ProCer_250ControlRoom.pcc");
            if (controlRoomF != null)
            {
                var controlRoomP = MEPackageHandler.OpenMEPackage(controlRoomF);

                // Fastest way to the shuttle ->ForceCrateExplode
                KismetHelper.CreateOutputLink(controlRoomP.GetUExport(765), "Done", controlRoomP.GetUExport(2932), 0);

                // The fastest way to the shuttle -> Destory crate object
                KismetHelper.CreateOutputLink(controlRoomP.GetUExport(765), "Done", controlRoomP.GetUExport(2963), 0);

                var outboundToHint = SeqTools.GetOutboundLinksOfNode(controlRoomP.GetUExport(766));
                if (outboundToHint.Count == 4)
                {
                    // Total hack, but it works, maybe
                    if (outboundToHint[0].Count == 3)
                    {
                        outboundToHint[0].RemoveAt(0);
                    }
                    if (outboundToHint[1].Count == 2)
                    {
                        outboundToHint[1].RemoveAt(0);
                    }

                    SeqTools.WriteOutboundLinksToNode(controlRoomP.GetUExport(766), outboundToHint);
                }

                controlRoomP.GetUExport(3323).WriteProperty(new FloatProperty(3, "FloatValue"));

                // Remove outputs from Delay 1s
                //KismetHelper.RemoveOutputLinks(controlRoomP.GetUExport(2231));

                //// Set Delay 1s to 'They're falling back'
                //KismetHelper.CreateOutputLink(controlRoomP.GetUExport(2231), "Finished", catwalkP.GetUExport(2247), 0);

                //controlRoomP.GetUExport(2231).WriteProperty(new FloatProperty(5, "Duration"));

                MERFileSystem.SavePackage(controlRoomP);
            }
        }

        private static void InventorySquadmate(HenchInfo hpi, string vanillaPackagePath, CaseInsensitiveConcurrentDictionary<HenchInfo> squadmatePackageMap, MERPackageCache henchCache)
        {
            Debug.WriteLine($"Opening packages for {hpi.HenchInternalName}");

            var sqm = henchCache.GetCachedPackage(vanillaPackagePath);
            hpi.Packages.Add(sqm);
            var endGmFile = henchCache.GetCachedPackage($"BioH_END_{hpi.HenchInternalName}_00.pcc", true);
            if (endGmFile != null)
            {
                // Liara doesn't have endgm file
                hpi.Packages.Add(MEPackageHandler.OpenMEPackage(endGmFile));
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
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_350DebriefRoom.pcc", true));
                    break;
                case "vixen": // Miranda
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioP_ProCer.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_300ShuttleBay.pcc", true));
                    hpi.Packages.Add(henchCache.GetCachedPackage("BioD_ProCer_350DebriefRoom.pcc", true));
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

            new UnshuffleablePower() {PowerName = "SFXPower_KasumiCloakTeleport", Pawn = "thief"},
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiUnique", Pawn = "thief"},
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiAssassinate", Pawn = "thief", CountsTowardsTalentCount = false},
            new UnshuffleablePower() {PowerName = "SFXPower_ZaeedUnique", Pawn = "veteran"},
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

        private static ExportEntry GetEvolution(IMEPackage package, List<ExportEntry> kitPowers, List<ExportEntry> evolvedPowersInKit, List<ExportEntry> powerEvolutions)
        {
            var power = powerEvolutions.PullFirstItem();
            int ix = 20;
            while (ix > 0 && evolvedPowersInKit.Any(x => x.ClassName == power.ClassName || x.InheritsFrom(power.ClassName)) && kitPowers.Any(x => power.InheritsFrom(x.ClassName)))
            {
                powerEvolutions.Add(power);
                power = powerEvolutions.PullFirstItem();
                ix--; //give up if we have to
            }

            return PackageTools.PortExportIntoPackage(package, power);
        }

        /// <summary>
        /// Generates the structs for UnlockRequirements that are used to setup a dependenc on this power
        /// </summary>
        /// <param name="powerClass">The kit-power to depend on.</param>
        /// <returns></returns>
        private static List<StructProperty> GetUnlockRequirementsForPower(ExportEntry powerClass, bool isLoyaltyRequirement)
        {
            void PopulateProps(PropertyCollection props, ExportEntry export, float rank)
            {
                props.Add(new ObjectProperty(export.UIndex, "PowerClass")); // The power that will be checked
                props.Add(new FloatProperty(rank, "Rank")); // Required rank. 1 means at least one point in it and is used for evolved powers which area always at 4.
                props.Add(new StringRefProperty(isLoyaltyRequirement ? 339163 : 0, "CustomUnlockText")); // "Locked: squad member is not loyal"
            }

            List<StructProperty> powerRequirements = new();

            // Unevolved power
            PropertyCollection props = new PropertyCollection();
            PopulateProps(props, powerClass, isLoyaltyRequirement ? 1 : 2); // Loyalty is rank 1. Others are rank 2
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            // EVOLVED POWER

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

            // Evolved power 1
            props = new PropertyCollection();
            PopulateProps(props, evolvedPower1.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

            // Evolved power 2
            var evolvedPower2 = baseDefaults.GetProperty<ObjectProperty>("EvolvedPowerClass2");
            props = new PropertyCollection();
            PopulateProps(props, evolvedPower2.ResolveToEntry(powerClass.FileRef) as ExportEntry, 1);
            powerRequirements.Add(new StructProperty("UnlockRequirement", props));

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
    }
}