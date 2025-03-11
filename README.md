# Concurrent Jobs Client

A command-line interface (CLI) application for interacting with the Concurrent Jobs Server. This client provides a user-friendly interface to start, monitor, and cancel asynchronous long-running jobs.

## Features

- **Interactive CLI Interface**: Menu-driven interface with color-coded output for better readability
- **Complete API Integration**: Interfaces with all server endpoints (create, check status, cancel jobs)
- **Job Monitoring**: Real-time monitoring of job status with the ability to cancel monitoring
- **Session History**: Tracks jobs started during the current session for easy reference
- **Robust Error Handling**: Comprehensive error handling with user-friendly messages
- **Configurable**: Uses appsettings.json for server URL and logging configuration
- **Cross-Platform**: Works on Windows, macOS, and Linux

## Architecture

The application follows clean architecture principles with a clear separation of concerns:

- **Models**: Data transfer objects for API requests and responses
- **Services**: Business logic and API client implementation
- **UI Layer**: Console-based user interface handling

Key components:
- `JobApiClient`: Handles HTTP communication with the server
- `JobService`: Provides higher-level job operations
- `MenuService`: Implements the console UI and user interactions

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Network access to the Concurrent Jobs Server

### Configuration

The application uses `appsettings.json` for configuration:

```json
{
  "ConnectionStrings": {
    "ServerUrl": "http://localhost:5051/api/"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System.Net.Http.HttpClient": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/client-.log",
          "rollingInterval": "Day",
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": [ "FromLogContext" ]
  }
}
```

Modify the `ServerUrl` to point to your Concurrent Jobs Server instance.

### Installation

1. Clone the repository
   ```
   git clone https://github.com/mugetsu2012/concurrent-jobs-client.git
   cd concurrent-jobs-client
   ```

2. Build the application
   ```
   dotnet build
   ```

3. Run the application
   ```
   dotnet run
   ```

## Usage

When you start the application, you'll be presented with a main menu:

```
===========================================
      CONCURRENT JOBS CLIENT
===========================================

MAIN MENU
1. Start a new job
2. Check job status
3. Cancel a job
4. Monitor a job (real-time updates)
5. List active jobs
0. Exit

Select an option:
```

### Starting a Job

Select option 1 and follow the prompts to enter a job type and name. The server will return a job ID that you can use to check status or cancel the job later.

### Checking Job Status

Select option 2 and enter a job ID. The client will display the current status of the job (Running, Completed, Cancelled, or Failed).

### Cancelling a Job

Select option 3 and enter a job ID. The client will attempt to cancel the job and report the result.

### Monitoring a Job

Select option 4 and enter a job ID. The client will poll the server for status updates until the job completes or you press a key to stop monitoring.

### Listing Active Jobs

Select option 5 to see a list of all jobs started during the current session, along with their current status.

## Logging

The application logs to both the console and a file:

- Console logs show key operations with timestamps
- File logs are stored in the `logs` directory with daily rotation
- Log level can be configured in `appsettings.json`

## Dependencies

- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Http
- Microsoft.Extensions.Logging
- Serilog