using System.Collections.Generic;

namespace Tedd.ChiaPlotter.Models
{
    internal class JobConfigFile
    {
        public int Version { get; set; }
        public Dictionary<int, Job> Jobs = new();
    }
}