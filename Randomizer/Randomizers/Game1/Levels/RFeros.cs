using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.TLK.ME1;
using LegendaryExplorerCore.Unreal;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Handlers;
using Serilog;

namespace Randomizer.Randomizers.Levels
{
    class RFeros
    {
        private static void RandomizeFerosColonistBattle(GameTarget target, RandomizationOption option)
        {
            option.CurrentOperation = "Randoming Feros";
            string fileContents = MERUtilities.GetEmbeddedTextAsset("colonistnames.xml");
            XElement rootElement = XElement.Parse(fileContents);
            var colonistnames = rootElement.Elements("colonistname").Select(x => x.Value).ToList();

            var colonyBattlePackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\WAR\DSG\BIOA_WAR20_03c_DSG.SFM"));
            var skywayBattlePackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\WAR\DSG\BIOA_WAR40_11_DSG.SFM"));
            var towerBattlePackage = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\WAR\DSG\BIOA_WAR20_04b_DSG.SFM"));

            var battlePackages = new[] { colonyBattlePackage, skywayBattlePackage, towerBattlePackage };

            foreach (var battlePackage in battlePackages)
            {
                var bioChallengeScaledPawns = battlePackage.Exports.Where(x => x.ClassName == "BioPawnChallengeScaledType" && x.ObjectName != "MIN_ZombieThorian" && x.ObjectName != "ELT_GethAssaultDrone").ToList();

                foreach (var export in bioChallengeScaledPawns)
                {
                    var strRef = export.GetProperty<StringRefProperty>("ActorGameNameStrRef");
                    var newStrRef = TLKBuilder.FindIdbyValue(colonistnames[0], battlePackage);
                    if (newStrRef == -1)
                    {
                        newStrRef = TLKBuilder.GetNewTLKID();
                    }
                    MERLog.Information($"Assigning Feros Colonist name {export.UIndex} => {colonistnames[0]}");
                    strRef.Value = newStrRef;
                    TLKBuilder.ReplaceString(newStrRef, colonistnames[0]); // Might need done in local TLK
                    colonistnames.RemoveAt(0);
                    export.WriteProperty(strRef);
                }
            }

            //Make random amount of thorian zombies attack at the same time
            var maxZombs = skywayBattlePackage.GetUExport(5748);
            maxZombs.WriteProperty(new IntProperty(ThreadSafeRandom.Next(3, 11), "IntValue"));

            var getNewLoopDelay = skywayBattlePackage.GetUExport(1103);
            getNewLoopDelay.WriteProperty(new FloatProperty(ThreadSafeRandom.NextFloat(0.1, 2), "Duration"));

            var riseFromFeignFinishDelay = skywayBattlePackage.GetUExport(1115);
            riseFromFeignFinishDelay.WriteProperty(new FloatProperty(ThreadSafeRandom.NextFloat(0, .7), "Duration"));

            //Randomly disable squadmates from not targeting enemies in Zhu's Hope and Tower
            var saveTheColonistPMCheckExports = new ExportEntry[] { colonyBattlePackage.GetUExport(1434), colonyBattlePackage.GetUExport(1437), colonyBattlePackage.GetUExport(1440), towerBattlePackage.GetUExport(576) };
            foreach (var saveColonist in saveTheColonistPMCheckExports)
            {
                if (ThreadSafeRandom.Next(8) == 0)
                {
                    // 1 in 6 chance your squadmates don't listen to your command
                    var props = saveColonist.GetProperties();
                    props.GetProp<ArrayProperty<StructProperty>>("OutputLinks")[0].GetProp<ArrayProperty<StructProperty>>("Links").Clear();
                    saveColonist.WriteProperties(props);
                }
            }

            foreach (var package in battlePackages)
            {
                MERFileSystem.SavePackage(package);
            }
        }

        public static bool PerformRandomization(GameTarget arg1, RandomizationOption arg2)
        {
            RandomizeFerosColonistBattle(arg1, arg2);
            return true;
        }
    }
}
