using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Randomizer.Randomizers.Game1.GalaxyMap
{
    public class SuffixedCluster
    {
        public string ClusterName;
        /// <summary>
        /// If string ends with "cluster"
        /// </summary>
        public bool SuffixedWithCluster;
        /// <summary>
        /// If string is suffixed with cluster-style word. Doesn't need cluster appended.
        /// </summary>
        public bool Suffixed;

        public SuffixedCluster(string clusterName, bool suffixed)
        {
            this.ClusterName = clusterName;
            this.Suffixed = suffixed;
            this.SuffixedWithCluster = clusterName.EndsWith("cluster", StringComparison.InvariantCultureIgnoreCase);
        }

        public override string ToString()
        {
            return $"SuffixedCluster ({ClusterName})";
        }
    }
}
