using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Enemy;
using ME2Randomizer.Classes.Randomizers.ME2.ExportTypes;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Helpers;
using ME3ExplorerCore.Misc;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
using Newtonsoft.Json;
using EnemyPowerChanger = ME2Randomizer.Classes.Randomizers.ME2.Enemy.EnemyPowerChanger;

namespace ME2Randomizer.Classes
{
    class DataFinder
    {
        private MainWindow mainWindow;
        private BackgroundWorker dataworker;

        public DataFinder(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            dataworker = new BackgroundWorker();

            dataworker.DoWork += FindPortableGuns;
            dataworker.RunWorkerCompleted += ResetUI;

            mainWindow.ShowProgressPanel = true;
            dataworker.RunWorkerAsync();
        }

        private void Fuzzer(object? sender, DoWorkEventArgs e)
        {
            var p = MEPackageHandler.OpenMEPackage(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\BioD_ProNor.pcc");
            var timSKM = p.GetUExport(20872);
            var skmH = ObjectBinary.From<SkeletalMesh>(timSKM);
            foreach (var bone in skmH.RefSkeleton)
            {
                if (!bone.Name.Name.Contains("eye", StringComparison.InvariantCultureIgnoreCase)
                && !bone.Name.Name.Contains("sneer", StringComparison.InvariantCultureIgnoreCase)
                && !bone.Name.Name.Contains("nose", StringComparison.InvariantCultureIgnoreCase))
                    continue;
                var v3 = bone.Position;
                v3.X *= ThreadSafeRandom.NextFloat(0.1, 1.9);
                v3.Y *= ThreadSafeRandom.NextFloat(0.1, 1.9);
                v3.Z *= ThreadSafeRandom.NextFloat(0.1, 1.9);
                bone.Position = v3;
            }
            timSKM.WriteBinary(skmH);
            p.Save();
        }

        #region ActorTypes

        private void FindActorTypes(object? sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x =>
                //                    !x.Contains("_LOC_")
                //&& x.Contains(@"CitHub", StringComparison.InvariantCultureIgnoreCase)
                //)
                //.OrderBy(x => x.Contains("_LOC_"))
                .ToList();

            // PackageName -> GesturePackage
            Dictionary<string, GesturePackage> sourceMapping = new Dictionary<string, GesturePackage>();
            int i = 0;
            mainWindow.CurrentOperationText = "Finding actor types";
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count;
            SortedSet<string> actorTypeNames = new SortedSet<string>();
            TLKHandler.StartHandler();
            foreach (var f in files)
            {
                mainWindow.CurrentProgressValue = i;
                i++;
                var p = MEPackageHandler.OpenMEPackage(f);
                var world = p.FindExport("TheWorld.PersistentLevel");
                if (world != null)
                {
                    var pl = ObjectBinary.From<Level>(world);
                    foreach (var actor in pl.Actors)
                    {
                        if (p.TryGetUExport(actor, out var actorE))
                        {
                            if (actorE.ClassName == "BioPawn")
                            {
                                if (actorE.GetProperty<ObjectProperty>("ActorType")?.ResolveToEntry(p) is ExportEntry atypeexp)
                                {
                                    var displayNameVal = atypeexp.GetProperty<StringRefProperty>("ActorGameNameStrRef");
                                    if (displayNameVal != null)
                                    {
                                        var displayName = TLKHandler.TLKLookupByLang(displayNameVal.Value, "INT");
                                        actorTypeNames.Add($"{atypeexp.ObjectName.Instanced}: {displayName}");
                                    }
                                    else
                                    {
                                        actorTypeNames.Add(atypeexp.ObjectName.Instanced);

                                    }
                                }
                            }
                        }
                    }
                }
            }

            foreach (var atn in actorTypeNames)
            {
                Debug.WriteLine(atn);
            }
        }

        #endregion

        #region Gestures
        private void BuildGestureFiles(object? sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x =>
                //                    !x.Contains("_LOC_")
                //&& x.Contains(@"CitHub", StringComparison.InvariantCultureIgnoreCase)
                //)
                .OrderBy(x => x.Contains("_LOC_"))
                .ToList();

            // PackageName -> GesturePackage
            Dictionary<string, GesturePackage> sourceMapping = new Dictionary<string, GesturePackage>();
            int i = 0;
            mainWindow.CurrentOperationText = "Finding gesture animations";
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count;
            foreach (var f in files)
            {
                mainWindow.CurrentProgressValue = i;
                i++;
                var p = MEPackageHandler.OpenMEPackage(f);
                var gesturePackages = p.Exports.Where(x => x.idxLink == 0 && x.ClassName == "Package" && RBioEvtSysTrackGesture.IsGesturePackage(x.ObjectName)).ToList();
                foreach (var gesturePackage in gesturePackages)
                {
                    // Get package
                    if (!sourceMapping.TryGetValue(gesturePackage.ObjectName, out var gp))
                    {
                        gp = new GesturePackage()
                        {
                            PackageName = gesturePackage.ObjectName
                        };
                        sourceMapping[gesturePackage.ObjectName] = gp;
                    }

                    gp.UpdatePackage(gesturePackage);
                }
            }

            var gestureSaveP = @"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\binary\gestures";
            i = mainWindow.CurrentProgressValue = 0;
            mainWindow.ProgressBar_Bottom_Max = sourceMapping.Count;
            mainWindow.CurrentOperationText = "Building Gesture Packages";

            foreach (var gestP in sourceMapping)
            {
                mainWindow.CurrentProgressValue = i++;
                var pPath = Path.Combine(gestureSaveP, gestP.Key + ".pcc");
                MEPackageHandler.CreateAndSavePackage(pPath, MERFileSystem.Game);
                var gestPackage = MEPackageHandler.OpenMEPackage(pPath);
                gestP.Value.PortIntoPackage(gestPackage);
                gestPackage.Save(compress: true);
            }

            //File.WriteAllLines(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\animseq.txt", alltags);
        }

        private class GesturePackage
        {
            public string PackageName { get; set; }
            public Dictionary<string, GestureGroup> Groups = new Dictionary<string, GestureGroup>();
            public void UpdatePackage(ExportEntry gesturePackage)
            {
                var subAnimSeqs = gesturePackage.FileRef.Exports.Where(x => x.idxLink == gesturePackage.UIndex && x.ClassName == "AnimSequence" && x.ObjectName != "AnimSequence").ToList();
                foreach (var animSeq in subAnimSeqs)
                {
                    var seqName = animSeq.GetProperty<NameProperty>("SequenceName").Value;
                    var groupName = animSeq.ObjectName.Name.Substring(animSeq.ObjectName.Instanced.Length - seqName.Instanced.Length - 1); // +1 for _
                    if (!Groups.TryGetValue(groupName, out var group))
                    {
                        group = new GestureGroup()
                        {
                            GestureGroupName = groupName
                        };
                        Groups[groupName] = group;
                    }

                    group.UpdateGroup(animSeq, seqName);

                }
            }

            public void PortIntoPackage(IMEPackage destPackage)
            {
                PackageCache c = new PackageCache();

                foreach (var v in Groups)
                {
                    v.Value.PortIntoPackage(destPackage, PackageName, c);
                }
                c.ReleasePackages();
            }
        }

        private class GestureGroup
        {
            public string GestureGroupName { get; set; }
            public List<GestureInstance> AnimSequences = new List<GestureInstance>();

            public void UpdateGroup(ExportEntry animSeq, NameReference seqName)
            {
                if (AnimSequences.All(x => x.SequenceName != seqName))
                {
                    AnimSequences.Add(new GestureInstance()
                    {
                        ObjectName = animSeq.ObjectName,
                        SequenceName = seqName,
                        ContainingPackageFile = Path.GetFileName(animSeq.FileRef.FilePath)
                    });
                }
            }

            public void PortIntoPackage(IMEPackage destPackage, string gesturePackageName, PackageCache c)
            {
                foreach (var v in AnimSequences)
                {
                    v.PortIntoPackage(destPackage, gesturePackageName, c);
                }
            }
        }

        public class GestureInstance
        {
            public NameReference ObjectName { get; set; }
            public NameReference SequenceName { get; set; }
            /// <summary>
            /// File that contains this gesture instance
            /// </summary>
            public string ContainingPackageFile { get; set; }

            public void PortIntoPackage(IMEPackage destPackage, string gesturePackageName, PackageCache c)
            {
                var sourceP = c.GetCachedPackage(ContainingPackageFile, true);
                var export = sourceP.FindExport($"{gesturePackageName}.{ObjectName.Instanced}");
                if (export == null)
                    Debugger.Break();
                EntryExporter.ExportExportToPackage(export, destPackage, out var _);
            }
        }

        #endregion

        #region AnimCutscenes
        class AnimCutsceneInfo
        {
            public string PackageFile { get; set; }
            public string SequenceFullName { get; set; }
            public int UIndex { get; private set; }
            public List<ACIVarLink> AttachedActorInfos { get; set; } = new List<ACIVarLink>();
            public AnimCutsceneInfo(ExportEntry interpNode)
            {
                PackageFile = Path.GetFileName(interpNode.FileRef.FilePath);
                SequenceFullName = interpNode.GetProperty<ObjectProperty>("ParentSequence").ResolveToEntry(interpNode.FileRef).InstancedFullPath;
                UIndex = interpNode.UIndex;

                var vars = SeqTools.GetVariableLinksOfNode(interpNode);
                foreach (var v in vars)
                {
                    if (v.LinkDesc == "Data")
                        continue; // It's nothing we care about
                    foreach (var vNode in v.LinkedNodes.OfType<ExportEntry>())
                    {
                        AttachedActorInfos.Add(new ACIVarLink(v.LinkDesc, vNode));
                    }
                }
            }
        }

        class ACIVarLink
        {
            public enum EObjectType
            {
                Invalid,
                FindByTag,
                DirectReference,
                DynamicObject,
                Player
            }
            public string FullPath { get; set; }
            public EObjectType ObjectType { get; set; }
            public string LinkDesc { get; set; }
            /// <summary>
            /// Only used if FindByTag ObjectType
            /// </summary>
            public string TagToFind { get; set; }
            public ACIVarLink(string linkDesc, ExportEntry node)
            {
                LinkDesc = linkDesc;
                FullPath = node.InstancedFullPath;
                TagToFind = node.GetProperty<StrProperty>("m_sObjectTagToFind")?.Value; // If class is wrong, we will not find this
                if (TagToFind != null)
                {
                    ObjectType = EObjectType.FindByTag;
                }
                else
                {
                    switch (node.ClassName)
                    {
                        case "SeqVar_Object":
                            var objValue = node.GetProperty<ObjectProperty>("ObjValue");
                            ObjectType = objValue != null ? EObjectType.DirectReference : EObjectType.DynamicObject;
                            break;
                        case "SeqVar_Player":
                            ObjectType = EObjectType.Player;
                            break;
                    }
                }

            }

            public bool IsAllowedTag()
            {
                if (TagToFind == null) return false;

                // TAG IS
                if (TagToFind == "Normandy2_") return false;
                if (TagToFind == "BIOTIC_ESCORT") return false; // Required for long walk to work properly
                if (TagToFind == "flare_geth") return false;
                if (TagToFind == "Geth_Sparks") return false;
                if (TagToFind == "Illusive_Chair") return false;
                if (TagToFind == "FireEXT") return false;
                if (TagToFind == "Skel_GarrusHelmet") return false;
                if (TagToFind == "ChairComfy012_") return false;
                if (TagToFind == "TableLab051_") return false;
                if (TagToFind == "DataPad1_") return false;
                if (TagToFind == "Cinegun") return false;
                if (TagToFind == "CineShieldGen") return false;
                if (TagToFind == "cutscene_Normandy") return false;
                if (TagToFind == "FlyingCar1") return false;
                if (TagToFind == "force_Bubble") return false;
                if (TagToFind == "Hatch_door_01") return false;
                if (TagToFind == "ElevatorDoorInterp") return false;
                if (TagToFind == "GunShip_flare") return false;
                if (TagToFind == "Minigun_Muzzle") return false;
                if (TagToFind == "SM_CutsceneGunship") return false;
                if (TagToFind == "DropShip2_") return false;

                if (TagToFind == "citgrl_LandingCar") return false;
                if (TagToFind == "Normandy21_") return false;
                if (TagToFind == "Normandy21_") return false;
                if (TagToFind == "Normandy21_") return false;
                if (TagToFind == "Normandy21_") return false;


                // Starts With
                if (TagToFind.StartsWith("Cam", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("CinePlatform", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("Prop_", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("holoscreen", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("Door_", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("CineDatapad", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("CineDropship", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("GunShip_", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("LeftTop", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("RightTop", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("RightBeam", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("LeftBeam", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("LeftInter", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("RightBack", StringComparison.InvariantCultureIgnoreCase)) return false;
                if (TagToFind.StartsWith("RightInter", StringComparison.InvariantCultureIgnoreCase)) return false;

                // Contains
                //                if (TagToFind.Contains("Normandy", StringComparison.InvariantCultureIgnoreCase)) return false;
                return true;
            }
        }

        private List<AnimCutsceneInfo> CutsceneInfos = new List<AnimCutsceneInfo>();

        private void AnalyzeAnimCutscenes(object? sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                .Where(x =>
                        //x.Contains(@"\DLC\", StringComparison.InvariantCultureIgnoreCase)
                        //&&
                        !x.Contains("_LOC_", StringComparison.InvariantCultureIgnoreCase)
                //x.Contains("ReaperCombat")
                )
                .ToList();

            int i = 0;
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count;
            foreach (var f in files)
            {
                mainWindow.CurrentProgressValue = i;
                i++;
                var p = MEPackageHandler.OpenMEPackage(f);
                var animCutsceneInterps = p.Exports.Where(x => x.ClassName == "SeqAct_Interp" && x.GetProperty<StrProperty>("ObjName") is StrProperty strp && strp.Value.StartsWith("ANIMCUTSCENE_")).ToList();
                foreach (var animCutscene in animCutsceneInterps)
                {
                    CutsceneInfos.Add(new AnimCutsceneInfo(animCutscene));
                }
            }

            var alltags = CutsceneInfos.SelectMany(x => x.AttachedActorInfos
                .Where(y => y.ObjectType == ACIVarLink.EObjectType.FindByTag && y.IsAllowedTag())
                .Select(y => y.TagToFind)).Distinct().ToList();

            File.WriteAllLines(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\allowedcutscenerandomizationtags.txt", alltags);
        }

        #endregion

        #region Min1Health
        private void FindMin1Health(object? sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                .Where(x =>
                x.Contains("procer", StringComparison.InvariantCultureIgnoreCase)
                //x.Contains("ReaperCombat")
                 )
                .ToList();

            foreach (var f in files)
            {
                var p = MEPackageHandler.OpenMEPackage(f);
                var modifyPPs = p.Exports.Where(x => x.ClassName == "BioSeqAct_ModifyPropertyPawn").ToList();
                foreach (var modifyPP in modifyPPs)
                {
                    var vlinks = SeqTools.GetVariableLinksOfNode(modifyPP);
                    var min1health = vlinks.FirstOrDefault(x => x.LinkDesc.Equals("Min1Health"));
                    if (min1health != null)
                    {
                        // is it set to true?
                        foreach (var node in min1health.LinkedNodes.OfType<ExportEntry>())
                        {
                            var isMin1 = node.GetProperty<IntProperty>("bValue")?.Value == 1;
                            if (isMin1)
                            {
                                var target = vlinks.FirstOrDefault(x => x.LinkDesc == "Target");
                                var seq = node.GetProperty<ObjectProperty>("ParentSequence").ResolveToEntry(p) as ExportEntry;
                                Debug.WriteLine($"Min1Health in {Path.GetFileName(f)}, export {node.UIndex}, sequence {seq.InstancedFullPath}, target {target.LinkedNodes[0].ClassName}");
                            }
                        }
                    }
                }

            }
        }
        #endregion

        private void ResetUI(object sender, RunWorkerCompletedEventArgs e)
        {
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.CurrentProgressValue = 0;
            mainWindow.ShowProgressPanel = false;
            mainWindow.CurrentOperationText = "Data finder done";
        }

        #region Guns
        private void FindPortableGuns(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x =>
                //x.Contains("blackstorm", StringComparison.InvariantCultureIgnoreCase)
                ////x.Contains("ReaperCombat")
                // )
                .ToList();
            mainWindow.CurrentOperationText = "Building Weapon List for enemies";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            var startupFileCache = GetGlobalCache();

            // Maps instanced full path to list of instances
            ConcurrentDictionary<string, List<EnemyWeaponChanger.GunInfo>> mapping = new ConcurrentDictionary<string, List<EnemyWeaponChanger.GunInfo>>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
            {
                mainWindow.CurrentOperationText = $"Building Weapon List for enemies [{numdone}/{numtodo}]";
                Interlocked.Increment(ref numdone);

                var package = MEPackageHandler.OpenMEPackage(file);
                var sfxweapons = package.Exports.Where(x => x.InheritsFrom("SFXWeapon") && x.IsClass && !x.IsDefaultObject);
                foreach (var skm in sfxweapons)
                {
                    BuildGunInfo(skm, mapping, package, startupFileCache, false);
                }
                mainWindow.CurrentProgressValue = numdone;
            });

            // Corrected, embedded guns that required file coalescing for portability
            var correctedGuns = Utilities.ListStaticAssets("binary.correctedloadouts.weapons");
            foreach (var cg in correctedGuns)
            {
                var pData = Utilities.GetEmbeddedStaticFile(cg, true);
                var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(pData), Utilities.GetFilenameFromAssetName(cg)); //just any path
                var sfxweapons = package.Exports.Where(x => x.InheritsFrom("SFXWeapon") && x.IsClass && !x.IsDefaultObject).ToList();
                foreach (var skm in sfxweapons)
                {
                    BuildGunInfo(skm, mapping, package, startupFileCache, true);
                }
            }
            // PERFORM REDUCE OPERATION

            // Order by not needing startup, then by filesize so we can have smallest files loaded
            foreach (var gunL in mapping)
            {
                gunL.Value.ReplaceAll(gunL.Value.OrderBy(x => x.RequiresStartupPackage).ThenBy(x => x.PackageFileSize).ToList());
            }

            // Build gun info list
            var reducedGunInfos = mapping.Select(x => x.Value[0]);

            var jsonList = JsonConvert.SerializeObject(reducedGunInfos, Formatting.Indented);
            File.WriteAllText(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\weaponlistme2.json", jsonList);

            UnusableGuns.RemoveAll(x => reducedGunInfos.Any(y => y.GunName == x.Key));
            File.WriteAllLines(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\unusableguns.json", UnusableGuns.Keys.ToList());

        }

        private static ConcurrentDictionary<string, string> UnusableGuns = new ConcurrentDictionary<string, string>();

        private void BuildGunInfo(ExportEntry skm, ConcurrentDictionary<string, List<EnemyWeaponChanger.GunInfo>> mapping, IMEPackage package, PackageCache startupFileCache, bool isCorrectedPackage)
        {
            // See if power is fully defined in package?
            var classInfo = ObjectBinary.From<UClass>(skm);
            if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                return; // This class cannot be used as a power, it is abstract

            var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
            var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            var usable = CheckImports(importDependencies, package, startupFileCache, out var missingImport);
            if (usable)
            {
                var pi = new EnemyWeaponChanger.GunInfo(skm, isCorrectedPackage);
                if ((pi.RequiresStartupPackage && !pi.PackageFileName.StartsWith("SFX"))
                    || pi.GunName.Contains("Player")
                    || pi.GunName.Contains("AsteroidRocketLauncher")
                    || pi.GunName.Contains("VehicleRocketLauncher")
                )
                {
                    // We do not allow startup files that have levels
                    pi.IsUsable = false;
                }
                if (pi.IsUsable)
                {
                    Debug.WriteLine($"Usable sfxweapon: {skm.InstancedFullPath} in {Path.GetFileName(package.FilePath)}");
                    if (!mapping.TryGetValue(skm.InstancedFullPath, out var instanceList))
                    {
                        instanceList = new List<EnemyWeaponChanger.GunInfo>();
                        mapping[skm.InstancedFullPath] = instanceList;
                    }

                    instanceList.Add(pi);
                }
            }
            else
            {
                Debug.WriteLine($"Not usable weapon: {skm.InstancedFullPath} in {Path.GetFileName(package.FilePath)}, missing import {missingImport.FullPath}");
                if (mapping.ContainsKey(skm.InstancedFullPath))
                {
                    UnusableGuns.Remove(skm.InstancedFullPath, out _);
                }
                else
                {
                    UnusableGuns[skm.InstancedFullPath] = missingImport.FullPath;
                }
            }
        }

        private void BuildVisibleLoadoutsMap(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x => x.Contains("SFXWeapon") || x.Contains("BioP"))
                .ToList();
            mainWindow.CurrentOperationText = "Scanning for stuff";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            var startupFileCache = GetGlobalCache();

            // Maps instanced full path to list of instances
            // Loadout full path -> caller supports visible weapons
            ConcurrentDictionary<string, bool> loadoutmap = new ConcurrentDictionary<string, bool>();
            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
            {
                mainWindow.CurrentOperationText = $"Scanning for stuff [{numdone}/{numtodo}]";
                Interlocked.Increment(ref numdone);

                var package = MEPackageHandler.OpenMEPackage(file);
                var pawnClasses = package.Exports.Where(x => x.InheritsFrom("SFXPawn") && x.IsClass && !x.IsDefaultObject);
                foreach (var skm in pawnClasses.Where(x => !loadoutmap.ContainsKey(x.InstancedFullPath)))
                {
                    // See if power is fully defined in package?
                    var classInfo = ObjectBinary.From<UClass>(skm);
                    if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                        continue; // This class cannot be used as a power, it is abstract

                    var defaults = package.GetUExport(classInfo.Defaults);

                    var supportsVisibleWeapons = defaults.GetProperty<BoolProperty>("bSupportsVisibleWeapons")?.Value ?? true;

                    // Get loadout
                    var actorTypeObj = defaults.GetProperty<ObjectProperty>("ActorType");
                    if (actorTypeObj != null && actorTypeObj.Value > 0)
                    {
                        var actorType = package.GetUExport(actorTypeObj.Value);
                        var loadoutObj = actorType.GetProperty<ObjectProperty>("Loadout");
                        if (loadoutObj != null && loadoutObj.Value > 0)
                        {
                            var loadout = package.GetUExport(loadoutObj.Value);
                            if (loadout.GetProperty<ArrayProperty<ObjectProperty>>("Weapons") != null)
                            {
                                loadoutmap[loadout.InstancedFullPath] = supportsVisibleWeapons;
                            }
                        }
                    }

                    // mark
                }
                mainWindow.CurrentProgressValue = numdone;
            });

            // PERFORM REDUCE OPERATION

            // Count the number of times a file is referenced for a power
            //Dictionary<string, int> fileUsages = new Dictionary<string, int>();
            //foreach (var powerPair in mapping)
            //{
            //    foreach (var powerInfo in powerPair.Value)
            //    {
            //        if (!fileUsages.TryGetValue(powerInfo.PackageFileName, out var fileUsageInt))
            //        {
            //            fileUsages[powerInfo.PackageFileName] = 1;
            //        }
            //        else
            //        {
            //            fileUsages[powerInfo.PackageFileName] = fileUsageInt + 1;
            //        }
            //    }
            //}

            // Sort file usages by count, highest to lowest
            //var gunListSS = fileUsages.Select(x => (x.Key, x.Value)).ToList();
            //gunListSS = gunListSS.OrderByDescending(x => x.Value).ToList();
            //var gunList = gunListSS.Select(x => x.Key).ToList(); // Drop the tuple part, we don't care about it

            //// Build power info list
            //var reducedGunInfos = new List<EnemyWeaponChanger.GunInfo>();

            //foreach (var gunFile in gunList)
            //{
            //    // Get powers that are in this file
            //    var items = mapping.Where(x => x.Value.Any(x => x.PackageFileName == gunFile)).ToList();
            //    foreach (var item in items)
            //    {
            //        reducedGunInfos.Add(item.Value.FirstOrDefault(x => x.PackageFileName == gunFile));
            //        mapping.Remove(item.Key, out var removedItem);
            //    }
            //}

            var jsonList = JsonConvert.SerializeObject(loadoutmap, Formatting.Indented);
            File.WriteAllText(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\weaponloadoutrules.json", jsonList);


            // Coagulate stuff
            //Dictionary<string, int> counts = new Dictionary<string, int>();
            //foreach (var v in listM)
            //{
            //    foreach (var k in v.Value)
            //    {
            //        int existingC = 0;
            //        counts.TryGetValue(k, out existingC);
            //        existingC++;
            //        counts[k] = existingC;
            //    }
            //}

            //foreach (var count in counts.OrderBy(x => x.Key))
            //{
            //    Debug.WriteLine($"{count.Key}\t\t\t{count.Value}");
            //}
        }
        #endregion

        #region Powers
        private void BuildPowerInfo(ExportEntry powerExport, ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>> mapping, IMEPackage package, PackageCache startupFileCache, bool isCorrectedPackage)
        {
            // See if power is fully defined in package?
            var classInfo = ObjectBinary.From<UClass>(powerExport);
            if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                return; // This class cannot be used as a power, it is abstract

            var dependencies = EntryImporter.GetAllReferencesOfExport(powerExport);
            var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            var usable = CheckImports(importDependencies, package, startupFileCache, out var missingImport);
            if (usable)
            {
                var pi = new EnemyPowerChanger.PowerInfo(powerExport, isCorrectedPackage);
                if ((pi.RequiresStartupPackage &&
                     !pi.PackageFileName.StartsWith("SFX"))
                //|| pi.GunName.Contains("Player")
                //|| pi.GunName.Contains("AsteroidRocketLauncher")
                //|| pi.GunName.Contains("VehicleRocketLauncher"

                )
                {
                    // We do not allow startup files that have levels
                    pi.IsUsable = false;
                }
                if (pi.IsUsable)
                {
                    Debug.WriteLine($"Usable sfxpower: {powerExport.InstancedFullPath} in {Path.GetFileName(package.FilePath)}");
                    if (!mapping.TryGetValue(powerExport.InstancedFullPath, out var instanceList))
                    {
                        instanceList = new List<EnemyPowerChanger.PowerInfo>();
                        mapping[powerExport.InstancedFullPath] = instanceList;
                    }

                    instanceList.Add(pi);
                }
            }
            else
            {
                Debug.WriteLine($"Not usable weapon: {powerExport.InstancedFullPath} in {Path.GetFileName(package.FilePath)}, missing import {missingImport.FullPath}");
                if (mapping.ContainsKey(powerExport.InstancedFullPath))
                {
                    UnusableGuns.Remove(powerExport.InstancedFullPath, out _);
                }
                else
                {
                    UnusableGuns[powerExport.InstancedFullPath] = missingImport.FullPath;
                }
            }
        }

        private void FindPortablePowers(object sender, DoWorkEventArgs e)
        {
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                //.Where(x => x.Contains("SFXPower") || x.Contains("BioP"))
                .Where(x => x.Contains("SFXPower_StasisNew"))
                .ToList();
            mainWindow.CurrentOperationText = "Scanning for stuff";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.ProgressBar_Bottom_Min = 0;

            var startupFileCache = GetGlobalCache();

            // Maps instanced full path to list of instances
            ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>> mapping = new ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>>();

            // Corrected, embedded powers that required file coalescing for portability or other corrections in order to work on enemies
            var correctedPowers = Utilities.ListStaticAssets("binary.correctedloadouts.powers");
            foreach (var cg in correctedPowers)
            {
                var pData = Utilities.GetEmbeddedStaticFile(cg, true);
                var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(pData), Utilities.GetFilenameFromAssetName(cg)); //just any path
                var sfxPowers = package.Exports.Where(x => x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject);
                foreach (var sfxPow in sfxPowers)
                {
                    BuildPowerInfo(sfxPow, mapping, package, startupFileCache, true);
                }
            }

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
              {
                  mainWindow.CurrentOperationText = $"Scanning for stuff [{numdone}/{numtodo}]";
                  Interlocked.Increment(ref numdone);

                  var package = MEPackageHandler.OpenMEPackage(file);
                  var powers = package.Exports.Where(x => x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject);
                  foreach (var skm in powers.Where(x => !mapping.ContainsKey(x.InstancedFullPath)))
                  {
                      // See if power is fully defined in package?
                      var classInfo = ObjectBinary.From<UClass>(skm);
                      if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                          continue; // This class cannot be used as a power, it is abstract

                      var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
                      var importDependencies = dependencies.OfType<ImportEntry>().ToList();
                      var usable = CheckImports(importDependencies, package, startupFileCache, out var missingImport);
                      if (usable)
                      {
                          var pi = new EnemyPowerChanger.PowerInfo(skm, false);

                          if (pi.IsUsable)
                          {
                              Debug.WriteLine($"Usable power: {skm.InstancedFullPath} in {package.FilePath}");
                              if (!mapping.TryGetValue(skm.InstancedFullPath, out var instanceList))
                              {
                                  instanceList = new List<EnemyPowerChanger.PowerInfo>();
                                  mapping[skm.InstancedFullPath] = instanceList;
                              }

                              instanceList.Add(pi);
                          }
                      }
                      else
                      {
                          //Debug.WriteLine($"Not usable power: {skm.InstancedFullPath} in {package.FilePath}");
                      }
                  }
                  mainWindow.CurrentProgressValue = numdone;
              });

            // PERFORM REDUCE OPERATION

            // Count the number of times a file is referenced for a power
            Dictionary<string, int> fileUsages = new Dictionary<string, int>();
            foreach (var powerPair in mapping)
            {
                foreach (var powerInfo in powerPair.Value)
                {
                    if (!fileUsages.TryGetValue(powerInfo.PackageFileName, out var fileUsageInt))
                    {
                        fileUsages[powerInfo.PackageFileName] = 1;
                    }
                    else
                    {
                        fileUsages[powerInfo.PackageFileName] = fileUsageInt + 1;
                    }
                }
            }

            // Sort file usages by count, highest to lowest
            var powerListSS = fileUsages.Select(x => (x.Key, x.Value)).ToList();
            powerListSS = powerListSS.OrderByDescending(x => x.Value).ToList();
            var powerList = powerListSS.Select(x => x.Key).ToList(); // Drop the tuple part, we don't care about it

            // Build power info list
            List<EnemyPowerChanger.PowerInfo> reducedPowerInfos = new List<EnemyPowerChanger.PowerInfo>();

            foreach (var powerFile in powerList)
            {
                // Get powers that are in this file
                var items = mapping.Where(x => x.Value.Any(x => x.PackageFileName == powerFile)).ToList();
                foreach (var item in items)
                {
                    reducedPowerInfos.Add(item.Value.FirstOrDefault(x => x.PackageFileName == powerFile));
                    mapping.Remove(item.Key, out var removedItem);
                }
            }

            var jsonList = JsonConvert.SerializeObject(reducedPowerInfos, Formatting.Indented);
            File.WriteAllText(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\powerlistme2.json", jsonList);


            // Coagulate stuff
            //Dictionary<string, int> counts = new Dictionary<string, int>();
            //foreach (var v in listM)
            //{
            //    foreach (var k in v.Value)
            //    {
            //        int existingC = 0;
            //        counts.TryGetValue(k, out existingC);
            //        existingC++;
            //        counts[k] = existingC;
            //    }
            //}

            //foreach (var count in counts.OrderBy(x => x.Key))
            //{
            //    Debug.WriteLine($"{count.Key}\t\t\t{count.Value}");
            //}
        }
        #endregion

        #region Utilities
        private static MERPackageCache GetGlobalCache()
        {
            var cache = new MERPackageCache();
            cache.GetCachedPackage("Core.pcc");
            cache.GetCachedPackage("SFXGame.pcc");
            cache.GetCachedPackage("Startup_INT.pcc");
            cache.GetCachedPackage("Engine.pcc");
            cache.GetCachedPackage("WwiseAudio.pcc");
            cache.GetCachedPackage("SFXOnlineFoundation.pcc");
            cache.GetCachedPackage("PlotManagerMap.pcc");
            cache.GetCachedPackage("GFxUI.pcc");
            return cache;
        }

        /// <summary>
        /// Checks to see if the listed imports can be reliably resolved as being in memory via their parents and localizations.
        /// </summary>
        /// <param name="imports"></param>
        private static bool CheckImports(List<ImportEntry> imports, IMEPackage package, PackageCache globalCache, out ImportEntry unresolvableEntry)
        {
            // Force into memory
            //var importExtras = EntryImporter.GetPossibleAssociatedFiles(package);
            MERPackageCache lpc = new MERPackageCache();
            //foreach (var ie in importExtras)
            //{
            //    lpc.GetCachedPackage(ie);
            //}

            // Enumerate and resolve all imports.
            bool canBeUsed = true;
            foreach (var import in imports)
            {
                if (import.InstancedFullPath == "BioVFX_Z_TEXTURES.Generic.Glass_Shards_Norm")
                    continue; // this is native for some reason

                //Debug.Write($@"Resolving {import.FullPath}...");
                var export = ResolveImport(import, globalCache, lpc);
                if (export != null)
                {
                    //Debug.WriteLine($@" OK");
                }
                else if (UnrealObjectInfo.IsAKnownNativeClass(import))
                {
                    // Debug.WriteLine($@" OK, in native");
                }
                else
                {
                    lpc.ReleasePackages();
                    unresolvableEntry = import;
                    return false;
                    Debug.WriteLine($@" {import.FullPath} UNRESOLVABLE!");
                    //Debugger.Break();
                }
            }
            lpc.ReleasePackages();
            unresolvableEntry = null;
            return true;
        }

        public static ExportEntry ResolveImport(ImportEntry entry, PackageCache globalCache, PackageCache localCache)
        {
            var entryFullPath = entry.InstancedFullPath;


            string containingDirectory = Path.GetDirectoryName(entry.FileRef.FilePath);
            var filesToCheck = new List<string>();
            CaseInsensitiveDictionary<string> gameFiles = MELoadedFiles.GetFilesLoadedInGame(entry.Game);

            string upkOrPcc = entry.Game == MEGame.ME1 ? ".upk" : ".pcc";
            // Check if there is package that has this name. This works for things like resolving SFXPawn_Banshee
            bool addPackageFile = gameFiles.TryGetValue(entry.ObjectName + upkOrPcc, out var efxPath) && !filesToCheck.Contains(efxPath);

            // Let's see if there is same-named top level package folder file. This will resolve class imports from SFXGame, Engine, etc.
            IEntry p = entry.Parent;
            if (p != null)
            {
                while (p.Parent != null)
                {
                    p = p.Parent;
                }

                if (p.ClassName == "Package")
                {
                    if (gameFiles.TryGetValue($"{p.ObjectName}{upkOrPcc}", out var efPath) && !filesToCheck.Contains(efxPath))
                    {
                        filesToCheck.Add(Path.GetFileName(efPath));
                    }
                    else if (entry.Game == MEGame.ME1)
                    {
                        if (gameFiles.TryGetValue(p.ObjectName + ".u", out var path) && !filesToCheck.Contains(efxPath))
                        {
                            filesToCheck.Add(Path.GetFileName(path));
                        }
                    }
                }
            }

            // 
            filesToCheck.Add(entry.PackageFile + upkOrPcc);

            if (addPackageFile)
            {
                filesToCheck.Add(Path.GetFileName(efxPath));
            }



            //add related files that will be loaded at the same time (eg. for BioD_Nor_310, check BioD_Nor_310_LOC_INT, BioD_Nor, and BioP_Nor)
            filesToCheck.AddRange(EntryImporter.GetPossibleAssociatedFiles(entry.FileRef));



            if (entry.Game == MEGame.ME3)
            {
                // Look in BIOP_MP_Common. This is not a 'safe' file but it is always loaded in MP mode and will be commonly referenced by MP files
                if (gameFiles.TryGetValue("BIOP_MP_COMMON.pcc", out var efPath))
                {
                    filesToCheck.Add(Path.GetFileName(efPath));
                }
            }


            //add base definition files that are always loaded (Core, Engine, etc.)
            foreach (var fileName in EntryImporter.FilesSafeToImportFrom(entry.Game))
            {
                if (gameFiles.TryGetValue(fileName, out var efPath))
                {
                    filesToCheck.Add(Path.GetFileName(efPath));
                }
            }

            //add startup files (always loaded)
            IEnumerable<string> startups;
            if (entry.Game == MEGame.ME2)
            {
                startups = gameFiles.Keys.Where(x => x.Contains("Startup_", StringComparison.InvariantCultureIgnoreCase) && x.Contains("_INT", StringComparison.InvariantCultureIgnoreCase)); //me2 this will unfortunately include the main startup file
            }
            else
            {
                startups = gameFiles.Keys.Where(x => x.Contains("Startup_", StringComparison.InvariantCultureIgnoreCase)); //me2 this will unfortunately include the main startup file
            }

            filesToCheck = filesToCheck.Distinct().ToList();

            foreach (var fileName in filesToCheck.Concat(startups.Select(x => Path.GetFileName(gameFiles[x]))))
            {
                //if (gameFiles.TryGetValue(fileName, out var fullgamepath) && File.Exists(fullgamepath))
                //{
                var export = containsImportedExport(fileName);
                if (export != null)
                {
                    return export;
                }
                //}

                //Try local.
                //                var localPath = Path.Combine(containingDirectory, fileName);
                //              if (!localPath.Equals(fullgamepath, StringComparison.InvariantCultureIgnoreCase) && File.Exists(localPath))
                //            {
                //var export = containsImportedExport(fileName);
                //if (export != null)
                //{
                //    return export;
                //}
                //          }
            }
            return null;

            //Perform check and lookup
            ExportEntry containsImportedExport(string packagePath)
            {
                //Debug.WriteLine($"Checking file {packagePath} for {entryFullPath}");

                var package = globalCache.GetCachedPackage(packagePath, false);
                package ??= localCache.GetCachedPackage(packagePath, true);
                var packName = Path.GetFileNameWithoutExtension(packagePath);
                var packageParts = entryFullPath.Split('.').ToList();
                if (packageParts.Count > 1 && packName == packageParts[0])
                {
                    packageParts.RemoveAt(0);
                    entryFullPath = string.Join(".", packageParts);
                }
                else if (packName == packageParts[0])
                {
                    //it's literally the file itself (an imported package like SFXGame)
                    return package.Exports.FirstOrDefault(x => x.idxLink == 0); //this will be at top of the tree
                }

                return package?.FindExport(entryFullPath);
            }
        }
        #endregion

    }
}
