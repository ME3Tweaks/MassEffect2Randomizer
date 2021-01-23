using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ME2Randomizer.Classes.Randomizers.Utility;
using ME2Randomizer.Classes;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using ME3ExplorerCore.Unreal.BinaryConverters;

namespace ME2Randomizer.Classes.Randomizers.ME2.Levels
{
    public static class Citadel
    {
        private static int CommanderShepEndorsementTLKId = 253684;

        // Endorsement line is 2.1ish seconds long.
        // BOTH LISTS MUST BE THE SAME LENGTH AND HAVE IDENTICAL TLK STRS!
        private static List<(string packageName, int uindex)> EndorsementCandidatesFemale = new List<(string packageName, int uindex)>()
        {
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3408), // Sorry. Don't remember, don't care.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3403), // I knew this was a mistake
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3421), // People die. I don't have time for this crap.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3503), // Geth, pirates, mercenary scum

            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 796), // Cheap touristy crap
            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 798), // HEY EVERYONE THIS STORE DISCRIMINATES AGAINST THE POOR

            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", 2693), // Are you naturally this bitchy or is it just me
        };

        private static List<(string packageName, int uindex)> EndorsementCandidatesMale = new List<(string packageName, int uindex)>()
        {
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3432), // Sorry. Don't remember, don't care.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3427), // I knew this was a mistake
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3445), // People die. I don't have time for this crap.
            ("BioD_CitHub_300UpperWing_LOC_INT.pcc", 3523), // Geth, pirates, mercenary scum

            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 811), // Cheap touristy crap
            ("BioD_CitHub_240Vendors_LOC_INT.pcc", 813), // HEY EVERYONE THIS STORE DISCRIMINATES AGAINST THE POOR

            ("BioD_Procer_350BriefRoom_LOC_INT.pcc", 2710), // Are you naturally this bitchy or is it just me

        };



        private static void RandomizeEndorsements()
        {
            // You start on Floor 2

            // Floor 2
            var cache = new PackageCache();
            RandomizeEndorsementLine(@"BioD_CitHub_240Vendors_LOC_INT.pcc", 808, 793, 26, 7, cache); //sirta, i think?
            RandomizeEndorsementLine(@"BioD_CitHub_300UpperWing_LOC_INT.pcc", 3539, 3514, 105, 18, cache); // gun turian
            RandomizeEndorsementLine(@"BioD_CitHub_420LowerSouth_LOC_INT.pcc", 1979, 1963, 43, 8, cache); //biotic
            RandomizeEndorsementLine(@"BioD_CitHub_420LowerSouth_LOC_INT.pcc", 2038, 2022, 44, 11, cache); //omni

        }

        private static void RandomizeEndorsementLine(string packageName, int maleUIndex, int femaleUIndex, int conversationUIndex, int replyIdx, PackageCache cache)
        {
            var package = cache.GetCachedPackage(packageName);

            var conversation = package.GetUExport(conversationUIndex);
            var replies = conversation.GetProperty<ArrayProperty<StructProperty>>("m_ReplyList");

            var endorsementLineMale = package.GetUExport(maleUIndex);
            var endorsementLineFemale = package.GetUExport(femaleUIndex);

            var replacementIndex = EndorsementCandidatesMale.RandomIndex();
            var maleReplacement = EndorsementCandidatesMale[replacementIndex];
            var femaleReplacement = EndorsementCandidatesFemale[replacementIndex];

            // Male
            var sourcePackage = cache.GetCachedPackage(maleReplacement.packageName);
            var sourceExport = sourcePackage.GetUExport(maleReplacement.uindex);
            WwiseTools.RepointWwiseStream(sourceExport, endorsementLineMale);

            // Female
            sourcePackage = cache.GetCachedPackage(femaleReplacement.packageName);
            sourceExport = sourcePackage.GetUExport(femaleReplacement.uindex);
            WwiseTools.RepointWwiseStream(sourceExport, endorsementLineFemale);

            // Update the TLK reference. Not sure how this works with FaceFX honestly (or if it does at all...)

            var newTlkId = extractTLKIdFromExportName(sourceExport);
            if (newTlkId > 0)
            {
                // Conversations srTExt seems to be used to tell what line to play. Not sure how.
                // We can't change this or it will break the audio


                var replyStruct = replies[replyIdx];
                var uiStr = replyStruct.Properties.GetProp<StringRefProperty>("srText");
                var oldTlkId = uiStr.Value;
                //uiStr.Value = newTlkId;
                //conversation.WriteProperty(replies);

                // We need to also update the FaceFX IDs to point to this new line, I guess?
                //UpdateFaceFXIDs(conversation, oldTlkId.ToString(), newTlkId.ToString());

                // Just overwrite the TLK string instead
                TLKHandler.ReplaceStringByRepoint(oldTlkId, newTlkId);
            }

            MERFileSystem.SavePackage(package);

        }

        // Works, but doesn't actually due to how FaceFX looks crap up

        //private static void UpdateFaceFXIDs(ExportEntry conversation, string oldTlkId, string newTlkId)
        //{
        //    var mFaceSets = conversation.GetProperty<ArrayProperty<ObjectProperty>>("m_aMaleFaceSets");
        //    var fFaceSets = conversation.GetProperty<ArrayProperty<ObjectProperty>>("m_aFemaleFaceSets");

        //    var allFaceSets = mFaceSets.Select(x => x.ResolveToEntry(conversation.FileRef) as ExportEntry).ToList();
        //    allFaceSets.AddRange(fFaceSets.Select(x => x.ResolveToEntry(conversation.FileRef) as ExportEntry));

        //    foreach (var fs in allFaceSets)
        //    {
        //        UpdateSingleFaceFXLine(fs, oldTlkId, newTlkId); //They're strings in the code
        //    }
        //}

        //private static void UpdateSingleFaceFXLine(ExportEntry animSet, string oldId, string newTlkId)
        //{
        //    var facefxas = ObjectBinary.From<FaceFXAnimSet>(animSet);
        //    foreach (var fxl in facefxas.Lines)
        //    {
        //        if (fxl.ID == oldId)
        //        {
        //            // No idea if this matters at all...
        //            fxl.ID = newTlkId;
        //        }
        //    }
        //}


        private static int extractTLKIdFromExportName(ExportEntry export)
        {
            //parse out tlk id?
            var splits = export.ObjectName.Name.Split('_', ',');
            for (int i = splits.Length - 1; i > 0; i--)
            {
                //backwards is faster
                if (int.TryParse(splits[i], out var parsed))
                {
                    return parsed;
                }
            }

            return -1;
        }

        internal static bool PerformRandomization(RandomizationOption notUsed)
        {
            RandomizeEndorsements();
            return true;
        }
    }
}