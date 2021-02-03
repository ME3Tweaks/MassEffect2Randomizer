using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MassEffectRandomizer.Classes;
using ME2Randomizer.Classes.gameini;
using ME2Randomizer.Classes.Randomizers.ME2.Coalesced;
using ME3ExplorerCore.GameFilesystem;
using ME3ExplorerCore.Misc;
using Serilog;

namespace ME2Randomizer.Classes.Randomizers.ME2.Misc
{
    public class Weapons
    {
        public static bool RandomizeSquadmateWeapons(RandomizationOption option)
        {
            var bg = CoalescedHandler.GetIniFile("BIOGame.ini");
            var loadoutSection = bg.GetOrAddSection("SFXGame.SFXPlayerSquadLoadoutData");

            // We need to minus them out
            loadoutSection.Entries.Add(new DuplicatingIni.IniEntry("!PlayerLoadoutInfo", "CLEAR"));
            loadoutSection.Entries.Add(new DuplicatingIni.IniEntry("!HenchLoadoutInfo", "CLEAR"));


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
                loadoutSection.Entries.Add(BuildLoadout(p));
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

        private static DuplicatingIni.IniEntry BuildLoadout(string pawnName)
        {
            var isPlayer = pawnName.Contains("Player");
            string key = isPlayer ? "+PlayerLoadoutInfo" : "+HenchLoadoutInfo";

            string value = $"(ClassName = {pawnName}, WeaponClasses = (";

            // Pick guns
            var totalGuns = ThreadSafeRandom.Next(3) + 2;
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
            return new DuplicatingIni.IniEntry(key, value);
        }


        public static bool RandomizeWeapons(RandomizationOption option)
        {
            var me2rbioweapon = CoalescedHandler.GetIniFile("BIOWeapon.ini");

            // We must manually fetch game files cause MERFS will return the ini from the dlc mod instead.
            ME2Coalesced me2basegamecoalesced = new ME2Coalesced(MERFileSystem.GetSpecificFile(@"BioGame\Config\PC\Cooked\Coalesced.ini"));

            Log.Information("Randomizing basegame weapon ini");
            var bioweapon = me2basegamecoalesced.Inis.FirstOrDefault(x => Path.GetFileName(x.Key) == "BIOWeapon.ini").Value;
            RandomizeWeaponIni(bioweapon, me2rbioweapon);

            var weaponInis = Directory.GetFiles(MEDirectories.GetDLCPath(MERFileSystem.Game), "BIOWeapon.ini", SearchOption.AllDirectories).ToList();
            foreach (var wi in weaponInis)
            {
                if (wi.Contains($"DLC_MOD_{MERFileSystem.Game}Randomizer"))
                    continue; // Skip randomizer folders

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
            "bInfiniteAmmo"
        };

        private static void RandomizeWeaponIni(DuplicatingIni vanillaFile, DuplicatingIni randomizerIni)
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
                                                float rangeExtension = range * 15f; //50%

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
                                    var isInt = int.TryParse(entry.Value, out var burstVal);
                                    var isFloat = float.TryParse(entry.Value, out var floatVal);
                                    switch (entry.Key)
                                    {
                                        case "BurstRounds":
                                            {
                                                var burstMax = burstVal * 2;
                                                entry.Value = (ThreadSafeRandom.Next(burstMax) + 1).ToString();
                                            }
                                            break;
                                        case "RateOfFireAI":
                                        case "DamageAI":
                                            {
                                                entry.Value = ThreadSafeRandom.NextFloat(.1, 2).ToString();
                                            }
                                            break;
                                            //case 
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
