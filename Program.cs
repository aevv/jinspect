using System.Reflection;
using JInspect.Commands;
using Spectre.Console.Cli;

var version = Assembly.GetExecutingAssembly()
    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
    ?? "unknown";

var plusIndex = version.IndexOf('+');
if (plusIndex >= 0) version = version[..plusIndex];

var app = new CommandApp<InspectCommand>();

app.Configure(config =>
{
    config.SetApplicationName("jinspect");
    config.SetApplicationVersion(version);
});

return app.Run(args);
