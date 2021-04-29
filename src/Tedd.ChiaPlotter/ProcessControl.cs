using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tedd.ChiaPlotter.Enums;
using Tedd.ChiaPlotter.Models;

namespace Tedd.ChiaPlotter
{
    internal class ProcessControl
    {
        private readonly JobStatusFile _jobStatus;

        public ProcessControl(JobStatusFile jobStatus)
        {
            _jobStatus = jobStatus;
            // TODO: Compare old status file to running process list so we can attach to any existing orphaned process
            // TODO: Start a background thread that checks status for all jobs, updates status, spawns next in queue, etc...

            var processDic = Process.GetProcesses().ToDictionary(k => k.Id);
            foreach (var jobKvp in _jobStatus.Jobs)
            {
                var jobId = jobKvp.Key;
                var job = jobKvp.Value;
                job.Running = false;
                if (processDic.TryGetValue(job.ProcessId, out var process))
                {
                    var cmd = GetCommandLine(process);
                    if (cmd.Contains("chia") && cmd.Contains("plots create"))
                    {
                        // This is a old, but still running process for jobId
                        job.Running = true;
                        job.Process = process;
                        Execute(jobId);
                    }
                }
            }
        }

        private static string GetCommandLine(Process process)
        {
            using var proc = StartProcess("wmic", $"process where \"processid = '{process.Id}'\" get commandline");
            proc.WaitForExit();
            var txt = proc.StandardOutput.ReadToEnd();
            return txt;
        }

        public List<JobStatus> GetJobList()
        {
            lock (_jobStatus.Jobs)
                return new(_jobStatus.Jobs.Values.Where(w => w.Enabled));
        }

        public void AddJob(int jobId, Job job)
        {
            // TODO: First scan process table to see if there is already a process that matches
            lock (_jobStatus.Jobs)
            {
                var jobStatus = new JobStatus()
                {
                    Id = jobId,
                    Job = job
                };
                _jobStatus.Jobs.Add(jobId, jobStatus);
                Execute(jobId);
            }
        }

        public void RemoveJob(int jobId)
        {
            // Disable job in active job list
            JobStatus? jobStatus;
            lock (_jobStatus.Jobs)
                if (_jobStatus.Jobs.TryGetValue(jobId, out jobStatus))
                    jobStatus.Enabled = false;

        }

        private void Execute(int jobId)
        {
            JobStatus jobStatus;
            lock (_jobStatus.Jobs)
            {
                if (!_jobStatus.Jobs.TryGetValue(jobId, out jobStatus))
                {
                    Console.WriteLine($"Error: Executing job id {jobId}, but could not find it in job status list.");
                    return;
                }
            }

            Task.Run(async () => await ExecuteInt(jobId, jobStatus));

        }

        private async Task ExecuteInt(int jobId, JobStatus jobStatus)
        {
            for (; ; )
            {
                await Task.Delay(100);
                // Task is disabled, so we exit
                if (!jobStatus.Enabled)
                    return;

                // Check if our pid is running
                jobStatus.Running = jobStatus.Process != null && !jobStatus.Process.HasExited;
                jobStatus.ProgressPercentage = -1f;

                // Is still running, read output and update status
                if (jobStatus.Running)
                {
                    if (jobStatus.OwnProcess)
                    {
                        // TODO: Read process output, determine how far along we are
                        var line = jobStatus.Process.StandardOutput.ReadLineAsync();
                    }
                }
                else
                {
                    jobStatus.Process?.Dispose();
                    // Are we done?
                    if (jobStatus.RunCount >= jobStatus.Job.PlotCount)
                    {
                        jobStatus.Status = JobStatusEnum.Done;
                        jobStatus.Running = false;
                        return;
                    }

                    // Not done yet, start process (again)
                    jobStatus.Process = StartProcess("chia", "args");
                    if (jobStatus.Process == null)
                    {
                        Console.WriteLine($"Error: Unable to execute plotter process for job id {jobId}: ");
                        return;
                    }
                    jobStatus.OwnProcess = true;
                    jobStatus.RunCount++;
                }
            }
        }

        public static Process? StartProcess(string cmd, string args)
            => Process.Start(new ProcessStartInfo()
            {
                FileName = cmd,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            });

    }
}