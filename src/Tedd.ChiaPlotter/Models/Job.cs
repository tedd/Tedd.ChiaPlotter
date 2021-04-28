namespace Tedd.ChiaPlotter.Models
{
    internal class Job
    {
        public string Temp1Dir { get; set; }
        public string Temp2Dir { get; set; }
        public string PlotDir { get; set; }
        public int ThreadCount { get; set; }
        public int MaxRamMB { get; set; }
        public int BucketCount { get; set; }
        public string QueueName { get; set; }
        public int PlotCount { get; set; }
        public int PlotParallelism { get; set; }
    }
}