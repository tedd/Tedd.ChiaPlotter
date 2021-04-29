using System.Collections.Generic;

namespace Tedd.ChiaPlotter.Models
{
    internal class JobConfigFile
    {
        // File version
        public int Version { get; set; }
        // Keep a counter for next available ID, so we don't confuse ID's we add.
        public int NextId { get; set; }

        public Dictionary<int, Job> Jobs = new();
    }
}