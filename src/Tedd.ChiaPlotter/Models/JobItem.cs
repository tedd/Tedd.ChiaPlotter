namespace Tedd.ChiaPlotter.Models
{
    internal class JobItem
    {
        public int Id { get; set; }
        public Job Job { get; set; }
        public JobStatus Status { get; set; } = new();
        public bool Enabled { get; set; } = true;
    }
}