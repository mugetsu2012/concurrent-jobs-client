using ConcurrentJobs.Client.Core.Services;
using ConcurrentJobs.Client.Services.Clients;
using Microsoft.Extensions.Logging;
using System.Text;

namespace ConcurrentJobs.Client.Services
{
    internal class MenuService(IJobService jobService, ILogger<MenuService> logger) : IMenuService
    {
        private readonly IJobService _jobService = jobService;
        private readonly ILogger<MenuService> _logger = logger;
        private readonly List<Guid> _activeJobs = [];

        public async Task RunMainMenuAsync()
        {
            bool exit = false;

            while (!exit)
            {
                Console.WriteLine("\nMAIN MENU");
                Console.WriteLine("1. Start a new job");
                Console.WriteLine("2. Check job status");
                Console.WriteLine("3. Cancel a job");
                Console.WriteLine("4. Monitor a job (real-time updates)");
                Console.WriteLine("5. List active jobs");
                Console.WriteLine("0. Exit");

                Console.Write("\nSelect an option: ");
                var input = Console.ReadLine();

                try
                {
                    switch (input)
                    {
                        case "1":
                            await StartJobMenuAsync();
                            break;
                        case "2":
                            await CheckJobStatusMenuAsync();
                            break;
                        case "3":
                            await CancelJobMenuAsync();
                            break;
                        case "4":
                            await MonitorJobMenuAsync();
                            break;
                        case "5":
                            await ListActiveJobsAsync();
                            break;
                        case "0":
                            exit = true;
                            Console.WriteLine("Exiting application. Goodbye!");
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Invalid option. Please try again.");
                            Console.ResetColor();
                            break;
                    }
                }
                catch (ApplicationException ex)
                {
                    DisplayError(ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in menu operation");
                    DisplayError($"Unexpected error: {ex.Message}");
                }
            }
        }

        private async Task StartJobMenuAsync()
        {
            Console.Clear();
            Console.WriteLine("=== START A NEW JOB ===");

            Console.Write("Enter job type: ");
            var jobType = Console.ReadLine() ?? string.Empty;

            Console.Write("Enter job name: ");
            var jobName = Console.ReadLine() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(jobType) || string.IsNullOrWhiteSpace(jobName))
            {
                DisplayError("Job type and name cannot be empty");
                return;
            }

            Console.WriteLine("\nStarting job...");
            try
            {
                var jobId = await _jobService.StartJobAsync(jobType, jobName);
                _activeJobs.Add(jobId); // Track this job

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Job started successfully with ID: {jobId}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to start job: {ex.Message}");
            }
        }

        private async Task CheckJobStatusMenuAsync()
        {
            Console.Clear();
            Console.WriteLine("=== CHECK JOB STATUS ===");

            var jobId = PromptForJobId();
            if (jobId == Guid.Empty)
            {
                return;
            }

            Console.WriteLine("\nChecking job status...");
            try
            {
                var status = await _jobService.GetJobStatusAsync(jobId);

                Console.WriteLine("\nJob Details:");
                Console.WriteLine($"ID: {status.JobId}");
                Console.WriteLine($"Type: {status.JobType}");
                Console.WriteLine($"Name: {status.JobName}");

                // Display status with color
                Console.Write("Status: ");
                Console.ForegroundColor = status.Status switch
                {
                    "Running" => ConsoleColor.Yellow,
                    "Completed" => ConsoleColor.Green,
                    "Cancelled" => ConsoleColor.Red,
                    "Failed" => ConsoleColor.DarkRed,
                    _ => ConsoleColor.White,
                };
                Console.WriteLine(status.Status);
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                DisplayError($"Failed to get job status: {ex.Message}");
            }
        }

        private async Task CancelJobMenuAsync()
        {
            Console.Clear();
            Console.WriteLine("=== CANCEL A JOB ===");

            var jobId = PromptForJobId();
            if (jobId == Guid.Empty)
            {
                return;
            }

            Console.WriteLine("\nCancelling job...");
            try
            {
                var result = await _jobService.CancelJobAsync(jobId);

                if (result)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Job with ID {jobId} has been cancelled successfully");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Could not cancel job with ID {jobId}. It may be already completed or not found.");
                    Console.ResetColor();

                    // Try to get current status if possible
                    try
                    {
                        var status = await _jobService.GetJobStatusAsync(jobId);
                        Console.WriteLine($"Current job status: {status.Status}");
                    }
                    catch (JobNotFoundException)
                    {
                        Console.WriteLine("The job was not found on the server.");
                    }
                    catch
                    {
                        // Ignore any errors when trying to get status after cancellation failure
                    }
                }
            }
            catch (ApplicationException ex)
            {
                DisplayError($"Failed to cancel job: {ex.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling job {JobId}", jobId);
                DisplayError($"Unexpected error while cancelling job: {ex.Message}");
            }
        }

        private async Task MonitorJobMenuAsync()
        {
            Console.Clear();
            Console.WriteLine("=== MONITOR A JOB ===");

            var jobId = PromptForJobId();
            if (jobId == Guid.Empty)
            {
                return;
            }

            Console.WriteLine("\nMonitoring job (press any key to stop)...");
            Console.WriteLine();

            using var cts = new CancellationTokenSource();

            // Create a TaskCompletionSource to signal when a key is pressed
            var keyPressTcs = new TaskCompletionSource<bool>();

            // Start a task to listen for key press
            var keyPressListenerTask = Task.Run(() => {
                Console.ReadKey(true);
                try
                {
                    // Only cancel if the token source hasn't been disposed
                    if (!cts.IsCancellationRequested)
                    {
                        cts.Cancel();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // Safely ignore this exception - the monitoring already finished
                }
                keyPressTcs.TrySetResult(true);
            });

            try
            {
                var monitorTask = _jobService.MonitorJobAsync(jobId, cts.Token);

                // Wait for either the job to complete or user to press a key
                await Task.WhenAny(monitorTask, keyPressTcs.Task);

                if (!monitorTask.IsCompleted)
                {
                    Console.WriteLine("\nMonitoring stopped by user.");
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("\nMonitoring cancelled.");
            }
            catch (Exception ex)
            {
                DisplayError($"Error while monitoring job: {ex.Message}");
            }
        }

        private async Task ListActiveJobsAsync()
        {
            Console.Clear();
            Console.WriteLine("=== ACTIVE JOBS ===");

            if (_activeJobs.Count == 0)
            {
                Console.WriteLine("No active jobs found in this session.");
                return;
            }

            Console.WriteLine("Jobs started in this session:");

            var table = new StringBuilder();
            table.AppendLine("Job ID                               | Status");
            table.AppendLine("-------------------------------------|---------------");

            foreach (var jobId in _activeJobs)
            {
                try
                {
                    var status = await _jobService.GetJobStatusAsync(jobId);
                    table.AppendLine($"{jobId} | {status.Status}");
                }
                catch
                {
                    table.AppendLine($"{jobId} | Error: Could not retrieve status");
                }
            }

            Console.WriteLine(table.ToString());
        }

        private Guid PromptForJobId()
        {
            // If we have active jobs, list them for convenience
            if (_activeJobs.Count > 0)
            {
                Console.WriteLine("Recent jobs from this session:");

                for (int i = 0; i < _activeJobs.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {_activeJobs[i]}");
                }

                Console.WriteLine("\nYou can enter a job number from the list or paste a full job ID.");
            }

            Console.Write("Enter job ID: ");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(input))
            {
                DisplayError("Job ID cannot be empty");
                return Guid.Empty;
            }

            // Check if the input is a number referencing our list
            if (int.TryParse(input, out int index) && index > 0 && index <= _activeJobs.Count)
            {
                return _activeJobs[index - 1];
            }

            // Try to parse as a Guid
            if (Guid.TryParse(input, out Guid jobId))
            {
                return jobId;
            }

            DisplayError("Invalid job ID format");
            return Guid.Empty;
        }

        private static void DisplayError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {message}");
            Console.ResetColor();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
