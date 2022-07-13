using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Game3.ExportTypes.Enemy
{
    /// <summary>
    /// Flags that turn on and off features in the installed Spawn Modifier kismet object
    /// </summary>
    public enum EGESMFlag
    {
        /// <summary>
        /// Changes the evasion custom actions of enemies
        /// </summary>
        RandomizeEvade,
        /// <summary>
        /// Randomizes the melee custom action of enemies
        /// </summary>
        RandomizeMelee,
        /// <summary>
        /// Changes what enemies spawn
        /// </summary>
        RandomizeEnemy,
        /// <summary>
        /// Turns on the spawn count randomizer
        /// </summary>
        RandomizeEnemySpawnCount,

        // Melee and Weapon randomizers are done in the initialization of the pawn
        // and do not need to use Kismet
    }

    /// <summary>
    /// Global settings for the SpawnModifier system
    /// </summary>
    internal class RGlobalEnemySpawnModifier
    {
        /// <summary>
        /// The list of enabled flags
        /// </summary>
        private static List<EGESMFlag> _enabledFlags = new();
        /// <summary>
        /// Enables or disables the feature flag
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="enabled"></param>
        public static void SetFeatureFlag(EGESMFlag flag, bool enabled)
        {
            if (enabled && !_enabledFlags.Contains(flag))
            {
                _enabledFlags.Add(flag);
            } else if (!enabled)
            {
                _enabledFlags.Remove(flag);
            }
        }

        /// <summary>
        /// Removes all enabled flags
        /// </summary>
        public static void ResetClass() => _enabledFlags.Clear();
        /// <summary>
        /// Gets the list of enabled flags
        /// </summary>
        /// <returns></returns>
        public static IReadOnlyList<EGESMFlag> GetEnabledFlags() => _enabledFlags;
    }
}
