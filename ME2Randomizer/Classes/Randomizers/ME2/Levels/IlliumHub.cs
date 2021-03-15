using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    /// <summary>
    /// TwrHub randomizer
    /// </summary>
    class IlliumHub
    {

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            RandomizeDancer();
            return true;
        }


        public static DancerSource[] DancerOptions = new[]
        {
            // Human reaper
            new DancerSource()
            {
                BodyAsset = new AssetSource()
                {
                    PackageFile = "BioD_EndGm2_425ReaperReveal.pcc",
                    AssetPath = "BIOG_CBT_RPR_NKD_R.NKDa.CBT_RPR_NKDa_MDL"
                },
                Location= new CFVector3() { X =-3750, Y=2280, Z=1310},
                Rotation = new CIVector3(){ X=0, Y=0, Z=16384},
                DrawScale = 0.011f
            },
            // N7 armor - no mesh change. So THICC
            new DancerSource()
            {
                KeepHead = true,
                BodyAsset = new AssetSource()
                {
                    PackageFile = "BioD_TwrHub_605Lounge1.pcc",
                    AssetPath = "BIOG_HMM_ARM_HVY_R.HVYb.HMM_ARM_HVYb_MDL"
                },
            },

            // Wrex armor - no head mesh change. So THICC
            new DancerSource()
            {
                KeepHead = true,
                BodyAsset = new AssetSource()
                {
                    PackageFile = "BioD_KroHub_100MainHub.pcc",
                    AssetPath = "BIOG_KRO_ARM_HVY_R.HVYc.KRO_ARM_HVYc_MDL"
                },
            },

            // Salarian Councilor
            new DancerSource()
            {
                KeepHead = true,
                BodyAsset = new AssetSource()
                {
                    PackageFile = "BioD_CitHub_Embassy.pcc",
                    AssetPath = "BIOG_SAL_ARM_CTH_R.CTHd.SAL_ARM_CTHd_MDL"
                },
            },
            // Collector
            new DancerSource()
            {
                KeepHead = false,
                BodyAsset = new AssetSource()
                {
                    PackageFile = "BioA_ShpCr2_210TurnIntoHusk.pcc",
                    AssetPath = "BIOG_COL_ARM_NKD_R.NKDa.COL_ARM_NKDa_MDL"
                },
            },
                // Husk
                new DancerSource()
                {
                    KeepHead = false,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioP_RprGtA.pcc",
                        AssetPath = "BIOG_ZMB_ARM_NKD_R.NKDd.ZMB_ARM_NKDd_MDL"
                    },
                },
                // Volus. Because why not
                new DancerSource()
                {
                    KeepHead = false,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_CitHub_Embassy.pcc",
                        AssetPath = "BIOG_NCA_VOL_NKD_R.NKDa.NCA_FAC_VOL_NKDa_MDL"
                    },
                },
                // Elcor. Because why not - lmao
                new DancerSource()
                {
                    KeepHead = false,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_CitHub_Embassy.pcc",
                        AssetPath = "BIOG_NCA_ELC_NKD_R.NKDa.NCA_ELC_NKDa_MDL"
                    },
                },
                // Hanar. Because why not
                new DancerSource()
                {
                    KeepHead = false,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_CitHub_Embassy.pcc",
                        AssetPath = "BIOG_NCA_HAN_NKD_R.NKDa.NCA_HAN_NKDa_MDL"
                    },
                },
                // Keeper
                new DancerSource()
                {
                    KeepHead = false,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_CitAsL_200StartPoint.pcc",
                        AssetPath = "BIOG_AMB_KEE_NKD_R.NKDa.AMB_KEE_NKDa_MDL"
                    },
                },
                // Shadow Broker - does not use material so doesn't require shader - eyes are screwed up hardcore
                new DancerSource()
                {
                    KeepHead = true,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_Exp1Lvl4_Stage3.pcc",
                        AssetPath = "BIOG_YAH_SBR_NKD_R.NKDa.YAH_SBR_NKDa_MDL"
                    },
                },
                // Thresher Maw
                new DancerSource()
                {
                    DrawScale = 0.1f,
                    KeepHead = true,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_KroKgL_104ThresherMaw.pcc",
                        AssetPath = "biog_cbt_maw_nkd_r.NKDa.CBT_MAW_NKDa_MDL"
                    },
                },
                // Collector General
                new DancerSource()
                {
                    DrawScale = 0.5f,
                    Location= new CFVector3() { X =-3750, Y=2280, Z=1310},
                    Rotation = new CIVector3(0,-22384,16384),
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_ShpCr2_210ColGen.pcc",
                        AssetPath = "BIOG_CBT_COL_NKD_R.NKDa.CBT_COL_NKDa_MDL"
                    },
                },
                // Space Cow
                new DancerSource()
                {
                    KeepHead = false,
                    Location= new CFVector3() { X =-3750, Y=2280, Z=1370},
                    Rotation = new CIVector3(-32768,6000,0),
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioD_Unc1Explore.pcc",
                        AssetPath = "BIOG_AMB_COW_NKD_R.NKDa.AMB_COW_NKDa_MDL"
                    },
                },
                // Pyjak
                new DancerSource()
                {
                    KeepHead = false,
                    BodyAsset = new AssetSource()
                    {
                        PackageFile = "BioP_KroHub.pcc",
                        AssetPath = "BIOG_AMB_MON_NKD_R.NKDa.AMB_MON_NKDa_MDL"
                    },
                },
        };

        public class AssetSource
        {
            public string PackageFile { get; set; }
            public string AssetPath { get; set; }

            public virtual ExportEntry GetAsset(MERPackageCache cache = null)
            {
                IMEPackage package = null;
                if (cache != null)
                    package = cache.GetCachedPackage(PackageFile);
                else
                {
                    var packageF = MERFileSystem.GetPackageFile(PackageFile);
                    if (packageF != null)
                    {
                        package = MEPackageHandler.OpenMEPackage(packageF);
                    }
                }

                return package?.FindExport(AssetPath);
            }

            public virtual bool IsAssetFileAvailable()
            {
                return MERFileSystem.GetPackageFile(PackageFile, false) != null;
            }
        }

        public class DancerSource
        {
            public AssetSource HeadAsset { get; set; }
            public AssetSource BodyAsset { get; set; }
            public CFVector3 Location { get; set; }
            public CIVector3 Rotation { get; set; }
            public float DrawScale { get; set; } = 1;
            public AssetSource MorphFace { get; set; }
            public bool KeepHead { get; set; }
            public bool RemoveMaterials { get; set; }
        }

        private static void RandomizeDancer()
        {
            var loungeF = MERFileSystem.GetPackageFile("BioD_TwrHub_202Lounge.pcc");
            if (loungeF != null && File.Exists(loungeF))
            {
                var package = MEPackageHandler.OpenMEPackage(loungeF);
                var bodySM = package.GetUExport(4509);
                var headSM = package.GetUExport(2778);

                // Install new head and body assets
                var newInfo = DancerOptions.RandomElement();
                while (newInfo.BodyAsset != null && !newInfo.BodyAsset.IsAssetFileAvailable())
                {
                    // Find another asset that is available
                    MERLog.Information($@"Asset {newInfo.BodyAsset.AssetPath} in {newInfo.BodyAsset.PackageFile} not available, repicking...");
                    newInfo = DancerOptions.RandomElement();
                }
                var newBody = PackageTools.PortExportIntoPackage(package, newInfo.BodyAsset.GetAsset());
                bodySM.WriteProperty(new ObjectProperty(newBody.UIndex, "SkeletalMesh"));

                if (newInfo.HeadAsset != null)
                {
                    var newHead = PackageTools.PortExportIntoPackage(package, newInfo.HeadAsset.GetAsset());
                    headSM.WriteProperty(new ObjectProperty(newHead.UIndex, "SkeletalMesh"));
                }
                else if (!newInfo.KeepHead)
                {
                    headSM.RemoveProperty("SkeletalMesh");
                }


                if (newInfo.DrawScale != 1)
                {
                    // Install DS3D on the archetype. It works. Not gonna question it
                    var ds = new CFVector3()
                    {
                        X = newInfo.DrawScale,
                        Y = newInfo.DrawScale,
                        Z = newInfo.DrawScale,
                    };
                    package.GetUExport(619).WriteProperty(ds.ToLocationStructProperty("DrawScale3D")); //hack
                }

                // Install any updates to locations/rotations
                var dancerInstance = package.GetUExport(4510); // contains location data for dancer which may need to be slightly adjusted
                if (newInfo.Location != null)
                {
                    dancerInstance.WriteProperty(newInfo.Location.ToLocationStructProperty("Location"));
                }
                if (newInfo.Rotation != null)
                {
                    dancerInstance.WriteProperty(newInfo.Rotation.ToRotatorStructProperty("Rotation"));
                }

                if (newInfo.MorphFace != null)
                {
                    var newHead = PackageTools.PortExportIntoPackage(package, newInfo.MorphFace.GetAsset());
                    headSM.WriteProperty(new ObjectProperty(newHead.UIndex, "MorphHead"));
                }
                MERFileSystem.SavePackage(package);
            }
        }
    }
}
