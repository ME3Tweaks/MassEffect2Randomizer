using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LegendaryExplorerCore.Packages;
using LegendaryExplorerCore.Unreal;
using LegendaryExplorerCore.Unreal.Classes;
using Serilog;

namespace Randomizer.Randomizers.Game1.Misc
{
    class RClassTalents
    {
        // NOTE THIS DOES NOT WORK IN ME1
        // UNSURE IN LE1

        /// <summary>
        /// Randomizes the talent list
        /// </summary>
        /// <param name="export">2DA Export</param>
        /// <param name="random">Random number generator</param>
        /*private bool ShuffleClassTalentsAndPowers(ExportEntry export, Random random)
        {
            //List of talents... i think. Taken from talent_talenteffectlevels
            //int[] talentsarray = { 0, 7, 14, 15, 21, 28, 29, 30, 35, 42, 49, 50, 56, 57, 63, 64, 84, 86, 91, 93, 98, 99, 108, 109, 119, 122, 126, 128, 131, 132, 134, 137, 138, 141, 142, 145, 146, 149, 150, 153, 154, 157, 158, 163, 164, 165, 166, 167, 168, 169, 170, 171, 174, 175, 176, 177, 178, 180, 182, 184, 186, 188, 189, 190, 192, 193, 194, 195, 196, 198, 199, 200, 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 215, 216, 217, 218, 219, 220, 221, 222, 223, 224, 225, 226, 227, 228, 229, 231, 232, 233, 234, 235, 236, 237, 238, 239, 240, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 284, 285, 286, 287, 288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 305, 306, 307, 310, 312, 313, 315, 317, 318, 320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332 };
            List<int> talentidstoassign = new List<int>();
            Bio2DA classtalents = new Bio2DA(export);

            //108 = Charm
            //109 = Intimidate
            //229 = Setup_Player -> Spectre Training
            //228 = Setup_Player_Squad
            int[] powersToNotReassign = { 108, 109 };
            var powersToReassignPlayerMaster = new List<int>();
            var powersToReassignSquadMaster = new List<int>();

            int isVisibleCol = classtalents.GetColumnIndexByName("IsVisible");

            //Get powers list
            for (int row = 0; row < classtalents.RowNames.Count(); row++)
            {
                var classId = classtalents[row, 0].IntValue;
                int talentId = classtalents[row, 1].IntValue;
                if (powersToNotReassign.Contains(talentId))
                {
                    continue;
                }

                var visibleInt = classtalents[row, isVisibleCol].IntValue;
                if (visibleInt != 0)
                {
                    if (classId == 10)
                    {
                        continue; //QA Cheat Class
                    }

                    if (classId < 6)
                    {
                        //Player class
                        powersToReassignPlayerMaster.Add(talentId);
                    }
                    else
                    {
                        //squadmate class
                        powersToReassignSquadMaster.Add(talentId);
                    }
                }
            }

            var playerPowersShuffled = TalentsShuffler.TalentShuffle(powersToReassignPlayerMaster, 6, 9, random);
            var squadPowersShuffled = TalentsShuffler.TalentShuffle(powersToReassignSquadMaster, 6, 9, random);

            //ASSIGN POWERS TO TABLE

            // >> Player
            for (int classId = 0; classId < 6; classId++)
            {
                int assignmentStartRow = (classId * 16) + 5; //16 powers per player, the first 5 of each are setup, the last 2 are charm/intimidate
                var talentList = playerPowersShuffled[classId];
                for (int i = 0; i < talentList.Count; i++)
                {
                    Log.Information("Talent randomizer [PLAYER - CLASSID " + classId + "]: Setting row " + (assignmentStartRow + i) + " to " + talentList[i]);
                    classtalents[assignmentStartRow + i, 1].IntValue = talentList[i];
                }
            }

            // >> Squad
            int currentClassId = -1;
            List<int> currentList = null;
            for (int i = 0; i < classtalents.RowNames.Count; i++)
            {
                int rowClassId = classtalents[i, 0].IntValue;
                if (rowClassId == 10 || rowClassId < 6) continue; //skip supersoldier, player classes
                int currentTalentId = classtalents[i, 1].IntValue;
                if (rowClassId != currentClassId)
                {
                    currentList = squadPowersShuffled[0];
                    squadPowersShuffled.RemoveAt(0);
                    currentClassId = rowClassId;
                    //Krogan only has 2 non-assignable powers
                    if (currentClassId == 7)
                    {
                        i += 2;
                    }
                    else
                    {
                        i += 3;
                    }
                }

                int newPowerToAssign = currentList[0];
                currentList.RemoveAt(0);
                Log.Information("Talent randomizer [SQUAD - CLASSID " + currentClassId + "]: Setting row " + i + " to " + newPowerToAssign);
                classtalents[i, 1].IntValue = newPowerToAssign;
            }

            //UPDATE UNLOCKS (in reverse)
            int prereqTalentCol = classtalents.GetColumnIndexByName("PrereqTalent0");
            for (int row = classtalents.RowNames.Count() - 1; row > 0; row--)
            {
                var hasPrereq = classtalents[row, prereqTalentCol] != null;
                if (hasPrereq)
                {
                    classtalents[row, prereqTalentCol].IntValue = classtalents[row - 1, 1].IntValue; //Talent ID of above row
                }
            }

            /*
            //REASSIGN POWERS
            int reassignmentAttemptsRemaining = 200;
            bool attemptingReassignment = true;
            while (attemptingReassignment)
            {
                reassignmentAttemptsRemaining--;
                if (reassignmentAttemptsRemaining < 0) { attemptingReassignment = false; }

                var playerReassignmentList = new List<int>();
                playerReassignmentList.AddRange(powersToReassignPlayerMaster);
                var squadReassignmentList = new List<int>();
                squadReassignmentList.AddRange(powersToReassignSquadMaster);

                playerReassignmentList.Shuffle(random);
                squadReassignmentList.Shuffle(random);

                int previousClassId = -1;
                for (int row = 0; row < classtalents.RowNames.Count(); row++)
                {
                    var classId = classtalents[row, 0].IntValue;
                    int existingTalentId = classtalents[row, 1].IntValue;
                    if (powersToNotReassign.Contains(existingTalentId)) { continue; }
                    var visibleInt = classtalents[row, isVisibleCol].IntValue;
                    if (visibleInt != 0)
                    {
                        if (classId == 10)
                        {
                            continue; //QA Cheat Class
                        }
                        if (classId < 6)
                        {
                            //Player class
                            int talentId = playerReassignmentList[0];
                            playerReassignmentList.RemoveAt(0);
                            classtalents[row, 1].SetData(talentId);
                        }
                        else
                        {

                            //squadmate class
                            int talentId = squadReassignmentList[0];
                            squadReassignmentList.RemoveAt(0);
                            classtalents[row, 1].SetData(talentId);
                        }
                    }
                }

                //Validate

                break;
            }

            if (reassignmentAttemptsRemaining < 0)
            {
                Debugger.Break();
                return false;
            }*/
        /*
            //Patch out Destroyer Tutorial as it may cause a softlock as it checks for kaidan throw
            var Pro10_08_Dsg = new ME1Package("BIOA_PRO10_08_DSG");
            ExportEntry GDInvulnerabilityCounter = (ExportEntry)Pro10_08_Dsg.getEntry(13521);
            var invulnCount = GDInvulnerabilityCounter.GetProperty<IntProperty>("IntValue");
            if (invulnCount != null && invulnCount.Value != 0)
            {
                invulnCount.Value = 0;
                GDInvulnerabilityCounter.WriteProperty(invulnCount);
                Pro10_08_Dsg.save();
            }


            //REASSIGN UNLOCK REQUIREMENTS
            Log.Information("Reassigned talents");
            classtalents.Write2DAToExport();

            return true;










            /*








            //OLD CODE
            for (int row = 0; row < classtalents.RowNames.Count(); row++)
            {
                int baseclassid = classtalents[row, 0].IntValue;
                if (baseclassid == 10)
                {
                    continue;
                }
                int isvisible = classtalents[row, 6].IntValue;
                if (isvisible == 0)
                {
                    continue;
                }
                talentidstoassign.Add(classtalents[row, 1].IntValue);
            }

            int i = 0;
            int spectretrainingid = 259;
            //while (i < 60)
            //{
            //    talentidstoassign.Add(spectretrainingid); //spectre training
            //    i++;
            //}

            //bool randomizeLevels = false; //will use better later
            Console.WriteLine("Randomizing Class talent list");

            int currentClassNum = -1;
            List<int> powersAssignedToThisClass = new List<int>();
            List<int> rowsNeedingPrereqReassignments = new List<int>(); //some powers require a prereq, this will ensure all powers are unlockable for this randomization
            List<int> talentidsNeedingReassignment = new List<int>(); //used only to filter out the list of bad choices, e.g. don't depend on self.
            List<int> powersAssignedAsPrereq = new List<int>(); //only assign 1 prereq to a power tree
            for (int row = 0; row < classtalents.RowNames.Count(); row++)
            {
                int baseclassid = classtalents[row, 0].IntValue;
                if (baseclassid == 10)
                {
                    continue;
                }
                if (currentClassNum != baseclassid)
                //this block only executes when we are changing classes in the list, so at this point
                //we have all of the info loaded about the class (e.g. all powers that have been assigned)
                {
                    if (powersAssignedToThisClass.Count() > 0)
                    {
                        List<int> possibleAllowedPrereqs = powersAssignedToThisClass.Except(talentidsNeedingReassignment).ToList();

                        //reassign prereqs now that we have a list of powers
                        foreach (int prereqrow in rowsNeedingPrereqReassignments)
                        {
                            int randomindex = -1;
                            int prereq = -1;
                            //while (true)
                            //{
                            randomindex = ThreadSafeRandom.Next(possibleAllowedPrereqs.Count());
                            prereq = possibleAllowedPrereqs[randomindex];
                            //powersAssignedAsPrereq.Add(prereq);
                            classtalents[prereqrow, 8].Data = BitConverter.GetBytes(prereq);
                            classtalents[prereqrow, 9].Data = BitConverter.GetBytes(ThreadSafeRandom.Next(5) + 4);
                            Console.WriteLine("Class " + baseclassid + "'s power on row " + row + " now depends on " + classtalents[prereqrow, 8].IntValue + " at level " + classtalents[prereqrow, 9].IntValue);
                            //}
                        }
                    }
                    rowsNeedingPrereqReassignments.Clear();
                    powersAssignedToThisClass.Clear();
                    powersAssignedAsPrereq.Clear();
                    currentClassNum = baseclassid;

                }
                int isvisible = classtalents[row, 6].IntValue;
                if (isvisible == 0)
                {
                    continue;
                }

                if (classtalents[row, 8] != null)
                {
                    //prereq
                    rowsNeedingPrereqReassignments.Add(row);
                }

                if (classtalents[row, 1] != null) //talentid
                {
                    //Console.WriteLine("[" + row + "][" + 1 + "]  (" + classtalents.columnNames[1] + ") value originally is " + classtalents[row, 1].GetDisplayableValue());

                    int randomindex = -1;
                    int talentindex = -1;
                    int reassignattemptsremaining = 250; //attempt 250 random attempts.
                    while (true)
                    {
                        reassignattemptsremaining--;
                        if (reassignattemptsremaining <= 0)
                        {
                            //this isn't going to work.
                            return false;
                        }
                        randomindex = ThreadSafeRandom.Next(talentidstoassign.Count());
                        talentindex = talentidstoassign[randomindex];
                        if (baseclassid <= 5 && talentindex == spectretrainingid)
                        {
                            continue;
                        }
                        if (!powersAssignedToThisClass.Contains(talentindex))
                        {
                            break;
                        }
                    }

                    talentidstoassign.RemoveAt(randomindex);
                    classtalents[row, 1].Data = BitConverter.GetBytes(talentindex);
                    powersAssignedToThisClass.Add(talentindex);
                    //Console.WriteLine("[" + row + "][" + 1 + "]  (" + classtalents.columnNames[1] + ") value is now " + classtalents[row, 1].GetDisplayableValue());
                }
                //if (randomizeLevels)
                //{
                //classtalents[row, 1].Data = BitConverter.GetBytes(ThreadSafeRandom.Next(1, 12));
                //}
            }oi
            classtalents.Write2DAToExport();
            return true;*/
        //}*/
    }
}
