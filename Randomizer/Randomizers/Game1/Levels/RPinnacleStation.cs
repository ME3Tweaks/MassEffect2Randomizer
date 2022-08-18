using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Serilog;

namespace Randomizer.Randomizers.Levels
{
    class RPinnacleStation
    {
        private void RandomizePinnacleScoreboard(Random random)
        {

        }

        public static bool PerformRandomization(GameTarget target, RandomizationOption option)
        {
            RandomizePinnalceScoreoard(target, option);
            return true;
        }

        private static void RandomizePinnalceScoreoard(GameTarget target, RandomizationOption option)
        {

            // Todo: Try to dynamically make image instead.

            var pinnacleTexturesPath = MERFileSystem.GetPackageFile(target, "bioa_prc2_ccsim05_dsg_LOC_int");
            if (pinnacleTexturesPath == null)
                return;
            
            MERLog.Information("Randomizing Pinnacle Station scoreboard");
            var pinnacleTextures = MERFileSystem.OpenMEPackage(pinnacleTexturesPath);
            var resourceItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.pinnaclestationscoreboard")).ToList();
            resourceItems.Shuffle();

            for (int i = 104; i < 118; i++)
            {
                var newBinaryResource = resourceItems[0];
                resourceItems.RemoveAt(0);
                var textureExport = pinnacleTextures.GetUExport(i);
                var props = textureExport.GetProperties();
                props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(newBinaryResource), "SourceFilePath"));
                props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
                textureExport.WriteProperties(props);
                var bytes = MEREmbedded.GetEmbeddedAsset("Binary",newBinaryResource).ToBytes();
                textureExport.WriteBinary(bytes);
            }
            MERFileSystem.SavePackage(pinnacleTextures);
        }
    }
}
