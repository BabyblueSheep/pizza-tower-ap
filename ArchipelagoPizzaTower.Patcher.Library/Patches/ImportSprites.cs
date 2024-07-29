using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using ArchpelagoPizzaTower.Patcher.Library;
using SixLabors.ImageSharp.Processing;

namespace ArchipelagoPizzaTower.Patcher.Library.Patches
{
    internal static partial class Patches
    {
        internal static void ImportSprites()
        {
            GamePatcher.NameToPageItem = new();
            const int pageDimension = 2048;
            int lastUsedX = 0, lastUsedY = 0, currentShelfHeight = 0;
            Image<Rgba32> texturePageImage = new(pageDimension, pageDimension);
            UndertaleEmbeddedTexture? texturePage = new();
            texturePage.TextureHeight = texturePage.TextureWidth = pageDimension;
            GamePatcher.Data.EmbeddedTextures.Add(texturePage);

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
                    UndertaleTexturePageItem pageItem = new();
                    pageItem.SourceX = (ushort)xCoord;
                    pageItem.SourceY = (ushort)yCoord;
                    pageItem.SourceWidth = pageItem.TargetWidth = pageItem.BoundingWidth = (ushort)sprite.Width;
                    pageItem.SourceHeight = pageItem.TargetHeight = pageItem.BoundingHeight = (ushort)sprite.Height;
                    pageItem.TexturePage = texturePage;
                    GamePatcher.Data.TexturePageItems.Add(pageItem);
                    lastUsedX += sprite.Width + 1; // One pixel padding
                    GamePatcher.NameToPageItem.Add(Path.GetFileNameWithoutExtension(filePath), GamePatcher.Data.TexturePageItems.Count - 1);

                }
            }
            AddAllSpritesFromDir(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/Textures");
            using (MemoryStream ms = new MemoryStream())
            {
                texturePageImage.Save(ms, PngFormat.Instance);
                texturePage.TextureData = new UndertaleEmbeddedTexture.TexData { TextureBlob = ms.ToArray() };
            }

            GamePatcher.MessageHandler($"Sprites imported");
        }
    }
}
