using Tedd.ChiaPlotter.Enums;

namespace Tedd.ChiaPlotter.Models
{
    internal class JobStatus
    {
        public JobStatusEnum Status { get; set; }
        public float ProgressPercentage { get; set; }
    }
}