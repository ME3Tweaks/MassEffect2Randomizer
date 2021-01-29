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

        private static DancerSource[] DancerOptions = new[]
        {
            // Human reaper
            //new DancerSource()
            //{
            //    BodyAsset = new AssetSource()
            //    {
            //        PackageFile = "BioD_EndGm2_425ReaperReveal.pcc",
            //        AssetPath = "BIOG_CBT_RPR_NKD_R.NKDa.CBT_RPR_NKDa_MDL"
            //    },
            //    Location= new CFVector3() { X =-3750, Y=2280, Z=1310},
            //    Rotation = new CIVector3(){ X=0, Y=0, Z=16384},
            //    DrawScale = 0.011f
            //},

            // N7 armor - no mesh change. So THICC
            new DancerSource()
            {
                KeepHead = true,
                BodyAsset = new AssetSource()
                {
                    PackageFile = "BioD_TwrHub_605Lounge1.pcc",
                    AssetPath = "BIOG_HMM_ARM_HVY_R.HVYb.HMM_ARM_HVYb_MDL"
                },
                //HeadAsset = new AssetSource()
                //{
                //    PackageFile = "BioD_TwrHub_605Lounge1.pcc",
                //    AssetPath = "BIOG_HMM_HED_ProMorph.Average.HMM_HED_PROAverage_MDL"
                //},
                //MorphFace = new AssetSource()
                //{
                //    PackageFile = "BioD_TwrHub_605Lounge1.pcc",
                //    AssetPath = "BIOFace_TwrHub.General.BioFace_imposter_conrad"
                //}
            },
        };

        class AssetSource
        {
            public string PackageFile { get; set; }
            public string AssetPath { get; set; }

            public ExportEntry GetAsset()
            {
                var packageF = MERFileSystem.GetPackageFile(PackageFile);
                return packageF != null ? MEPackageHandler.OpenMEPackage(packageF).FindExport(AssetPath) : null;
            }
        }

        class DancerSource
        {
            public AssetSource HeadAsset { get; set; }
            public AssetSource BodyAsset { get; set; }
            public CFVector3 Location { get; set; }
            public CIVector3 Rotation { get; set; }
            public float DrawScale { get; set; } = 1;
            public AssetSource MorphFace { get; set; }
            public bool KeepHead { get; set; }
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
