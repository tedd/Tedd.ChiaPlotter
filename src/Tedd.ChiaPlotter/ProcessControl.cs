using System.Collections.Generic;
using System.Linq;
using Tedd.ChiaPlotter.Models;

namespace Tedd.ChiaPlotter
{
    internal class ProcessControl
    {
        private readonly JobStatusFile _jobStatus;

        private Dictionary<int, JobItem> Jobs { get; set; } = new();
        public ProcessControl(JobStatusFile jobStatus)
        {
            _jobStatus = jobStatus;
            // TODO: Compare old status file to running process list so we can attach to any existing orphaned process
            // TODO: Start a background thread that checks status for all jobs, updates status, spawns next in queue, etc...
        }

        public List<JobItem> GetJobList()
        {
            lock (Jobs)
                return new(Jobs.Values.Where(w => w.Enabled));
        }

        public void AddJob(int jobId, Job job)
        {
            // TODO: First scan process table to see if there is already a process that matches
            lock (Jobs)
            {
                var jobItem = new JobItem()
                {
                    Id = jobId,
                    Job = job
                };
                Jobs.Add(jobId, jobItem);
                Execute(jobId);
            }
        }

        public void RemoveJob(int jobId)
        {
            // Remove job. Should it be killed too? Cleanup temp files?
            lock (Jobs)
            {
                if (Jobs.TryGetValue(jobId, out var jobItem))
                    jobItem.Enabled = false;

                // TODO: Should we kill running job?
            }
        }

        private void Execute(int jobId)
        {
            // TODO: Check if this process is already running
            // TODO: If not, execute it
        }

    }
}