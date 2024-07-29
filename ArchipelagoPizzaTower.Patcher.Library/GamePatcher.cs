using ArchipelagoPizzaTower.Patcher.Library;
using ArchipelagoPizzaTower.Patcher.Library.Patches;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ArchpelagoPizzaTower.Patcher.Library
{
    public static class GamePatcher
    {
        public delegate void MessageHandlerDelegate(string message);
        public static MessageHandlerDelegate MessageHandler;

        internal static UndertaleData Data { get; set; }
        internal static Dictionary<string, int> NameToPageItem { get; set; }

        public static void Patch(string folderpath, bool excludeSkins = false)
        {
            Data = new UndertaleData();
            string dataPath = folderpath + @"\data.win";
            MessageHandler($"File found: {dataPath}\n");

            using (FileStream fs = new FileInfo(dataPath).OpenRead())
            {
                Data = UndertaleIO.Read(fs, messageHandler: message => MessageHandler($"{message}"));
            }
            MessageHandler("Data read");
            string gameName = Data.GeneralInfo.DisplayName.Content;
            MessageHandler($"Game discovered: {gameName}");
            if (gameName.ToLower() != "pizza tower")
            {
                throw new NotSupportedException("Mod is only compatible with Pizza Tower");
            }
            if (File.Exists(folderpath + @"\steam_api64.dll"))
                File.Delete(folderpath + @"\steam_api64.dll");
            if (File.Exists(folderpath + @"\Steamworks_x64.dll"))
                File.Delete(folderpath + @"\Steamworks_x64.dll");
            MessageHandler($"DRM removed"); // no goddamn clue why the patches dont work if the game has a connection to steam lol

            MessageHandler($"Patching game");
            PatchGame(folderpath, excludeSkins);
            MessageHandler($"Game patched");

            MessageHandler($"Patching lang files");
            PatchLang(folderpath);
            MessageHandler($"Lang files patched");
            MessageHandler($"All done!");
        }

        // https://github.com/randovania/YAMS/blob/main/YAMS-LIB/Program.cs
        public static void PatchGame(string folderpath, bool excludeSkins = false)
        {
            if (Data is null)
                throw new ArgumentNullException("Mod data is not loaded");
            string dataPath = folderpath + @"\data.win";

            UndertaleString strName = Data.Strings.MakeString("PizzaTower_AP");
            Data.GeneralInfo.FileName = strName;
            Data.GeneralInfo.Name = strName;

            Patches.ImportSprites();

            if (!excludeSkins)
                Patches.AddCustomSkins();

            Patches.AddExtension(folderpath);
            Patches.AddCustomInput();
            Patches.ModifyMainMenu();
            Patches.AddChatMenu();
            Patches.ModifyPauseMenu();

            using (FileStream fs = new FileInfo(dataPath).OpenWrite())
            {
                UndertaleIO.Write(fs, Data, messageHandler: message => MessageHandler($"{message}"));
            }
        }

        public static void PatchLang(string folderpath)
        {
            string origLangPath = folderpath + @"\lang\english.txt";
            string extraLangPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/english.txt";

            using (StreamReader reader = new(extraLangPath))
            using (StreamWriter writer = new(origLangPath, true))
            {
                writer.WriteLine();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    writer.WriteLine(line);
                }
            }
        }
    }
}
