namespace Tedd.ChiaPlotter.Models
{
    internal class JobItem: Job
    {
        public int Id { get; set; }
        public JobStatus Status { get; set; }
        public bool Enabled { get; set; }
    }
}