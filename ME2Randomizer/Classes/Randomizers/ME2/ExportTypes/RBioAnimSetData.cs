using ME3ExplorerCore.Packages;
using ME3ExplorerCore.Unreal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ME2Randomizer.Classes.Randomizers.ME2.ExportTypes
{
    class RBioAnimSetData
    {
        private static string[] boneGroupNamesToRandomize = new[]
{
            "ankle",
            "wrist",
            "finger",
            "elbow",
            "toe",
            "sneer"
        };

        private static bool CanRandomize(ExportEntry export) => !export.IsDefaultObject && export.ClassName == @"BioAnimSetData";
        public static bool RandomizeExport(ExportEntry export, Random random, RandomizationOption option)
        {
            if (!CanRandomize(export)) return false;
            //build groups
            var actualList = export.GetProperty<ArrayProperty<NameProperty>>("TrackBoneNames");

            Dictionary<string, List<string>> randomizationGroups = new Dictionary<string, List<string>>();
            foreach (var key in boneGroupNamesToRandomize)
            {
                randomizationGroups[key] = actualList.Where(x => x.Value.Name.Contains(key, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.Value.Name).ToList();
                randomizationGroups[key].Shuffle(random);
            }

            foreach (var prop in actualList)
            {
                var propname = prop.Value.Name;
                var randoKey = randomizationGroups.Keys.FirstOrDefault(x => propname.Contains(x, StringComparison.InvariantCultureIgnoreCase));
                //Debug.WriteLine(propname);
                if (randoKey != null)
                {
                    var randoKeyList = randomizationGroups[randoKey];
                    prop.Value = randoKeyList[0];
                    randoKeyList.RemoveAt(0);
                }
            }

            export.WriteProperty(actualList);
            return true;
        }
    }
}
