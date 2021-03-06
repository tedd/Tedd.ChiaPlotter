using System;
using System.Collections;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ConsoleTables;
using Tedd.ChiaPlotter.Enums;
using Tedd.ChiaPlotter.Models;

namespace Tedd.ChiaPlotter
{
    class Program
    {
        private static string _jobsConfigFile = "";
        private static string _jobsStatusFile = "";
        private static int _checkConfigChangeMs = 1_000;
        internal static string ChiaExe = "chia";


        // <param name="verbose">Show verbose output</param>
        // <param name="killRunning">RemoveJob: Kill running process (optional)</param>
        // <param name="plotParallelism">AddJob: Number of plots to generate in parallel (optional, default: 1)</param>
        // <param name="queueName">AddJob: Queue for job (optional, default: default)</param>
        /// <summary>
        /// Chia plotting helper
        /// </summary>
        /// <param name="action">Action: Start, List, AddJob, RemoveJob</param>
        /// <param name="chiaExe">AddJob: Location of chia executable (there are 3, this is the one under resources) (required)</param>
        /// <param name="keyFingerprint">AddJob: Key fingerprint from your keychain (optional)</param>
        /// <param name="farmerPK">AddJob: Farmer Public Key (optional)</param>
        /// <param name="poolPK">AddJob: Farmer Public Key (optional)</param>
        /// <param name="temp1Dir">AddJob: Temp directory 1 (required)</param>
        /// <param name="temp2Dir">AddJob: Temp directory 2 (optional)</param>
        /// <param name="plotDir">AddJob: Plot directory (required)</param>
        /// <param name="threadCount">AddJob: Number of threads (optional)</param>
        /// <param name="maxRamMB">AddJob: Max RAM usage (in MB) (optional, default: 4096)</param>
        /// <param name="bucketCount">AddJob: Number of buckets (optional, default: 128)</param>
        /// <param name="plotCount">AddJob: Number of plots to create (optional, default: 1)</param>
        /// <param name="jobId">RemoveJob: Job id to remove (required)</param>
        /// <param name="jobConfigFile">Job config file to use (optional, default: ChiaPlotter_Config.json)</param>
        /// <param name="jobStatusFile">Job status file to use (optional, default: ChiaPlotter_Status.json)</param>
        static async Task<int> Main(ActionEnum action = ActionEnum.None,
            //            bool daemon = false, bool verbose = false,
            string? chiaExe = null, int keyFingerprint = 0, string? farmerPK = null, string? poolPK = null, string? temp1Dir = null, string? temp2Dir = null, string? plotDir = null, int threadCount = 2, int maxRamMB = 4096, int bucketCount = 128, int plotCount = 1, //int plotParallelism = 1, string queueName = "default",
            int jobId = -1, //bool killRunning = false,
            string jobConfigFile = "ChiaPlotter_Config.json", string jobStatusFile = "ChiaPlotter_Status.json")
        {
            _jobsConfigFile = jobConfigFile;
            _jobsStatusFile = jobStatusFile;
            switch (action)
            {
                case ActionEnum.None:
                    return (int)await ShowHelp();

                case ActionEnum.Start:
                    return (int)await Start();

                case ActionEnum.ListJobs:
                    return (int)await ListJobs();

                case ActionEnum.RemoveJob when jobId == -1:
                    return (int)await ShowHelp($"Missing -{nameof(jobId)} parameter.");
                case ActionEnum.RemoveJob:
                    return (int)await RemoveJob(jobId);

                case ActionEnum.AddJob when keyFingerprint == 0 && (string.IsNullOrWhiteSpace(farmerPK) || string.IsNullOrWhiteSpace(poolPK)):
                    return (int)await ShowHelp($"Missing -{nameof(keyFingerprint)} parameter. Or missing -{nameof(farmerPK)} and -{nameof(poolPK)} parameters.");
                case ActionEnum.AddJob when string.IsNullOrWhiteSpace(temp1Dir):
                    return (int)await ShowHelp($"Missing -{nameof(temp1Dir)} parameter.");
                case ActionEnum.AddJob when string.IsNullOrWhiteSpace(chiaExe):
                    return (int)await ShowHelp($"Missing -{nameof(chiaExe)} parameter.");
                case ActionEnum.AddJob:
                    ChiaExe = chiaExe;
                    return (int)await AddJob(new Job()
                    {
                        Temp1Dir = temp1Dir,
                        Temp2Dir = temp2Dir,
                        PlotDir = plotDir,
                        ThreadCount = threadCount,
                        MaxRamMB = maxRamMB,
                        BucketCount = bucketCount,
                        //QueueName = queueName,
                        PlotCount = plotCount,
                        //PlotParallelism = plotParallelism
                    });
                default:
                    return (int)ExitCodes.UnknownAction;
            }
        }

        #region ShowHelp
        private static async Task<ExitCodes> ShowHelp()
        {
            Console.WriteLine("A started process will look for changes in config file.");
            await System.CommandLine.DragonFruit.CommandLine.ExecuteAssemblyAsync(typeof(AutoGeneratedProgram).Assembly, new string[] { "--help" }, "");
            return ExitCodes.Help;
        }

        private static async Task<ExitCodes> ShowHelp(string error)
        {
            Console.WriteLine($"Error: {error}");
            Console.WriteLine();
            return await ShowHelp();
        }
        #endregion

        #region Read / Write files
        #region JobConfig
        private static async Task<JobConfigFile> ReadJobConfig()
        {
            if (!File.Exists(_jobsConfigFile))
                return new();

            var jobConfig = System.Text.Json.JsonSerializer.Deserialize<JobConfigFile>(await File.ReadAllTextAsync(_jobsConfigFile));
            if (jobConfig!.Jobs == null)
                jobConfig.Jobs = new();
            return jobConfig;
        }
        private static async Task WriteJobConfig(JobConfigFile jobConfig)
        {
            // Increase version number for each write
            jobConfig.Version++;

            await File.WriteAllTextAsync(_jobsConfigFile, System.Text.Json.JsonSerializer.Serialize(jobConfig));
        }
        #endregion

        #region JobStatus
        private static async Task<JobStatusFile> ReadJobStatus()
        {
            if (!File.Exists(_jobsConfigFile))
                return new();

            var jobStatus = System.Text.Json.JsonSerializer.Deserialize<JobStatusFile>(await File.ReadAllTextAsync(_jobsStatusFile));
            if (jobStatus!.Jobs == null)
                jobStatus.Jobs = new();
            return jobStatus;
        }
        private static async Task WriteJobStatus(JobStatusFile jobStatus)
        {
            // Increase version number for each write
            jobStatus.Version++;

            await File.WriteAllTextAsync(_jobsStatusFile, System.Text.Json.JsonSerializer.Serialize(jobStatus));
        }
        #endregion
        #endregion

        #region Action: ListJobs
        private static async Task<ExitCodes> ListJobs()
        {
            // Job config file is required.
            // We expect this executable to be executed from weird places, so check if file exists + some debug info could be helpful.
            if (!File.Exists(_jobsConfigFile))
            {
                Console.WriteLine($"Error: Job config file \"{_jobsConfigFile}\" not found in current directory \"{Directory.GetCurrentDirectory()}\".");
                return ExitCodes.JobConfigFileNotFound;
            }
            // Read job config file
            var jobConfig = await ReadJobConfig();
            var jobStatusFile = await ReadJobStatus();

            Console.WriteLine($"Job config file: {_jobsConfigFile}");
            Console.WriteLine($"Job status file: {_jobsStatusFile}");
            Console.WriteLine($"Job count: {jobConfig.Jobs.Count}");
            Console.WriteLine();
            // nameof(Job.QueueName), nameof(Job.PlotParallelism),
            var table = new ConsoleTable("Id", "Status", "Done %", nameof(Job.PlotCount), nameof(Job.PlotDir), nameof(Job.Temp1Dir), nameof(Job.Temp2Dir), nameof(Job.BucketCount), nameof(Job.MaxRamMB), nameof(Job.ThreadCount));
            foreach (var jobKVP in jobConfig.Jobs.OrderBy(kvp => kvp.Key))
            {
                // Get status
                jobStatusFile.Jobs.TryGetValue(jobKVP.Key, out var jobStatus);
                if (jobStatus == null)
                    jobStatus = new();
                var job = jobKVP.Value;
                // Add to table
                // job.QueueName, job.PlotParallelism, 
                table.AddRow(jobKVP.Key, jobStatus.Status.ToString(), ((int)jobStatus.ProgressPercentage).ToString() + "%", job.PlotCount, job.PlotDir, job.Temp1Dir, job.Temp2Dir, job.BucketCount, job.MaxRamMB, job.ThreadCount);
            }
            // Write table to console
            table.Write(Format.Default);
            Console.WriteLine();

            return ExitCodes.Success;
        }
        #endregion

        #region Action: AddJob
        private static async Task<ExitCodes> AddJob(Job job)
        {
            // Read job config file
            var jobConfig = await ReadJobConfig();

            // Get next free id
            var nextId = ++jobConfig.NextId;

            // Add job
            if (jobConfig.Jobs == null)
                jobConfig.Jobs = new();
            jobConfig.Jobs.Add(nextId, job);

            // Write job config file
            await WriteJobConfig(jobConfig);

            return ExitCodes.Success;
        }
        #endregion

        #region Action: RemoveJob
        private static async Task<ExitCodes> RemoveJob(int jobId)
        {
            // Read job config file
            var jobConfig = await ReadJobConfig();

            // If job doesn't exist exit with error code
            if (!jobConfig.Jobs.ContainsKey(jobId))
                return ExitCodes.JobNotFound;

            // Remove job
            jobConfig.Jobs.Remove(jobId);

            // Write job config file
            await WriteJobConfig(jobConfig);

            return ExitCodes.Success;

        }
        #endregion

        #region Action: Start
        private static async Task<ExitCodes> Start()
        {
            // Job config file is required.
            // We expect this executable to be executed from weird places, so check if file exists + some debug info could be helpful.
            if (!File.Exists(_jobsConfigFile))
            {
                Console.WriteLine($"Error: Job config file \"{_jobsConfigFile}\" not found in current directory \"{Directory.GetCurrentDirectory()}\".");
                return ExitCodes.JobConfigFileNotFound;
            }

            using var appExitCTS = new CancellationTokenSource();
            var appExit = new ManualResetEvent(false);
            // For gracefull shutdown, trap unload event
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                appExitCTS.Cancel();
                appExit.WaitOne(1_000);
            };
            //Console.TreatControlCAsInput = true;
            Console.CancelKeyPress += (sender, args) =>
            {
                args.Cancel = true;
                appExitCTS.Cancel();
            };


            // No jobs? No problem. (But print a warning)
            if ((await ReadJobConfig()).Jobs.Count == 0)
                Console.WriteLine("Warning: No jobs found.");


            var currentJobConfig = new JobConfigFile();
            var jobStatus = await ReadJobStatus();
            var processControl = new ProcessControl(jobStatus);

            while (!appExitCTS.IsCancellationRequested)
            {
                // If config file has changed, read it again and update ProcessControl
                // TODO: Figure out efficient way to check file for changes (date+size? hash whole file?)
                if (true)
                {
                    // Read new config and update ProcessControl with delta
                    var jobConfig = await ReadJobConfig();
                    // Add jobs to ProcessControl that was added to config
                    foreach (var jobKVP in jobConfig.Jobs)
                    {
                        // New job, needs to be added
                        if (!currentJobConfig.Jobs.ContainsKey(jobKVP.Key))
                            processControl.AddJob(jobKVP.Key, jobKVP.Value);
                    }
                    // Remove jobs from ProcessControl that was removed from config
                    foreach (var jobKVP in currentJobConfig.Jobs)
                    {
                        if (!jobConfig.Jobs.ContainsKey(jobKVP.Key))
                            processControl.RemoveJob(jobKVP.Key);
                    }

                    currentJobConfig = jobConfig;
                }

                // Delay between file checks
                await Task.Delay(_checkConfigChangeMs, appExitCTS.Token);

                // Write status
                //jobStatus.Jobs.Clear();
                //foreach (var js in processControl.GetJobList())
                //    jobStatus.Jobs.Add(js.Id, js.Status);
                await WriteJobStatus(jobStatus);
            }
            appExit.Set();
            return ExitCodes.Success;
        }
        #endregion

    }
}
