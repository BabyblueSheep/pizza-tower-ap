using ArchpelagoPizzaTower.Patcher.Library;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ArchipelagoPizzaTower.Patcher.Commands.PatchCommand;

namespace ArchipelagoPizzaTower.Patcher.Commands
{
    [Description("Patches Pizza Tower to have base randomizer functionality, without online support.")]
    public sealed class PatchCommand : Command<PatchSettings>
    {
        public sealed class PatchSettings : CommandSettings
        {
            [CommandArgument(0, "[FOLDERPATH]")]
            [Description("The location path of the Pizza Tower folder.")]
            public string FolderPath { get; set; }
        }

        public override int Execute(CommandContext context, PatchSettings settings)
        {
            if (File.Exists(settings.FolderPath) || Directory.Exists(settings.FolderPath))
            {
                try
                {
                    GamePatcher.MessageHandler = AnsiConsole.WriteLine;
                    GamePatcher.Patch(Path.GetDirectoryName(settings.FolderPath));
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
            }
            else
            {
                AnsiConsole.WriteException(new FileNotFoundException("No valid folder found!"));
            }
            return 0;
        }
    }
}
