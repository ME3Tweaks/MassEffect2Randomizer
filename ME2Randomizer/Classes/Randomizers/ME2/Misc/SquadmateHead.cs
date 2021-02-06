using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Gammtek.Extensions.Collections.Generic;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.SharpDX;
using ME3ExplorerCore.Unreal;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class SquadmateHead
    {
        private static PackageCache HeadAssetCache = new PackageCache();

        public static void ResetClass()
        {
            HeadAssetCache.ReleasePackages();
        }

        class SquadMate
        {
            public string ClassName { get; set; }
            public bool IsFemale { get; set; }
            public bool IsSwappable { get; set; } = true;
            public string NamePrefix { get; set; }

            public string GetCharName()
            {
                return ClassName.Substring("SFXPawn_".Length);
            }
        }
        private static SquadMate[] SquadmatePawnClasses = new[]
        {
            new SquadMate() {ClassName = "SFXPawn_Garrus", NamePrefix = "Gar"},
            new SquadMate() {ClassName = "SFXPawn_Grunt", NamePrefix = "Gru"},
            new SquadMate() {ClassName = "SFXPawn_Jack", IsFemale = true, NamePrefix = "Ja"},
            new SquadMate() {ClassName = "SFXPawn_Jacob", NamePrefix = "Jac"},
            new SquadMate() {ClassName = "SFXPawn_Legion", NamePrefix = "Leg"}, // Is swappable?
            new SquadMate() {ClassName = "SFXPawn_Miranda", IsFemale = true, NamePrefix = "Mir"},
            new SquadMate() {ClassName = "SFXPawn_Mordin",NamePrefix = "Mor"},
            new SquadMate() {ClassName = "SFXPawn_Samara", IsFemale = true,NamePrefix = "Sam"},
            new SquadMate() {ClassName = "SFXPawn_Tali", IsSwappable = false, NamePrefix = "Ta"},
            new SquadMate() {ClassName = "SFXPawn_Thane",NamePrefix = "Th"},
            new SquadMate() {ClassName = "SFXPawn_Wilson",NamePrefix = "Wil"},

            // DLC
            new SquadMate() {ClassName = "SFXPawn_Zaeed", NamePrefix = "Za"}, //VT
            new SquadMate() {ClassName = "SFXPawn_Kasumi", IsFemale = true, NamePrefix = "Ka"}, //MT
            new SquadMate() {ClassName = "SFXPawn_Liara", IsFemale = true,NamePrefix = "Li"}, //EXP_Part01
        };

        class HeadAssetSource : IlliumHub.AssetSource
        {
            /// <summary>
            /// Invoked after porting asset, to fix it up in the new pawn
            /// </summary>
            public Action<ExportEntry> PostPortingFixupDelegate { get; set; }
            public bool IsFemaleAsset { get; set; }
            public float GenderSwapDrawScale { get; set; } = 1f;
            public float MaleToFemaleZCorrection { get; set; } = 1f;
            /// <summary>
            /// Is this asset stored in the executable?
            /// </summary>
            public bool IsCorrectedAsset { get; set; }

            /// <summary>
            /// List of pawns this asset can be installed into. If null, everything is OK
            /// </summary>
            public string[] AllowedPawns { get; set; }
            /// <summary>
            /// If this package should be imported using memory safe. Means it's in a master BIOG file and must be adjusted to work properly
            /// </summary>
            public bool UseMemorySafe { get; set; }
            /// <summary>
            /// An additional hair asset to install for the pawn
            /// </summary>
            public string HairAssetPath { get; set; }
            /// <summary>
            /// End of made up name
            /// </summary>
            public string NameSuffix { get; set; }
            /// <summary>
            /// Is this asset an existing squadmate's head?
            /// </summary>
            public bool IsSquadmateHead { get; set; }

            public override ExportEntry GetAsset()
            {
                if (IsCorrectedAsset)
                {
                    var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile($"correctedmeshes.heads.{PackageFile}")));
                    return package.FindExport(AssetPath);
                }
                else
                {
                    var packageF = MERFileSystem.GetPackageFile(PackageFile);
                    return packageF != null ? MEPackageHandler.OpenMEPackage(packageF).FindExport(AssetPath) : null;
                }
            }

            public ExportEntry GetHairAsset()
            {
                if (IsCorrectedAsset)
                {
                    var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile($"correctedmeshes.heads.{PackageFile}")));
                    return package.FindExport(HairAssetPath);
                }
                else
                {
                    var packageF = MERFileSystem.GetPackageFile(PackageFile);
                    return packageF != null ? MEPackageHandler.OpenMEPackage(packageF).FindExport(HairAssetPath) : null;
                }
            }

            public bool CanApplyTo(SquadMate squadmateInfo)
            {
                if (AllowedPawns == null) return true;
                return AllowedPawns.Contains(squadmateInfo.ClassName);
            }

        }

        private static HeadAssetSource[] HeadAssetSources = new[]
        {
            new HeadAssetSource()
            {
                PackageFile = "BioH_Vixen_00.pcc",
                AssetPath = "BIOG_HMF_HED_PROMorph_R.PROMiranda.HMF_HED_PRO_Miranda_MDL",
                IsFemaleAsset = true,
                NameSuffix = "anda",
                IsSquadmateHead = true
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Assassin_00.pcc",
                AssetPath = "BIOG_DRL_HED_PROThane_R.Thane.DRL_HED_PROTHANE_MDL",
                GenderSwapDrawScale = 0.961f, //Male -> Female. Might need more for miranda. This is tuned for samara
                NameSuffix="ne",
                IsSquadmateHead = true
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Convict_00.pcc",
                AssetPath = "BIOG_HMF_HED_PROMorph_R.PROJack.HMF_HED_PROJack_MDL",
                IsFemaleAsset = true,
                NameSuffix = "ck",
                    IsSquadmateHead = true
            },
            //new HeadAssetSource()
            //{
            //      // HAS TOO MANY BONES
            //    PackageFile = "BioH_Geth_00.pcc",
            //    AssetPath = "BIOG_GTH_HED_PROMorph.GTH_HED_PROLegion_MDL",
            //    NameSuffix = "ion"
            //},
            new HeadAssetSource()
            {
                PackageFile = "BioH_Garrus_00.pcc",
                AssetPath = "BIOG_TUR_HED_PROMorph_R.PROGarrus.TUR_HED_PROGarrus_Damage_MDL",
                NameSuffix = "rus",
                IsSquadmateHead = true
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Leading_00.pcc",
                AssetPath = "BIOG_HMM_HED_PROMorph.Jacob.HMM_HED_PROJacob_MDL",
                GenderSwapDrawScale = 0.961f, //Male -> Female
                NameSuffix = "cob",
                    IsSquadmateHead = true
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Mystic_00.pcc",
                AssetPath = "BIOG_ASA_HED_PROMorph_R.Samara.ASA_HED_PROSamara_MDL",
                NameSuffix = "ara",
                IsSquadmateHead = true
            },


            // Non squadmates
            new HeadAssetSource()
            {
                PackageFile = "BioD_OmgHub_400Alley.pcc",
                AssetPath = "BIOG_ALN_HED_PROMorph_R.ALN_HED_PROBase_MDL",
                AllowedPawns = new[]
                {
                    "SFXPawn_Garrus",
                    "SFXPawn_Zaeed",
                    "SFXPawn_Grunt"
                },
                NameSuffix = "cha"
            },
            new HeadAssetSource()
            {
                PackageFile = "IllusiveNoChest.pcc",
                AssetPath = "BIOG_HMM_HED_PROMorph.IllusiveMan_MER.HMM_HED_PROIllusiveMan_MDL",
                GenderSwapDrawScale = 0.961f, //Male -> Female
                IsCorrectedAsset = true,
                NameSuffix = "per"
            },
            new HeadAssetSource()
            {
                PackageFile = "BIOG_HMM_HED_PROMorph.pcc",
                AssetPath = "Kaiden.HMM_HED_PROKaiden_MDL",
                UseMemorySafe = true,
                GenderSwapDrawScale = 0.95f, //Male -> Female. Might need more for miranda. This is tuned for samara
                PostPortingFixupDelegate = MakeKaidanNoLongerHulk,
                NameSuffix = "dan"
            },
            new HeadAssetSource()
            {
                PackageFile = "BioD_HorCr1_303AshKaidan.pcc",
                AssetPath = "BIOG_HMF_HED_PROMorph_R.PROAshley.HMF_HED_PRO_Ashley_MDL_LOD0",
                HairAssetPath = "BIOG_HMF_HIR_PRO.Ashley.HMF_HIR_PROAshley_MDL",
                UseMemorySafe = true,
                GenderSwapDrawScale = 0.99f, // Shrink neck just a bit. Ruins miranda, she might only work with female assets
                NameSuffix="ley"
            }
        };

        private static void MakeKaidanNoLongerHulk(ExportEntry kaidanMDL)
        {
            var kaidanFacMaterial = kaidanMDL.FileRef.FindExport("BIOG_HMM_HED_PROMorph.Kaiden.HMM_HED_PROKaiden_Face_Mat_1a");
            var kaidanScalpMaterial = kaidanMDL.FileRef.FindExport("BIOG_HMM_HED_PROMorph.Kaiden.HMM_HED_PROKaiden_Scalp_Mat_1a");

            var vectorsFC = VectorParameter.GetVectorParameters(kaidanFacMaterial);
            var color = new Vector4();
            color.W = 0.6455554f;
            color.X = 0.3157628f;
            color.Y = 0.2157644f;
            color.Z = 1f;
            vectorsFC[0].ParameterValue = color;
            VectorParameter.WriteVectorParameters(kaidanFacMaterial, vectorsFC);

            var vectorsSC = VectorParameter.GetVectorParameters(kaidanScalpMaterial);
            vectorsSC[1].ParameterValue = color;
            VectorParameter.WriteVectorParameters(kaidanScalpMaterial, vectorsSC);
        }

        public static bool RandomizeExport(ExportEntry export, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            return ForcedRun(export);
        }

        private static bool CanRandomize(ExportEntry export)
        {
            // It must be a default object and have the correct class set for the defaults
            // Check superclass as our objectname is wrong
            if (export.IsDefaultObject && export.ObjectName.Name.StartsWith("Default__SFXPawn_") && SquadmatePawnClasses.Any(x => export.Class.InheritsFrom(x.ClassName)))
                return true;
            return false;
        }

        private static bool CanRandomize2(ExportEntry export)
        {
            if (export.IsDefaultObject || export.ClassName != "SkeletalMeshComponent") return false;
            var smp = export.GetProperty<ObjectProperty>("SkeletalMesh");
            if (smp == null || smp.Value == 0) return false;
            var entry = smp.ResolveToEntry(export.FileRef) as IEntry;
            var fpath = entry.InstancedFullPath;
            if (HeadAssetSources.Any(x => x.IsSquadmateHead && x.AssetPath == fpath)) // is this an existing squadmate head asset?
                return true; // It's a model

            // Check for Jacob or Wilson. Not entirely sure how we can do this reliably...

            //// It must be a default object and have the correct class set for the defaults
            //// Check superclass as our objectname is wrong
            //if (export.IsDefaultObject && export.ObjectName.Name.StartsWith("Default__SFXPawn_") && SquadmatePawnClasses.Any(x => export.Class.InheritsFrom(x.ClassName)))
            //    return true;
            return false;
        }

        public static bool RandomizeExport2(ExportEntry headMeshExp, RandomizationOption option)
        {
            if (!CanRandomize2(headMeshExp)) return false;
            Debug.WriteLine($"Can randomize SQM HED {headMeshExp.InstancedFullPath} in {Path.GetFileName(headMeshExp.FileRef.FilePath)}");

            var skelMesh = headMeshExp.GetProperty<ObjectProperty>("SkeletalMesh");
            if (skelMesh != null)
            {
                // Select a new asset.
                var existingAsset = skelMesh.ResolveToEntry(headMeshExp.FileRef);
                var newAsset = HeadAssetSources.RandomElement();
                var squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => newAsset.AssetPath.Contains(x.GetCharName()));
                if(squadmateInfo == null)
                    Debugger.Break();

                while (newAsset.AssetPath == existingAsset.InstancedFullPath || !newAsset.CanApplyTo(squadmateInfo))
                {
                    // Ensure change
                    newAsset = HeadAssetSources.RandomElement();
                }

                // Port in the new asset.
                var sourceExp = newAsset.GetAsset();
                var newMdl = PackageTools.PortExportIntoPackage(headMeshExp.FileRef, sourceExp, useMemorySafeImport: newAsset.UseMemorySafe, cache: HeadAssetCache);

                // Write the properties.
                skelMesh.Value = newMdl.UIndex;
                headMeshExp.WriteProperty(skelMesh);

                var newName = squadmateInfo.NamePrefix + newAsset.NameSuffix;
                var newTlkId = TLKHandler.GetNewTLKID();
                TLKHandler.ReplaceString(newTlkId, newName);
                headMeshExp.WriteProperty(new StringRefProperty(newTlkId, "PrettyName"));
                if (headMeshExp.GetProperty<ObjectProperty>("ActorType")?.ResolveToEntry(headMeshExp.FileRef) is ExportEntry actorTypeExp)
                {
                    actorTypeExp.indexValue = ThreadSafeRandom.Next(265789564); // make it memory unique. Not sure this matters if pawn is not
                    var strRef = actorTypeExp.GetProperty<StringRefProperty>("ActorGameNameStrRef");
                    strRef.Value = newTlkId;
                    actorTypeExp.WriteProperty(strRef);
                }

                // This is only useful in BioH files!!
                // Clean up the materials in the instance of the pawn.
                // Have to do full search cause naming system doesn't seem consistent
                // Only look for children of TheWorld so we can do integer check
                var persistentLevel = headMeshExp.ClassName == "BioPawn" ? null : headMeshExp.FileRef.FindExport("TheWorld.PersistentLevel");
                var instance = headMeshExp.ClassName == "BioPawn" ? headMeshExp : headMeshExp.FileRef.Exports.FirstOrDefault(x => x.idxLink == persistentLevel.UIndex && x.ClassName == myClass);
                if (instance != null)
                {
                    if (instance.GetProperty<ObjectProperty>("HeadMesh")?.ResolveToEntry(headMeshExp.FileRef) is ExportEntry instHeadMesh)
                    {
                        instHeadMesh.RemoveProperty("Materials");
                        if (squadmateInfo.IsFemale != newAsset.IsFemaleAsset)
                        {
                            // We need to size it
                            instHeadMesh.WriteProperty(new FloatProperty(newAsset.GenderSwapDrawScale, "Scale"));
                        }
                    }
                }

                // Remove morph head from the biopawn, if any, as this will corrupt the head
                headMeshExp.RemoveProperty("MorphHead");

                // Add hair asset if necessary
                if (newAsset.HairAssetPath != null)
                {
                    // Port in hair
                    var hairMDL = PackageTools.PortExportIntoPackage(headMeshExp.FileRef, newAsset.GetHairAsset(), useMemorySafeImport: newAsset.UseMemorySafe, cache: HeadAssetCache);

                    // Clone existing mesh
                    var hairMeshExp = EntryCloner.CloneEntry(headMeshExp);
                    hairMeshExp.ObjectName = "HairMesh";
                    hairMeshExp.RemoveProperty("AnimTreeTemplate");
                    hairMeshExp.WriteProperty(new ObjectProperty(hairMDL.UIndex, "SkeletalMesh"));

                    // Write hair mesh prop
                    headMeshExp.WriteProperty(new ObjectProperty(hairMeshExp.UIndex, "m_oHairMesh"));
                }

                if (squadmateInfo.ClassName == "SFXPawn_Thane")
                {
                    // We must update his mesh to get rid of those lime windshield wipers

                    IMEPackage newMeshP;
                    if (headMeshExp.ObjectName == "Default__SFXPawn_Thane_02")
                    {
                        // Install DLC version of mesh
                        newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelidsDLC.pcc")));
                    }
                    else
                    {
                        // Install basegame version of mesh
                        newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelids.pcc")));
                    }

                    var meshExp = headMeshExp.GetProperty<ObjectProperty>("Mesh").ResolveToEntry(headMeshExp.FileRef) as ExportEntry;
                    var meshVal = meshExp.GetProperty<ObjectProperty>("SkeletalMesh").ResolveToEntry(headMeshExp.FileRef) as ExportEntry;
                    var newMDL = newMeshP.FindExport(meshVal.InstancedFullPath);

                    // Technically this should work
                    EntryImporter.ReplaceExportDataWithAnother(newMDL, meshVal);

                }

                // Post install fixup
                newAsset.PostPortingFixupDelegate?.Invoke(newMdl);
                return true;
            }

            return false;
        }

        private static bool ForcedRun(ExportEntry export, bool doWorldCheck = true)
        {
            var myClass = export.ClassName;
            var squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => export.Class.InheritsFrom(x.ClassName));
            if (squadmateInfo != null && export.GetProperty<ObjectProperty>("HeadMesh")?.ResolveToEntry(export.FileRef) is ExportEntry headMeshExp)
            {
                var skelMesh = headMeshExp.GetProperty<ObjectProperty>("SkeletalMesh");
                if (skelMesh != null)
                {
                    // Select a new asset.
                    var existingAsset = skelMesh.ResolveToEntry(export.FileRef);
                    var newAsset = HeadAssetSources.RandomElement();
                    while (newAsset.AssetPath == existingAsset.InstancedFullPath || !newAsset.CanApplyTo(squadmateInfo))
                    {
                        // Ensure change
                        newAsset = HeadAssetSources.RandomElement();
                    }

                    // Port in the new asset.
                    var sourceExp = newAsset.GetAsset();
                    var newMdl = PackageTools.PortExportIntoPackage(export.FileRef, sourceExp, useMemorySafeImport: newAsset.UseMemorySafe, cache: HeadAssetCache);

                    // Write the properties.
                    skelMesh.Value = newMdl.UIndex;
                    headMeshExp.WriteProperty(skelMesh);

                    var newName = squadmateInfo.NamePrefix + newAsset.NameSuffix;
                    var newTlkId = TLKHandler.GetNewTLKID();
                    TLKHandler.ReplaceString(newTlkId, newName);
                    export.WriteProperty(new StringRefProperty(newTlkId, "PrettyName"));
                    if (export.GetProperty<ObjectProperty>("ActorType")?.ResolveToEntry(export.FileRef) is ExportEntry actorTypeExp)
                    {
                        actorTypeExp.indexValue = ThreadSafeRandom.Next(265789564); // make it memory unique. Not sure this matters if pawn is not
                        var strRef = actorTypeExp.GetProperty<StringRefProperty>("ActorGameNameStrRef");
                        strRef.Value = newTlkId;
                        actorTypeExp.WriteProperty(strRef);
                    }

                    // Clean up the materials in the instance of the pawn.
                    // Have to do full search cause naming system doesn't seem consistent
                    // Only look for children of TheWorld so we can do integer check
                    var persistentLevel = export.ClassName == "BioPawn" ? null : export.FileRef.FindExport("TheWorld.PersistentLevel");
                    var instance = export.ClassName == "BioPawn" ? export : export.FileRef.Exports.FirstOrDefault(x => x.idxLink == persistentLevel.UIndex && x.ClassName == myClass);
                    if (instance != null)
                    {
                        if (instance.GetProperty<ObjectProperty>("HeadMesh")?.ResolveToEntry(export.FileRef) is ExportEntry instHeadMesh)
                        {
                            instHeadMesh.RemoveProperty("Materials");
                            if (squadmateInfo.IsFemale != newAsset.IsFemaleAsset)
                            {
                                // We need to size it
                                instHeadMesh.WriteProperty(new FloatProperty(newAsset.GenderSwapDrawScale, "Scale"));
                            }
                        }
                    }

                    // Remove morph head from the biopawn, if any, as this will corrupt the head
                    export.RemoveProperty("MorphHead");

                    // Add hair asset if necessary
                    if (newAsset.HairAssetPath != null)
                    {
                        // Port in hair
                        var hairMDL = PackageTools.PortExportIntoPackage(export.FileRef, newAsset.GetHairAsset(), useMemorySafeImport: newAsset.UseMemorySafe, cache: HeadAssetCache);

                        // Clone existing mesh
                        var hairMeshExp = EntryCloner.CloneEntry(headMeshExp);
                        hairMeshExp.ObjectName = "HairMesh";
                        hairMeshExp.RemoveProperty("AnimTreeTemplate");
                        hairMeshExp.WriteProperty(new ObjectProperty(hairMDL.UIndex, "SkeletalMesh"));

                        // Write hair mesh prop
                        export.WriteProperty(new ObjectProperty(hairMeshExp.UIndex, "m_oHairMesh"));
                    }

                    if (squadmateInfo.ClassName == "SFXPawn_Thane")
                    {
                        // We must update his mesh to get rid of those lime windshield wipers

                        IMEPackage newMeshP;
                        if (export.ObjectName == "Default__SFXPawn_Thane_02")
                        {
                            // Install DLC version of mesh
                            newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelidsDLC.pcc")));
                        }
                        else
                        {
                            // Install basegame version of mesh
                            newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelids.pcc")));
                        }

                        var meshExp = export.GetProperty<ObjectProperty>("Mesh").ResolveToEntry(export.FileRef) as ExportEntry;
                        var meshVal = meshExp.GetProperty<ObjectProperty>("SkeletalMesh").ResolveToEntry(export.FileRef) as ExportEntry;
                        var newMDL = newMeshP.FindExport(meshVal.InstancedFullPath);

                        // Technically this should work
                        EntryImporter.ReplaceExportDataWithAnother(newMDL, meshVal);

                    }

                    // Post install fixup
                    newAsset.PostPortingFixupDelegate?.Invoke(newMdl);
                    return true;
                }
            }

            return false;
        }

        public static bool FilePrerun(IMEPackage package, RandomizationOption arg2)
        {
            var packagename = Path.GetFileName(package.FilePath);
            if (packagename.Equals("BioP_ProCer.pcc", StringComparison.InvariantCultureIgnoreCase) //Miranda, Jacob
                //|| packagename.Equals("BioP_RprGtA.pcc", StringComparison.InvariantCultureIgnoreCase) //Legion // Used as an import for level
                //|| packagename.Equals("BioP_TwrAsA.pcc", StringComparison.InvariantCultureIgnoreCase) // Thane // Used as an import at end of level
                // Tali is in BioP_ProFre, but we can't edit her head so it doesn't matter
            )
            {
                Log.Information($"Removing squadmate class from persistent package references list {packagename}");
                // We need to update the squadmate pawns
                var worldInfo = package.FindExport("TheWorld.PersistentLevel.BioWorldInfo_0"); //procer
                var persistObjs = worldInfo.GetProperty<ArrayProperty<ObjectProperty>>("m_AutoPersistentObjects");
                persistObjs.RemoveAll(x => x.ResolveToEntry(package).ClassName.Contains("SFXPawn_")); // Do not store Miranda or Jacob pawns in memory

                worldInfo.WriteProperty(persistObjs);
                return true;
            }

            // There is file for miranda on BioA_ZyaVtl_100... why...??

            return false;
        }
    }
}