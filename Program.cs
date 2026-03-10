using JInspect.Commands;
using Spectre.Console.Cli;

var app = new CommandApp<InspectCommand>();

app.Configure(config =>
{
    config.SetApplicationName("jinspect");
});

return app.Run(args);
