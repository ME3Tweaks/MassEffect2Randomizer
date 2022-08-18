using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using Newtonsoft.Json;
using Randomizer.MER;
using Randomizer.Randomizers;
using Randomizer.Randomizers.Utility;

namespace RandomizerUI.Classes
{
    class DataFinder
    {
        private MainWindow mainWindow;
        private BackgroundWorker dataworker;

        void srUpdate(object? o, EventArgs eventArgs)
        {
            if (o is RandomizationOption option)
            {
                mainWindow.ProgressBarIndeterminate = option.ProgressIndeterminate;
                mainWindow.CurrentProgressValue = option.ProgressValue;
                mainWindow.ProgressBar_Bottom_Max = option.ProgressMax;
                if (option.CurrentOperation != null)
                {
                    mainWindow.CurrentOperationText = option.CurrentOperation;
                }
            }
        }

        public DataFinder(MainWindow mainWindow)
        {
#if DEBUG
            this.mainWindow = mainWindow;
            dataworker = new BackgroundWorker();

            // For UI binding.
            RandomizationOption option = new RandomizationOption();
            option.OnOperationUpdate += srUpdate;
            dataworker.DoWork += MERDebug.DebugPrintActorNames;
            //dataworker.RunWorkerCompleted += MERDebug.DebugPrintActorNames;
            mainWindow.ShowProgressPanel = true;
            dataworker.RunWorkerAsync(option);
            dataworker.RunWorkerCompleted += (sender, args) =>
            {
                option.OnOperationUpdate -= srUpdate;
                mainWindow.ShowProgressPanel = false;
            };
#endif
        }

        //private void Fuzzer(object? sender, DoWorkEventArgs e)
        //{
        //    var p = MEPackageHandler.OpenMEPackage(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\BioD_ProNor.pcc");
        //    var timSKM = p.GetUExport(20872);
        //    var skmH = ObjectBinary.From<SkeletalMesh>(timSKM);
        //    foreach (var bone in skmH.RefSkeleton)
        //    {
        //        if (!bone.Name.Name.Contains("eye", StringComparison.InvariantCultureIgnoreCase)
        //        && !bone.Name.Name.Contains("sneer", StringComparison.InvariantCultureIgnoreCase)
        //        && !bone.Name.Name.Contains("nose", StringComparison.InvariantCultureIgnoreCase))
        //            continue;
        //        var v3 = bone.Position;
        //        v3.X *= ThreadSafeRandom.NextFloat(0.1, 1.9);
        //        v3.Y *= ThreadSafeRandom.NextFloat(0.1, 1.9);
        //        v3.Z *= ThreadSafeRandom.NextFloat(0.1, 1.9);
        //        bone.Position = v3;
        //    }
        //    timSKM.WriteBinary(skmH);
        //    p.Save();
        //}
        /*

        #region Music
        private void BuildMusicInfo(object sender, DoWorkEventArgs e)
        {
            // SETUP STAGE 1
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, includeAFCs: true).Values
                .Where(x => Path.GetFileNameWithoutExtension(x).StartsWith("SFXGame") || Path.GetFileNameWithoutExtension(x).StartsWith("BioS") || (x.EndsWith(".afc")/* && x.Contains("wwise", StringComparison.InvariantCultureIgnoreCase)))
                //.Where(x => x.Contains("SFXPower_StasisNew"))
                .ToList();
            mainWindow.CurrentOperationText = "Finding music";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count(x => x.RepresentsPackageFilePath());
            mainWindow.CurrentProgressValue = 0;

            // PREP WORK
            //var startupFileCache = GetGlobalCache();

            // Maps instanced full path to list of instances
            ConcurrentDictionary<string, List<RMusic.MusicStreamInfo>> mapping = new();
            var syncObj = new object();
            foreach (var sf in files)
            {
                mainWindow.CurrentOperationText = $"Finding music [{numdone}/{numtodo}]";
                mainWindow.CurrentProgressValue = numdone;
                Interlocked.Increment(ref numdone);
                if (!sf.RepresentsPackageFilePath())
                    continue; // Not a package
                var package = MEPackageHandler.OpenMEPackage(sf);
                foreach (var exp in package.Exports.Where(x => !x.IsDefaultObject && x.ClassName == "WwiseStream"))
                {
                    RMusic.MusicStreamInfo musInfo = new RMusic.MusicStreamInfo(exp, files);
                    if (musInfo.IsUsable)
                    {
                        Debug.WriteLine($"Usable music: {musInfo.StreamFullPath}");
                        lock (syncObj)
                        {
                            if (!mapping.TryGetValue(musInfo.StreamFullPath, out var existingList))
                            {
                                existingList = new List<RMusic.MusicStreamInfo>();
                                mapping[musInfo.StreamFullPath] = existingList;
                            }

                            existingList.Add(musInfo);
                        }
                    }
                }
            }

            // Perform reduce operation
            List<RMusic.MusicStreamInfo> reduced = new();
            foreach (var v in mapping)
            {
                // We only care about the count. Not the individual infos.
                var item = v.Value.First();
                item.InstanceCount = v.Value.Count;
                reduced.Add(item);
            }

            var jsonList = JsonConvert.SerializeObject(reduced, Formatting.Indented);
            File.WriteAllText(@"C:\Users\mgame\source\repos\ME2Randomizer\ME2Randomizer\staticfiles\text\musiclistme2.json", jsonList);
        }
        #endregion
        */
        /*
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
                                        actorTypeNames.Add($"{atypeexp.ObjectName.Instanced}: {displayNameVal.Value} {displayName}");
                                    }
                                    else
                                    {
                                        // try behavior lookup instead
                                        var behavior = actorE.GetProperty<ObjectProperty>("m_oBehavior");
                                        if (behavior?.ResolveToEntry(p) is ExportEntry behav)
                                        {
                                            displayNameVal = behav.GetProperty<StringRefProperty>("ActorGameNameStrRef");
                                            if (displayNameVal != null)
                                            {
                                                var displayName = TLKHandler.TLKLookupByLang(displayNameVal.Value, "INT");
                                                actorTypeNames.Add($"{atypeexp.ObjectName.Instanced}: {displayNameVal.Value} {displayName}");
                                                continue;
                                            }
                                        }

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
        */

        /*

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
                MERPackageCache c = new MERPackageCache();

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

        #region Shifthead
        private void HeadShift(object? sender, DoWorkEventArgs e)
        {
            var p = MEPackageHandler.OpenMEPackage(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\BioH_Leading_00-orig.pcc");
            var mdl = p.GetUExport(5250); ;
            var objBin = ObjectBinary.From<SkeletalMesh>(mdl);

            float shiftAmt = -10;
            // Shift head
            //foreach (var refSkelItem in objBin.RefSkeleton)
            //{
            //    var pos = refSkelItem.Position;
            //    pos.X += 20;
            //    refSkelItem.Position = pos;
            //}

            foreach (var lod in objBin.LODModels)
            {
                foreach (var vertex in lod.VertexBufferGPUSkin.VertexData)
                {
                    var pos = vertex.Position;
                    pos.Z += shiftAmt;
                    vertex.Position = pos;
                }
            }

            mdl.WriteBinary(objBin);
            p.Save(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\BioH_Leading_00.pcc");
        }
        #endregion
        */
        private void ResetUI(object sender, RunWorkerCompletedEventArgs e)
        {
            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.CurrentProgressValue = 0;
            mainWindow.ShowProgressPanel = false;
            mainWindow.CurrentOperationText = "Data finder done";
        }

        /*

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
            mainWindow.CurrentProgressValue = 0;

            var startupFileCache = MERFileSystem.GetGlobalCache();

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
            var correctedGuns = MERUtilities.ListStaticAssets("binary.correctedloadouts.weapons");
            foreach (var cg in correctedGuns)
            {
                var pData = MERUtilities.GetEmbeddedStaticFile(cg, true);
                var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(pData), MERUtilities.GetFilenameFromAssetName(cg)); //just any path
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
        private static ConcurrentDictionary<string, string> UnusablePowers = new ConcurrentDictionary<string, string>();

        private void BuildGunInfo(ExportEntry skm, ConcurrentDictionary<string, List<EnemyWeaponChanger.GunInfo>> mapping, IMEPackage package, PackageCache startupFileCache, bool isCorrectedPackage)
        {
            // See if power is fully defined in package?
            var classInfo = ObjectBinary.From<UClass>(skm);
            if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                return; // This class cannot be used as a power, it is abstract

            var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
            var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            var usable = CheckImports(importDependencies, package, startupFileCache, null, out var missingImport);
            if (usable)
            {
                var pi = new EnemyWeaponChanger.GunInfo(skm, isCorrectedPackage);
                if ((pi.RequiresStartupPackage && !pi.PackageFileName.StartsWith("SFX"))
                    || pi.GunName.Contains("Player")
                    || pi.GunName.Contains("AsteroidRocketLauncher")
                    || pi.GunName.Contains("VehicleRocketLauncher")
                    || pi.GunName.Contains("FreezeGun") // Doesn't fire
                    || pi.GunName.Contains("ArcProjector") // Fires but uses player variables for targeting so it doesn't work
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
            mainWindow.CurrentProgressValue = 0;

            var startupFileCache = MERFileSystem.GetGlobalCache();

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
        private void BuildPowerInfo(ExportEntry powerExport, ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>> mapping, IMEPackage package, PackageCache startupFileCache, PackageCache localCache, bool isCorrectedPackage)
        {
            // See if power is fully defined in package?
            var classInfo = ObjectBinary.From<UClass>(powerExport);
            if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                return; // This class cannot be used as a power, it is abstract

            var dependencies = EntryImporter.GetAllReferencesOfExport(powerExport);
            var importDependencies = dependencies.OfType<ImportEntry>().ToList();
            var usable = CheckImports(importDependencies, package, startupFileCache, localCache, out var missingImport);
            if (usable)
            {
                //if (powerExport.ObjectName.Name == "SFXPower_Flashbang_NPC")
                //    Debugger.Break();
                var pi = new EnemyPowerChanger.PowerInfo(powerExport, isCorrectedPackage);
                if ((pi.RequiresStartupPackage && !pi.PackageFileName.StartsWith("SFX")))
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
                else
                {
                    Debug.WriteLine($"Denied power {pi.PowerName}");
                }
            }
            else
            {
                Debug.WriteLine($"Not usable power: {powerExport.InstancedFullPath} in {Path.GetFileName(package.FilePath)}, missing import {missingImport.FullPath}");
                if (mapping.ContainsKey(powerExport.InstancedFullPath))
                {
                    UnusablePowers.Remove(powerExport.InstancedFullPath, out _);
                }
                else
                {
                    UnusablePowers[powerExport.InstancedFullPath] = missingImport.FullPath;
                }
            }
        }

        private void FindPortablePowers(object sender, DoWorkEventArgs e)
        {
            // SETUP STAGE 1
            var files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                .Where(x => x.Contains("SFXPower") || x.Contains("SFXCharacter"))
                //.Where(x => x.Contains("SFXPower_StasisNew"))
                .ToList();
            mainWindow.CurrentOperationText = "Finding portable powers (stage 1)";
            int numdone = 0;
            int numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.CurrentProgressValue = 0;

            // PREP WORK
            var startupFileCache = MERFileSystem.GetGlobalCache();

            // Maps instanced full path to list of instances
            ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>> mapping = new ConcurrentDictionary<string, List<EnemyPowerChanger.PowerInfo>>();

            // STAGE 1====================

            // Corrected, embedded powers that required file coalescing for portability or other corrections in order to work on enemies
            var correctedPowers = MERUtilities.ListStaticAssets("binary.correctedloadouts.powers");
            foreach (var cg in correctedPowers)
            {
                var pData = MERUtilities.GetEmbeddedStaticFile(cg, true);
                var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(pData), MERUtilities.GetFilenameFromAssetName(cg)); //just any path
                var sfxPowers = package.Exports.Where(x => x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject);
                foreach (var sfxPow in sfxPowers)
                {
                    BuildPowerInfo(sfxPow, mapping, package, startupFileCache, null, true);
                }
            }

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
              {
                  mainWindow.CurrentOperationText = $"Finding portable powers (stage 1) [{numdone}/{numtodo}]";
                  Interlocked.Increment(ref numdone);
                  MERPackageCache localCache = new MERPackageCache();
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
                      var usable = CheckImports(importDependencies, package, startupFileCache, localCache, out var missingImport);
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

            // PHASE 2

            files = MELoadedFiles.GetFilesLoadedInGame(MEGame.ME2, true, false).Values
                .Where(x => x.Contains("BioD")
                || x.Contains("BioP"))
                .ToList();
            mainWindow.CurrentOperationText = "Finding portable powers (Stage 2)";
            numdone = 0;
            numtodo = files.Count;

            mainWindow.ProgressBarIndeterminate = false;
            mainWindow.ProgressBar_Bottom_Max = files.Count();
            mainWindow.CurrentProgressValue = 0;

            Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 3 }, (file) =>
            {
                mainWindow.CurrentOperationText = $"Finding portable powers (Stage 2) [{numdone}/{numtodo}]";
                Interlocked.Increment(ref numdone);
                if (!file.Contains("BioD"))
                    return; // BioD only
                MERPackageCache localCache = new MERPackageCache();
                var package = MEPackageHandler.OpenMEPackage(file);
                var powers = package.Exports.Where(x => x.InheritsFrom("SFXPower") && x.IsClass && !x.IsDefaultObject && !mapping.ContainsKey(x.InstancedFullPath));
                foreach (var skm in powers.Where(x => !mapping.ContainsKey(x.InstancedFullPath)))
                {
                    // See if power is fully defined in package?
                    var classInfo = ObjectBinary.From<UClass>(skm);
                    if (classInfo.ClassFlags.Has(UnrealFlags.EClassFlags.Abstract))
                        continue; // This class cannot be used as a power, it is abstract

                    var dependencies = EntryImporter.GetAllReferencesOfExport(skm);
                    var importDependencies = dependencies.OfType<ImportEntry>().ToList();
                    var usable = CheckImports(importDependencies, package, startupFileCache, localCache, out var missingImport);
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
        /// <summary>
        /// Checks to see if the listed imports can be reliably resolved as being in memory via their parents and localizations.
        /// </summary>
        /// <param name="imports"></param>
        private static bool CheckImports(List<ImportEntry> imports, IMEPackage package, PackageCache globalCache, PackageCache lpc, out ImportEntry unresolvableEntry)
        {
            // Force into memory
            //var importExtras = EntryImporter.GetPossibleAssociatedFiles(package);
            lpc ??= new MERPackageCache();
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
                if (import.InstancedFullPath.StartsWith("Engine.") || import.InstancedFullPath.StartsWith("Core."))
                    continue; // These are waste of time to resolve as they'll be there.

                //Debug.Write($@"Resolving {import.FullPath}...");
                var export = ResolveImport(import, globalCache, lpc);
                if (export != null)
                {
                    //Debug.WriteLine($@" OK");
                }
                else if (GlobalUnrealObjectInfo.IsAKnownNativeClass(import))
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

        public static void FindUnreferencedObjects(object? sender, DoWorkEventArgs doWorkEventArgs)
        {
            var package = MEPackageHandler.OpenMEPackage(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\BioP_EndGm3_LOC_INT.pcc");
            var objReferencer = package.Exports.FirstOrDefault(x => x.idxLink == 0 && x.ObjectName == "ObjectReferencer");
            if (objReferencer != null)
            {
                var allObjects = package.Exports.ToList();
                var refs = objReferencer.GetProperty<ArrayProperty<ObjectProperty>>("ReferencedObjects");
                foreach (var refX in refs)
                {
                    var eRef = refX.ResolveToEntry(package) as ExportEntry;
                    if (allObjects.Contains(eRef))
                    {
                        // Find references
                        allObjects = allObjects.Except(EntryImporter.GetAllReferencesOfExport(eRef, false).OfType<ExportEntry>()).ToList();
                        allObjects.Remove(eRef);
                    }
                }
                Debug.WriteLine("Objects with no reference:");
                foreach (var obj in allObjects)
                {
                    Debug.WriteLine($"  {obj.UIndex} {obj.InstancedFullPath}");
                }
            }



        }
        #endregion

        #region Debug

        private void DebugPorting(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            //var testName = "BioD_KroPrL_100Ruins.pcc";
            var testName = "BioD_SunTlA_202BaseCamp.pcc";
            var sourceP = MEPackageHandler.OpenMEPackage($@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\{testName}");

            // Port in powers. Do not use them.
            EnemyPowerChanger.LoadPowers();
            List<EnemyPowerChanger.PowerInfo> powersToPort = new();
            powersToPort.Add(EnemyPowerChanger.Powers.FirstOrDefault(x => x.PowerName == "SFXPower_Carnage"));
            //powersToPort.Add(EnemyPowerChanger.Powers.FirstOrDefault(x => x.PowerName == "SFXPower_Pull"));
            //powersToPort.Add(EnemyPowerChanger.Powers.FirstOrDefault(x => x.PowerName == "SFXPower_Reave"));
            //powersToPort.Add(EnemyPowerChanger.Powers.FirstOrDefault(x => x.PowerName == "SFXPower_Fortification_Vorcha"));


            foreach (var power in powersToPort)
            {
                Debug.WriteLine($"Porting power: {power.PowerName}");

                // Test single pull in
                //var sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage("SFXCharacterClass_Adept.pcc");
                //var sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage(power.PackageFileName);
                //var sourceExport = sourcePackage.FindExport("BioVFX_B_Powers.08_Pull.Pull_VFX_Appearance");
                //EntryExporter.ExportExportToPackage(sourceExport, sourceP, out _);

                // Test pulling in SFXPower_Pull without VFX prop
                //var sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage("BioP_ProCer.pcc");
                //var sourceExport = sourcePackage.FindExport("SFXGameContent_Powers.SFXPower_Pull");
                //var defaults = sourceExport.GetDefaults();
                ////defaults.WriteProperty(new ObjectProperty(0,"VFX")); // Setting this to zero makes it work. But porting the item this references in also doesn't break it.
                //EntryExporter.ExportExportToPackage(sourceExport, sourceP, out _);

                // Pull in VFX then pull in power
                //var sourcePackage = NonSharedPackageCache.Cache.GetCachedPackage("BioP_ProCer.pcc");
                //var

                //sourcePackage.FindExport("BioVFX_B_Lift.PostProcess.FB_MotionBlur_Distorting").RemoveProperty("Effects"); // Fixes the issue.
                //sourcePackage.FindExport("BioVFX_B_Lift.PostProcess.FB_MotionBlur_Distorting.BioMaterialInstanceEffect_0").RemoveProperty("Material");

                //var sourceExport = sourcePackage.FindExport("SFXGameContent_Powers.SFXPower_Shockwave");
                //var defaults = sourceExport.GetDefaults();
                //defaults.WriteProperty(new ObjectProperty(0, "VFX")); // Setting this to zero doesn't change anything if you port in the upper stuff afterwards
                //defaults.RemoveProperty("EvolvedPowerClass1");
                //defaults.RemoveProperty("EvolvedPowerClass2");
                //defaults.RemoveProperty("PowerScriptClass");
                //sourceExport = sourcePackage.FindExport("SFXGameContent_Powers.SFXPowerScript_PullProjectile");
                //defaults = sourceExport.GetDefaults();
                //defaults.RemoveProperty("m_oCrustEffect");

                //sourceP = NonSharedPackageCache.Cache.GetCachedPackage("/c.pcc");
                //EntryExporter.ExportExportToPackage(sourceExport, sourceP, out _);
                //sourceP.Save();



                // Way power does it
                //power.PackageFileName = "SFXCharacterClass_Adept.pcc";
                //power.SourceUIndex = sourceExport.UIndex;
                EnemyPowerChanger.PortPowerIntoPackage(sourceP, power, out _);
            }

            sourceP.Save($@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\{testName}");
            //ME2Debug.TestAllImportsInMERFS();
        }

        private void Debug3(object sender, DoWorkEventArgs dweb)
        {

        }

        private void DebugPorting2(object sender, DoWorkEventArgs doWorkEventArgs)
        {

            var sourcePower = MEPackageHandler.OpenMEPackage($@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\SFXCharacterClass_Sentinel.pcc").FindExport(@"SFXGameContent_Powers.SFXPower_Overload");
            var testPackage = MEPackageHandler.OpenMEPackage($@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\SFXCharacterClass_Adept.pcc");
            PackageTools.PortExportIntoPackage(testPackage, sourcePower);
            (sourcePower.FileRef as MEPackage).CompareToPackageDetailed(testPackage);

            /*
            EnemyPowerChanger.LoadPowers();
            var power = EnemyPowerChanger.Powers.FirstOrDefault(x => x.PowerName == "SFXPower_Carnage");

            var testName = "BioD_SunTlA_202BaseCamp";
            //var sourceP = MEPackageHandler.OpenMEPackage($@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC\{testName}");

            // PORTING INTO NEW PACKAGE FOR COMPARISON
            //var sourceF = @"F:\ME3Explorer\ME3Explorer\Resources\exec\ME2EmptyLevel.pcc";
            //var biodF = $@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\BioD_TEST.pcc";
            //var biopF = $@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC\BioP_TEST.pcc";

            var bioD = MEPackageHandler.OpenMEPackage(Path.Combine(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC", testName + ".pcc"));
            var bioP = MEPackageHandler.OpenMEPackage(Path.Combine(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\CookedPC", testName + ".pcc"));

            power.PackageFileName = "BioP_OmgPrA.pcc";
            power.SourceUIndex = 516;
            Debug.WriteLine($"Porting power BioP: {power.PowerName}");
            EnemyPowerChanger.PortPowerIntoPackage(bioP, power, out _);
            bioP.Save(Path.Combine(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC", testName + "-P.pcc"));

            power.PackageFileName = "BioD_JnkKgA_140Rescue.pcc";
            power.SourceUIndex = 402;
            Debug.WriteLine($"Porting power BioD: {power.PowerName}");
            EnemyPowerChanger.PortPowerIntoPackage(bioD, power, out _);
            bioD.Save(Path.Combine(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC", testName + "-D.pcc"));

            // Test differences
            void printStats(IMEPackage p)
            {
                Debug.WriteLine($"Names: {p.NameCount}");
                Debug.WriteLine($"Imports: {p.ImportCount}");
                Debug.WriteLine($"Exports: {p.ExportCount}");
            }

            Debug.WriteLine("BioP Info------");
            printStats(bioP);
            Debug.WriteLine("");
            Debug.WriteLine("BioD Info------");
            printStats(bioD);
            Debug.WriteLine("");
            Debug.WriteLine("Differences:");

            // Names
            Debug.WriteLine("Names in BioD but not BioP:");
            foreach (var name in bioD.Names.Except(bioP.Names))
            {
                Debug.WriteLine($" - {name}");
            }

            Debug.WriteLine("Names in BioP but not BioD:");
            foreach (var name in bioP.Names.Except(bioD.Names))
            {
                Debug.WriteLine($" - {name}");
            }

            // Imports
            var bioDImports = bioD.Imports.Select(x => x.InstancedFullPath).ToList();
            var bioPImports = bioP.Imports.Select(x => x.InstancedFullPath).ToList();
            Debug.WriteLine("Imports in BioD but not BioP:");
            foreach (var imp in bioDImports.Except(bioPImports))
            {
                Debug.WriteLine($" - {imp}");
            }

            Debug.WriteLine("Imports in BioP but not BioD:");
            foreach (var imp in bioPImports.Except(bioDImports))
            {
                Debug.WriteLine($" - {imp}");
            }

            // Exports
            var bioDExports = bioD.Exports.Select(x => x.InstancedFullPath).ToList();
            var bioPExports = bioP.Exports.Select(x => x.InstancedFullPath).ToList();
            Debug.WriteLine("Exports in BioD but not BioP:");
            foreach (var exp in bioDExports.Except(bioPExports))
            {
                Debug.WriteLine($" - {exp}");
            }

            Debug.WriteLine("Exports in BioP but not BioD:");
            foreach (var exp in bioPExports.Except(bioDExports))
            {
                Debug.WriteLine($" - {exp}");
            }

            (bioD as MEPackage).CompareToPackageDetailed(bioP);
        }


        private void FindSerialSizeMismatches(object? sender, DoWorkEventArgs doWorkEventArgs)
        {
            var packages = Directory.GetFiles(@"B:\SteamLibrary\steamapps\common\Mass Effect 2\BioGame\DLC\DLC_MOD_ME2Randomizer\CookedPC", "*.pcc");
            foreach (var packageF in packages)
            {
                var package = MEPackageHandler.OpenMEPackage(packageF);
                foreach (var v in package.Exports)
                {
                    var binary = ObjectBinary.FromDEBUG(v);
                }
            }
        }

        #endregion

        */
    }
}
