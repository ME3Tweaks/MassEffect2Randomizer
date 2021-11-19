using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Levels
{
    class REnd
    {

        private void RandomizeEnding(Random random)
        {
            Log.Information("Randomizing ending");
            mainWindow.ProgressBarIndeterminate = true;

            mainWindow.CurrentOperationText = "Randomizing ending";
            ME1Package backdropFile = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\CRD\BIOA_CRD00.SFM"));
            var paragonItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.endingbackdrops.paragon")).ToList();
            var renegadeItems = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(x => x.StartsWith("MassEffectRandomizer.staticfiles.exportreplacements.endingbackdrops.renegade")).ToList();
            paragonItems.Shuffle(random);
            renegadeItems.Shuffle(random);
            var paragonTexture = backdropFile.getUExport(1067);
            var renegadeConversationTexture = backdropFile.getUExport(1068); //For backdrop of anderson/udina conversation
            var renegadeTexture = backdropFile.getUExport(1069);

            var paragonItem = paragonItems[0];
            var renegadeItem = renegadeItems[0];

            paragonTexture.setBinaryData(Utilities.GetEmbeddedStaticFilesBinaryFile(paragonItem, true));
            renegadeTexture.setBinaryData(Utilities.GetEmbeddedStaticFilesBinaryFile(renegadeItem, true));

            Log.Information("Backdrop randomizer, setting paragon backdrop to " + Path.GetFileName(paragonItem));
            Log.Information("Backdrop randomizer, setting renegade backdrop to " + Path.GetFileName(renegadeItem));

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

            backdropFile.save();
            ModifiedFiles[backdropFile.FileName] = backdropFile.FileName;


            ME1Package finalCutsceneFile = new ME1Package(Utilities.GetGameFile(@"BioGame\CookedPC\Maps\CRD\DSG\BIOA_CRD00_00_DSG.SFM"));
            var weaponTypes = (new[] { "STW_AssaultRifle", "STW_Pistol", "STW_SniperRifle", "STW_ShotGun" }).ToList();
            weaponTypes.Shuffle(random);
            Log.Information("Ending randomizer, setting renegade weapon to " + weaponTypes[0]);
            var setWeapon = finalCutsceneFile.getUExport(979);
            props = setWeapon.GetProperties();
            var eWeapon = props.GetProp<EnumProperty>("eWeapon");
            eWeapon.Value = weaponTypes[0];
            setWeapon.WriteProperties(props);

            //Move Executor to Renegade
            var executor = finalCutsceneFile.getUExport(923);
            Utilities.SetLocation(executor, -25365, 20947, 430);
            Utilities.SetRotation(executor, -30);

            //Move SubShep to Paragon
            var subShep = finalCutsceneFile.getUExport(922);
            Utilities.SetLocation(subShep, -21271, 8367, -2347);
            Utilities.SetRotation(subShep, -15);

            //Ensure the disable light environment has references to our new pawns so they are property lit.
            var toggleLightEnviro = finalCutsceneFile.getUExport(985);
            props = toggleLightEnviro.GetProperties();
            var linkedVars = props.GetProp<ArrayProperty<StructProperty>>("VariableLinks")[0].GetProp<ArrayProperty<ObjectProperty>>("LinkedVariables"); //Shared True
            if (linkedVars.Count == 1)
            {
                linkedVars.Add(new ObjectProperty(2621)); //executor
                linkedVars.Add(new ObjectProperty(2620)); //subshep
            }


            toggleLightEnviro.WriteProperties(props);

            finalCutsceneFile.save();
            ModifiedFiles[finalCutsceneFile.FileName] = finalCutsceneFile.FileName;
        }
    }
}
