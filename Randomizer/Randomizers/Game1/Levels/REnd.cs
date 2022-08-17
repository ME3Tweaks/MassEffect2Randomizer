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
using Randomizer.Randomizers.Utility;
using Serilog;

namespace Randomizer.Randomizers.Levels
{
    class REnd
    {

        private void RandomizeEnding(GameTarget target, RandomizationOption option)
        {
            MERLog.Information("Randomizing ending");
            option.ProgressIndeterminate = true;
            option.CurrentOperation = "Randomizing ending";
            var backdropFile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\CRD\BIOA_CRD00"));
            var paragonItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.endingbackdrops.paragon")).ToList();
            var renegadeItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.endingbackdrops.renegade")).ToList();
            paragonItems.Shuffle();
            renegadeItems.Shuffle();
            var paragonTexture = backdropFile.GetUExport(1067);
            var renegadeConversationTexture = backdropFile.GetUExport(1068); //For backdrop of anderson/udina conversation
            var renegadeTexture = backdropFile.GetUExport(1069);

            var paragonItem = paragonItems[0];
            var renegadeItem = renegadeItems[0];

            paragonTexture.WriteBinary(MEREmbedded.GetEmbeddedAsset("Binary", paragonItem).ToBytes());
            renegadeTexture.WriteBinary(MEREmbedded.GetEmbeddedAsset("Binary", renegadeItem).ToBytes());

            MERLog.Information("Backdrop randomizer, setting paragon backdrop to " + Path.GetFileName(paragonItem));
            MERLog.Information("Backdrop randomizer, setting renegade backdrop to " + Path.GetFileName(renegadeItem));

            var props = paragonTexture.GetProperties();
            props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(paragonItem), "SourceFilePath"));
            props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
            paragonTexture.WriteProperties(props);

            props = renegadeTexture.GetProperties();
            props.AddOrReplaceProp(new StrProperty("MASS EFFECT RANDOMIZER - " + Path.GetFileName(renegadeItem), "SourceFilePath"));
            props.AddOrReplaceProp(new StrProperty(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), "SourceFileTimestamp"));
            renegadeTexture.WriteProperties(props);

            int texturePackageNameIndex = backdropFile.findName("BIOA_END20_T");
            if (texturePackageNameIndex != -1)
            {
                backdropFile.replaceName(texturePackageNameIndex, "BIOA_END20_MER_T");
            }

            MERFileSystem.SavePackage(backdropFile);

            var finalCutsceneFile = MERFileSystem.OpenMEPackage(MERFileSystem.GetPackageFile(target, @"BioGame\CookedPC\Maps\CRD\DSG\BIOA_CRD00_00_DSG"));
            var weaponTypes = (new[] { "STW_AssaultRifle", "STW_Pistol", "STW_SniperRifle", "STW_ShotGun" }).ToList();
            weaponTypes.Shuffle();
            MERLog.Information("Ending randomizer, setting renegade weapon to " + weaponTypes[0]);
            var setWeapon = finalCutsceneFile.GetUExport(979);
            props = setWeapon.GetProperties();
            var eWeapon = props.GetProp<EnumProperty>("eWeapon");
            eWeapon.Value = weaponTypes[0];
            setWeapon.WriteProperties(props);

            //Move Executor to Renegade
            var executor = finalCutsceneFile.GetUExport(923);
            LocationTools.SetLocation(executor, -25365, 20947, 430);
            LocationTools.SetRotation(executor, -30);

            //Move SubShep to Paragon
            var subShep = finalCutsceneFile.GetUExport(922);
            LocationTools.SetLocation(subShep, -21271, 8367, -2347);
            LocationTools.SetRotation(subShep, -15);

            //Ensure the disable light environment has references to our new pawns so they are property lit.
            var toggleLightEnviro = finalCutsceneFile.GetUExport(985);
            props = toggleLightEnviro.GetProperties();
            var linkedVars = props.GetProp<ArrayProperty<StructProperty>>("VariableLinks")[0].GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables"); //Shared True
            if (linkedVars.Count == 1)
            {
                linkedVars.Add(new ObjectProperty(2621)); //executor
                linkedVars.Add(new ObjectProperty(2620)); //subshep
            }


            toggleLightEnviro.WriteProperties(props);

            MERFileSystem.SavePackage(finalCutsceneFile);
        }
    }
}
