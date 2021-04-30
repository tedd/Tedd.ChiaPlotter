using System.Collections.Generic;

namespace Tedd.ChiaPlotter.Models
{
    internal class Job
    {
        public int KeyFingerprint { get; set; }
        public string PoolPK { get; set; }
        public string FarmerPK { get; set; }

        public string Temp1Dir { get; set; }
        public string Temp2Dir { get; set; }
        public string PlotDir { get; set; }
        public int ThreadCount { get; set; }
        public int MaxRamMB { get; set; }
        public int BucketCount { get; set; }
        public string QueueName { get; set; }
        public int PlotCount { get; set; }
        public int PlotParallelism { get; set; }

        public string CreateCommandlineArgs()
        {
            var p = new List<string>()
            {
                "-k32", "-n1", $"-d{PlotDir}",$"-b{MaxRamMB}",$"-u{BucketCount}",$"-r{ThreadCount}",$"-t{Temp1Dir}"
            };
            if (!string.IsNullOrWhiteSpace(Temp2Dir))
                p.Add($"-2{Temp2Dir}");
            if (KeyFingerprint != 0)
                p.Add($"-a{KeyFingerprint}");
            if (!string.IsNullOrWhiteSpace(PoolPK))
                p.Add($"-p{PoolPK}");
            if (!string.IsNullOrWhiteSpace(FarmerPK))
                p.Add($"-f{FarmerPK}");

            return string.Join(" ", p);
        }
    }
}