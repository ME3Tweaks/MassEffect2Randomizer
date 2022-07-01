using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using LegendaryExplorerCore.Coalesced;
using LegendaryExplorerCore.Misc;
using ME3TweaksCore.GameFilesystem;
using ME3TweaksCore.Helpers;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using Randomizer.Randomizers.Game2.Game2FileFormats;
using Randomizer.Randomizers.Handlers;
using Serilog;

namespace Randomizer.Randomizers.Game2.Misc
{
    public class Weapons
    {
        public static bool RandomizeSquadmateWeapons(GameTarget target, RandomizationOption option)
        {
            var bg = CoalescedHandler.GetIniFile("BIOGame.ini");
            var loadoutSection = bg.GetOrAddSection("SFXGame.SFXPlayerSquadLoadoutData");

            // We need to minus them out
            loadoutSection.AddEntry(new CoalesceProperty("PlayerLoadoutInfo", new CoalesceValue("CLEAR", CoalesceParseAction.RemoveProperty)));
            loadoutSection.AddEntry(new CoalesceProperty("HenchLoadoutInfo", new CoalesceValue("CLEAR", CoalesceParseAction.RemoveProperty)));

            var pawns = new[]
            {
                "SFXPawn_PlayerAdept",
                "SFXPawn_PlayerEngineer",
                "SFXPawn_PlayerSoldier",
                "SFXPawn_PlayerSentinel",
                "SFXPawn_PlayerInfiltrator",
                "SFXPawn_PlayerVanguard",
                "SFXPawn_Garrus",
                "SFXPawn_Grunt",
                "SFXPawn_Jack",
                "SFXPawn_Jacob",
                "SFXPawn_Legion",
                "SFXPawn_Miranda",
                "SFXPawn_Mordin",
                "SFXPawn_Samara",
                "SFXPawn_Tali",
                "SFXPawn_Thane",
                "SFXPawn_Wilson", //Not used afaik

                // DLC
                "SFXPawn_Zaeed", //VT
                "SFXPawn_Kasumi", //MT
                "SFXPawn_Liara", //EXP_Part01
            };

            foreach (var p in pawns)
            {
                loadoutSection.AddEntry(BuildLoadout(p));
            }

            return true;
        }

        private static string[] LoadoutNames = new[]
        {
            // Heavy weapons are player only, apparently
            "LoadoutWeapons_AssaultRifles",
            "LoadoutWeapons_HeavyPistols",
            "LoadoutWeapons_SniperRifles",
            "LoadoutWeapons_Shotguns",
            "LoadoutWeapons_AutoPistols"
        };

        private static CoalesceProperty BuildLoadout(string pawnName)
        {
            var isPlayer = pawnName.Contains("Player");
            string key = isPlayer ? "PlayerLoadoutInfo" : "HenchLoadoutInfo";

            string value = $"(ClassName = {pawnName}, WeaponClasses = (";

            // Pick guns
            var totalGuns = ThreadSafeRandom.Next(3) + 2;
            if (!isPlayer)
                totalGuns = 2;
            List<string> guns = new List<string>();
            while (guns.Count < totalGuns)
            {
                var nGun = LoadoutNames.RandomElement();
                if (!guns.Contains(nGun))
                {
                    guns.Add(nGun);
                }
            }

            // Build the list
            bool isFirst = true;
            foreach (var gun in guns)
            {
                if (isFirst)
                {
                    isFirst = false;
                }
                else
                {
                    value += ",";
                }

                value += gun;
            }

            if (isPlayer)
            {
                value += ",LoadoutWeapons_HeavyWeapons";
            }

            value += "))";
            return new CoalesceProperty(key, new CoalesceValue(value, CoalesceParseAction.AddUnique));
        }


        public static bool RandomizeWeapons(GameTarget target, RandomizationOption option)
        {
            var me2rbioweapon = CoalescedHandler.GetIniFile("BIOWeapon.ini");

            // We must manually fetch game files cause MERFS will return the ini from the dlc mod instead.
            ME2Coalesced me2basegamecoalesced = new ME2Coalesced(MERFileSystem.GetSpecificFile(target,@"BioGame\Config\PC\Cooked\Coalesced.ini"));

            MERLog.Information("Randomizing basegame weapon ini");
            var bioweapon = me2basegamecoalesced.Inis.FirstOrDefault(x => Path.GetFileName(x.Key) == "BIOWeapon.ini").Value;
            RandomizeWeaponIni(bioweapon, me2rbioweapon);

            var weaponInis = Directory.GetFiles(M3Directories.GetDLCPath(target), "BIOWeapon.ini", SearchOption.AllDirectories).ToList();
            foreach (var wi in weaponInis)
            {
                if (wi.Contains($"DLC_MOD_{target.Game}Randomizer"))
                    continue; // Skip randomizer folders
                MERLog.Information($@"Randomizing weapon ini {wi}");
                //Log.Information("Randomizing weapons in ini: " + wi);
                var dlcWeapIni = DuplicatingIni.LoadIni(wi);
                RandomizeWeaponIni(dlcWeapIni, me2rbioweapon);
                //if (!MERFileSystem.UsingDLCModFS)
                //{
                //    Log.Information("Writing DLC BioWeapon: " + wi);
                //    File.WriteAllText(wi, dlcWeapIni.ToString());
                //}
            }

            return true;
        }


        private static string[] KeysToNotRandomize = new[]
        {
            "GUIClassName",
            "GUIImage",
            "IconRef",
            "GeneralDescription",
            "GUIClassName",
            "GUIClassDescription",
            "PrettyName",
            "bInfiniteAmmo",
            "ShortDescription",
            "NoAmmoFireSoundDelay",
            "bUpgradesBasicWeapon",
            "LowAmmoSoundThreshold",
            "SteamSoundThreshold",
            "LaserTimer",
            "LaserFireTimer",
            "TracerSpawnOffset",
            "TraceRange",
            "bUseSniperCam",
            "bZoomSnapEnabled",
            "bNotRegularWeaponGUI",
            "ZoomSnapDuration",
            "bFrictionEnabled",
            "MaxMagneticCorrectionAngle",
            "MagneticCorrectionThresholdAngle",
            "InitialSpareMagazines",
            "InitialMagazines",
            "MinZoomSnapDistance",
            "MaxZoomSnapDistance",
            "RecoilYawFrequench",
            "RecoilYawBias",
            "BeamInterpTime",

        };

        private static void RandomizeWeaponIni(DuplicatingIni vanillaFile, CoalesceAsset randomizerIni)
        {
            foreach (var section in vanillaFile.Sections)
            {
                var sectionsplit = section.Header.Split('.').ToList();
                if (sectionsplit.Count > 1)
                {
                    var objectname = sectionsplit[1];
                    if (objectname.StartsWith("SFXWeapon_") || objectname.StartsWith("SFXHeavyWeapon_"))
                    {
                        //We can randomize this section of the ini.
                        Debug.WriteLine($"Randomizing weapon {objectname}");
                        var outSection = randomizerIni.GetOrAddSection(section.Header);
                        foreach (var entry in section.Entries)
                        {
                            if (KeysToNotRandomize.Contains(entry.Key, StringComparer.InvariantCultureIgnoreCase))
                            {
                                continue; // Do not touch this key
                            }
                            if (entry.HasValue)
                            {
                                // if (entry.Key == "Damage") Debugger.Break();
                                string value = entry.Value;

                                //range check
                                if (value.StartsWith("("))
                                {
                                    value = value.Substring(0, value.IndexOf(')') + 1); //trim off trash on end (like ; comment )
                                    var p = StringStructParser.GetCommaSplitValues(value);
                                    if (p.Count == 2)
                                    {
                                        try
                                        {
                                            bool isInt = false;
                                            float x = 0;
                                            float y = 0;
                                            bool isZeroed = false;
                                            if (int.TryParse(p["X"].TrimEnd('f'), out var intX) && int.TryParse(p["Y"].TrimEnd('f'), out var intY))
                                            {
                                                //integers
                                                if (intX < 0 && intY < 0)
                                                {
                                                    Debug.WriteLine($" BELOW ZERO INT: {entry.Key} for {objectname}: {entry.RawText}");
                                                }

                                                bool validValue = false;
                                                for (int i = 0; i < 10; i++)
                                                {

                                                    bool isMaxMin = intX > intY;
                                                    //bool isMinMax = intY < intX;
                                                    isZeroed = intX == 0 && intY == 0;
                                                    if (isZeroed)
                                                    {
                                                        validValue = true;
                                                        break;
                                                    }; //skip
                                                    bool isSame = intX == intY;
                                                    bool belowzeroInt = intX < 0 || intY < 0;
                                                    bool abovezeroInt = intX > 0 || intY > 0;

                                                    int Max = isMaxMin ? intX : intY;
                                                    int Min = isMaxMin ? intY : intX;

                                                    int range = Max - Min;
                                                    if (range == 0) range = Max;
                                                    if (range == 0)
                                                        Debug.WriteLine("Range still 0");
                                                    int rangeExtension = range / 2; //50%

                                                    int newMin = Math.Max(0, ThreadSafeRandom.Next(Min - rangeExtension, Min + rangeExtension));
                                                    int newMax = ThreadSafeRandom.Next(Max - rangeExtension, Max + rangeExtension);
                                                    intX = isMaxMin ? newMax : newMin;
                                                    intY = isMaxMin ? newMin : newMax; //might need to check zeros
                                                                                       //if (entry.Key.Contains("MagSize")) Debugger.Break();

                                                    if (intX != 0 || intY != 0)
                                                    {
                                                        x = intX;
                                                        y = intY;
                                                        if (isSame) x = intY;
                                                        if (!belowzeroInt && (x <= 0 || y <= 0))
                                                        {
                                                            continue; //not valid. Redo this loop
                                                        }
                                                        if (abovezeroInt && (x <= 0 || y <= 0))
                                                        {
                                                            continue; //not valid. Redo this loop
                                                        }

                                                        validValue = true;
                                                        break; //break loop
                                                    }
                                                }

                                                if (!validValue)
                                                {
                                                    Debug.WriteLine($"Failed rerolls: {entry.Key} for {objectname}: {entry.RawText}");
                                                }
                                            }
                                            else
                                            {
                                                //if (section.Header.Contains("SFXWeapon_GethShotgun")) Debugger.Break();

                                                //floats
                                                //Fix error in bioware's coalesced file
                                                if (p["X"] == "0.65.0f") p["X"] = "0.65f";
                                                float floatx = float.Parse(p["X"].TrimEnd('f'));
                                                float floaty = float.Parse(p["Y"].TrimEnd('f'));
                                                bool belowzeroFloat = false;
                                                if (floatx < 0 || floaty < 0)
                                                {
                                                    Debug.WriteLine($" BELOW ZERO FLOAT: {entry.Key} for {objectname}: {entry.RawText}");
                                                    belowzeroFloat = true;
                                                }

                                                bool isMaxMin = floatx > floaty;
                                                bool isMinMax = floatx < floaty;
                                                bool isSame = floatx == floaty;
                                                isZeroed = floatx == 0 && floaty == 0;
                                                if (isZeroed)
                                                {
                                                    continue;
                                                }; //skip

                                                float Max = isMaxMin ? floatx : floaty;
                                                float Min = isMaxMin ? floaty : floatx;

                                                float range = Max - Min;
                                                if (range == 0) range = 0.1f * Max;
                                                float rangeExtension = range * .5f; //50%
                                                if (ThreadSafeRandom.Next(10) == 0)
                                                {
                                                    rangeExtension = range * 15f; // Extreme
                                                }


                                                float newMin = Math.Max(0, ThreadSafeRandom.NextFloat(Min - rangeExtension, Min + rangeExtension));
                                                float newMax = ThreadSafeRandom.NextFloat(Max - rangeExtension, Max + rangeExtension);
                                                if (!belowzeroFloat)
                                                {
                                                    //ensure they don't fall below 0
                                                    if (newMin < 0)
                                                        newMin = Math.Max(newMin, Min / 2);

                                                    if (newMax < 0)
                                                        newMax = Math.Max(newMax, Max / 2);

                                                    //i have no idea what i'm doing
                                                }
                                                floatx = isMaxMin ? newMax : newMin;
                                                floaty = isMaxMin ? newMin : newMax; //might need to check zeros
                                                x = floatx;
                                                y = floaty;
                                                if (isSame) x = y;
                                            }

                                            if (isZeroed)
                                            {
                                                continue; //skip
                                            }

                                            // Write out the new value
                                            outSection.SetSingleEntry(entry.Key, $"(X={x},Y={y})");
                                        }
                                        catch (Exception e)
                                        {
                                            Log.Error($"Cannot randomize weapon stat {objectname} {entry.Key}: {e.Message}");
                                        }
                                    }
                                }
                                else
                                {
                                    //Debug.WriteLine(entry.Key);
                                    var initialValue = entry.Value.ToString();
                                    var isInt = int.TryParse(entry.Value, out var valueInt);
                                    var isFloat = float.TryParse(entry.Value, out var valueFloat);
                                    switch (entry.Key)
                                    {
                                        case "BurstRounds":
                                            {
                                                var burstMax = valueInt * 2;
                                                entry.Value = (ThreadSafeRandom.Next(burstMax) + 1).ToString();
                                            }
                                            break;
                                        case "RateOfFireAI":
                                        case "DamageAI":
                                            {
                                                entry.Value = ThreadSafeRandom.NextFloat(.1, 2).ToString(CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "RecoilInterpSpeed":
                                        case "RecoilFadeSpeed":
                                        case "RecoilZoomFadeSpeed":
                                        case "RecoilYawScale":
                                        case "RecoilYawFrequency":
                                        case "RecoilYawNoise":
                                        case "DamageHench":
                                        case "BurstRefireTime":
                                        case "ZoomAccFirePenalty":
                                        case "ZoomAccFireInterpSpeed":
                                        case "FirstHitDamage":
                                        case "SecondHitDamage":
                                        case "ThirdHitDamage":
                                            {
                                                entry.Value = ThreadSafeRandom.NextFloat(valueFloat / 2, valueFloat * 1.5).ToString(CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "bIsAutomatic":
                                            {
                                                var curValue = bool.Parse(entry.Value);
                                                entry.Value = ThreadSafeRandom.Next(5) == 0 ? (!curValue).ToString() : entry.Value.ToString();
                                            }
                                            break;
                                        case "MinRefireTime":
                                            {
                                                entry.Value = ThreadSafeRandom.NextFloat(0.01, 1).ToString();
                                            }
                                            break;
                                        case "AccFirePenalty":
                                        case "AccFireInterpSpeed":
                                            {
                                                entry.Value = ThreadSafeRandom.NextFloat(0, valueFloat * 1.75).ToString(CultureInfo.InvariantCulture);
                                            }
                                            break;
                                        case "AmmoPerShot":
                                            {
                                                if (ThreadSafeRandom.Next(10) == 0)
                                                {
                                                    entry.Value = "2";
                                                }
                                                // Otherwise do not change
                                            }
                                            break;
                                        case "AIBurstRefireTimeMin":
                                            entry.Value = ThreadSafeRandom.NextFloat(0, 2).ToString(CultureInfo.InvariantCulture);
                                            break;
                                        case "AIBurstRefireTimeMax":
                                            entry.Value = ThreadSafeRandom.NextFloat(1, 5).ToString(CultureInfo.InvariantCulture);
                                            break;
                                        case "MaxSpareAmmo":
                                            entry.Value = ThreadSafeRandom.Next(valueInt / 10, valueInt * 2).ToString(CultureInfo.InvariantCulture);
                                            break;
                                        default:
                                            Debug.WriteLine($"Undone key: {entry.Key}");
                                            break;
                                    }

                                    if (entry.Value != initialValue)
                                    {
                                        outSection.SetSingleEntry(entry.Key, entry.Value);
                                    }
                                }
                            }
                        }

                        // whats this do?
                        //if (section.Entries.All(x => x.Key != "Damage"))
                        //{
                        //    float X = ThreadSafeRandom.NextFloat(2, 7);
                        //    float Y = ThreadSafeRandom.NextFloat(2, 7);
                        //    section.Entries.Add(new DuplicatingIni.IniEntry($"Damage=(X={X},Y={Y})"));
                        //}
                    }
                }
            }
        }

    }
}
