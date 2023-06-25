using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.Collections;
using ME3TweaksCore.Targets;
using Randomizer.MER;

namespace Randomizer.Randomizers.Shared.Classes
{

    /// <summary>
    /// Helper class for dealing with Gestures
    /// </summary>
    class GestureManager
    {
#if __GAME1__
        public static readonly string[] RandomGesturePackages =
        {

        };
#endif
#if __GAME2__
        public static readonly string[] RandomGesturePackages =
        {
            "HMM_DP_ArmsCross",
            "HMM_AM_Towny",
            "HMF_AM_Towny",
            "HMM_DL_HandChop",
            "HMM_DL_Smoking",
            "HMM_FC_Angry"
            // A lot more need to be added from the list
        };
#endif


#if __GAME3__
        public static readonly string[] RandomGesturePackages =
        {
            // KID ANIMATION
            "HMC_AM_Scared",
            "HMC_AM_MoveStartStop",
            "HMC_DP_Crawl",
            "HMC_AM_StandingDefault",

            // CHOKE FIGHT
            "HMF_2P_Choke",
            "HMF_2P_GarrusRomance",
            "HMF_2P_Main", // has kiss

            // "HMF_2P_PHT_SynchMelee", // NOT IN GESTURES

            "HMF_DP_BackAgainstWall",
            "HMF_DP_ArmsCrossed",
            "HMF_DP_BarActions",
            "HMF_DP_HandsFace",

            "HMF_FC_Communicator",
            "HMF_FC_Custom",

            // "HMM_2P_BAN_Synch",
            // "HMM_2P_ATA_Synch",
            "HMM_2P_ChokeLift",
            "HMM_2P_Choking",
            "HMM_2P_Comraderie",
            "HMM_2P_Consoling",
            "HMM_2P_Conspire",
            "HMM_2P_End002",
            "HMM_2P_ForcedExit",
            "HMM_2P_Grab",

            "HMM_2P_GunInterrupt",
            "HMM_2P_Handshake",
            "HMM_2P_HeadButt",
            "HMM_2P_HoldingHands",
            "HMM_2P_Hostage",
            "HMM_2P_InjuredAgainstWall",
            "HMM_2P_KaiLengDeath",
            "HMM_2P_KissCheek",
            "HMM_2P_KissMale",
            "HMM_2P_LiftPillar",
            "HMM_2P_Main",
            // "HMM_2P_PHT_SyncMelee",
            "HMM_2P_PinAgainstWall",
            "HMM_2P_PunchInterrupt",
            "HMM_2P_ThaneKaiLengFight",
            //"HMM_AM_BeckonPistol",
            //"HMM_AM_BeckonRifle",
            "HMM_AM_Biotic",
            "HMM_AM_Environmental",
            "HMM_AM_Gamble",
            "HMM_AM_HandsClap",
            "HMM_DG_Deaths",
            "HMM_DG_Exploration",
            "HMM_DL_Decline",
            "HMM_DL_ElusiveMan",
            "HMM_DL_EmoStates",
            "HMM_DL_Gestures",
            "HMM_DL_HandChop",
            "HMM_DL_HandDismiss",
            "HMM_DL_HenchActions",
            "HMM_DL_Melee",
            "HMM_DL_PoseBreaker",
            "HMM_DL_Smoking",
            "HMM_DL_Sparring",
            "HMM_DL_StandingDefault",
            "HMM_DP_ArmsCross",
            "HMM_DP_ArmsCrossedBack",
            "HMM_DP_BarActions",
            "HMM_DP_CatchDogTags",
            "HMM_DP_ChinTouch",
            "HMM_DP_ClenchFist",
            "HMM_DP_HandOnHip",
            "HMM_DP_HandsBehindBack",
            "HMM_DP_HandsFace",
            "HMM_DP_Salute",
            "HMM_DP_Shuttle",
            "HMM_DP_ShuttleTurbulence",
            "HMM_DP_ToughGuy",
            "HMM_DP_Whisper",
            "HMM_FC_Angry",
            "HMM_FC_Communicator",
            "HMM_FC_DesignerCutscenes",
            "HMM_FC_Main",
            "HMM_FC_Sad",
            "HMM_FC_Startled",
            "NCA_ELC_EX_AnimSet",
            "NCA_VOL_DL_AnimSet",
            "PTY_EX_Geth",
            "PTY_EX_Asari",
            "PTY_EX_Krogan",
            //"RPR_BAN_CB_Banshe",
            // "RPR_HSK_AM_Husk",
            // "RPR_HSK_CB_2PMelee", // Is in special .RPR subpackage, will need special handling
            // "RPR_HSK_CB_Husk",
            "YAH_SBR_CB_AnimSet",
            "HMF_AM_Towny", // Dance?
            "HMM_AM_Towny", // Dance
            "HMM_AM_ThinkingFrustration",
            "HMM_AM_Talk",
            "HMF_AM_Talk",
            "HMM_WI_Exercise", // Situp
            "HMM_AM_EatSushi", // Citadel Act I
            "HMF_AM_Party", // Citadel Party
            "HMM_AM_SurrenderPrisoner", // citadel
            "2P_EscapeToDeath", // citadel
            "2P_AM_Kitchen",
            "2P_AM_PushUps",
            "HMF_2P_GarrusShepardTango",
            "2P_BigKiss",
            "HMM_DP_SitFloorInjured",
            "HMM_AM_Possession",
        };
#endif
        /// <summary>
        /// Maps a name of an animation package to the actual unreal package name it sits under in packages
        /// </summary>
        private static Dictionary<string, string> mapAnimSetOwners;
        public static void Init(GameTarget target, bool loadGestures = true)
        {
            MERLog.Information("Initializing GestureManager");
            // Load gesture mapping
            var gesturesFile = MERFileSystem.GetPackageFile(target, "GesturesConfigDLC.pcc");
            if (!File.Exists(gesturesFile))
            {
                gesturesFile = MERFileSystem.GetPackageFile(target, "GesturesConfig.pcc");
            }

            var gesturesPackage = MERFileSystem.OpenMEPackage(gesturesFile, preventSave: true);
            // name can change if it's dlc so we just do this
            var gestureRuntimeData = gesturesPackage.Exports.FirstOrDefault(x => x.ClassName == "BioGestureRuntimeData");
            var gestMap = ObjectBinary.From<BioGestureRuntimeData>(gestureRuntimeData);

            // Map it for strings since we don't want NameReferences.
            // Also load gestures cache
            _gesturePackageCache = new MERPackageCache(target, null, true);
            mapAnimSetOwners = new Dictionary<string, string>(gestMap.m_mapAnimSetOwners.Count);
            foreach (var v in gestMap.m_mapAnimSetOwners)
            {
                mapAnimSetOwners[v.Key] = v.Value;
                if (loadGestures && RandomGesturePackages.Contains(v.Key.Name))
                {
                    _gesturePackageCache.GetCachedPackageEmbedded(target.Game, $"Gestures.{v.Value.Name}.pcc"); // We don't capture the result - we just preload
                }
            }
        }

        /// <summary>
        /// Determines if the listed object path matches a key in the gesture mapping values (the result value)
        /// </summary>
        /// <param name="instancedFullPath">The path of the object to check against</param>
        /// <returns></returns>
        public static bool IsGestureGroupPackage(string instancedFullPath)
        {
            return mapAnimSetOwners.Values.Any(x => x.Equals(instancedFullPath, StringComparison.InvariantCultureIgnoreCase));
        }

        private static MERPackageCache _gesturePackageCache;

        public static IMEPackage GetGesturePackage(string gestureGroupName)
        {
            if (mapAnimSetOwners.TryGetValue(gestureGroupName, out var packageName))
            {
                return _gesturePackageCache.GetCachedPackage($"Gestures.{packageName}.pcc", false);
            }
            Debug.WriteLine($"PACKAGE NOT FOUND IN GESTURE MAP {gestureGroupName}");
            return null;
        }

        /// <summary>
        /// Gets a random looping gesture. Can return null
        /// </summary>
        /// <returns></returns>
        public static GestureInfo GetRandomMERGesture()
        {
            int retryCount = 10;
            while (retryCount > 0)
            {
                retryCount--;

                IMEPackage randomGesturePackage = null;
                string gestureGroup = null;
                while (randomGesturePackage == null)
                {
                    gestureGroup = RandomGesturePackages.RandomElement();
                    randomGesturePackage = GetGesturePackage(gestureGroup);
                }
                var candidates = randomGesturePackage.Exports.Where(x => x.ClassName == "AnimSequence" && x.ParentName == mapAnimSetOwners[gestureGroup]
                                                                                                       && x.ObjectName.Name.StartsWith(gestureGroup+"_")
                                                                                                       && !x.ObjectName.Name.StartsWith(gestureGroup+"_Alt") // This is edge case for animation names
                                                                                                       ).ToList();
                var randGesture = candidates.RandomElement();

                // Get animations that loop.
                if (randGesture.ObjectName.Name.Contains("Exit", StringComparison.InvariantCultureIgnoreCase) ||
                    randGesture.ObjectName.Name.Contains("Enter", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                // Make sure it has the right animgroup - some are subsets of another - e.g. ArmsCrossed and ArmsCrossed_Alt


                return new GestureInfo()
                {
                    GestureAnimSequence = randGesture,
                    GestureGroup = gestureGroup
                };
            }

            return null;
        }

        /// <summary>
        /// Generates a new BioDynamicAnimSet export under the specified parent
        /// </summary>
        /// <param name="target"></param>
        /// <param name="parent"></param>
        /// <param name="group"></param>
        /// <param name="seq"></param>
        /// <param name="animSetData"></param>
        /// <returns></returns>
        public static ExportEntry GenerateBioDynamicAnimSet(GameTarget target, ExportEntry parent, GestureInfo gestInfo, bool isKismet = false)
        {
            // The incoming gestinfo might be pointing to the cached embedded version.
            // We look up the value in the given package to ensure we use the right values.

            var animSeq = parent.FileRef.FindExport(gestInfo.GestureAnimSequence.InstancedFullPath);
            var animSet = animSeq.GetProperty<ObjectProperty>("m_pBioAnimSetData").ResolveToEntry(parent.FileRef);

            PropertyCollection props = new PropertyCollection();
            props.Add(new NameProperty(gestInfo.GestureGroup, "m_nmOrigSetName"));
            props.Add(new ArrayProperty<ObjectProperty>(new[] { new ObjectProperty(animSeq) }, "Sequences"));
            props.Add(new ObjectProperty(animSet, "m_pBioAnimSetData"));

            BioDynamicAnimSet bin = new BioDynamicAnimSet()
            {
                SequenceNamesToUnkMap = new UMultiMap<NameReference, int>(1)
                {
                    {gestInfo.GestureName, 1} // If we ever add support for multiple we do it here.
                }
            };

            var rop = new RelinkerOptionsPackage() { Cache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true) };
            var bioDynObj = new ExportEntry(parent.FileRef, parent, parent.FileRef.GetNextIndexedName(isKismet ? $"KIS_DYN_{gestInfo.GestureGroup}" : "BioDynamicAnimSet"), properties: props, binary: bin)
            {
                Class = EntryImporter.EnsureClassIsInFile(parent.FileRef, "BioDynamicAnimSet", rop)
            };

            // These will always be unique
            if (isKismet)
            {
                bioDynObj.indexValue = 0;
            }
            parent.FileRef.AddExport(bioDynObj);
            return bioDynObj;
        }
    }
}
