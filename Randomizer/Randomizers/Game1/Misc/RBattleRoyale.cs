using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LegendaryExplorerCore.Misc;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Packages.CloningImportingAndRelinking;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.BinaryConverters;
using LegendaryExplorerCore.Unreal.Classes;
using LegendaryExplorerCore.Unreal.ObjectInfo;
using ME3TweaksCore.Targets;
using Randomizer.MER;
using RoboSharp;

namespace Randomizer.Randomizers.Game1.Misc
{
    class RBattleRoyale
    {
        private static IMEPackage TemplatePackage; // Needs way to null out

        public static bool RandomizeFile(GameTarget target, IMEPackage package, RandomizationOption option)
        {
            // todo: Maybe don't make a new 2DA for everything
            // todo: optmize by not writing/reading full props list.

            var bioPawns = package.Exports.Where(x => x.IsA("BioPawn")).ToList();
            var level = package.FindExport("TheWorld.PersistentLevel");
            List<ExportEntry> addedSquads = new List<ExportEntry>();
            foreach (var bp in bioPawns)
            {
                var behaviorProp = bp.GetProperty<ObjectProperty>("m_oBehavior");
                if (behaviorProp == null)
                    continue;

                var behavior = behaviorProp.ResolveToEntry(package) as ExportEntry;
                if (behavior != null)
                {
                    var squadProp = behavior.GetProperty<ObjectProperty>("Squad");
                    if (squadProp != null && squadProp.Value != 0)
                    {
                        // Already has a squad
                        continue;
                    }

                    bp.RemoveProperty("bAmbientCreature");

                    // GENERATE NEW SQUAD OBJECT (Makes them hostile)
                    var newSquad = ExportCreator.CreateExport(package, "MERBioSquadCombat", "BioSquadCombat", level, relinkResults);
                    newSquad.WriteProperty(new BoolProperty(false, "m_bHasCover"));
                    newSquad.WriteProperty(new BoolProperty(false, "m_bCheckPlayPen"));
                    var strategyArray = new ArrayProperty<StructProperty>("StrategyArray");
                    GenerateStrategy(strategyArray, "Idle");
                    GenerateStrategy(strategyArray, "Search");
                    GenerateStrategy(strategyArray, "Charge");
                    GenerateStrategy(strategyArray, "Skirmish");
                    GenerateStrategy(strategyArray, "Defend");
                    newSquad.WriteProperty(strategyArray);

                    newSquad.WriteBinary(new byte[4]); // COUNT = 0 (not sure what these are)
                    squadProp = new ObjectProperty(newSquad, "Squad");
                    behavior.WriteProperty(squadProp);
                    addedSquads.Add(newSquad);

                    // GIVE THEM TALENT AND GUNS VIA THEIR BIOPAWNCHALLENGESCALEDTYPE
                    // CLONE SO EVERYONE HAS DIFF GUNS

                    var pawnTypeOriginalProp = behavior.GetProperty<ObjectProperty>("m_oActorType");
                    if (pawnTypeOriginalProp != null)
                    {
                        LoadTemplatePackage();

                        var originalPawnType = pawnTypeOriginalProp.ResolveToEntry(package) as ExportEntry;
                        var clonedOriginalPawnType = EntryCloner.CloneTree(originalPawnType);
                        clonedOriginalPawnType.ObjectName = new NameReference(clonedOriginalPawnType.ObjectName.Name + "MER", ThreadSafeRandom.Next()); // Memory Unique

                        clonedOriginalPawnType.WriteProperty(new BoolProperty(true, "m_bTargetable"));
                        clonedOriginalPawnType.WriteProperty(new BoolProperty(true, "m_bCombatTargetable"));

                        var ai = EntryImporter.EnsureClassIsInFile(package, "BioAI_HumanoidMinion", new RelinkerOptionsPackage()); // Todo: Cache?
                        clonedOriginalPawnType.WriteProperty(new ObjectProperty(ai, "AIController"));

                        ConfigureBPCSTForBR(clonedOriginalPawnType);

                        pawnTypeOriginalProp.Value = clonedOriginalPawnType.UIndex;
                        behavior.WriteProperty(pawnTypeOriginalProp);


                        // Add BioWeaponRanged object
                        // Todo: Cache?
                        EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, TemplatePackage.FindExport("MERWeaponRanged_0"), package, behavior, true, new RelinkerOptionsPackage(), out var merWeaponRanged);
                        behavior.WriteProperty(new ObjectProperty(merWeaponRanged, "m_pWeapon"));
                    }



                    //    // ATTRIBUTE
                    //    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, TemplatePackage.FindExport("MER2DA_Attribute"), package, bp, true, out var attribute2DA);
                    //    SetBRAttributes(equipment2DA);

                    //    // EQUIPMENT (GUNS)
                    //    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, TemplatePackage.FindExport("MER2DA_Equipment"), package, bp, true, out var equipment2DA);
                    //    SetBRGuns(equipment2DA);

                    //    // TALENT (POWERS)
                    //    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, TemplatePackage.FindExport("MER2DA_Talent"), package, bp, true, out var talent2DA);
                    //    SetBRTalent(talent2DA);
                }
            }

            if (addedSquads.Any())
            {
                var me1PersistentLevel = ObjectBinary.From<Level>(level);
                me1PersistentLevel.Actors.AddRange(addedSquads.Select(x => x.UIndex));
                level.WriteBinary(me1PersistentLevel);
            }

            return true;
        }

        private static void LoadTemplatePackage()
        {
            if (TemplatePackage != null)
                return;

            var packageStream = MEREmbedded.GetEmbeddedPackage(MEGame.LE1, "AkuzeModeHelper.pcc");
            TemplatePackage = MEPackageHandler.OpenMEPackageFromStream(packageStream);
        }

        private static void relinkResults(List<EntryStringPair> obj)
        {
            if (obj.Any())
            {
                Debugger.Break();
            }

        }

        private static void GenerateStrategy(ArrayProperty<StructProperty> strategyArray, string state)
        {
            var props = new PropertyCollection();
            props.Add(new NameProperty(state, "StateName"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(0, 1), "MaxLikelihood"));
            props.Add(new FloatProperty(ThreadSafeRandom.NextFloat(0, 1), "CurrentLikelihood"));
            var sChoice = new StructProperty("StrategyChoice", props);
            strategyArray.Add(sChoice);
        }


        private static void ConfigureBPCSTForBR(ExportEntry bioPawnChallengeScaledType)
        {
            var props = bioPawnChallengeScaledType.GetProperties();

            // Equipment
            {
                var equipmentProp = props.GetProp<ObjectProperty>("m_tblEquipment");
                if (equipmentProp == null)
                {
                    // We need to install a table for this pawn so it has equipment
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, TemplatePackage.FindExport("MEREquipmentTbl"), bioPawnChallengeScaledType.FileRef, bioPawnChallengeScaledType, true, new RelinkerOptionsPackage(), out var newTableEntry);
                    equipmentProp = new ObjectProperty(newTableEntry, "m_tblEquipment");
                    props.AddOrReplaceProp(equipmentProp);
                    newTableEntry.ObjectName = new NameReference(newTableEntry.ObjectName.Name + "MER", ThreadSafeRandom.Next());
                    SetBREquipment(newTableEntry as ExportEntry);
                }
                else
                {
                    // Has existing equipment
                    if (equipmentProp?.ResolveToEntry(bioPawnChallengeScaledType.FileRef) is ExportEntry equipment)
                    {
                        var newEquipment = EntryCloner.CloneEntry(equipment);
                        newEquipment.ObjectName = new NameReference(newEquipment.ObjectName.Name + "MER", ThreadSafeRandom.Next());
                        SetBREquipment(newEquipment);
                        equipmentProp.Value = newEquipment.UIndex;
                    }
                }
            }

            // Attributes
            {
                var attributeProp = props.GetProp<ObjectProperty>("m_tblAttributes");
                if (attributeProp == null)
                {
                    // We need to install a table for this pawn so it has attributes
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, TemplatePackage.FindExport("MERAttributeTbl"), bioPawnChallengeScaledType.FileRef, bioPawnChallengeScaledType, true, new RelinkerOptionsPackage(), out var newTableEntry);
                    attributeProp = new ObjectProperty(newTableEntry, "m_tblAttributes");
                    props.AddOrReplaceProp(attributeProp);
                    newTableEntry.ObjectName = new NameReference(newTableEntry.ObjectName.Name + "MER", ThreadSafeRandom.Next());
                    SetBRAttribute(newTableEntry as ExportEntry);
                }
                else
                {
                    // Has existing attributes
                    if (attributeProp?.ResolveToEntry(bioPawnChallengeScaledType.FileRef) is ExportEntry talents)
                    {
                        var newAttributes = EntryCloner.CloneEntry(talents);
                        newAttributes.ObjectName = new NameReference(newAttributes.ObjectName.Name + "MER", ThreadSafeRandom.Next());
                        SetBRAttribute(newAttributes);
                        attributeProp.Value = newAttributes.UIndex;
                    }
                }
            }

            // Talents
            {
                var talentProp = props.GetProp<ObjectProperty>("m_tblTalents");
                if (talentProp == null)
                {
                    // We need to install a table for this pawn so it has talent
                    EntryImporter.ImportAndRelinkEntries(EntryImporter.PortingOption.CloneAllDependencies, TemplatePackage.FindExport("MERTalentTbl"), bioPawnChallengeScaledType.FileRef, bioPawnChallengeScaledType, true, new RelinkerOptionsPackage(), out var newTableEntry);
                    talentProp = new ObjectProperty(newTableEntry, "m_tblTalents");
                    props.AddOrReplaceProp(talentProp);
                    newTableEntry.ObjectName = new NameReference(newTableEntry.ObjectName.Name + "MER", ThreadSafeRandom.Next());
                    SetBRTalent(newTableEntry as ExportEntry);
                }
                else
                {
                    // Has existing talents
                    var talents = talentProp?.ResolveToEntry(bioPawnChallengeScaledType.FileRef) as ExportEntry;
                    if (talents != null)
                    {
                        var newTalents = EntryCloner.CloneEntry(talents);
                        newTalents.ObjectName = new NameReference(newTalents.ObjectName.Name + "MER", ThreadSafeRandom.Next());
                        SetBRTalent(newTalents);
                        talentProp.Value = newTalents.UIndex;
                    }
                }
            }

            props.RemoveNamedProperty("m_eDefaultClassification"); // Not sure this does anything...

            bioPawnChallengeScaledType.WriteProperties(props);
        }


        private static int[] AssignableGuns = new[]
        {
            3, // pistol
            4, // assault rifle
            //5, // shotgun
            52, // sniper rifle
            22, // geth pulse
            23, // geth antitank
            25, // geth drone
            350, // gethgun sniper
            29, // minion pistol
            30, // minion assault rifle
            //31, // minion shotgun
            32, // minion sniper rifle
        };

        private static void SetBREquipment(ExportEntry newEquipment)
        {
            var e2DA = new Bio2DA(newEquipment);

            // FIRST ROW
            e2DA["1", "Weapon"].IntValue = AssignableGuns.RandomElement();
            e2DA["1", "WeaponSoph"].IntValue = 1;

            e2DA["1", "Armor"].IntValue = 13; //AssignableGuns.RandomElement();
            e2DA["1", "ArmorSoph"].IntValue = 1;

            // second gun might have issues as it could be same slot. we'd have to categorize guns
            e2DA["1", "SecondaryWeapon"].IntValue = 31; // shotgun
            e2DA["1", "WeaponSoph"].IntValue = 1;


            // Armor?
            e2DA["6", "ArmorSoph"].IntValue = 2;
            e2DA["12", "ArmorSoph"].IntValue = 3;
            e2DA["18", "ArmorSoph"].IntValue = 4;
            e2DA["24", "ArmorSoph"].IntValue = 5;
            e2DA["30", "ArmorSoph"].IntValue = 6;
            e2DA["36", "ArmorSoph"].IntValue = 7;
            e2DA["42", "ArmorSoph"].IntValue = 8;
            e2DA["48", "ArmorSoph"].IntValue = 9;
            e2DA["54", "ArmorSoph"].IntValue = 10;

            // Grenades?

            // Scaling
            e2DA["6", "WeaponSoph"].IntValue = 2;
            e2DA["12", "WeaponSoph"].IntValue = 3;
            e2DA["18", "WeaponSoph"].IntValue = 4;
            e2DA["24", "WeaponSoph"].IntValue = 5;
            e2DA["30", "WeaponSoph"].IntValue = 6;
            e2DA["36", "WeaponSoph"].IntValue = 7;
            e2DA["42", "WeaponSoph"].IntValue = 8;
            e2DA["48", "WeaponSoph"].IntValue = 9;
            e2DA["54", "WeaponSoph"].IntValue = 10;

            e2DA.Write2DAToExport(newEquipment);
        }

        private static void SetBRAttribute(ExportEntry newEquipment)
        {
            var a2DA = new Bio2DA(newEquipment);
            var healthMax = -44;

            // FIRST ROW
            a2DA["1", "m_Stamina"].IntValue = 20;
            a2DA["1", "m_Focus"].IntValue = 10;
            a2DA["1", "m_Precision"].IntValue = 10;
            a2DA["1", "m_Coordination"].IntValue = 40;
            a2DA["1", "m_Coordination"].IntValue = healthMax;
            a2DA["1", "m_StabilityRegenRate"].IntValue = 0;
            a2DA["1", "m_StabilityMax"].IntValue = 0;
            a2DA["1", "m_DamageDurationMult"].IntValue = 0;
            a2DA["1", "m_ResistanceBiotic"].IntValue = 1;
            a2DA["1", "m_ResistanceSuppression"].IntValue = 1;
            a2DA["1", "m_ResistanceTech"].IntValue = 1;

            int row = 2;
            while (row < 75)
            {
                healthMax -= 5; // not sure why this is minus
                a2DA[row.ToString(), "m_HealthMax"].IntValue = healthMax;
                row++;
            }

            a2DA.Write2DAToExport(newEquipment);
        }

        private static void SetBRTalent(ExportEntry newEquipment)
        {
            var t2DA = new Bio2DA(newEquipment);

            // FIRST ROW
            t2DA["1", "Talent1"].IntValue = 161; // Not sure what this is
            t2DA["1", "Talent1Rank"].IntValue = 1;

            t2DA.Write2DAToExport(newEquipment);
        }
    }
}
