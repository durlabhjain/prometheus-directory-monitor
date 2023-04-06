# Purpose

Demo prometheus exporter that exposes the size of directory/ directories as Prometheus metrics

# Configuration

Settings available:

<pre>
{
    "directory": "./",
    "port": 8000,
    "intervalInSeconds": 60
}
</pre>

1. directory: a comma separated list of directories to monitor
2. port: port to run service on
3. intervalInSeconds: refresh interval in seconds to recalculate the size

## File name

For C# - use appsettings.json
For NodeJS - use config.local.json

# Note:

Calculating the directory size is time taking/ resource intensive task

# TODO:

- [ ] Windows Service
- [ ] Linux Service