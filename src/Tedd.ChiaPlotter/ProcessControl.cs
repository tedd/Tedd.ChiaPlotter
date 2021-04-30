using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
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
            var txt = proc.StandardOutput.ReadToEnd()
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Skip(1)
                .First();

            return txt;
        }

        public List<JobStatus> GetJobList()
        {
            lock (_jobStatus.Jobs)
                return new(_jobStatus.Jobs.Values.Where(w => w.Enabled));
        }

        public void AddJob(int jobId, Job job)
        {
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
            lock (_jobStatus.Jobs)
                if (_jobStatus.Jobs.TryGetValue(jobId, out var jobStatus))
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
                await Task.Delay(1_000);
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
                    jobStatus.LogFile = Path.Combine(jobStatus.Job.Temp1Dir, $"PlotterLog_{jobId:D4}.txt");
                    jobStatus.Process = StartProcess("cmd", $@" /c ""chia plots create {jobStatus.Job.CreateCommandlineArgs()} > {jobStatus.LogFile} 2>&1""");
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
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            });

    }
}