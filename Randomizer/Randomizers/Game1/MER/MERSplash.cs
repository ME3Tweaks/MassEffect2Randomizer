using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Game1.MER
{
    class MERSplash
    {
        public void AddMERSplash(Random random)
        {
            ME1Package entrymenu = new ME1Package(Utilities.GetEntryMenuFile());

            //Connect attract to BWLogo
            var attractMovie = entrymenu.getUExport(729);
            var props = attractMovie.GetProperties();
            var movieName = props.GetProp<StrProperty>("m_sMovieName");
            movieName.Value = "merintro";
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[1].GetProp<ArrayProperty<StructProperty>>("Links")[0].GetProp<ObjectProperty>("LinkedOp").Value = 732; //Bioware logo
            attractMovie.WriteProperties(props);

            //Rewrite ShowSplash to BWLogo to point to merintro instead
            var showSplash = entrymenu.getUExport(736);
            props = showSplash.GetProperties();
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links")[1].GetProp<ObjectProperty>("LinkedOp").Value = 729; //attractmovie logo
            showSplash.WriteProperties(props);

            //Visual only (for debugging): Remove connection to 

            //Update inputs to point to merintro comparebool
            var guiinput = entrymenu.getUExport(738);
            props = guiinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                outlink.GetProp<ArrayProperty<StructProperty>>("Links")[0].GetProp<ObjectProperty>("LinkedOp").Value = 2936; //Comparebool
            }

            guiinput.WriteProperties(props);

            var playerinput = entrymenu.getUExport(739);
            props = playerinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                var links = outlink.GetProp<ArrayProperty<StructProperty>>("Links");
                foreach (var link in links)
                {
                    link.GetProp<ObjectProperty>("LinkedOp").Value = 2936; //Comparebool
                }
            }

            playerinput.WriteProperties(props);

            //Clear old unused inputs for attract
            guiinput = entrymenu.getUExport(737);
            props = guiinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                outlink.GetProp<ArrayProperty<StructProperty>>("Links").Clear();
            }

            guiinput.WriteProperties(props);

            playerinput = entrymenu.getUExport(740);
            props = playerinput.GetProperties();
            foreach (var outlink in props.GetProp<ArrayProperty<StructProperty>>("OutputLinks"))
            {
                outlink.GetProp<ArrayProperty<StructProperty>>("Links").Clear();
            }

            playerinput.WriteProperties(props);

            //Connect CompareBool outputs
            var mercomparebool = entrymenu.getUExport(2936);
            props = mercomparebool.GetProperties();
            var outlinks = props.GetProp<ArrayProperty<StructProperty>>("OutputLinks");
            //True
            var outlink1 = outlinks[0].GetProp<ArrayProperty<StructProperty>>("Links");
            StructProperty newLink = null;
            if (outlink1.Count == 0)
            {
                PropertyCollection p = new PropertyCollection();
                p.Add(new ObjectProperty(2938, "LinkedOp"));
                p.Add(new IntProperty(0, "InputLinkIdx"));
                p.Add(new NoneProperty());
                newLink = new StructProperty("SeqOpOutputInputLink", p);
                outlink1.Add(newLink);
            }
            else
            {
                newLink = outlink1[0];
            }

            newLink.GetProp<ObjectProperty>("LinkedOp").Value = 2938;

            //False
            var outlink2 = outlinks[1].GetProp<ArrayProperty<StructProperty>>("Links");
            newLink = null;
            if (outlink2.Count == 0)
            {
                PropertyCollection p = new PropertyCollection();
                p.Add(new ObjectProperty(2934, "LinkedOp"));
                p.Add(new IntProperty(0, "InputLinkIdx"));
                p.Add(new NoneProperty());
                newLink = new StructProperty("SeqOpOutputInputLink", p);
                outlink2.Add(newLink);
            }
            else
            {
                newLink = outlink2[0];
            }

            newLink.GetProp<ObjectProperty>("LinkedOp").Value = 2934;

            mercomparebool.WriteProperties(props);

            //Update output of setbool to next comparebool, point to shared true value
            var setBool = entrymenu.getUExport(2934);
            props = setBool.GetProperties();
            props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links")[0].GetProp<ObjectProperty>("LinkedOp").Value = 729; //CompareBool (step 2)
            props.GetProp<ArrayProperty<StructProperty>>("VariableLinks")[1].GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables")[0].Value = 2952; //Shared True
            setBool.WriteProperties(props);


            //Default setbool should be false, not true
            var boolValueForMERSkip = entrymenu.getUExport(2955);
            var bValue = boolValueForMERSkip.GetProperty<IntProperty>("bValue");
            bValue.Value = 0;
            boolValueForMERSkip.WriteProperty(bValue);

            //Extract MER Intro
            var merIntroDir = Path.Combine(Utilities.GetAppDataFolder(), "merintros");
            if (Directory.Exists(merIntroDir))
            {
                var merIntros = Directory.GetFiles(merIntroDir, "*.bik").ToList();
                string merToExtract = merIntros[ThreadSafeRandom.Next(merIntros.Count)];
                File.Copy(merToExtract, Utilities.GetGameFile(@"BioGame\CookedPC\Movies\merintro.bik"), true);
                entrymenu.save();
                //Add to fileindex
                var fileIndex = Utilities.GetGameFile(@"BioGame\CookedPC\FileIndex.txt");
                var filesInIndex = File.ReadAllLines(fileIndex).ToList();
                if (filesInIndex.All(x => x != @"Movies\MERIntro.bik"))
                {
                    filesInIndex.Add(@"Movies\MERIntro.bik");
                    File.WriteAllLines(fileIndex, filesInIndex);
                }
                ModifiedFiles[entrymenu.FileName] = entrymenu.FileName;
            }

        }

    }
}
