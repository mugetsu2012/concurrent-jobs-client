using ConcurrentJobs.Client.Core.Clients;
using ConcurrentJobs.Client.Core.Services;
using ConcurrentJobs.Client.Services;
using ConcurrentJobs.Client.Services.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace ConcurrentJobs.Client
{
    internal class Program
    {
        private static IServiceProvider? _serviceProvider;
        private static IConfiguration? _configuration;

        public static async Task Main()
        {
            // Build configuration
            _configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(_configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            try
            {
                // Set up dependency injection
                var services = new ServiceCollection();
                ConfigureServices(services);
                _serviceProvider = services.BuildServiceProvider();

                // Get the menu service
                var menuService = _serviceProvider.GetRequiredService<IMenuService>();
                var logger = _serviceProvider.GetRequiredService<ILogger<Program>>();

                logger.LogInformation("Starting Concurrent Jobs Client...");

                // Display welcome message
                Console.Clear();
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("===========================================");
                Console.WriteLine("      CONCURRENT JOBS CLIENT");
                Console.WriteLine("===========================================");
                Console.ResetColor();

                // Run the main menu in a loop
                await menuService.RunMainMenuAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An unhandled exception occurred");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An unexpected error occurred: " + ex.Message);
                Console.ResetColor();
            }
            finally
            {
                // Clean up services
                if (_serviceProvider is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            // Add configuration
            services.AddSingleton(_configuration!);

            // Configure logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            // Get server URL from configuration
            var serverUrl = _configuration!.GetConnectionString("ServerUrl")
                ?? throw new InvalidOperationException("Server URL not configured in appsettings.json");

            // Add HttpClient
            services.AddHttpClient<IJobApiClient, JobApiClient>(client =>
            {
                client.BaseAddress = new Uri(serverUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register services
            services.AddSingleton<IMenuService, MenuService>();
            services.AddTransient<IJobService, JobService>();
        }
    }
}
