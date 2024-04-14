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

            void AddScript(string scriptName, string codeName, string code)
            {
                Data.Scripts.Add(new UndertaleScript { Name = Data.Strings.MakeString(scriptName), Code = AddCode(codeName, code) });
            }
            UndertaleCode AddCode(string codeName, string code)
            {
                UndertaleCode codeObject = new UndertaleCode();
                codeObject.Name = Data.Strings.MakeString(codeName);
                codeObject.ReplaceGML(code, Data);
                Data.Code.Add(codeObject);
                return codeObject;
            }

            UndertaleGameObject AddObject(string name)
            {
                UndertaleGameObject gameObject = new()
                {
                    Name = Data.Strings.MakeString(name),
                };
                Data.GameObjects.Add(gameObject);
                return gameObject;
            }
            void AddEvent(UndertaleGameObject gameObject, int eventType, uint eventSubType, string codeName, string code)
            {
                UndertaleCode script = AddCode(codeName, code);
                UndertalePointerList<UndertaleGameObject.Event> events = gameObject.Events[eventType];
                UndertaleGameObject.EventAction gameEventAction = new()
                {
                    CodeId = script
                };
                UndertaleGameObject.Event gameEvent = new()
                {
                    EventSubtype = eventSubType
                };
                gameEvent.Actions.Add(gameEventAction);
                events.Add(gameEvent);
            }

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

            MessageHandler($"Sprites imported");
            #endregion

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

            MessageHandler($"Custom skins added");
            #endregion

            #region Custom input typing
            UndertaleGameObject customInput = AddObject("obj_custominput");
            AddEvent(customInput, 0, 0, "gml_Object_obj_custominput_Create_0", @"
                enabled_chars = @""ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 1234567890""
                blinking = true
                blink_speed = 15
                alarm[0] = blink_speed
                text = """"
                keyboard_string = """"
            ");
            AddEvent(customInput, 2, 0, "gml_Object_obj_custominput_Alarm_0", @"
                blinking = !blinking
                alarm[0] = blink_speed
            ");
            AddEvent(customInput, 9, 1, "gml_Object_obj_custominput_KeyPress_1", @"
                blinking = true
                blink_speed = 15

                if keyboard_check(vk_control)
                {
                    if (keyboard_check(ord(""V"")))
                        text += clipboard_get_text();
                }
                else
                {
                    if keyboard_check(vk_backspace)
                    {
                        text = string_copy(text, 1, string_length(text) - 1);
                    }
                    else
                    {
                        text += keyboard_string
                    }
                }

                for (var i = 1; i < string_length(text); i++)
                {
                    var letter = string_char_at(text, i)
                    if string_pos(letter, enabled_chars) == 0
                    {
                        text = string_replace_all(text, letter, """");
                    }
                }
                text = string_copy(text, 1, 64);
                keyboard_string = """"
            ");

            #endregion

            #region Main menu addition
            Data.Code.ByName("gml_Object_obj_mainmenu_Create_0").AppendGML(@"
                connectselect = 0
                is_typing = false
                ap_ip = """"
                ap_port = """"
                ap_name = """"
                ap_password = """"
                text_input = instance_create(0, 0, obj_custominput)

                did_tip = false
            ", Data);


            Data.Code.ByName("gml_Object_obj_mainmenu_Step_0").AppendGML(@"
                if (state == 0 << 0)
                {
                    if (keyboard_check_pressed(vk_f1))
                    {
                        connectselect = 0
                        state = 1812 << 0
				        switch currentselect
				        {
					        case 0:
						        sprite_index = spr_titlepep_left
						        break
					        case 1:
						        sprite_index = spr_titlepep_middle
						        break
					        case 2:
						        sprite_index = spr_titlepep_right
						        break
                        }
                    }
                }
                else if (state == 1812 << 0)
                {
                    if (!did_tip)
                    {
                        with (create_transformation_tip(lang_get_value(""menu_apmenutip"")))
                        {
                            alarm[1] = 330;
                        }
                        did_tip = true;
                    }

                    if (is_typing)
                    {
                        switch(connectselect)
                        {
                            case 0:
                                ap_ip = text_input.text
                                break
                            case 1:
                                ap_port = text_input.text
                                break
                            case 2:
                                ap_name = text_input.text
                                break
                            case 3:
                                ap_password = text_input.text
                                break
                        }
                        
                        if (keyboard_check_pressed(vk_enter))
                        {
                            is_typing = false
                        }
                    }
                    else
                    {
                        connectselect += key_down2 - key_up2
                        connectselect = clamp(connectselect, 0, 5)
                    
                        if keyboard_check_pressed(vk_enter)
                        {
                            keyboard_string = """"
                            switch(connectselect)
                            {
                                case 0:
                                    is_typing = true
                                    text_input.text = ap_ip
                                    break
                                case 1:
                                    is_typing = true
                                    text_input.text  = ap_port
                                    break
                                case 2:
                                    is_typing = true
                                    text_input.text  = ap_name
                                    break
                                case 3:
                                    is_typing = true
                                    text_input.text  = ap_password
                                    break
                                case 5:
                                    state = 0 << 0
                                    break
                            }
                        }
                    }
                }
            ", Data);

            AddTexture("spr_connectap", 104, 66, "spr_connectap_0");
            Data.Code.ByName("gml_Object_obj_mainmenu_Draw_0").AppendGML(@"
                draw_set_alpha(extrauialpha)
                lang_draw_sprite(asset_get_index(""spr_connectap""), 0, 400, 5)
                scr_draw_text_arr(440, 40, scr_compile_icon_text(""[y]""), c_white, extrauialpha)
                draw_set_alpha(1)
            ", Data);

            Data.Code.ByName("gml_Object_obj_mainmenu_Draw_64").AppendGML(@"
                
                if (state == 1812 << 0)
                {
                    draw_set_alpha(0.5)
                    draw_rectangle_color(0, 0, room_width, room_height, c_black, c_black, c_black, c_black, 0)
                    draw_set_alpha(1)
                    draw_set_font(lang_get_font(""bigfont""))
                    draw_set_halign(fa_center)
                    draw_set_valign(fa_middle)
                    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) - 150, lang_get_value(""menu_archipelago""), c_white, c_white, c_white, c_white, 1)

                    c0 = (connectselect == 0) ? c_white : c_gray;
                    c1 = (connectselect == 1) ? c_white : c_gray;
                    c2 = (connectselect == 2) ? c_white : c_gray;
                    c3 = (connectselect == 3) ? c_white : c_gray;
                    c4 = (connectselect == 4) ? c_white : c_gray;
                    c5 = (connectselect == 5) ? c_white : c_gray;

                    draw_set_font(lang_get_font(""creditsfont""))
                    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 0,embed_value_string(lang_get_value(""menu_apip""), [ap_ip]), c0, c0, c0, c0, 1)
                    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 30, embed_value_string(lang_get_value(""menu_apport""), [ap_port]), c1, c1, c1, c1, 1)
                    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 60, embed_value_string(lang_get_value(""menu_apname""), [ap_name]), c2, c2, c2, c2, 1)
                    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 90, embed_value_string(lang_get_value(""menu_appass""), [ap_password]), c3, c3, c3, c3, 1)
                    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 120, lang_get_value(""menu_apconnect""), c4, c4, c4, c4, 1)
                    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 150, lang_get_value(""menu_apleave""), c5, c5, c5, c5, 1)
                }
                draw_set_font(lang_get_font(""creditsfont""));
                
            ", Data);

            MessageHandler($"Added new submenu to main menu");
            #endregion

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
