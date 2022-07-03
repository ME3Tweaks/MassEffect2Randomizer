using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Kismet;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Utility;
using Randomizer.Shared;

namespace Randomizer.Randomizers.Game3.Levels
{
    /// <summary>
    /// Randomize for the CitHub series of levels
    /// </summary>
    internal class CitHub
    {
        private static LEXOpenable[] VIClientEffects = new[]
        {
            // DEFAULT
            new LEXOpenable()
            {
                EntryClass = "RvrClientEffect",
                EntryPath = "BioVFX_Env_Hologram.Character.VCFX.Character_Hologram_Crust_VCFX_orange",
                FilePath = "BioD_CitHub_Underbelly.pcc"
            },
            // Blue with lines
            new LEXOpenable()
            {
                EntryClass = "RvrClientEffect",
                EntryPath = "BioVFX_Env_Hologram.Character.VCFX.Char_Holo_Crust_VCFX_blue_Static",
                FilePath = "BioD_CitHub_001ProCit_LOC_INT.pcc"
            },
            // Prothean VI
            new LEXOpenable()
            {
                EntryClass = "RvrClientEffect",
                EntryPath = "BioVFX_Env_Cat002.VCFX.Prothean_VI_Crust_01",
                FilePath = "BioD_Cat004_750TIMConv_LOC_INT.pcc"
            },

            // Robots
            new LEXOpenable()
            {
                EntryClass = "RvrClientEffect",
                EntryPath = "BioVFX_Exp3_CitCas.VCFX.Robot_Boxing_Crust_1",
                FilePath = "BioD_CitCas_Robot.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "RvrClientEffect",
                EntryPath = "BioVFX_Exp3_CitCas.VCFX.Robot_Boxing_Crust_2",
                FilePath = "BioD_CitCas_Robot.pcc"
            },
            //Wet
            new LEXOpenable()
            {
                EntryClass = "RvrClientEffect",
                EntryPath = "BioVFX_Env_Rain.VCFX.Wet_Crust_VCFX",
                FilePath = "BioD_Cat003_220LockersVisual.pcc"
            },

            // Gore
            new LEXOpenable()
            {
                EntryClass = "RvrClientEffect",
                EntryPath = "BioVFX_C_Blood.VCFX.Cin.Gore_Crust_Reaper_wGuts",
                FilePath = "BioD_KroGru_800Exit_LOC_INT.pcc"
            },
        };

        /// <summary>
        /// List of OBJECTS for VI line options.
        /// Objects can be:
        /// string - new AFC file name
        /// LEXOpenable - reference to existing audio
        /// </summary>
        private static object[] VILineOptions = new object[]
        {
            // NEW AUDIO
            "MERMyBrand",
            "MERSpecialEyes2",

            // EXISTING AUDIO
            #region LOUD SCREAMING
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "Wwise_Weapons_P_Spitfire.Exertion_Falling_Example_01_wav",
                FilePath = "BioD_Cit001_300CarLot.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "Wwise_Weapons_P_Spitfire.Exertion_Falling_Example_02_wav",
                FilePath = "BioD_Cit001_300CarLot.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "Wwise_Weapons_P_Spitfire.Exertion_Falling_Example_03_wav",
                FilePath = "BioD_Cit001_300CarLot.pcc"
            },
            #endregion
            #region STATIC
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "proear_crashedchopper_v_d.Audio.Int.en-us,global_anderson,proear_crashedchopper_v,00599866_m_wav",
                FilePath = "BioD_ProEar_420Radio_LOC_INT.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "proear_crashedchopper_v_d.Audio.Int.en-us,hench_kaidan,proear_crashedchopper_v,00589316_m_wav",
                FilePath = "BioD_ProEar_420Radio_LOC_INT.pcc"
            },
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "proear_crashedchopper_v_d.Audio.Int.en-us,hench_ashley,proear_crashedchopper_v,00589318_m_wav",
                FilePath = "BioD_ProEar_420Radio_LOC_INT.pcc"
            },
            #endregion
            new LEXOpenable()
            {
                EntryClass = "WWiseStream",
                EntryPath = "promar_tram_chat_v_D.Audio.Int.en-us,promar_cerberus_second,promar_tram_chat_v,00696923_m_wav",
                FilePath = "BioD_ProMar_530Gondolas_LOC_INT.pcc"
            },
        };

        public static bool RandomizeLevel(GameTarget target, RandomizationOption option)
        {
            RandomizeVI(target, option);
            return true;
        }

        private static void RandomizeVI(GameTarget target, RandomizationOption option)
        {
            #region AUDIO
            var underBellyLOCF = MERFileSystem.GetPackageFile(target, @"BioD_CitHub_Underbelly_LOC_INT.pcc");
            var underBellyLOCP = MEPackageHandler.OpenMEPackage(underBellyLOCF);

            MERPackageCache sourceCache = new MERPackageCache(target, null, false);
            foreach (var v in underBellyLOCP.Exports.Where(x => x.ClassName == "WwiseStream" && x.ObjectName.Name.Contains("citwrd_shepard_vi")))
            {
                // Todo: Filter to only VI
                Debug.WriteLine($"Randoming audio: {v.InstancedFullPath}");
                var randomLine = VILineOptions.RandomElement();

                if (randomLine is LEXOpenable existingAudio)
                {
                    var sourcePackage = sourceCache.GetCachedPackage(MERFileSystem.GetPackageFile(target, existingAudio.FilePath));
                    WwiseTools.RepointWwiseStream(sourcePackage.FindExport(existingAudio.EntryPath), v);
                }
                else if (randomLine is string newMERAudio)
                {
                    WwiseTools.RepointWwiseStreamToSingleAFC(v, newMERAudio);
                }
            }
            sourceCache.Dispose();
            MERFileSystem.SavePackage(underBellyLOCP);
            #endregion

            #region CRUST VFX
            var underBellyDF = MERFileSystem.GetPackageFile(target, @"BioD_CitHub_Underbelly.pcc");
            var underBellyDP = MEPackageHandler.OpenMEPackage(underBellyDF);

            sourceCache = new MERPackageCache(target, MERCaches.GlobalCommonLookupCache, true);
            var sequence = underBellyDP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Shepard_VI");

            // Remove the existing RVRSpawnClientEffect usage.
            var seqObjsToSkip = KismetHelper.GetSequenceObjects(sequence).OfType<ExportEntry>().Where(x => x.ClassName == "RvrSeqAct_SpawnClientEffect");
            foreach (var v in seqObjsToSkip)
            {
                SeqTools.SkipSequenceElement(v, "Out");
            }

            var delay = SequenceObjectCreator.CreateSequenceObject(underBellyDP, "SeqAct_Delay", sourceCache);
            var randFloat = SequenceObjectCreator.CreateSequenceObject(underBellyDP, "SeqVar_RandomFloat", sourceCache);
            randFloat.WriteProperty(new FloatProperty(0.1f, "Min"));
            randFloat.WriteProperty(new FloatProperty(2f, "Max"));
            KismetHelper.CreateVariableLink(delay, "Duration", randFloat);

            var randSwitch = MERSeqTools.InstallRandomSwitchIntoSequence(target, sequence, VIClientEffects.Length);
            KismetHelper.CreateOutputLink(delay, "Finished", randSwitch);

            // We do it this way since it's kind of an ambiguous index - for potential future proofing

            // Create the starts and stops
            List<ExportEntry> createdSpawnEffects = new List<ExportEntry>();
            var shepVi = underBellyDP.Exports.FirstOrDefault(x => x.ClassName == "SeqVar_Object" && x.GetProperty<NameProperty>("VarName") != null && x.GetProperty<NameProperty>("VarName").Value == "obj_vi_shepard");
            for (int i = 0; i < VIClientEffects.Length; i++)
            {
                var rcvr = SequenceObjectCreator.CreateSequenceObject(underBellyDP, "RvrSeqAct_StoppableClientEffect", sourceCache);
                KismetHelper.RemoveAllLinks(rcvr);
                KismetHelper.CreateVariableLink(rcvr, "Target", shepVi);
                KismetHelper.CreateOutputLink(randSwitch, $"Link {(i + 1)}", rcvr);
                KismetHelper.AddObjectsToSequence(sequence, false, rcvr);

                var effect = VIClientEffects[i];
                var portedEffect = PackageTools.PortExportIntoPackage(target, effect, underBellyDP);
                rcvr.WriteProperty(new ObjectProperty(portedEffect, "m_pEffect"));
                createdSpawnEffects.Add(rcvr);
                // We do not hook up to LOOP here since start/stop logic would be both!
            }

            // All created - now we fire stops
            for (int i = 0; i < createdSpawnEffects.Count; i++)
            {
                var effect = createdSpawnEffects[i];
                KismetHelper.CreateOutputLink(delay, $"Finished", effect, 1); // STOP
            }

            // Loop to ourself in the delay
            KismetHelper.CreateOutputLink(delay, "Finished", delay);


            KismetHelper.CreateOutputLink(underBellyDP.FindExport("TheWorld.PersistentLevel.Main_Sequence.Shepard_VI.SeqAct_AttachToEvent_0"), "Out", delay); // hookup our logic
            KismetHelper.AddObjectsToSequence(sequence, false, delay, randFloat);
            MERFileSystem.SavePackage(underBellyDP);
            sourceCache.Dispose();
            #endregion
        }
    }
}
