using System.Diagnostics;
using System.Text.Json.Serialization;
using Tedd.ChiaPlotter.Enums;

namespace Tedd.ChiaPlotter.Models
{
    internal class JobStatus
    {
        private volatile bool _enabled = true;
        private volatile bool _running;
        public int Id { get; set; }
        public Job Job { get; set; }

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public JobStatusEnum Status { get; set; }
        public float ProgressPercentage { get; set; }
        public int ProcessId { get; set; } = -1;

        public bool Running
        {
            get => _running;
            set => _running = value;
        }

        [JsonIgnore]
        public Process? Process { get; set; }
        /// <summary>
        /// Did we create process object?
        /// </summary>
        [JsonIgnore]
    
        public bool OwnProcess { get; set; }
        /// <summary>
        /// How many plots has been generated
        /// </summary>
        public int RunCount { get; set; }

        public string LogFile { get; set; }
    }
}