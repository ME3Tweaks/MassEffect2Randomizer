using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ME2Randomizer.Classes;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
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
                if (evo1 == null || evo2 == null)
                    Debugger.Break();
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
            public TalentSet(HenchPowerInfo hpi, List<HTalent> allPowers)
            {
                int numPassives = 0;

                int numPowersToAssign = 4 - hpi.FixedPowers.Count;
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

            public bool SetEvolutions(HenchPowerInfo hpi, List<HTalent> availableEvolutions)
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

        class HenchPowerInfo
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
            /// Loadout InstancedFullPath
            /// </summary>
            public string LoadoutIFP { get; set; }

            public HenchPowerInfo(string internalName)
            {
                HenchInternalName = internalName;
            }

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
            /// Commits the HenchTalentSet to the packages in the Packages list and sets up the loyalty requirement for the final power.
            /// Also sets up the auto evolution
            /// </summary>
            public void InstallPowers()
            {

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

        public static bool ShuffleSquadmateAbilities(RandomizationOption option)
        {
            // We can have up to 5 UI powers assigned. However, kits will only have 4 powers total assigned as each pawn only has 4 in vanilla gameplay.

            var squadmatePackageMap = new CaseInsensitiveConcurrentDictionary<HenchPowerInfo>();

            #region Build squadmate sets
            var henchFiles = MERFileSystem.LoadedFiles.Keys.Where(x => x.StartsWith("BioH_") && !x.Contains("_LOC_") && !x.Contains("END") && x != "BioH_SelectGUI" && x.Contains("00")).ToList();
            option.CurrentOperation = "Inventorying henchmen powers";
            int numDone = 0;
            option.ProgressMax = henchFiles.Count;
            Parallel.ForEach(henchFiles, h =>
            {
                //foreach (var h in henchFiles)
                //{
                var internalName = h.Substring(5); // Remove BioH_
                internalName = internalName.Substring(0, internalName.IndexOf("_", StringComparison.InvariantCultureIgnoreCase)); // "Assassin"
                HenchPowerInfo hpi = new HenchPowerInfo(internalName);

                Debug.WriteLine($"Opening packages for {internalName}");

                var sqm = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile(h));
                hpi.Packages.Add(sqm);
                var endGmFile = MERFileSystem.GetPackageFile($"BioH_END_{internalName}_00.pcc", false);
                if (endGmFile != null)
                {
                    // Liara doesn't have endgm file
                    hpi.Packages.Add(MEPackageHandler.OpenMEPackage(endGmFile));
                }

                squadmatePackageMap[internalName] = hpi;

                // Build out the rest of the list for this pawn
                int sqmIndex = 0;
                while (true)
                {
                    sqmIndex++;
                    var fName = $"BioH_{internalName}_0{sqmIndex}.pcc";
                    var newPackageF = MERFileSystem.GetPackageFile(fName, false);
                    if (newPackageF != null)
                    {
                        hpi.Packages.Add(MEPackageHandler.OpenMEPackage(newPackageF));
                        hpi.Packages.Add(MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_END_{internalName}_0{sqmIndex}.pcc")));
                    }
                    else
                    {
                        // No more squadmates in this set
                        break;
                    }
                }
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
            Parallel.ForEach(squadmatePackageMap, henchInfo =>
            {
                //foreach (var henchInfo in squadmatePackageMap)
                //{
                var henchP = henchInfo.Value.Packages[0];
                var loadout = henchP.Exports.FirstOrDefault(x => x.ClassName == "SFXPlayerSquadLoadoutData");
                henchInfo.Value.LoadoutIFP = loadout.InstancedFullPath;
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
                        henchInfo.Value.FixedPowers.Add(new HTalent(p, isFixedPower: true));

                        if (usi.CountsTowardsTalentCount)
                        {
                            henchInfo.Value.NumPowersToAssign--;
                        }
                    }
                }

                Debug.WriteLine($"{henchInfo.Value.HenchInternalName} contributed {numContributed} powers");
                Interlocked.Increment(ref numDone);
                option.ProgressValue = numDone;
                //}
            });
            #endregion


            // Step 2. Shuffle lists to ensure randomness of assignments
            //talentPoolMaster.Shuffle();
            //evolvedTalentPoolMaster.Shuffle();

            // Step 3. Precalculate talent sets that will be assigned
            int baseAttempt = 0;
            bool powersConfigured = false; //if a solution was found
            while (!powersConfigured)
            {
                // Reset
                Debug.WriteLine($"Assigning hench base talent sets, attempt #{baseAttempt++}");
                var basePowerPool = talentPoolMaster.ToList();
                basePowerPool.Shuffle();

                foreach (var hpi in squadmatePackageMap)
                {
                    hpi.Value.ResetTalents();
                }

                // Attempt to build a list of compatible base powers
                bool powerSetsValid = true;
                foreach (var hpi in squadmatePackageMap.Values)
                {
                    if (!hpi.BuildTalentSet(basePowerPool) || hpi.HenchTalentSet == null || !hpi.HenchTalentSet.IsBaseValid)
                    {
                        powerSetsValid = false;
                        break; // Try again, this is not a valid solution to the problem
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
                        foundSolution &= hpi.BuildEvolvedTalentSet(assignmentEvolutions);
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
                    MERLog.Information($"{hpi.HenchInternalName}-----");
                    foreach (var pow in hpi.HenchTalentSet.Powers)
                    {
                        MERLog.Information($" - BP {pow.PowerName} ({pow.BaseName})");
                    }
                    foreach (var pow in hpi.HenchTalentSet.EvolvedPowers)
                    {
                        MERLog.Information($" - EP {pow.PowerName} ({pow.BaseName})");
                    }
                }
            }

            // Step 4. Assign and map powers

            int kitIndex = 0;

            // SINGLE THREAD FOR NOW!! Ini handler is not thread safe

            option.CurrentOperation = "Installing randomized henchmen powers";
            option.ProgressValue = 0;
            option.ProgressMax = squadmatePackageMap.Values.Sum(x => x.Packages.Count);
            numDone = 0;
            foreach (var hpi in squadmatePackageMap.Values.OrderByDescending(x=>x.HenchTalentSet.Powers.Count))
            {
                object tlkSync = new object();
                MERLog.Information($"Assigning talent set for {hpi.HenchInternalName}");

                Parallel.ForEach(hpi.Packages, new ParallelOptions() { MaxDegreeOfParallelism = 3 }, package =>
                  {
                      //foreach (var loadout in assets)
                      //{
                      // We must load a new copy of the package file into memory, one for reading, one for modifying,
                      // otherwise it will be concurrent modification
                      var loadout = package.FindExport(hpi.LoadoutIFP);

                      List<MappedPower> configuredPowers = new List<MappedPower>();

                      var loadoutProps = loadout.GetProperties();
                      var powersList = loadoutProps.GetProp<ArrayProperty<ObjectProperty>>("Powers");

                      var originalCount = powersList.Count;
                      var powersToKeep = powersList.Where(x => !CanBeShuffled(x.ResolveToEntry(package) as ExportEntry, out _)).ToList();
                      powersList.Clear();

                      var talentSet = hpi.HenchTalentSet;
                      kitIndex++;

                      // Assign base powers
                      int powIndex = 0;
                      // Lists for updating the class description in the character creator
                      List<string> combatPowers = new List<string>();
                      List<string> ammoPowers = new List<string>();
                      foreach (var basePower in talentSet.Powers)
                      {
                          if (hpi.FixedPowers.Contains(basePower))
                              continue; // Do not modify this
                          if (basePower.IsAmmoPower)
                              ammoPowers.Add(basePower.PowerName);
                          else if (basePower.IsCombatPower)
                              combatPowers.Add(basePower.PowerName);
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
                      powersList.AddRange(powersToKeep);

                      // Write loadout data
                      if (powersList.Count != originalCount)
                      {
                          Debugger.Break();
                      }

                      loadoutProps.AddOrReplaceProp(powersList);

                      // Build the autoranks
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

                          var power = loadout.FileRef.FindExport(talentSet.Powers[i].PowerExport.InstancedFullPath);
                          var defaults = power.GetDefaults();

                          var properties = defaults.GetProperties();
                          properties.RemoveNamedProperty("Rank");
                          properties.RemoveNamedProperty("UnlockRequirements");

                          if (i == 0 || i == 2)
                          {
                              properties.Add(new FloatProperty(1, "Rank"));
                          }
                          else if (i == 1 || i == 3 || i == 4)
                          {
                              // Has dependency
                              var dependencies = new ArrayProperty<StructProperty>("UnlockRequirements");
                              dependencies.AddRange(GetUnlockRequirementsForPower(loadout.FileRef.FindExport(talentSet.Powers[i - 1].PowerExport.InstancedFullPath)));
                              properties.Add(dependencies);
                          }

                          defaults.WriteProperties(properties);
                      }

                      // Update the string ref for the class description
                      //var tlkStrRef = ClasStrRefMap[kit];
                      //var existingStr = TLKHandler.TLKLookupByLang(tlkStrRef, "INT");
                      //var existingLines = existingStr.Split('\n').ToList();
                      //var powersLineIdx = existingLines.FindIndex(x => x.StartsWith("Power Training:"));
                      //var weaponsLineIdx = existingLines.FindIndex(x => x.StartsWith("Weapon Training:"));
                      //var ammoLineIdx = existingLines.FindIndex(x => x.StartsWith("Ammo Training:"));

                      //existingLines[powersLineIdx] = $"Power Training: {string.Join(", ", combatPowers)}";
                      //var ammoText = $"Ammo Training: {string.Join(", ", ammoPowers)}";

                      //if (ammoLineIdx >= 0 && !ammoPowers.Any())
                      //{
                      //    existingLines.RemoveAt(ammoLineIdx); // no ammo powers. Remove the line
                      //}
                      //else if (ammoLineIdx == -1 && ammoPowers.Any())
                      //{
                      //    existingLines.Insert(weaponsLineIdx + 1, ammoText); // Adding ammo powers. Add the line
                      //}
                      //else if (ammoLineIdx >= 0 && ammoText.Any())
                      //{
                      //    // Replace existing line
                      //    existingLines[ammoLineIdx] = ammoText;
                      //}
                      //else if (ammoLineIdx == -1 && !ammoPowers.Any())
                      //{
                      //    // Do nothing. There's no ammo line and there's no ammo powers.
                      //}
                      //else
                      //{
                      //    // Should not occur!
                      //    Debugger.Break();
                      //}

                      //TLKHandler.ReplaceString(tlkStrRef, string.Join('\n', existingLines), "INT");

                      // Update the autolevel up s t ruct


                      MERFileSystem.SavePackage(loadout.FileRef);
                      Interlocked.Increment(ref numDone);
                      option.ProgressValue = numDone;
                  });
            }

            return true;
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
            new UnshuffleablePower() {PowerName = "SFXPower_StasisNew_Liara", Pawn="liara"},

            new UnshuffleablePower() {PowerName = "SFXPower_KasumiCloakTeleport", Pawn="thief"},
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiUnique", Pawn="thief"},
            new UnshuffleablePower() {PowerName = "SFXPower_KasumiAssassinate", Pawn="thief", CountsTowardsTalentCount = false},
            new UnshuffleablePower() {PowerName = "SFXPower_ZaeedUnique", Pawn="veteran"},

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
        private static List<StructProperty> GetUnlockRequirementsForPower(ExportEntry powerClass)
        {
            void PopulateProps(PropertyCollection props, ExportEntry export, float rank)
            {
                props.Add(new ObjectProperty(export.UIndex, "PowerClass")); // The power that will be checked
                props.Add(new FloatProperty(rank, "Rank")); // Required rank. 1 means at least one point in it and is used for evolved powers which area always at 4.
                props.Add(new StringRefProperty(0, "CustomUnlockText")); // Unused
            }

            List<StructProperty> powerRequirements = new();

            // Unevolved power
            PropertyCollection props = new PropertyCollection();
            PopulateProps(props, powerClass, 2);
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

        private static bool CanShufflePower(ExportEntry power)
        {
            //if (power.ObjectName.Name.Contains("Passive")) return false; // Passives are actually visible and upgradable
            if (power.ObjectName.Name.Contains("SFXPower_Player")) return false;
            if (power.ObjectName.Name.Contains("FirstAid")) return false;
            return true;
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


            int i = 0;
            while (i < 17)
            {
                i++;
            }
            return alui;
        }
    }
}
