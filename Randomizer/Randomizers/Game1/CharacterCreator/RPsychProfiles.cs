using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using Randomizer.MER;

namespace Randomizer.Randomizers.Game1.CharacterCreator
{
    class RPsychProfiles
    {
        private void RandomizeCharacterCreatorSingular(Random random, List<ME1TalkFile> Tlks)
        {
            //non-2da character creator changes.

            //Randomize look at targets
            ME1Package biog_uiworld = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\BIOG_UIWorld.sfm"));
            var bioInerts = biog_uiworld.Exports.Where(x => x.ClassName == "BioInert").ToList();
            foreach (ExportEntry ex in bioInerts)
            {
                RandomizeLocation(ex, random);
            }

            //Randomize face-zoom in
            //var zoomInOnFaceInterp = biog_uiworld.getUExport(385);
            //var eulerTrack = zoomInOnFaceInterp.GetProperty<StructProperty>("EulerTrack");
            //var points = eulerTrack?.GetProp<ArrayProperty<StructProperty>>("Points");
            //if (points != null)
            //{
            //    var s = points[2]; //end point
            //    var outVal = s.GetProp<StructProperty>("OutVal");
            //    if (outVal != null)
            //    {
            //        FloatProperty x = outVal.GetProp<FloatProperty>("X");
            //        //FloatProperty y = outVal.GetProp<FloatProperty>("Y");
            //        //FloatProperty z = outVal.GetProp<FloatProperty>("Z");
            //        x.Value = ThreadSafeRandom.NextFloat(0, 360);
            //        //y.Value = y.Value * ThreadSafeRandom.NextFloat(1 - amount * 3, 1 + amount * 3);
            //        //z.Value = z.Value * ThreadSafeRandom.NextFloat(1 - amount * 3, 1 + amount * 3);
            //    }
            //}

            //zoomInOnFaceInterp.WriteProperty(eulerTrack);
            biog_uiworld.save();

            //Psych Profiles
            string fileContents = Utilities.GetEmbeddedStaticFilesTextFile("psychprofiles.xml");

            XElement rootElement = XElement.Parse(fileContents);
            var childhoods = rootElement.Descendants("childhood").Where(x => x.Value != "").Select(x => (x.Attribute("name").Value, string.Join("\n", x.Value.Split('\n').Select(s => s.Trim())))).ToList();
            var reputations = rootElement.Descendants("reputation").Where(x => x.Value != "").Select(x => (x.Attribute("name").Value, string.Join("\n", x.Value.Split('\n').Select(s => s.Trim())))).ToList();

            childhoods.Shuffle();
            reputations.Shuffle();

            var backgroundTlkPairs = new List<(int nameId, int descriptionId)>();
            backgroundTlkPairs.Add((45477, 34931)); //Spacer
            backgroundTlkPairs.Add((45508, 34940)); //Earthborn
            backgroundTlkPairs.Add((45478, 34971)); //Colonist
            for (int i = 0; i < 3; i++)
            {
                foreach (var tlk in Tlks)
                {
                    tlk.ReplaceString(backgroundTlkPairs[i].nameId, childhoods[i].Item1);
                    tlk.ReplaceString(backgroundTlkPairs[i].descriptionId, childhoods[i].Item2);
                }
            }

            backgroundTlkPairs.Clear();
            backgroundTlkPairs.Add((45482, 34934)); //Sole Survivor
            backgroundTlkPairs.Add((45483, 34936)); //War Hero
            backgroundTlkPairs.Add((45484, 34938)); //Ruthless
            for (int i = 0; i < 3; i++)
            {
                foreach (var tlk in Tlks)
                {
                    tlk.ReplaceString(backgroundTlkPairs[i].nameId, reputations[i].Item1);
                    tlk.ReplaceString(backgroundTlkPairs[i].descriptionId, reputations[i].Item2);
                }
            }
        }
    }
}
