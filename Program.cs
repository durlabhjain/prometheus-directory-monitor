using Prometheus;
using System.Net;
using System;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

var builder = new ConfigurationBuilder().AddJsonFile(
    "appsettings.json",
    optional: true,
    reloadOnChange: true
);

// Build the configuration
var config = builder.Build();

// Read a configuration value
var myValue = config.GetValue<string>("MyKey");
Console.WriteLine($"MyKey value is: {myValue}");

var directoryPaths = config.GetValue<string>("directory", "./").Split(",");
var port = config.GetValue<int>("port", 1234);
var intervalInSeconds = config.GetValue<int>("intervalInSeconds", 60);

Metrics.SuppressDefaultMetrics();

// Start the metrics server on your preferred port number.
using var server = new MetricServer(port: port);

try
{
    server.Start();
}
catch (HttpListenerException ex)
{
    Console.WriteLine($"Failed to start metric server: {ex.Message}");
    Console.WriteLine(
        "You may need to grant permissions to your user account if not running as Administrator:"
    );
    Console.WriteLine($"netsh http add urlacl url=http://+:${port}/metrics user=DOMAIN\\user");
    return;
}

var gauge = Metrics.CreateGauge(
    "directory_size_bytes",
    "The size of the specified directory in bytes",
    new GaugeConfiguration { LabelNames = new[] { "directory_path" } }
);

_ = Task.Run(
    async delegate
    {
        while (true)
        {
            foreach (var directoryPath in directoryPaths)
            {
                var sizeInBytes = GetDirectorySize(directoryPath);
                gauge.WithLabels(directoryPath).Set(sizeInBytes);
            }
            await Task.Delay(TimeSpan.FromSeconds(intervalInSeconds));
        }
    }
);

// Metrics published in this sample:
// * built-in process metrics giving basic information about the .NET runtime (enabled by default)
// * metrics from .NET Event Counters (enabled by default, updated every 10 seconds)
// * metrics from .NET Meters (enabled by default)
// * the custom sample counter defined above
Console.WriteLine($"Open http://localhost:{port}/metrics in a web browser.");
Console.WriteLine("Press enter to exit.");
Console.ReadLine();

static long GetDirectorySize(string directoryPath)
{
    var sw = new Stopwatch();
    sw.Start();
    Console.WriteLine($"Calculating directory size: {directoryPath}");
    var directory = new DirectoryInfo(directoryPath);
    if (!directory.Exists)
    {
        throw new ArgumentException($"Directory does not exist: {directoryPath}");
    }

    long size = 0;
    var files = directory.EnumerateFiles("*", SearchOption.AllDirectories);
    foreach (var file in files)
    {
        size += file.Length;
    }
    sw.Stop();
    Console.WriteLine(
        $"Time taken to calculate directory size: {sw.ElapsedMilliseconds} ms - {size}"
    );
    return size;
}
