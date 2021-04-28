using System.Collections.Generic;

namespace Tedd.ChiaPlotter.Models
{
    internal class JobStatusFile
    {
        public int Version { get; set; }
        public Dictionary<int, JobStatus> Jobs = new();
    }
}