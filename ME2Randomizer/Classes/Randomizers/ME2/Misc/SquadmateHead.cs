using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME2Randomizer.Classes.Randomizers.ME2.Levels;
using ME2Randomizer.Classes.Randomizers.Utility;
using LegendaryExplorerCore.Gammtek.Extensions.Collections.Generic;
using LegendaryExplorerCore.Helpers;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.SharpDX;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    class SquadmateHead
    {
        private static MERPackageCache HeadAssetCache = new MERPackageCache();

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
            public bool SameGenderOnly { get; set; }
            public string InternalName { get; set; }

            public string GetCharName()
            {
                return ClassName.Substring("SFXPawn_".Length);
            }
        }
        private static SquadMate[] SquadmatePawnClasses = new[]
        {
            new SquadMate() {ClassName = "SFXPawn_Garrus", InternalName="Garrus",NamePrefix = "Gar"},
            new SquadMate() {ClassName = "SFXPawn_Grunt", InternalName="Garrus",NamePrefix = "Gru"},
            new SquadMate() {ClassName = "SFXPawn_Jack", InternalName="Convict",IsFemale = true, NamePrefix = "Ja", SameGenderOnly = true}, // her neck is too smol
            new SquadMate() {ClassName = "SFXPawn_Jacob", InternalName="Leading",NamePrefix = "Jac"},
            new SquadMate() {ClassName = "SFXPawn_Legion", InternalName="Geth",NamePrefix = "Leg"}, // Is swappable?
            new SquadMate() {ClassName = "SFXPawn_Miranda", InternalName="Vixen",IsFemale = true, NamePrefix = "Mir", SameGenderOnly = true}, // her neck really ruins things
            new SquadMate() {ClassName = "SFXPawn_Mordin",InternalName="Professor",NamePrefix = "Mor"},
            new SquadMate() {ClassName = "SFXPawn_Samara", InternalName="Mystic",IsFemale = true,NamePrefix = "Sam"},
            new SquadMate() {ClassName = "SFXPawn_Tali", InternalName="Tali",IsSwappable = false, NamePrefix = "Ta"},
            new SquadMate() {ClassName = "SFXPawn_Thane",InternalName="Assassin",NamePrefix = "Tha"},
            new SquadMate() {ClassName = "SFXPawn_Wilson",NamePrefix = "Wil"},

            // DLC
            new SquadMate() {ClassName = "SFXPawn_Zaeed", InternalName="Veteran",NamePrefix = "Za"}, //VT
            new SquadMate() {ClassName = "SFXPawn_Kasumi", InternalName="Thief",IsFemale = true, NamePrefix = "Ka"}, //MT
            new SquadMate() {ClassName = "SFXPawn_Liara", InternalName="Liara",IsFemale = true,NamePrefix = "Li"}, //EXP_Part01
        };

        class HeadAssetSource : IlliumHub.AssetSource
        {
            /// <summary>
            /// Invoked after porting asset, to fix it up in the new pawn
            /// </summary>
            public Action<SquadMate, ExportEntry> PostPortingFixupDelegate { get; set; }
            public bool IsFemaleAsset { get; set; }
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

            public override ExportEntry GetAsset(MERPackageCache cache = null)
            {
                if (IsCorrectedAsset)
                {
                    var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(MERUtilities.GetEmbeddedStaticFilesBinaryFile($"correctedmeshes.heads.{PackageFile}")));
                    return package.FindExport(AssetPath);
                }
                else
                {
                    var packageF = MERFileSystem.GetPackageFile(PackageFile);
                    return packageF != null ? MERFileSystem.OpenMEPackage(packageF).FindExport(AssetPath) : null;
                }
            }

            public ExportEntry GetHairAsset()
            {
                if (IsCorrectedAsset)
                {
                    var package = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(MERUtilities.GetEmbeddedStaticFilesBinaryFile($"correctedmeshes.heads.{PackageFile}")));
                    return package.FindExport(HairAssetPath);
                }
                else
                {
                    var packageF = MERFileSystem.GetPackageFile(PackageFile);
                    return packageF != null ? MERFileSystem.OpenMEPackage(packageF).FindExport(HairAssetPath) : null;
                }
            }

            public bool CanApplyTo(SquadMate squadmateInfo)
            {
                if (squadmateInfo.SameGenderOnly && IsFemaleAsset != squadmateInfo.IsFemale)
                    return false;
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
                NameSuffix="ne",
                IsSquadmateHead = true,
                IsCorrectedAsset = true,
                DisallowedPawns = new []
                {
                    "SFXPawn_Thane" // Prevent installing this corrected asset onto thane
                }
            },
            // Thane LOOKUP INFO ONLY
            new HeadAssetSource()
            {
                PackageFile = "BioH_Assassin_00.pcc", // Why is this path wrong?
                AssetPath = "BIOG_DRL_HED_PROThane_R.Thane.DRL_HED_PROTHANE_MDL",
                NameSuffix="ne",
                IsSquadmateHead = true,
                IsUsable = false,
            },
            // DLC pack thane
            new HeadAssetSource()
            {
                PackageFile = "BioH_Assassin_02.pcc",
                AssetPath = "BIOG_DRL_HED_PROThane_ALT_R.Thane.DRL_HED_THANE_ALT_MDL",
                NameSuffix="ne",
                IsSquadmateHead = true,
                IsUsable = false,
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
                PackageFile = "BioH_Convict_02.pcc",
                AssetPath = "BIOG_HMF_HED_PROJack_ALT_R.Jack.HMF_HED_PROJack_ALT_MDL",
                IsFemaleAsset = true,
                NameSuffix = "ck",
                IsSquadmateHead = true,
                IsUsable = false
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Professor_00.pcc",
                AssetPath = "BIOG_SAL_HED_PROMorph_R.Mordin.SAL_HED_PROMordin_MDL",
                NameSuffix = "din",
                IsSquadmateHead = true,
                DisallowedPawns = new[]
                {
                    "SFXPawn_Jack",
                    "SFXPawn_Miranda",
                    "SFXPawn_Samara",
                    "SFXPawn_Kasumi",
                    "SFXPawn_Morinth",
                }
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
                PostPortingFixupDelegate = GarrusHeadZFix,
                DisallowedPawns = new []
                {
                    "SFXPawn_Jack",
                    "SFXPawn_Miranda",
                    "SFXPawn_Samara",
                    "SFXPawn_Kasumi",
                    "SFXPawn_Morinth",
                }
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Garrus_02.pcc",
                AssetPath = "BIOG_TUR_HED_PROGarrus_ALT_R.Garrus.TUR_HED_Garrus_ALT_MDL",
                NameSuffix = "rus",
                IsSquadmateHead = true,
                PostPortingFixupDelegate = GarrusHeadZFixDLC,
                DisallowedPawns = new []
                {
                    "SFXPawn_Jack",
                    "SFXPawn_Miranda",
                    "SFXPawn_Samara",
                    "SFXPawn_Kasumi",
                    "SFXPawn_Morinth",
                },
                IsUsable = false
            },


            new HeadAssetSource()
            {
                PackageFile = "BioH_Leading_00.pcc",
                AssetPath = "BIOG_HMM_HED_PROMorph.Jacob.HMM_HED_PROJacob_MDL",
                NameSuffix = "cob",
                IsSquadmateHead = true,
                DisallowedPawns = new[]
                {
                    "SFXPawn_Jacob" // Do not allow putting same head onto jacob (since he uses facemorph this will prevent this check from even being reached)
                }
            },
            new HeadAssetSource()
            {
                PackageFile = "BioH_Mystic_00.pcc",
                AssetPath = "BIOG_ASA_HED_PROMorph_R.Samara.ASA_HED_PROSamara_MDL",
                NameSuffix = "ara",
                IsSquadmateHead = true,
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
            new HeadAssetSource()
            {
                PackageFile = "BioH_Grunt_02.pcc",
                AssetPath = "BIOG_KRO_HED_PROGrunt_ALT_R.Grunt.KRO_HED_Grunt_ALT_MDL",
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
                IsCorrectedAsset = true,
                NameSuffix = "per",
            },
            new HeadAssetSource()
            {
                PackageFile = "BIOG_HMM_HED_PROMorph.pcc",
                AssetPath = "Kaiden.HMM_HED_PROKaiden_MDL",
                UseMemorySafe = true,
                PostPortingFixupDelegate = MakeKaidanNoLongerHulk,
                NameSuffix = "dan",
            },
            new HeadAssetSource()
            {
                PackageFile = "BioD_HorCr1_303AshKaidan.pcc",
                AssetPath = "BIOG_HMF_HED_PROMorph_R.PROAshley.HMF_HED_PRO_Ashley_MDL_LOD0",
                HairAssetPath = "BIOG_HMF_HIR_PRO.Ashley.HMF_HIR_PROAshley_MDL",
                UseMemorySafe = true,
                IsFemaleAsset = true,
                NameSuffix="ley"
            }
        };

        private static void GarrusHeadZFix(SquadMate bodyInfo, ExportEntry newHead)
        {
            if (bodyInfo.InternalName != "Grunt")
            {
                MERLog.Information($@"Fixing garrus head Z in {newHead.FileRef.FilePath}");
                var objBin = ObjectBinary.From<SkeletalMesh>(newHead);

                float shiftAmt = -10;
                foreach (var lod in objBin.LODModels)
                {
                    foreach (var vertex in lod.VertexBufferGPUSkin.VertexData)
                    {
                        var pos = vertex.Position;
                        pos.Z += shiftAmt;
                        vertex.Position = pos;
                    }
                }
                newHead.WriteBinary(objBin);
            }
        }

        private static void GarrusHeadZFixDLC(SquadMate bodyInfo, ExportEntry newHead)
        {
            GarrusHeadZFix(bodyInfo, newHead);

            var newHolo = newHead.FileRef.FindExport("BIOG_TUR_HED_PROGarrus_ALT_R.Garrus.Visor_Alt_Hologram");
            if (newHolo != null)
            {
                var data = newHolo.Data;
                //RandomizeRGBA(data, 0x70C, false);
                RHolograms.RandomizeRGBA(data, 0x54E, false);
                MERLog.Information($@"Randomized Garrus DLC head material {newHolo.InstancedFullPath} in {newHolo.FileRef.FilePath}");
                newHolo.Data = data;
            }
        }

        /// <summary>
        /// Fixes Z for body - like mordin and garrus who have higher heads
        /// </summary>
        /// <param name="bodyInfo"></param>
        /// <param name="newHead"></param>
        private static void BodyHeadZFix(SquadMate bodyInfo, ExportEntry newHead)
        {
            if (bodyInfo.InternalName == "Professor" || bodyInfo.InternalName == "Garrus")
            {
                MERLog.Information($@"Fixing Mordin's head Z in {newHead.FileRef.FilePath}");
                var objBin = ObjectBinary.From<SkeletalMesh>(newHead);

                float shiftAmt = 7;
                foreach (var lod in objBin.LODModels)
                {
                    foreach (var vertex in lod.VertexBufferGPUSkin.VertexData)
                    {
                        var pos = vertex.Position;
                        pos.Z += shiftAmt;
                        vertex.Position = pos;
                    }
                }
                newHead.WriteBinary(objBin);
            }
        }


        // Kaidan for some reason has green headmesh. This fixes that
        private static void MakeKaidanNoLongerHulk(SquadMate squadMate, ExportEntry kaidanMDL)
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

        private static bool CanRandomize2(ExportEntry export, out ObjectProperty skelMesh)
        {
            skelMesh = null;
            if (export.IsDefaultObject || export.ClassName != "SkeletalMeshComponent" || export.ObjectFlags.Has(UnrealFlags.EObjectFlags.DebugPostLoad)) return false;
            //if (export.UIndex == 5373)
            //    Debugger.Break();
            var fName = Path.GetFileNameWithoutExtension(export.FileRef.FilePath);
            var isHenchFile = fName != null && fName.StartsWith("BioH_") && fName != "BioH_SelectGUI";
            int henchVersion = -1;

            // Get version
            var lastIndex = isHenchFile ? fName.LastIndexOf("_") : 0;
            if (lastIndex > 0) 
            {
                int.TryParse(fName.Substring(lastIndex + 1), out henchVersion);
            }



            skelMesh = export.GetProperty<ObjectProperty>("SkeletalMesh");
            if (skelMesh == null || skelMesh.Value == 0)
            {
                var par = export.Archetype as ExportEntry;
                if (par != null)
                {
                    skelMesh = par.GetProperty<ObjectProperty>("SkeletalMesh");
                }
            }

            if (skelMesh == null || skelMesh.Value == 0) return false;


            var entry = skelMesh.ResolveToEntry(export.FileRef);
            var fpath = entry.InstancedFullPath;
            if (export.Parent != null && HeadAssetSources.Any(x => x.IsSquadmateHead && x.AssetPath == fpath)) // is this an existing squadmate head asset?
            {
                if (henchVersion >= 0)
                {
                    var sqmVersion = int.Parse(fName.Substring(lastIndex + 1));
                    int num = export.Parent.ObjectName.Instanced.LastIndexOf("_");
                    if (int.TryParse(export.Parent.ObjectName.Instanced.Substring(num + 1), out var localVersion))
                    {
                        return localVersion == sqmVersion;
                    }
                    else if (sqmVersion > 0)
                    {
                        // 0 != sqmVersion check
                        MERLog.Information($@"SQMHEAD: Not randomizing unused superclass asset {export.InstancedFullPath}");
                        return false;
                    }
                }

                return true; // It's a model
            }

            var parentObj = export.Parent as ExportEntry;
            if (parentObj != null)
            {
                var parentProps = parentObj.GetProperties();
                var headMeshProp = parentProps.GetProp<ObjectProperty>("HeadMesh");
                if (headMeshProp != null && headMeshProp.Value == export.UIndex)
                {
                    // Check for Jacob or Wilson. Not entirely sure how we can do this reliably...
                    // This seems to be coded to find embedded jacob/wilson pawns?
                    if (parentObj.IsDefaultObject && export.ObjectName.Name.Contains("HeadMesh") && (parentObj.Class.InheritsFrom("SFXPawn_Jacob") || parentObj.Class.InheritsFrom("SFXPawn_Wilson")))
                    {
                        return true;
                    }

                    // Specific finder for jacob biopawns
                    // Look for biopawn instances (Like in 110ROMJacob) with tag containing 'Leading'
                    if (parentObj.ClassName == "BioPawn"
                        && parentObj.GetProperty<ObjectProperty>("ActorType") is ObjectProperty actorTypeObj
                        && actorTypeObj.ResolveToEntry(export.FileRef) is IEntry actorType
                        && actorType.ObjectName.Name.Contains("leading", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            //// It must be a default object and have the correct class set for the defaults
            //// Check superclass as our objectname is wrong
            //if (export.IsDefaultObject && export.ObjectName.Name.StartsWith("Default__SFXPawn_") && SquadmatePawnClasses.Any(x => export.Class.InheritsFrom(x.ClassName)))
            //    return true;
            return false;
        }

        public static bool RandomizeExport2(ExportEntry headMeshExp, RandomizationOption option)
        {
            if (!CanRandomize2(headMeshExp, out var skelMesh)) return false;
            var fname = Path.GetFileName(headMeshExp.FileRef.FilePath);
            Debug.WriteLine($"Can randomize SQM HED {headMeshExp.InstancedFullPath} in {fname}");
            if (skelMesh != null)
            {
                // Select a new asset.
                var existingAsset = skelMesh.ResolveToEntry(headMeshExp.FileRef);
                var newAsset = HeadAssetSources.RandomElement();
                var squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => existingAsset.ObjectName.Name.Contains(x.GetCharName(), StringComparison.InvariantCultureIgnoreCase));
                if (squadmateInfo == null)
                {
                    // Check if Wilson or Jacob BASIC
                    squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => headMeshExp.Parent.ObjectName.Name.Contains(x.GetCharName(), StringComparison.InvariantCultureIgnoreCase));
                    if (squadmateInfo == null)
                    {
                        // Check tag?
                        var parentTag = (headMeshExp.Parent as ExportEntry).GetProperty<NameProperty>("Tag");
                        if (parentTag != null && parentTag.Value.Name.StartsWith("hench_"))
                        {
                            var searchName = parentTag.Value.Name;
                            squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => searchName.Contains(x.InternalName, StringComparison.InvariantCultureIgnoreCase));
                        }
                    }
                }

                if (squadmateInfo == null)
                {
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
                headMeshExp.ObjectFlags |= UnrealFlags.EObjectFlags.DebugPostLoad; // Mark as modified so we do not re-randomize it

                // update the bone positions... dunno if this is a good idea or not
                //UpdateBonePositionsForHead(existingAsset, newMdl);
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



                if (squadmateInfo.InternalName == "Mystic")
                {
                    var filename = Path.GetFileName(headMeshExp.FileRef.FilePath);
                    if (filename.StartsWith("BioH_") && filename.Contains("Mystic"))
                    {
                        // We also need to change Morinth. It'll look weird in game but not much we can do about it
                        // We should probably ensure we make new TLK name for them
                        var strRefNode = headMeshExp.FileRef.FindExport("TheWorld.PersistentLevel.Main_Sequence.BioSeqVar_StrRefLiteral_0");
                        if (strRefNode != null)
                        {
                            var morName = $"Morin{newAsset.NameSuffix}";
                            var morTlk = TLKHandler.GetNewTLKID();
                            TLKHandler.ReplaceString(morTlk, morName);
                            strRefNode.WriteProperty(new IntProperty(morTlk, "m_srStringID"));
                        }
                    }
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
                    StripHeadMaterials(instance, owningPawn);
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
                    hairMeshExp.Archetype = null;
                    hairMeshExp.ObjectName = "HairMesh";

                    hairMeshExp.RemoveProperty("AnimTreeTemplate");
                    hairMeshExp.WriteProperty(new ObjectProperty(hairMDL.UIndex, "SkeletalMesh"));

                    // Write hair mesh prop
                    owningPawn.WriteProperty(new ObjectProperty(hairMeshExp.UIndex, owningPawn.IsDefaultObject ? "HairMesh" : "m_oHairMesh"));


                    if (instance != null && instance.ClassName.StartsWith("SFXPawn_") && newAsset.HairAssetPath != null)
                    {
                        // Add a blank hairmesh object that archetypes off the class version

                        // Clone headmesh
                        var subHeadMeshExp = instance.GetProperty<ObjectProperty>("HeadMesh").ResolveToEntry(headMeshExp.FileRef) as ExportEntry;

                        var subhairMeshExp = EntryCloner.CloneEntry(subHeadMeshExp);
                        subhairMeshExp.ObjectName = "SubHairMesh";
                        subhairMeshExp.Archetype = hairMeshExp;
                        var hairMeshExpProps = hairMeshExp.GetProperties();
                        hairMeshExpProps.RemoveAll(x => x.Name.Name != "ShadowParent" && x.Name.Name != "ParentAnimComponent");
                        subhairMeshExp.WriteProperties(hairMeshExpProps);

                        // Write hair mesh prop
                        instance.WriteProperty(new ObjectProperty(subhairMeshExp.UIndex, "m_oHairMesh"));
                    }
                }

                if (squadmateInfo.ClassName == "SFXPawn_Thane")
                {
                    // We must update his mesh to get rid of those lime windshield wipers

                    IMEPackage newMeshP;
                    var parent = headMeshExp.Parent as ExportEntry;
                    if (parent.ObjectName == "Default__SFXPawn_Thane_02")
                    {
                        // Install DLC version of mesh
                        newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(MERUtilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelidsDLC.pcc")));
                    }
                    else
                    {
                        // Install basegame version of mesh
                        newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(MERUtilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelids.pcc")));
                    }

                    var meshExp = parent.GetProperty<ObjectProperty>("Mesh").ResolveToEntry(headMeshExp.FileRef) as ExportEntry;
                    var targetMesh = (meshExp.GetProperty<ObjectProperty>("SkeletalMesh") ?? ((ExportEntry)meshExp.Archetype).GetProperty<ObjectProperty>("SkeletalMesh")).ResolveToEntry(headMeshExp.FileRef) as ExportEntry;
                    var newMDL = newMeshP.FindExport(targetMesh.InstancedFullPath);

                    // Technically this should work
                    //EntryImporter.ReplaceExportDataWithAnother(newMDL, targetMesh);
                    var relinkFailures = EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.ReplaceSingular, newMDL, targetMesh.FileRef, targetMesh, true, out _, errorOccuredCallback: x => Debugger.Break());
                    if (relinkFailures.Any())
                    {
                        MERLog.Fatal(@"FAILURE RELINKING THANE'S NO-EYELID MESH");
                        Debugger.Break();
                    }
                }

                // Post install fixup
                newAsset.PostPortingFixupDelegate?.Invoke(squadmateInfo, newMdl);

                // body fixups (checks they can run before performing)
                BodyHeadZFix(squadmateInfo, newMdl);

                return true;
            }

            return false;
        }

        private static void StripHeadMaterials(ExportEntry instance, IEntry owningPawn)
        {
            if (instance.ClassName == "SkeletalMeshComponent")
            {
                instance.RemoveProperty("Materials");
            }
            else if (instance.GetProperty<ObjectProperty>("HeadMesh")?.ResolveToEntry(instance.FileRef) is ExportEntry instHeadMesh)
            {
                instHeadMesh.RemoveProperty("Materials");
                // Strip parent materials as well
                if (instHeadMesh != null && instHeadMesh.Archetype is ExportEntry archetype)
                {
                    archetype.RemoveProperty("Materials"); // Nuke the archetype ones as well
                    instHeadMesh = archetype;
                }
            }


            //while (owningPawn is ExportEntry opExp)
            //{
            //    if (opExp.IsDefaultObject)
            //    {
            //        //Default__SFXPawn__BeepBoop
            //        opExp.GetProperty<ObjectProperty>()
            //    }


            //    owningPawn = opExp.SuperClass;
            //}
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

        //private static bool ForcedRun(ExportEntry export, bool doWorldCheck = true)
        //{
        //    var myClass = export.ClassName;
        //    var squadmateInfo = SquadmatePawnClasses.FirstOrDefault(x => export.Class.InheritsFrom(x.ClassName));
        //    if (squadmateInfo != null && export.GetProperty<ObjectProperty>("HeadMesh")?.ResolveToEntry(export.FileRef) is ExportEntry headMeshExp)
        //    {
        //        var skelMesh = headMeshExp.GetProperty<ObjectProperty>("SkeletalMesh");
        //        if (skelMesh != null)
        //        {
        //            // Select a new asset.
        //            var existingAsset = skelMesh.ResolveToEntry(export.FileRef);
        //            var newAsset = HeadAssetSources.RandomElement();
        //            while (newAsset.AssetPath == existingAsset.InstancedFullPath || !newAsset.CanApplyTo(squadmateInfo))
        //            {
        //                // Ensure change
        //                newAsset = HeadAssetSources.RandomElement();
        //            }

        //            // Port in the new asset.
        //            var sourceExp = newAsset.GetAsset();
        //            var newMdl = PackageTools.PortExportIntoPackage(export.FileRef, sourceExp, useMemorySafeImport: newAsset.UseMemorySafe, cache: HeadAssetCache);

        //            // Write the properties.
        //            skelMesh.Value = newMdl.UIndex;
        //            headMeshExp.WriteProperty(skelMesh);

        //            var newName = squadmateInfo.NamePrefix + newAsset.NameSuffix;
        //            var newTlkId = TLKHandler.GetNewTLKID();
        //            TLKHandler.ReplaceString(newTlkId, newName);
        //            export.WriteProperty(new StringRefProperty(newTlkId, "PrettyName"));
        //            if (export.GetProperty<ObjectProperty>("ActorType")?.ResolveToEntry(export.FileRef) is ExportEntry actorTypeExp)
        //            {
        //                actorTypeExp.indexValue = ThreadSafeRandom.Next(265789564); // make it memory unique. Not sure this matters if pawn is not
        //                var strRef = actorTypeExp.GetProperty<StringRefProperty>("ActorGameNameStrRef");
        //                strRef.Value = newTlkId;
        //                actorTypeExp.WriteProperty(strRef);
        //            }

        //            // Clean up the materials in the instance of the pawn.
        //            // Have to do full search cause naming system doesn't seem consistent
        //            // Only look for children of TheWorld so we can do integer check
        //            var persistentLevel = export.ClassName == "BioPawn" ? null : export.FileRef.FindExport("TheWorld.PersistentLevel");
        //            var instance = export.ClassName == "BioPawn" ? export : export.FileRef.Exports.FirstOrDefault(x => x.idxLink == persistentLevel.UIndex && x.ClassName == myClass);

        //            // Instance will be of type BioPawn or SFXPawn_XXX and will NOT be the defaults
        //            if (instance != null)
        //            {
        //                if (instance.GetProperty<ObjectProperty>("HeadMesh")?.ResolveToEntry(export.FileRef) is ExportEntry instHeadMesh)
        //                {
        //                    instHeadMesh.RemoveProperty("Materials");
        //                    // scaling breaks a lot of shit
        //                    //if (squadmateInfo.IsFemale != newAsset.IsFemaleAsset)
        //                    //{
        //                    //    // We need to size it
        //                    //    instHeadMesh.WriteProperty(new FloatProperty(newAsset.GenderSwapDrawScale, "Scale"));
        //                    //}
        //                }
        //            }

        //            // Remove morph head from the biopawn, if any, as this will corrupt the head
        //            export.RemoveProperty("MorphHead");

        //            // Add hair asset if necessary
        //            if (newAsset.HairAssetPath != null)
        //            {
        //                // Port in hair
        //                var hairMDL = PackageTools.PortExportIntoPackage(export.FileRef, newAsset.GetHairAsset(), useMemorySafeImport: newAsset.UseMemorySafe, cache: HeadAssetCache);

        //                // Clone existing mesh
        //                var hairMeshExp = EntryCloner.CloneEntry(headMeshExp);
        //                hairMeshExp.ObjectName = "HairMesh";
        //                hairMeshExp.RemoveProperty("AnimTreeTemplate");
        //                hairMeshExp.WriteProperty(new ObjectProperty(hairMDL.UIndex, "SkeletalMesh"));

        //                // Write hair mesh prop
        //                export.WriteProperty(new ObjectProperty(hairMeshExp.UIndex, "m_oHairMesh"));

        //                if (instance != null && instance.ClassName.StartsWith("SFXPawn_") && newAsset.HairAssetPath != null)
        //                {
        //                    // Add a blank hairmesh object that archetypes off the class version

        //                    // Clone headmesh
        //                    var subHeadMeshExp = instance.GetProperty<ObjectProperty>("HeadMesh").ResolveToEntry(export.FileRef) as ExportEntry;

        //                    var subhairMeshExp = EntryCloner.CloneEntry(subHeadMeshExp);
        //                    subhairMeshExp.ObjectName = "SubHairMesh";
        //                    subhairMeshExp.Archetype = hairMeshExp;
        //                    var hairMeshExpProps = hairMeshExp.GetProperties();
        //                    hairMeshExpProps.RemoveAll(x => x.Name.Name != "ShadowParent" && x.Name.Name != "ParentAnimComponent");
        //                    subhairMeshExp.WriteProperties(hairMeshExpProps);

        //                    // Write hair mesh prop
        //                    instance.WriteProperty(new ObjectProperty(hairMeshExp.UIndex, "m_oHairMesh"));
        //                }
        //            }

        //            if (squadmateInfo.ClassName == "SFXPawn_Thane")
        //            {
        //                // We must update his mesh to get rid of those lime windshield wipers

        //                IMEPackage newMeshP;
        //                if (export.ObjectName == "Default__SFXPawn_Thane_02")
        //                {
        //                    // Install DLC version of mesh
        //                    newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelidsDLC.pcc")));
        //                }
        //                else
        //                {
        //                    // Install basegame version of mesh
        //                    newMeshP = MEPackageHandler.OpenMEPackageFromStream(new MemoryStream(Utilities.GetEmbeddedStaticFilesBinaryFile("correctedmeshes.body.ThaneBodyNoEyelids.pcc")));
        //                }

        //                var meshExp = export.GetProperty<ObjectProperty>("Mesh").ResolveToEntry(export.FileRef) as ExportEntry;
        //                var meshVal = meshExp.GetProperty<ObjectProperty>("SkeletalMesh").ResolveToEntry(export.FileRef) as ExportEntry;
        //                var newMDL = newMeshP.FindExport(meshVal.InstancedFullPath);

        //                // Technically this should work
        //                EntryImporter.ReplaceExportDataWithAnother(newMDL, meshVal);

        //            }

        //            // Post install fixup
        //            newAsset.PostPortingFixupDelegate?.Invoke(newMdl);
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        public static bool FilePrerun(IMEPackage package, RandomizationOption arg2)
        {
            var packagename = Path.GetFileName(package.FilePath);
            if (packagename.Equals("BioP_ProCer.pcc", StringComparison.InvariantCultureIgnoreCase)) // Miranda and Jacob pawns at end of stage
            {
                // Remove these pawns from BioP memory
                // Might be able to make it lose the references
                MERLog.Information("Fixing BioP_ProCer Miranda/Jacob");
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

                var bwi = package.FindExport("TheWorld.PersistentLevel.BioWorldInfo_0");
                var refObjs = bwi.GetProperty<ArrayProperty<ObjectProperty>>("m_AutoPersistentObjects");
                refObjs.Remove(new ObjectProperty(1562));
                refObjs.Remove(new ObjectProperty(1555));
                bwi.WriteProperty(refObjs);
            }

            else if (packagename.Equals("BioD_ProCer_300ShuttleBay.pcc", StringComparison.InvariantCultureIgnoreCase)) // Miranda shoots wilson
            {
                MERLog.Information("Fixing end of ProCer Miranda");

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
#if DEBUG
                mirandaIdx = 1; //debug
#endif
                var mirandaSourcePackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_Vixen_0{mirandaIdx}.pcc"));
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
                MERLog.Information("Fixing end of ProCer Jacob/Miranda");

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
                var mirandaSourcePackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile($"BioH_Vixen_0{mirandaIdx}.pcc"));
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

                // Port in SFXPawn_Miranda, SFXPawn_Jacob. Maybe use their alternate outfits?
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
                //MERFileSystem.SavePackage(package);
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
                if (!(oldProp is ObjectProperty) && (matchingProp == null || oldProp.Name == "Location" || oldProp.Name == "Rotation" || oldProp.Name == "Tag"))
                {
                    props.AddOrReplaceProp(oldProp);
                }
            }
            newPawnInstance.WriteProperties(props);
        }
    }
}