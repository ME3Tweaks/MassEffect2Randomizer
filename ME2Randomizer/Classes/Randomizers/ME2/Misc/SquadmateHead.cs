using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Packages.CloningImportingAndRelinking;
using ME3ExplorerCore.SharpDX;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;
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
            /// List of pawns this asset explictly cannot be installed into. If null, there are no blacklisted pawns
            /// </summary>
            public string[] DisallowedPawns { get; set; }
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
            /// <summary>
            /// Can this asset actually be used?
            /// </summary>
            public bool IsUsable { get; set; } = true;

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
                if (AllowedPawns == null && DisallowedPawns == null) return true;
                if (DisallowedPawns != null && DisallowedPawns.Contains(squadmateInfo.ClassName))
                    return false;
                if (AllowedPawns == null)
                    return true;
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
                PackageFile = "ThaneNoChest.pcc",
                AssetPath = "BIOG_DRL_HED_PROThane_R.Thane.DRL_HED_PROTHANE_NOCOLLAR_MDL",
                GenderSwapDrawScale = 0.961f, //Male -> Female. Might need more for miranda. This is tuned for samara
                NameSuffix="ne",
                IsSquadmateHead = true,
                IsCorrectedAsset = true
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Convict_00.pcc",
                AssetPath = "BIOG_HMF_HED_PROMorph_R.PROJack.HMF_HED_PROJack_MDL",
                IsFemaleAsset = true,
                NameSuffix = "ck",
                IsSquadmateHead = true
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Professor_00.pcc",
                AssetPath = "BIOG_SAL_HED_PROMorph_R.Mordin.SAL_HED_PROMordin_MDL",
                NameSuffix = "din",
                IsSquadmateHead = true
            },
            new HeadAssetSource()
            {
                  // HAS TOO MANY BONES
                PackageFile = "BioH_Geth_00.pcc",
                AssetPath = "BIOG_GTH_HED_PROMorph.GTH_HED_PROLegion_MDL",
                NameSuffix = "ion",
                IsUsable = false
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Garrus_00.pcc",
                AssetPath = "BIOG_TUR_HED_PROMorph_R.PROGarrus.TUR_HED_PROGarrus_Damage_MDL",
                NameSuffix = "rus",
                IsSquadmateHead = true,
                DisallowedPawns = new []
                {
                    "SFXPawn_Miranda"
                }
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Leading_00.pcc",
                AssetPath = "BIOG_HMM_HED_PROMorph.Jacob.HMM_HED_PROJacob_MDL",
                GenderSwapDrawScale = 0.961f, //Male -> Female
                NameSuffix = "cob",
                    IsSquadmateHead = true,
                    DisallowedPawns = new []
                    {
                        "SFXPawn_Miranda"
                    }
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Mystic_00.pcc",
                AssetPath = "BIOG_ASA_HED_PROMorph_R.Samara.ASA_HED_PROSamara_MDL",
                NameSuffix = "ara",
                IsSquadmateHead = true,
                DisallowedPawns = new []
                {
                    "SFXPawn_Miranda"
                }
            },

            new HeadAssetSource()
            {
                PackageFile = "BioH_Veteran_00.pcc",
                AssetPath = "BIOG_HMM_HED_PROZaeed.Zaeed.HMM_HED_PROZaeed_MDL",
                NameSuffix = "eed",
                IsSquadmateHead = true,
                IsUsable = false
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Thief_00.pcc",
                AssetPath = "BIOG_HMF_HED_PROKasumi_R.Head.HMF_HED_PROKasumi_MDL",
                NameSuffix = "umi",
                IsSquadmateHead = true,
                IsUsable = false
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Grunt_00.pcc",
                AssetPath = "BIOG_KRO_HED_PROMorph.Grunt.KRO_HED_Grunt_MDL",
                NameSuffix = "unt",
                IsSquadmateHead = true,
                IsUsable = false
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
                NameSuffix = "per",
                DisallowedPawns = new []
                {
                    "SFXPawn_Miranda"
                }
            },
            new HeadAssetSource()
            {
                PackageFile = "BIOG_HMM_HED_PROMorph.pcc",
                AssetPath = "Kaiden.HMM_HED_PROKaiden_MDL",
                UseMemorySafe = true,
                GenderSwapDrawScale = 0.95f, //Male -> Female. Might need more for miranda. This is tuned for samara
                PostPortingFixupDelegate = MakeKaidanNoLongerHulk,
                NameSuffix = "dan",
                DisallowedPawns = new []
                {
                    "SFXPawn_Miranda"
                }
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
            var color = new CFVector4();
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

        private static bool CanRandomize2(ExportEntry export)
        {
            if (export.IsDefaultObject || export.ClassName != "SkeletalMeshComponent") return false;
            //if (export.UIndex == 3473)
            //    Debugger.Break();
            var smp = export.GetProperty<ObjectProperty>("SkeletalMesh");
            if (smp == null || smp.Value == 0) return false;
            var entry = smp.ResolveToEntry(export.FileRef) as IEntry;
            var fpath = entry.InstancedFullPath;
            if (HeadAssetSources.Any(x => x.IsSquadmateHead && x.AssetPath == fpath)) // is this an existing squadmate head asset?
                return true; // It's a model

            // Check for Jacob or Wilson. Not entirely sure how we can do this reliably...
            var parentObj = export.Parent as ExportEntry;
            if (parentObj != null && parentObj.IsDefaultObject && export.ObjectName.Name.Contains("HeadMesh") && (parentObj.Class.InheritsFrom("SFXPawn_Jacob") || parentObj.Class.InheritsFrom("SFXPawn_Wilson")))
            {
                return true;
            }

            //// It must be a default object and have the correct class set for the defaults
            //// Check superclass as our objectname is wrong
            //if (export.IsDefaultObject && export.ObjectName.Name.StartsWith("Default__SFXPawn_") && SquadmatePawnClasses.Any(x => export.Class.InheritsFrom(x.ClassName)))
            //    return true;
            return false;
        }

        public static bool RandomizeExport2(ExportEntry headMeshExp, RandomizationOption option)
        {
            if (!CanRandomize2(headMeshExp)) return false;
            var fname = Path.GetFileName(headMeshExp.FileRef.FilePath);
            Debug.WriteLine($"Can randomize SQM HED {headMeshExp.InstancedFullPath} in {fname}");
            var skelMesh = headMeshExp.GetProperty<ObjectProperty>("SkeletalMesh");
            if (skelMesh != null)
            {
                // Select a new asset.
                var existingAsset = skelMesh.ResolveToEntry(headMeshExp.FileRef);
                var newAsset = HeadAssetSources.RandomElement();
                var squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => existingAsset.ObjectName.Name.Contains(x.GetCharName(), StringComparison.InvariantCultureIgnoreCase));
                if (squadmateInfo == null)
                {
                    // Check if Wilson or Jacob
                    squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => headMeshExp.Parent.ObjectName.Name.Contains(x.GetCharName(), StringComparison.InvariantCultureIgnoreCase));
                    if (squadmateInfo == null)
                        Debugger.Break();
                }

                while (!newAsset.IsUsable || newAsset.AssetPath == existingAsset.InstancedFullPath || !newAsset.CanApplyTo(squadmateInfo))
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

                // update the bone positions... dunno if this is a good idea or not
                UpdateBonePositionsForHead(existingAsset, newMdl);
                newMdl.ObjectName = newMdl.ObjectName.Name + $"_{squadmateInfo.ClassName}ified";

                // Get parent object
                var owningPawn = headMeshExp.Parent as ExportEntry;
                if (owningPawn == null)
                    Debugger.Break();
                var owningPawnIsClassDef = owningPawn.ClassName.StartsWith("SFXPawn_");

                var newName = squadmateInfo.NamePrefix + newAsset.NameSuffix;
                var newTlkId = TLKHandler.GetNewTLKID();
                TLKHandler.ReplaceString(newTlkId, newName);
                if (owningPawn.IsDefaultObject)
                {
                    owningPawn.WriteProperty(new StringRefProperty(newTlkId, "PrettyName"));
                }


                if (owningPawn.GetProperty<ObjectProperty>("ActorType")?.ResolveToEntry(owningPawn.FileRef) is ExportEntry actorTypeExp)
                {
                    if (!PackageTools.IsPersistentPackage(Path.GetFileName(owningPawn.FileRef.FilePath)))
                    {
                        actorTypeExp.indexValue = ThreadSafeRandom.Next(265789564); // make it memory unique.
                    }

                    var strRef = actorTypeExp.GetProperty<StringRefProperty>("ActorGameNameStrRef");
                    if (strRef != null)
                    {
                        strRef.Value = newTlkId;
                        actorTypeExp.WriteProperty(strRef);
                    }
                }

                // Clean up the materials in the instance of the pawn.
                // Have to do full search cause naming system doesn't seem consistent
                // Only look for children of TheWorld so we can do integer check
                var persistentLevel = owningPawn.ClassName == "BioPawn" ? null : owningPawn.FileRef.FindExport("TheWorld.PersistentLevel");
                var instance = owningPawn.ClassName == "BioPawn" ? headMeshExp : owningPawn.FileRef.Exports.FirstOrDefault(x => x.idxLink == persistentLevel.UIndex && x.IsA(owningPawn.ClassName));
                if (instance != null)
                {
                    if (instance.ClassName == "SkeletalMeshComponent")
                    {
                        instance.RemoveProperty("Materials");
                        if (squadmateInfo.IsFemale != newAsset.IsFemaleAsset && instance.GetProperty<ObjectProperty>("SkeletalMesh") is ObjectProperty obj && obj.Value != 0)
                        {
                            // We need to size it
                            instance.WriteProperty(new FloatProperty(newAsset.GenderSwapDrawScale, "Scale"));
                        }
                    }
                    else if (instance.GetProperty<ObjectProperty>("HeadMesh")?.ResolveToEntry(owningPawn.FileRef) is ExportEntry instHeadMesh)
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
                owningPawn.RemoveProperty("MorphHead");

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
                    owningPawn.WriteProperty(new ObjectProperty(hairMeshExp.UIndex, owningPawn.IsDefaultObject ? "HairMesh" : "m_oHairMesh"));
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

        private static void UpdateBonePositionsForHead(IEntry existingAsset, ExportEntry newMdl)
        {
            ExportEntry oldMdl = existingAsset as ExportEntry;
            oldMdl ??= EntryImporter.ResolveImport(existingAsset as ImportEntry);

            var oldBin = ObjectBinary.From<SkeletalMesh>(oldMdl);
            var newBin = ObjectBinary.From<SkeletalMesh>(newMdl);

            Dictionary<MeshBone, MeshBone> boneMap = new Dictionary<MeshBone, MeshBone>();
            foreach (var bone in oldBin.RefSkeleton)
            {
                var matchingNewBone = newBin.RefSkeleton.FirstOrDefault(x => x.Name.Name == bone.Name.Name);
                if (matchingNewBone != null)
                {
                    // Update it's data
                    matchingNewBone.Orientation = bone.Orientation;
                    matchingNewBone.Position = bone.Position;
                }
            }

            newMdl.WriteBinary(newBin);
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
                            // scaling breaks a lot of shit
                            //if (squadmateInfo.IsFemale != newAsset.IsFemaleAsset)
                            //{
                            //    // We need to size it
                            //    instHeadMesh.WriteProperty(new FloatProperty(newAsset.GenderSwapDrawScale, "Scale"));
                            //}
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
            if (packagename.Equals("BioP_ProCer.pcc", StringComparison.InvariantCultureIgnoreCase)) // Miranda and Jacob pawns at end of stage
            {
                Log.Information("Fixing BioP_ProCer Miranda/Jacob");
                //package.GetUExport(1562).ObjectName = "SFXPawn_Miranda_UNUSED";
                //package.GetUExport(1555).ObjectName = "SFXPawn_Ja_UNUSED";
                //package.GetUExport(1556).ObjectName = "SFXPawn_Jacob_UNUSED";

                List<IEntry> entriesToTrash = new List<IEntry>();
                entriesToTrash.Add(package.GetUExport(3421)); //jacob
                entriesToTrash.Add(package.GetUExport(3420)); // miranda
                entriesToTrash.Add(package.GetUExport(1555)); //def jacob
                entriesToTrash.Add(package.GetUExport(1556)); // def miranda
                entriesToTrash.Add(package.GetUExport(1562)); // def miranda
                entriesToTrash.Add(package.GetUExport(1563)); // def miranda
                EntryPruner.TrashEntriesAndDescendants(entriesToTrash);
            }

            else if (packagename.Equals("BioD_ProCer_300ShuttleBay.pcc", StringComparison.InvariantCultureIgnoreCase)) // Miranda shoots wilson
            {
                Log.Information("Fixing end of ProCer Miranda");

                var oldMirandaProps = package.GetUExport(3555).GetProperties();
                // Bust the imports so we can port things in.
                List<IEntry> entriesToTrash = new List<IEntry>();
                entriesToTrash.Add(package.GetImport(-435)); //sfxgamepawns import
                entriesToTrash.Add(package.GetUExport(3555)); // Miranda instance
                EntryPruner.TrashEntriesAndDescendants(entriesToTrash);

                // Port in SFXPawn_Miranda. Maybe use their alternate outfits?
                var world = package.FindExport("TheWorld.PersistentLevel");

                // Miranda
                var mirandaIdx = ThreadSafeRandom.Next(2);
                mirandaIdx = 1; //debug
                var mirandaSourcePackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_Vixen_0{mirandaIdx}.pcc"));
                var classPath = $"TheWorld.PersistentLevel.SFXPawn_Miranda_";
                if (mirandaIdx != 0)
                {
                    classPath += $"0{mirandaIdx}_";
                }

                classPath += "0";
                var exportToPortIn = mirandaSourcePackage.FindExport(classPath);
                var newMirandaPawn = PackageTools.PortExportIntoPackage(package, exportToPortIn, world.UIndex, false);
                newMirandaPawn.ObjectName = new NameReference("SFXPawn_Miranda", 2);

                CopyPawnInstanceProps(oldMirandaProps, newMirandaPawn);

                // Need to repoint existing things that pointed to the original pawns back to the new ones

                // Miranda
                SeqTools.WriteObjValue(package.GetUExport(3450), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3451), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3454), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3490), newMirandaPawn);

                // Update level
                var worldBin = ObjectBinary.From<Level>(world);
                worldBin.Actors[609] = newMirandaPawn;
                world.WriteBinary(worldBin);

                MERFileSystem.SavePackage(package);
                return true;
            }
            else if (packagename.Equals("BioD_ProCer_350BriefRoom.pcc", StringComparison.InvariantCultureIgnoreCase)) // Miranda and Jacob pawns at end of stage
            {
                Log.Information("Fixing end of ProCer Jacob/Miranda");

                // Bust the imports so we can port things in.
                List<IEntry> entriesToTrash = new List<IEntry>();
                entriesToTrash.Add(package.GetImport(-421)); //jacob
                entriesToTrash.Add(package.GetImport(-422)); // miranda
                entriesToTrash.Add(package.GetImport(-675)); //def jacob
                entriesToTrash.Add(package.GetImport(-676)); // def miranda

                entriesToTrash.Add(package.GetUExport(3314)); // Miranda instance
                entriesToTrash.Add(package.GetUExport(3313)); // Jacob instance

                var oldMirandaProps = package.GetUExport(3314).GetProperties();
                var oldJacobProps = package.GetUExport(3313).GetProperties();

                EntryPruner.TrashEntriesAndDescendants(entriesToTrash);

                // Port in SFXPawn_Miranda, SFXPawn_Jacob instances. Maybe use their alternate outfits?
                var world = package.FindExport("TheWorld.PersistentLevel");

                // Miranda
                var mirandaIdx = ThreadSafeRandom.Next(2);
                mirandaIdx = 1;
                var mirandaSourcePackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_Vixen_0{mirandaIdx}.pcc"));
                var classPath = $"TheWorld.PersistentLevel.SFXPawn_Miranda_";
                if (mirandaIdx != 0)
                {
                    classPath += $"0{mirandaIdx}_";
                }

                classPath += "0";
                var exportToPortIn = mirandaSourcePackage.FindExport(classPath);
                var newMirandaPawn = PackageTools.PortExportIntoPackage(package, exportToPortIn, world.UIndex, false);
                newMirandaPawn.ObjectName = new NameReference("SFXPawn_Miranda", 2);

                // Jacob
                var jacobIdx = ThreadSafeRandom.Next(2);
                var jacobSourcePackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_Leading_0{jacobIdx}.pcc"));
                classPath = "TheWorld.PersistentLevel.SFXPawn_Jacob_";
                if (jacobIdx != 0)
                {
                    classPath += $"0{jacobIdx}_0";
                }
                else
                {
                    classPath += "1"; // he is not zero indexed for some reason
                }

                exportToPortIn = jacobSourcePackage.FindExport(classPath);
                var newJacobPawn = PackageTools.PortExportIntoPackage(package, exportToPortIn, world.UIndex, false);
                newJacobPawn.ObjectName = new NameReference("SFXPawn_Jacob", 1);

                // Update the properties to match the old ones
                CopyPawnInstanceProps(oldMirandaProps, newMirandaPawn);
                CopyPawnInstanceProps(oldJacobProps, newJacobPawn);

                // Need to make them targetable 
                SetPawnTargetable(newJacobPawn, true);
                SetPawnTargetable(newMirandaPawn, true);

                // Need to repoint existing things that pointed to the original pawns back to the new ones

                // Miranda
                SeqTools.WriteOriginator(package.GetUExport(458), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3243), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3247), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3249), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3252), newMirandaPawn);
                SeqTools.WriteObjValue(package.GetUExport(3255), newMirandaPawn);

                // Jacob
                SeqTools.WriteOriginator(package.GetUExport(457), newJacobPawn);
                SeqTools.WriteObjValue(package.GetUExport(3242), newJacobPawn);
                SeqTools.WriteObjValue(package.GetUExport(3254), newJacobPawn);

                // Update level
                var worldBin = ObjectBinary.From<Level>(world);
                worldBin.Actors[43] = newMirandaPawn;
                worldBin.Actors[44] = newJacobPawn;
                world.WriteBinary(worldBin);

                //mirandaI.ObjectName = "SFXPawn_Miranda_NOTUSED";

                //jacobI.ObjectName = "SFXPawn_Jacob_NOTUSED";

                //// Port in SFXPawn_Miranda, SFXPawn_Jacob. Maybe use their alternate outfits?
                //var mirandaIdx = ThreadSafeRandom.Next(2);
                //var mirandaSourcePackage = MEPackageHandler.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_Vixen_0{mirandaIdx}.pcc"));
                //var classPath = "SFXGamePawns.SFXPawn_Miranda";
                //if (mirandaIdx != 0)
                //{
                //    classPath += "_" + mirandaIdx;
                //}
                //var exportToPortIn = mirandaSourcePackage.FindExport(classPath);
                //var newClass = PackageTools.PortExportIntoPackage(package, exportToPortIn);
                //var mirandaPawn = package.GetUExport(3314);
                //mirandaPawn.Class = newClass;
                //var mirandaBin = mirandaPawn.GetPrePropBinary();
                //mirandaBin.OverwriteRange(0x0, BitConverter.GetBytes(newClass.UIndex));
                //mirandaBin.OverwriteRange(0x4, BitConverter.GetBytes(newClass.UIndex));
                //mirandaPawn.WritePrePropsAndProperties(mirandaBin, mirandaPawn.GetProperties());
                MERFileSystem.SavePackage(package);
                return true;
            }

            // There is file for miranda on BioA_ZyaVtl_100... why...??

            return false;
        }

        private static void SetPawnTargetable(ExportEntry pawn, bool targetable)
        {
            var pawnBehavior = pawn.GetProperty<ObjectProperty>("m_oBehavior").ResolveToEntry(pawn.FileRef) as ExportEntry;
            var jbp = pawnBehavior.GetProperties();
            jbp.AddOrReplaceProp(new BoolProperty(targetable, "m_bTargetable"));
            jbp.AddOrReplaceProp(new BoolProperty(true, "m_bTargetableOverride"));
            pawnBehavior.WriteProperties(jbp);
        }

        private static void CopyPawnInstanceProps(PropertyCollection oldPawnProps, ExportEntry newPawnInstance)
        {
            var props = newPawnInstance.GetProperties();
            foreach (var oldProp in oldPawnProps)
            {
                var matchingProp = props.FirstOrDefault(x => x.Name == oldProp.Name);
                if (matchingProp == null || oldProp.Name == "Location" || oldProp.Name == "Rotation" || oldProp.Name == "Tag")
                {
                    props.AddOrReplaceProp(oldProp);
                }
            }
            newPawnInstance.WriteProperties(props);
        }
    }
}