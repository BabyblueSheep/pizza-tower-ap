using ArchipelagoPizzaTower.Patcher.Commands;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using System.Reflection;

namespace ArchipelagoPizzaTower.Patcher
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var app = new CommandApp();
            AnsiConsole.Write(new CanvasImage("Assets/HelpImage.png"));
            AnsiConsole.Write(new Markup($"Archipelago Pizza Tower Patcher [yellow]{typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "?"}[/]"));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();

            app.Configure(config =>
            {
                config.AddCommand<PatchCommand>("patch");
            });
            app.Run(args);
        }
    }
}
