using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.GameFilesystem;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using Newtonsoft.Json;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game1.Misc
{
    /// <summary>
    /// Describes a mission reward, and the info required to install it.
    /// </summary>
    class MissionReward
    {
        /// <summary>
        /// The plot bool index for the treasure
        /// </summary>
        public int TreasurePlotBoolIdx { get; set; }
        /// <summary>
        /// The plot bool sref string. Not sure if this is needed.
        /// </summary>
        public string TreasurePlotBoolSrefName { get; set; }
        /// <summary>
        /// The treasure ID
        /// </summary>
        public int TreasureIDIdx { get; set; }
        /// <summary>
        /// The string property for the treasure ID. Not sure if this is needed.
        /// </summary>
        public string TreasureIDSrefName { get; set; }
        /// <summary>
        /// IFP of the codex image (weapons only)
        /// </summary>
        public string CodexImageIFP { get; set; }

        /// <summary>
        /// A few items for some reason don't have CodexImage. So we have to set this to differentiate.
        /// </summary>
        public bool IsWeapon { get; set; }

        /// <summary>
        /// Package that contains this originally
        /// </summary>
        public string SourcePackage { get; set; }

        /// <summary>
        /// The IFP of the sequence object that holds this mission reward
        /// </summary>
        public string SequenceObjectIFP { get; set; }

        /// <summary>
        /// The packages where this award can be obtained in the level that will also have its treasure usages adjusted.
        /// </summary>
        public List<string> PickupPackages { get; set; }
    }

    /// <summary>
    /// Randomizes the mission rewards you get 
    /// </summary>
    class MissionRewards
    {
#if DEBUG
        internal static bool Inventory(GameTarget target, RandomizationOption option)
        {
            List<MissionReward> rewards = new List<MissionReward>();
            Dictionary<int, SortedSet<string>> plotBoolUsages = new Dictionary<int, SortedSet<string>>();
            var loadedFiles = MELoadedFiles.GetFilesLoadedInGame(target.Game, gameRootOverride: target.TargetPath);

            int done = 0;
            int total = loadedFiles.Count;
            option.ProgressIndeterminate = true;

            foreach (var lf in loadedFiles)
            {
                done++;
                option.CurrentOperation = $"Inventorying files [{done}/{total}]";

                // not hooked up
                if (lf.Key == "BioD_N7Shipwreck.pcc")
                    continue;
                if (lf.Key == "BioD_.pcc")
                    continue;
                if (lf.Key.StartsWith("BioA"))
                    continue; // These never have them

                var p = MERFileSystem.OpenMEPackage(lf.Value);


                // Inventory plot bool usages
                foreach (var pbf in p.Exports.Where(x => x.ClassName == "BioSeqVar_StoryManagerStateId"))
                {
                    var idx = pbf.GetProperty<IntProperty>("m_nIndex");
                    if (idx != null)
                    {
                        if (!plotBoolUsages.TryGetValue(idx.Value, out var fileUsages))
                        {
                            fileUsages = new SortedSet<string>();
                            plotBoolUsages[idx] = fileUsages;
                        }

                        fileUsages.Add(lf.Key);
                    }
                }

                var researchTechFound = p.Exports.FirstOrDefault(x => x.ObjectName == "Research_Tech_Found");
                var newWeaponFound = p.Exports.FirstOrDefault(x => x.ObjectName == "New_Weapon_Found");
                if (newWeaponFound != null && researchTechFound != null)
                {
                    Debug.WriteLine($"Inventorying {lf.Key}");

                    var researchTechObjs = SeqTools.GetAllSequenceElements(researchTechFound).OfType<ExportEntry>().ToList();
                    {
                        var seqStart = researchTechObjs.First(x => x.ClassName == "SeqEvent_SequenceActivated");
                        InventoryOutboundResearch(lf.Key, seqStart, rewards);
                    }

                    var newWeaponObjs = SeqTools.GetAllSequenceElements(newWeaponFound).OfType<ExportEntry>().ToList();
                    {
                        var seqStart = newWeaponObjs.First(x => x.ClassName == "SeqEvent_SequenceActivated");
                        InventoryOutboundWeapon(lf.Key, seqStart, rewards);
                    }
                }
            }

            // Add data about usages of the plot
            foreach (var award in rewards)
            {
                var containingPackages = plotBoolUsages[award.TreasureIDIdx];
                containingPackages.Remove(award.SourcePackage);
                award.PickupPackages = containingPackages.ToList();

            }

            File.WriteAllText(@"C:\Users\mgame\source\repos\ME2Randomizer\Randomizer\Randomizers\Game2\Assets\Text\missionrewards.json", JsonConvert.SerializeObject(rewards, Formatting.Indented));

            return true;
        }

        private static void InventoryOutboundWeapon(string key, ExportEntry node, List<MissionReward> rewards)
        {
            var outlinks = SeqTools.GetOutboundLinksOfNode(node);
            var linkedOp = outlinks[0][0].LinkedOp;
            bool checkNext = false;
            if (linkedOp != null && linkedOp.ClassName == "SequenceReference")
            {
                var varLinks = SeqTools.GetVariableLinksOfNode(linkedOp as ExportEntry);
                if (varLinks.Count == 4)
                {
                    // 3 items is blank one. more than 3 means populated
                    MissionReward mw = new MissionReward() { SourcePackage = key, SequenceObjectIFP = linkedOp.InstancedFullPath };
                    ReadPlotData(varLinks, mw);
                    if (varLinks[3].LinkedNodes.Count == 1)
                    {
                        var codexObj = varLinks[3].LinkedNodes[0] as ExportEntry;
                        if (codexObj != null)
                        {
                            mw.CodexImageIFP = codexObj.GetProperty<ObjectProperty>("ObjValue").ResolveToEntry(node.FileRef).InstancedFullPath;
                        }
                    }
                    mw.IsWeapon = true;
                    rewards.Add(mw);
                    Debug.WriteLine($" > Weapon reward in {key}");
                    checkNext = true;
                }
            }

            if (checkNext)
            {
                // Get next node(s) as we might have multiple pieces
                var nextOutlinks = SeqTools.GetOutboundLinksOfNode(node);
                if (nextOutlinks.Count == 1 && nextOutlinks[0].Count == 1 && nextOutlinks[0][0].LinkedOp is ExportEntry maybeSeqNext && maybeSeqNext.ClassName == "SequenceReference")
                {
                    // CALL ON NEXT
                    InventoryOutboundWeapon(key, maybeSeqNext, rewards);
                }
            }
        }

        private static void InventoryOutboundResearch(string key, ExportEntry node, List<MissionReward> rewards)
        {
            var outlinks = SeqTools.GetOutboundLinksOfNode(node);
            var linkedOp = outlinks[0][0].LinkedOp;
            bool checkNext = false;
            if (linkedOp != null && linkedOp.ClassName == "SequenceReference")
            {
                var varLinks = SeqTools.GetVariableLinksOfNode(linkedOp as ExportEntry);
                if (varLinks.Count == 4)
                {
                    // 3 items is blank one. more than 3 means populated
                    MissionReward mw = new MissionReward() { SourcePackage = key, SequenceObjectIFP = linkedOp.InstancedFullPath };
                    ReadPlotData(varLinks, mw);
                    mw.IsWeapon = false;
                    rewards.Add(mw);
                    Debug.WriteLine($" > Research reward in {key}");
                    checkNext = true;
                }
            }

            if (checkNext)
            {
                // Get next node(s) as we might have multiple pieces
                var nextOutlinks = KismetHelper.GetOutboundLinksOfNode(node);
                if (nextOutlinks.Count == 1 && nextOutlinks[0].Count == 1 && nextOutlinks[0][0].LinkedOp is ExportEntry maybeSeqNext && maybeSeqNext.ClassName == "SequenceReference")
                {
                    // CALL ON NEXT
                    InventoryOutboundResearch(key, maybeSeqNext, rewards);
                }
            }
        }

        private static void ReadPlotData(List<SeqTools.VarLinkInfo> seqObjs, MissionReward mw)
        {
            var plotObj = seqObjs[0].LinkedNodes[0] as ExportEntry;
            mw.TreasurePlotBoolIdx = plotObj.GetProperty<IntProperty>("m_nIndex");
            mw.TreasurePlotBoolSrefName = plotObj.GetProperty<StrProperty>("m_sRefName")?.Value;

            var treasureId = seqObjs[1].LinkedNodes[0] as ExportEntry;
            mw.TreasureIDIdx = treasureId.GetProperty<IntProperty>("m_nIndex");
            mw.TreasureIDSrefName = treasureId.GetProperty<StrProperty>("m_sRefName")?.Value;
        }

#endif
        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        internal static void Init(GameTarget target)
        {
            //MERResourceCache.GetCachedPackage();
        }


        internal static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            // Load randomization list.
            var rewardsJson = MERUtilities.GetEmbeddedTextAsset("missionrewards.json");
            var allRewards = JsonConvert.DeserializeObject<List<MissionReward>>(rewardsJson);
            var researchRewards = allRewards.Where(x => x.IsWeapon == false).ToList();
            var weaponRewards = allRewards.Where(x => x.IsWeapon == true).ToList();

            // Shuffle the individual reward types. We traverse allRewards in order.
            researchRewards.Shuffle();
            weaponRewards.Shuffle();

            // Load rewards image packages as we will need to port those in.
            using MERPackageCache texturePackageCache = new MERPackageCache(target);

            // LE2: This is a big chungus
            var codexPackage = texturePackageCache.GetCachedPackage("GUI_Codex_Images.pcc");
            EntryExporter.PrepareGlobalFileForPorting(codexPackage, "GUI_Codex_Images");

            texturePackageCache.GetCachedPackage("BioD_ZyaVTL_630Revenge.pcc"); // Flamethrower is in here

            if (target.Game == MEGame.ME2)
            {
                // Have to open individual DLC packages..
                // TODO: IDENTIFY WHICH DLC PACKAGES NEED THIS DONE
            }


            option.ProgressValue = 0;
            option.ProgressMax = allRewards.Count;
            option.ProgressIndeterminate = false;

            IMEPackage currentPackage = null;
            foreach (var originalReward in allRewards)
            {
                if (currentPackage == null || !Path.GetFileName(currentPackage.FilePath).CaseInsensitiveEquals(originalReward.SourcePackage))
                {
                    // Save if one is opePn
                    if (currentPackage != null)
                        MERFileSystem.SavePackage(currentPackage);
                    // Open the new package
                    currentPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, originalReward.SourcePackage));
                }

                //if (originalReward.SourcePackage == "BioD_ShpCr2_170HubRoom2.pcc")
                //    Debugger.Break();

                var awardSeqRef = currentPackage.FindExport(originalReward.SequenceObjectIFP);
                MissionReward newAward = originalReward.IsWeapon ? weaponRewards.PullFirstItem() : researchRewards.PullFirstItem();

                var varLinks = SeqTools.GetVariableLinksOfNode(awardSeqRef);

                // UPDATE PLOT ITEM
                var plotObj = varLinks[0].LinkedNodes[0] as ExportEntry;
                if (newAward.TreasurePlotBoolSrefName != null)
                {
                    plotObj.WriteProperty(new StrProperty(newAward.TreasurePlotBoolSrefName, "m_sRefName"));
                }
                else
                {
                    plotObj.RemoveProperty("m_sRefName");
                }
                plotObj.WriteProperty(new IntProperty(newAward.TreasurePlotBoolIdx, "m_nIndex"));

                // UPDATE TREASURE ID
                var treasureId = varLinks[1].LinkedNodes[0] as ExportEntry;
                if (newAward.TreasureIDSrefName != null)
                {
                    treasureId.WriteProperty(new StrProperty(newAward.TreasureIDSrefName, "m_sRefName"));
                }
                else
                {
                    treasureId.RemoveProperty("m_sRefName");
                }
                treasureId.WriteProperty(new IntProperty(newAward.TreasureIDIdx, "m_nIndex"));
                treasureId.WriteProperty(new IntProperty(newAward.TreasureIDIdx, "IntValue"));

                // UPDATE CODEX IMAGE
                if (newAward.CodexImageIFP != null)
                {
                    // new item has a codex image
                    ExportEntry codexImageObj = null;
                    if (varLinks[3].LinkedNodes.Count == 0)
                    {
                        // doesn't currently have a codex image
                        codexImageObj = SequenceObjectCreator.CreateSequenceObject(currentPackage, "SeqVar_Object"); //todo: Cach
                        KismetHelper.AddObjectToSequence(codexImageObj, SeqTools.GetParentSequence(awardSeqRef));
                        varLinks[3].LinkedNodes.Add(codexImageObj);
                    }
                    else
                    {
                        // currently has one
                        codexImageObj = varLinks[3].LinkedNodes[0] as ExportEntry;
                    }

                    InstallNewSourceTexture(currentPackage, codexImageObj, originalReward, newAward, texturePackageCache);
                }
                else
                {
                    // new item doesn't have a codex image
                    varLinks[3].LinkedNodes.Clear(); // Ensure no link(s).
                }

                SeqTools.WriteVariableLinksToNode(awardSeqRef, varLinks);

                // Update the pickup packages
                foreach (var pickupPackageF in originalReward.PickupPackages)
                {
                    var pickupPackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, pickupPackageF));
                    foreach (var pbf in pickupPackage.Exports.Where(x => x.ClassName == "BioSeqVar_StoryManagerStateId"))
                    {
                        var idx = pbf.GetProperty<IntProperty>("m_nIndex");
                        if (idx != null && idx == originalReward.TreasureIDIdx)
                        {
                            pbf.WriteProperty(new StrProperty(newAward.TreasurePlotBoolSrefName, "m_sRefName")); // these are the same, I guess?
                            pbf.WriteProperty(new IntProperty(newAward.TreasureIDIdx, "m_nIndex"));
                            pbf.WriteProperty(new IntProperty(newAward.TreasureIDIdx, "IntValue"));
                        }
                    }

                    MERFileSystem.SavePackage(pickupPackage);
                }


                option.ProgressValue++;
            }

            // Save if one is open
            if (currentPackage != null)
                MERFileSystem.SavePackage(currentPackage);
            currentPackage.Dispose();
            currentPackage = null;

            // Handle Plot unlocks in ShpCr2
            option.ProgressIndeterminate = true;
            var shpCr2 = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, "BioD_ShpCr2_130TurnIntoHusk.pcc"));
            string ifp = target.Game == MEGame.LE2 ? "TheWorld.PersistentLevel.Main_Sequence.TREASURE.Show_Choice_GUI_0.TREASURE_1.SeqAct_Switch_0" : throw new Exception("This value needs looked up in ME2!");
            var swExp = shpCr2.FindExport(ifp);
            var outbounds = SeqTools.GetOutboundLinksOfNode(swExp);
            foreach (var ob in outbounds)
            {
                var awardTreasure = SeqTools.GetOutboundLinksOfNode(ob[0].LinkedOp as ExportEntry)[0][0].LinkedOp as ExportEntry;
                var state = SeqTools.GetVariableLinksOfNode(awardTreasure)[0].LinkedNodes[0] as ExportEntry;
                AddBonusWeapon(state.GetProperty<StrProperty>("m_sRefName"));
            }

            // Handle images for ShpCr2.
            var variations = new[] { "Assault_Variations", "Shotgun_Variations", "Sniper_Variations" };
            foreach (var vari in variations)
            {
                var sequence = shpCr2.FindExport("TheWorld.PersistentLevel.Main_Sequence.TREASURE." + vari);
                var treasureTokens = SeqTools.GetAllSequenceElements(sequence).OfType<ExportEntry>().Where(x => x.ClassName == "SFXSeqAct_TreasureTokens");
                foreach (var tk in treasureTokens)
                {
                    var srefName = (SeqTools.GetVariableLinksOfNode(tk)[0].LinkedNodes[0] as ExportEntry).GetProperty<StrProperty>("m_sRefName");
                    var relevantInfo = allRewards.First(x => x.TreasurePlotBoolSrefName == srefName);
                    var choiceImage = SeqTools.GetVariableLinksOfNode(MERSeqTools.GetNextNode(tk, 0))[7].LinkedNodes[0] as ExportEntry;
                    if (relevantInfo.CodexImageIFP != null)
                    {
                        InstallNewSourceTexture(shpCr2, choiceImage, null, relevantInfo, texturePackageCache);
                    }
                    else
                    {
                        InstallNewSourceTexture(shpCr2, choiceImage, null, relevantInfo, texturePackageCache, "gui_codex_images.ShiftyCow_512");
                    }
                }
            }
            MERFileSystem.SavePackage(shpCr2);

            return true;
        }

        private static void InstallNewSourceTexture(IMEPackage currentPackage, ExportEntry codexImageObj, MissionReward originalReward, MissionReward newAward, MERPackageCache texturePackageCache, string forceTextureIFP = null)
        {
            ExportEntry sourceTexture = null;

            foreach (var p in texturePackageCache.GetPackages())
            {
                sourceTexture = p.FindExport(forceTextureIFP ?? newAward.CodexImageIFP);
                if (sourceTexture != null)
                    break;
            }

            if (sourceTexture == null)
                Debugger.Break();

            // Port in the new image.
            EntryExporter.ExportExportToPackage(sourceTexture, currentPackage, out var newTexture);
            codexImageObj.WriteProperty(new ObjectProperty(newTexture, "ObjValue"));
            if (originalReward != null)
            {
                Debug.WriteLine($"Reward change: {originalReward.CodexImageIFP} -> {newAward.CodexImageIFP}");
            }
        }

        private static readonly string[] ShpCr2OriginalBonuses = new[]
        {
            "Tre_Wpn_BonusAssaultRifle",
            "Tre_Wpn_BonusShotgun",
            "Tre_Wpn_BonusSniperRifle",
            "Tre_Wpn_SuperAssaultRifle",
            "Tre_Wpn_SuperShotgun",
            "Tre_Wpn_SuperSniperRifle",
        };

        /// <summary>
        /// These are flags to determine if you already picked up a bonus weapon or talent on ShpCr2.
        /// </summary>
        private static void AddBonusWeapon(string bonusWeaponFlag)
        {
            // We need to rebuild the bonus mission flags.
            var biogame = CoalescedHandler.GetIniFile("BioGame.ini");
            var hasBonusWeapon = biogame.GetOrAddSection("SFXGameContent.SFXSeqAct_HasBonusWeapon");
            if (hasBonusWeapon.Entries.Count == 0)
            {
                hasBonusWeapon.Entries.Add(new DuplicatingIni.IniEntry("!BonusWeaponFlags", "CLEAR"));
            }

            if (bonusWeaponFlag.StartsWith("Tre_"))
                bonusWeaponFlag = bonusWeaponFlag.Substring(4);
            else
            {
                Debug.WriteLine("NOT TRE STARD!");
            }

            hasBonusWeapon.Entries.Add(new DuplicatingIni.IniEntry("+BonusWeaponFlags", bonusWeaponFlag));
        }
    }
}
