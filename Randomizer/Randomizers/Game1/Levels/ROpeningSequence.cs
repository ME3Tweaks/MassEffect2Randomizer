using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Serilog;

namespace Randomizer.Randomizers.Game1.Levels
{
    class ROpeningSequence
    {
        private static void ShepFacesCamera(GameTarget target, RandomizationOption option)
        {
//            Log.Information("Randomizing open cutscene");
//            mainWindow.ProgressBarIndeterminate = true;
//            mainWindow.CurrentOperationText = "Randomizing opening cutscene";
//            RandomizeOpeningCrawl(random, Tlks);
//            //RandomizeOpeningSequence(random); //this was just sun tint. Part of sun tint randomizer 
//            //Log.Information("Applying fly-into-earth interp modification");
//            ME1Package p = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\NOR\LAY\BIOA_NOR10_13_LAY.SFM"));
//            //p.getUExport(220).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.InterpMoveTrack_EarthCardIntro_220.bin");
//            Log.Information("Randomizing earth texture");

//            var earthItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.earthbackdrops")).ToList();
//            earthItems.Shuffle(random);
//            var newAsset = earthItems[0];
//            var earthTexture = p.getUExport(508);
//            earthTexture.setBinaryData(Utilities.GetEmbeddedStaticFilesBinaryFile(newAsset, true));
//            var props = earthTexture.GetProperties();
//            props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(newAsset), "SourceFilePath"));
//            props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
//            earthTexture.WriteProperties(props);
//            earthTexture.idxObjectName = p.FindNameOrAdd("earthMER"); //ensure unique name

//            //DEBUG ONLY: NO INTRO MUSIC

//#if DEBUG
//            var director = p.getUExport(206);
//            var tracks = director.GetProperty<ArrayProperty<ObjectProperty>>("InterpTracks");
//            var o = tracks.FirstOrDefault(x => x.Value == 227); //ME Music
//            tracks.Remove(o);
//            director.WriteProperty(tracks);
//#endif

//            Log.Information("Applying shepard-faces-camera modification");
//            p.getUExport(219).Data = Utilities.GetEmbeddedStaticFilesBinaryFile("exportreplacements.InterpMoveTrack_PlayerFaceCameraIntro_219.bin");
//            p.save();
        }

        private static void RandomizeOpeningSequence(GameTarget target, RandomizationOption option)
        {
            Log.Information($"Randomizing opening cutscene");

            ME1Package p = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\PRO\CIN\BIOA_GLO00_A_Opening_Flyby_CIN.SFM"));
            foreach (var ex in p.Exports)
            {
                if (ex.ClassName == "BioSunFlareComponent" || ex.ClassName == "BioSunFlareStreakComponent")
                {
                    var tint = ex.GetProperty<StructProperty>("FlareTint");
                    if (tint != null)
                    {
                        RandomizeTint(random, tint, false);
                        ex.WriteProperty(tint);
                    }
                }
                else if (ex.ClassName == "BioSunActor")
                {
                    var tint = ex.GetProperty<StructProperty>("SunTint");
                    if (tint != null)
                    {
                        RandomizeTint(random, tint, false);
                        ex.WriteProperty(tint);
                    }
                }
            }

            p.save();
        }

        public static bool PerformRandomization(GameTarget arg1, RandomizationOption arg2)
        {
            RandomizeOpeningSequence(arg1, arg2);
            ShepFacesCamera(arg1, arg2);
            return true;
        }
    }
}
