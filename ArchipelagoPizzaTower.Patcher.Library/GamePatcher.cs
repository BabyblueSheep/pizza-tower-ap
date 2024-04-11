using ArchipelagoPizzaTower.Patcher.Library;
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

        internal static UndertaleData? Data { get; set; }

        public static void Patch(string folderpath)
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
            PatchGame(folderpath);
            MessageHandler($"Game patched");

            MessageHandler($"Patching lang files");
            PatchLang(folderpath);
            MessageHandler($"Lang files patched");
            MessageHandler($"All done!");
        }

        // https://github.com/randovania/YAMS/tree/main
        public static void PatchGame(string folderpath)
        {
            if (Data is null)
                throw new ArgumentNullException("Mod data is not loaded");
            string dataPath = folderpath + @"\data.win";
            
            #region Import all custom sprites
            Dictionary<string, int> nameToPageItemDict = new();
            const int pageDimension = 1024;
            int lastUsedX = 0, lastUsedY = 0, currentShelfHeight = 0;
            Image<Rgba32> texturePageImage = new(pageDimension, pageDimension);
            UndertaleEmbeddedTexture? texturePage = new();
            texturePage.TextureHeight = texturePage.TextureWidth = pageDimension;
            Data.EmbeddedTextures.Add(texturePage);

            void AddAllSpritesFromDir(string dirPath)
            {
                foreach (string subDir in Directory.GetDirectories(dirPath))
                {
                    AddAllSpritesFromDir(subDir);
                }

                foreach (string filePath in Directory.GetFiles(dirPath))
                {
                    string extension = new FileInfo(filePath).Extension;
                    if (string.IsNullOrWhiteSpace(extension) || extension == ".md" || extension == ".txt") 
                        continue;

                    Image sprite = Image.Load(filePath);
                    currentShelfHeight = Math.Max(currentShelfHeight, sprite.Height);
                    if (lastUsedX + sprite.Width > pageDimension)
                    {
                        lastUsedX = 0;
                        lastUsedY += currentShelfHeight;
                        currentShelfHeight = sprite.Height + 1; // One pixel padding

                        if (sprite.Width > pageDimension)
                        {
                            throw new NotSupportedException($"Sprite ({filePath}) is bigger than the max size of a {pageDimension} texture page");
                        }
                    }
                    if (lastUsedY + sprite.Height > pageDimension) throw new NotSupportedException($"{pageDimension} texture page is already full");

                    int xCoord = lastUsedX;
                    int yCoord = lastUsedY;
                    texturePageImage.Mutate(i => i.DrawImage(sprite, new Point(xCoord, yCoord), 1));
                    UndertaleTexturePageItem pageItem = new UndertaleTexturePageItem();
                    pageItem.SourceX = (ushort)xCoord;
                    pageItem.SourceY = (ushort)yCoord;
                    pageItem.SourceWidth = pageItem.TargetWidth = pageItem.BoundingWidth = (ushort)sprite.Width;
                    pageItem.SourceHeight = pageItem.TargetHeight = pageItem.BoundingHeight = (ushort)sprite.Height;
                    pageItem.TexturePage = texturePage;
                    Data.TexturePageItems.Add(pageItem);
                    lastUsedX += sprite.Width + 1; // One pixel padding
                    nameToPageItemDict.Add(Path.GetFileNameWithoutExtension(filePath), Data.TexturePageItems.Count - 1);

                }
            }
            AddAllSpritesFromDir(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/Textures");
            using (MemoryStream ms = new MemoryStream())
            {
                texturePageImage.Save(ms, PngFormat.Instance);
                texturePage.TextureData = new UndertaleEmbeddedTexture.TexData { TextureBlob = ms.ToArray() };
            }

            void ReplaceTexture(string textureName, string fileName) => Data.Sprites.ByName(textureName).Textures[0].Texture = Data.TexturePageItems[nameToPageItemDict[fileName]];
            void AddTexture(string textureName, uint width, uint height, string fileName)
            {
                UndertaleSprite sprite = new()
                {
                    Name = Data.Strings.MakeString(textureName),
                    Height = height,
                    Width = width,
                    MarginRight = (int)(height - 1),
                    MarginBottom = (int)(width - 1),
                    OriginX = 0,
                    OriginY = 0
                };
                UndertaleSprite.TextureEntry texture = new();
                texture.Texture = Data.TexturePageItems[nameToPageItemDict[fileName]];
                sprite.Textures.Add(texture);
                Data.Sprites.Add(sprite);
            }
            #endregion
            MessageHandler($"Sprites imported");
            #region Add custom skins
            ReplaceTexture("spr_peppalette", "spr_peppalette_0");
            ReplaceTexture("spr_ratmountpalette", "spr_ratmountpalette_0");
            ReplaceTexture("spr_noisepalette", "spr_noisepalette_0");
            ReplaceTexture("spr_noisepalette_rage", "spr_noisepalette_rage_0");

            AddTexture("spr_appattern1", 19, 18, "spr_appattern1_0");


            Data.Code.ByName("gml_Object_obj_palettedresser_Create_0").AppendGML(@"
                array_push(player_palettes[0], [""ap_blue"", 1, 16])
                array_push(player_palettes[1], [""ap_blue"", 1, 29])
                array_push(player_palettes[0], [""ap_color"", 1, 12, asset_get_index(""spr_appattern1"")])
                array_push(player_palettes[1], [""ap_color"", 1, 12, asset_get_index(""spr_appattern1"")])
            ", Data);
            #endregion
            MessageHandler($"Custom skins added");

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
