using Randomizer.MER;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Game2.Talents
{
    /// <summary>
    /// Defines a set of talents for a henchman - 4 powers and their evolutions
    /// </summary>
    internal class TalentSet
    {
        /// <summary>
        /// If we were able to find a solution of compatible powers for this set
        /// </summary>
        public bool IsBaseValid { get; }
        /// <summary>
        /// Creates a talent set from the list of all powers that are available for use. The list WILL be modified so ensure it is a clone
        /// </summary>
        /// <param name="allPowers"></param>
        public TalentSet(HenchLoadoutInfo hpi, List<HTalent> allPowers)
        {
            int numPassives = 0;

            int numPowersToAssign = hpi.NumPowersToAssign;
            for (int i = 0; i < numPowersToAssign; i++)
            {
                var talent = allPowers.PullFirstItem();
                int retry = allPowers.Count;
                while (retry > 0 && Powers.Any(x => x.BaseName == talent.BaseName))
                {
                    allPowers.Add(talent);
                    talent = allPowers.PullFirstItem();
                    retry--;
                    if (retry <= 0)
                    {
                        IsBaseValid = false;
                        return;
                    }
                }

                if (talent.BasePower.ObjectName.Name.Contains("Passive"))
                {
                    numPassives++;
                    if (numPassives > 3)
                    {
                        // We must ensure there is not a class full of passives
                        // or the evolutions will not have a solution
                        // only doing half allows us to give the evolution solution finder
                        // a better chance at a quick solution
                        IsBaseValid = false;
                        return;
                    }
                }
                Powers.Add(talent);
            }

            // Add in the fixed powers
            //if (hpi.FixedPowers.Any())
            //{
            //    Powers.AddRange(hpi.FixedPowers);
            //    Powers.Shuffle();
            //}

            IsBaseValid = true;
        }

        public bool SetEvolutions(HenchLoadoutInfo hpi, List<HTalent> availableEvolutions)
        {
            // 1. Calculate the number of required bonus evolutions
            var numToPick = (Powers.Count(x => x.HasEvolution())/* - hpi.FixedPowers.Count*/) * 2;
            while (numToPick > 0)
            {
                var numAttempts = availableEvolutions.Count;
                var evolutionToCheck = availableEvolutions.PullFirstItem();
                while (numAttempts > 0 && EvolvedPowers.Any(x => x.PowerExport.InstancedFullPath == evolutionToCheck.PowerExport.InstancedFullPath)) // Ensure there are no duplicate power exports
                {
                    // Repick
                    availableEvolutions.Add(evolutionToCheck);
                    evolutionToCheck = availableEvolutions.PullFirstItem();
                    numAttempts--;
                    if (numAttempts == 0)
                    {
                        Debug.WriteLine("Could not find suitable evolution for talentset!");
                        return false; // There is no viable solution
                    }
                }

                EvolvedPowers.Add(evolutionToCheck);
                numToPick--;
            }

            return true;
        }

        /// <summary>
        /// The base power set of the kit
        /// </summary>
        public List<HTalent> Powers { get; } = new();

        /// <summary>
        /// The evolution power pool. Should not contain any items that are the same as an item in the Powers list
        /// </summary>
        public List<HTalent> EvolvedPowers { get; } = new();
    }
}
