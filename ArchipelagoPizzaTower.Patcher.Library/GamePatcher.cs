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

            ImportSprites();

            if (!excludeSkins)
                AddCustomSkins();

            AddExtension(folderpath);

            AddCustomInput();

            ModifyMainMenu();

            AddChatMenu();

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

        private static void ImportSprites()
        {
            NameToPageItem = new();
            const int pageDimension = 2048;
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
                    UndertaleTexturePageItem pageItem = new();
                    pageItem.SourceX = (ushort)xCoord;
                    pageItem.SourceY = (ushort)yCoord;
                    pageItem.SourceWidth = pageItem.TargetWidth = pageItem.BoundingWidth = (ushort)sprite.Width;
                    pageItem.SourceHeight = pageItem.TargetHeight = pageItem.BoundingHeight = (ushort)sprite.Height;
                    pageItem.TexturePage = texturePage;
                    Data.TexturePageItems.Add(pageItem);
                    lastUsedX += sprite.Width + 1; // One pixel padding
                    NameToPageItem.Add(Path.GetFileNameWithoutExtension(filePath), Data.TexturePageItems.Count - 1);

                }
            }
            AddAllSpritesFromDir(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/Textures");
            using (MemoryStream ms = new MemoryStream())
            {
                texturePageImage.Save(ms, PngFormat.Instance);
                texturePage.TextureData = new UndertaleEmbeddedTexture.TexData { TextureBlob = ms.ToArray() };
            }

            MessageHandler($"Sprites imported");
        }

        private static void AddCustomSkins()
        {
            HelperMethods.ReplaceTexture("spr_peppalette", "spr_peppalette_0");
            HelperMethods.ReplaceTexture("spr_ratmountpalette", "spr_ratmountpalette_0");
            HelperMethods.ReplaceTexture("spr_noisepalette", "spr_noisepalette_0");
            HelperMethods.ReplaceTexture("spr_noisepalette_rage", "spr_noisepalette_rage_0");

            HelperMethods.AddTexture("spr_appattern1", 19, 18, "spr_appattern1_0");


            Data.Code.ByName("gml_Object_obj_palettedresser_Create_0").AppendGML(@"
array_push(player_palettes[0], [""ap_blue"", 1, 16])
array_push(player_palettes[1], [""ap_blue"", 1, 29])
array_push(player_palettes[0], [""ap_color"", 1, 12, asset_get_index(""spr_appattern1"")])
array_push(player_palettes[1], [""ap_color"", 1, 12, asset_get_index(""spr_appattern1"")])
            ", Data);

            Data.Scripts.ByName("gml_Script_scr_get_texture_array").Code = HelperMethods.AddCode("gml_Script_scr_get_texture_array_archipelago", @"
return [[""ap_color"", asset_get_index(""spr_appattern1"")], [""funny"", 4398], [""itchy"", 511], [""pizza"", 3125], [""stripes"", 1806], [""goldemanne"", 4065], [""bones"", 4322], [""pp"", 4293], [""war"", 917], [""john"", 4315], [""candy"", 4546], [""bloodstained"", 2999], [""bat"", 3635], [""pumpkin"", 1988], [""fur"", 2047], [""flesh"", 4577], [""racer"", 699], [""comedian"", 656], [""banana"", 4035], [""noiseTV"", 3629], [""madman"", 4488], [""bubbly"", 3863], [""welldone"", 2633], [""grannykisses"", 1930], [""towerguy"", 2277]];
            ");

            MessageHandler($"Custom skins added");
        }

        private static void AddExtension(string folderPath)
        {
            UndertaleExtension apExtension = new()
            {
                Name = Data.Strings.MakeString("ArchipelagoPizzaTower.GameMakerExtension"),
                ClassName = Data.Strings.MakeString(""),
                Version = Data.Strings.MakeString("1.0.0"),
                FolderName = Data.Strings.MakeString("")
            };
            Data.Extensions.Add(apExtension);

            UndertaleExtensionFile extensionFile = new()
            {
                Kind = UndertaleExtensionKind.Dll,
                Filename = Data.Strings.MakeString("ArchipelagoPizzaTower.GameMakerExtension_x64.dll"),
                InitScript = Data.Strings.MakeString(""),
                CleanupScript = Data.Strings.MakeString("")
            };
            apExtension.Files.Add(extensionFile);

            extensionFile.AddFunction("ap_connect", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String);
            extensionFile.AddFunction("ap_disconnect", UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_connect_slot", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String, UndertaleExtensionVarType.String, UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_poll", UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_get_state", UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_wants_deathlink", UndertaleExtensionVarType.Double);

            File.Copy(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/ArchipelagoPizzaTower.GameMakerExtension.dll", folderPath + @"\ArchipelagoPizzaTower.GameMakerExtension_x64.dll");

            UndertaleGameObject apObject = HelperMethods.AddObject("obj_archipelago");
            apObject.AddEvent(EventType.Create, 0, "gml_Object_obj_archipelago_Create_0", @"
persistent = true 

ip_address = """"
ip_port = """"
name = """"
password = """"

sent_info = false
            ");
            apObject.AddEvent(EventType.Alarm, 0, "gml_Object_obj_archipelago_Alarm_0", @"
ap_connect(ip_address + "":"" + ip_port)
            ");

            apObject.AddEvent(EventType.Step, (int)EventSubtypeStep.Step, "gml_Object_obj_archipelago_Step_0", @"
ap_poll()
if (ap_get_state() == 3 and !sent_info)
{
    ap_connect_slot(name, password, ap_wants_deathlink())
    sent_info = true
}
            ");

            MessageHandler($"Added Archipelago extension");
        }

        private static void AddCustomInput()
        {
            UndertaleGameObject customInput = HelperMethods.AddObject("obj_custominput");
            customInput.AddEvent(EventType.Create, 0, "gml_Object_obj_custominput_Create_0", @"
enabled_chars = @""ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz.:!0123456789?'\ÁÉÍÓÚáéíóú_-[]▼()&#风雨廊桥전태양*яиБжидГзв¡¿Ññ "" + ""\""""
blinking = true
pointer_position = 0
blink_speed = 30
alarm[0] = blink_speed
text = """"
keyboard_string = """"
text_limit = 64
            ");
            customInput.AddEvent(EventType.Alarm, 0, "gml_Object_obj_custominput_Alarm_0", @"
blinking = !blinking
alarm[0] = blink_speed
            ");
            customInput.AddEvent(EventType.Step, (int)EventSubtypeStep.EndStep, "gml_Object_obj_custominput_Step_2", @"
                
            ");
            customInput.AddEvent(EventType.KeyPress, (int)EventSubtypeKey.vk_anykey, "gml_Object_obj_custominput_KeyPress_1", @"
blinking = true
alarm[0] = blink_speed

if keyboard_check(vk_control)
{
    if (keyboard_check(ord(""V"")))
    {
        text = string_insert(clipboard_get_text(), text, pointer_position + 1)
        pointer_position += string_length(clipboard_get_text())
    }
}
else
{
    if keyboard_check(vk_backspace)
    {
        text = string_delete(text, pointer_position, 1)
        pointer_position -= 1
    }
    else if keyboard_check(vk_left)
    {
        pointer_position -= 1
    }
    else if keyboard_check(vk_right)
    {
        pointer_position += 1
    }
    else
    {
        text = string_insert(string_copy(keyboard_string, string_length(keyboard_string), 1), text, pointer_position + 1)
        pointer_position += 1
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
text = string_copy(text, 1, text_limit);
pointer_position = clamp(pointer_position, 0, string_length(text))
keyboard_string = """"
            ");

            MessageHandler($"Added custom input object");
        }

        private static void ModifyMainMenu()
        {
            Data.Code.ByName("gml_Object_obj_mainmenu_Create_0").AppendGML(@"
connectselect = 0
is_typing = false

ini_open_from_string(obj_savesystem.ini_str_options)
ap_ip = ini_read_string(""Archipelago"", ""ip_address"", """")
ap_port = ini_read_string(""Archipelago"", ""ip_port"", """")
ap_name = ini_read_string(""Archipelago"", ""slot_name"", """")
ap_password = ini_read_string(""Archipelago"", ""password"", """")
obj_savesystem.ini_str_options = ini_close()

text_input = instance_create(0, 0, obj_custominput)

did_tip = false
            ", Data);

            Data.Code.ByName("gml_Object_obj_mainmenu_Step_0").AppendGML(@"
if (state == 0 << 0)
{
    if (keyboard_check_pressed(vk_f1))
    {
        fmod_event_one_shot(""event:/sfx/enemies/ufolivelaser"");
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
            fmod_event_one_shot(""event:/sfx/ui/step"")
        }
    }
    else
    {
        connectselect += key_down2 - key_up2
        connectselect = clamp(connectselect, 0, 5)
                    
        if keyboard_check_pressed(vk_enter) or key_jump
        {
            keyboard_string = """"
            switch(connectselect)
            {
                case 0:
                    fmod_event_one_shot(""event:/sfx/ui/step"")
                    is_typing = true
                    text_input.text = ap_ip
                    text_input.pointer_position = 64
                    break
                case 1:
                    fmod_event_one_shot(""event:/sfx/ui/step"")
                    is_typing = true
                    text_input.text  = ap_port
                    text_input.pointer_position = 64
                    break
                case 2:
                    fmod_event_one_shot(""event:/sfx/ui/step"")
                    is_typing = true
                    text_input.text  = ap_name
                    text_input.pointer_position = 64
                    break
                case 3:
                    fmod_event_one_shot(""event:/sfx/ui/step"")
                    is_typing = true
                    text_input.text  = ap_password
                    text_input.pointer_position = 64
                    break
                case 4:
                    if (ap_get_state() == 4)
                    {
                        ap_disconnect()
                        instance_destroy(obj_archipelago)
                        fmod_event_one_shot(""event:/sfx/misc/collect"")
                    }
                    else
                    {
                        ap_connect(ap_ip + "":"" + ap_port)
                        var archipelago = instance_create(0, 0, obj_archipelago)
                        archipelago.name = ap_name
                        archipelago.password = ap_password
                        state = 1912 << 0
                    }
                    break
                case 5:
                    ini_open_from_string(obj_savesystem.ini_str_options)
                    ini_write_string(""Archipelago"", ""ip_address"", ap_ip)
                    ini_write_string(""Archipelago"", ""ip_port"", ap_port)
                    ini_write_string(""Archipelago"", ""slot_name"", ap_name)
                    ini_write_string(""Archipelago"", ""password"", ap_password)
                    obj_savesystem.ini_str_options = ini_close()
                    gamesave_async_save_options()
                    state = 0 << 0
                    break
            }
        }
    }
}
else if (state == 1912 << 0)
{
    if ap_get_state() == 4
    {
        state = 0 << 0
        fmod_event_one_shot(""event:/sfx/misc/collecttoppin"")
    }
    if (key_jump)
    {
        state = 0 << 0
        ap_disconnect()
        instance_destroy(obj_archipelago)
        fmod_event_one_shot(""event:/sfx/misc/collect"")
    }
}
            ", Data);

            HelperMethods.AddTexture("spr_connectap", 104, 66, "spr_connectap_0");
            Data.Code.ByName("gml_Object_obj_mainmenu_Draw_0").AppendGML(@"
draw_set_alpha(extrauialpha)
lang_draw_sprite(asset_get_index(""spr_connectap""), 0, 400, 5)
scr_draw_text_arr(440, 40, scr_compile_icon_text(""[y]""), c_white, extrauialpha)
draw_set_alpha(1)
            ", Data);

            HelperMethods.AddTexture("spr_menu_archipelago", 70, 83, "spr_menu_archipelago_0", "spr_menu_archipelago_1", "spr_menu_archipelago_2");
            HelperMethods.AddTexture("spr_menu_connecting", 111, 103, "spr_menu_connecting_0", "spr_menu_connecting_1", "spr_menu_connecting_2", "spr_menu_connecting_3", "spr_menu_connecting_4", "spr_menu_connecting_5", "spr_menu_connecting_6", "spr_menu_connecting_7");
            HelperMethods.AddTexture("spr_creditsfont_cursor", 8, 30, "spr_creditsfont_cursor_0");
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

    draw_sprite(spr_menu_archipelago, index, (obj_screensizer.actual_width / 2) + 120 + 210 - 35, (obj_screensizer.actual_height / 2) - 150 - 41)
	draw_sprite(spr_menu_archipelago, index, (obj_screensizer.actual_width / 2) - 120 - 210 - 35, (obj_screensizer.actual_height / 2) - 150 - 41)

    c0 = (connectselect == 0) ? c_white : c_gray
    c1 = (connectselect == 1) ? c_white : c_gray
    c2 = (connectselect == 2) ? c_white : c_gray
    c3 = (connectselect == 3) ? c_white : c_gray
    c4 = (connectselect == 4) ? c_white : c_gray
    c5 = (connectselect == 5) ? c_white : c_gray

    draw_set_font(lang_get_font(""creditsfont""))
    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 0, embed_value_string(lang_get_value(""menu_apip""), [ap_ip]), c0, c0, c0, c0, 1)
    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 30, embed_value_string(lang_get_value(""menu_apport""), [ap_port]), c1, c1, c1, c1, 1)
    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 60, embed_value_string(lang_get_value(""menu_apname""), [ap_name]), c2, c2, c2, c2, 1)
    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 90, embed_value_string(lang_get_value(""menu_appass""), [ap_password]), c3, c3, c3, c3, 1)
    if ap_get_state() == 4
        tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 120, lang_get_value(""menu_apdisconnect""), c4, c4, c4, c4, 1)
    else
        tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 120, lang_get_value(""menu_apconnect""), c4, c4, c4, c4, 1)
    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 150, lang_get_value(""menu_apleave""), c5, c5, c5, c5, 1)
                    
    if (is_typing and text_input.blinking)
    {
        switch (connectselect)
        {
            case 0:
                var str = embed_value_string(lang_get_value(""menu_apip""), [ap_ip])
                var text_width = string_width(string_copy(str, 1, text_input.pointer_position + 11)) - string_width(str) / 2
                draw_sprite(spr_creditsfont_cursor, 0, (obj_screensizer.actual_width / 2) + text_width - 4, (obj_screensizer.actual_height / 2) - 7.5)
                break
            case 1:
                var str = embed_value_string(lang_get_value(""menu_apport""), [ap_port])
                var text_width = string_width(string_copy(str, 1, text_input.pointer_position + 5)) - string_width(str) / 2
                draw_sprite(spr_creditsfont_cursor, 0, (obj_screensizer.actual_width / 2) + text_width - 4, (obj_screensizer.actual_height / 2) + 30 - 7.5)
                break
            case 2:
                var str = embed_value_string(lang_get_value(""menu_apname""), [ap_name])
                var text_width = string_width(string_copy(str, 1, text_input.pointer_position + 10)) - string_width(str) / 2
                draw_sprite(spr_creditsfont_cursor, 0, (obj_screensizer.actual_width / 2) + text_width - 4, (obj_screensizer.actual_height / 2) + 60 - 7.5)
                break
            case 3:
                var str = embed_value_string(lang_get_value(""menu_appass""), [ap_password])
                var text_width = string_width(string_copy(str, 1, text_input.pointer_position + 9)) - string_width(str) / 2
                draw_sprite(spr_creditsfont_cursor, 0, (obj_screensizer.actual_width / 2) + text_width - 4, (obj_screensizer.actual_height / 2) + 90 - 7.5)
                break
        }
    }
                    
    draw_set_font(lang_get_font(""bigfont""))
}
else if (state == 1912 << 0)
{
    draw_set_alpha(0.7)
    draw_rectangle_color(0, 0, room_width, room_height, c_black, c_black, c_black, c_black, 0)
    draw_set_alpha(1)
    draw_set_font(lang_get_font(""bigfont""))
    draw_set_halign(fa_center)
    draw_set_valign(fa_middle)

    draw_sprite(spr_menu_connecting, index * 2, (obj_screensizer.actual_width / 2) - 55.5, (obj_screensizer.actual_height / 2) - 51.5)

    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 120, lang_get_value(""menu_apcancel""), c_white, c_white, c_white, c_white, 1)
}
", Data);

            MessageHandler($"Added new submenu to main menu");
        }

        private static void AddChatMenu()
        {
            #region Hatred
            Image<Rgba32> texturePageImage = new(200, 200);
            UndertaleEmbeddedTexture? texturePage = new();
            texturePage.TextureHeight = texturePage.TextureWidth = 200;
            Data.EmbeddedTextures.Add(texturePage);

            Image image = Image.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/spr_chatbg_0.png");
            texturePageImage.Mutate(i => i.DrawImage(image, new Point(0, 0), 1));

            UndertaleTexturePageItem pageItem = new();
            pageItem.SourceX = 0;
            pageItem.SourceY = 0;
            pageItem.SourceWidth = pageItem.TargetWidth = pageItem.BoundingWidth = (ushort)image.Width;
            pageItem.SourceHeight = pageItem.TargetHeight = pageItem.BoundingHeight = (ushort)image.Height;
            pageItem.TexturePage = texturePage;
            Data.TexturePageItems.Add(pageItem);

            UndertaleSprite sprite = new()
            {
                Name = Data.Strings.MakeString("spr_chatbg"),
                Height = 200,
                Width = 200,
                MarginRight = 199,
                MarginBottom = 199,
                OriginX = 0,
                OriginY = 0
            };
            UndertaleSprite.TextureEntry texture = new();
            texture.Texture = Data.TexturePageItems.Last();
            sprite.Textures.Add(texture);
            Data.Sprites.Add(sprite);

            using (MemoryStream ms = new MemoryStream())
            {
                texturePageImage.Save(ms, PngFormat.Instance);
                texturePage.TextureData = new UndertaleEmbeddedTexture.TexData { TextureBlob = ms.ToArray() };
            }
            #endregion

            Data.Code.ByName("gml_Object_obj_debugcontroller_Create_0").ReplaceGML(@"
                instance_destroy()
            ", Data);

            UndertaleGameObject chatMenu = HelperMethods.AddObject("obj_apchatmenu");

            chatMenu.AddEvent(EventType.Create, 0, "gml_Object_obj_chatmenu_Create_0", @"
persistent = true
depth = -1111

var offset_shader = asset_get_index(""sh_scrolling_offset"")
uni_offset = shader_get_uniform(offset_shader, ""u_offset"")
uni_res = shader_get_uniform(offset_shader, ""u_resolution"")

offset_x = 0
offset_y = 0

start_height = 0
intended_height = 0
current_height = 0
scaling_up = false
scaling_down = false
scale_progress = 0

active = false


            ");
            chatMenu.AddEvent(EventType.Step, (int)EventSubtypeStep.Step, "gml_Object_obj_chatmenu_Step_0", @"
offset_x += 0.005
offset_y += 0.005

if (scaling_up or scaling_down)
{
    scale_progress += 0.05
    if (scale_progress == 1)
    {
        scaling_up = false
        scaling_down = false
    }

    current_height = lerp(current_height, intended_height, sin(scale_progress))
}
            ");
            chatMenu.AddEvent(EventType.KeyPress, (int)EventSubtypeKey.vk_tab, "gml_Object_obj_chatmenu_KeyPress_9", @"
active = !active
if (active)
{
    scaling_up = true
    scaling_down = false
    start_height = current_height
    intended_height = obj_screensizer.actual_height * 0.85
    scale_progress = 0
}
else
{
    scaling_up = false
    scaling_down = true
    start_height = current_height
    intended_height = 0
    scale_progress = 0
}
            ");


            Data.Code.ByName("gml_Room_Mainmenu_Create").AppendGML(@"
instance_destroy(obj_apchatmenu)
            ", Data);
            Data.Code.ByName("gml_Room_tower_entrancehall_Create").AppendGML(@"
global.chatmenu = instance_create_unique(0, 0, obj_apchatmenu)
            ", Data);

            HelperMethods.AddShader("sh_scrolling_offset");

            HelperMethods.AddTexture("spr_chatborder", 65, 34, "spr_chatborder_0");
            HelperMethods.AddTexture("spr_chattextborder", 33, 33, "spr_chattextborder_0");
            Data.Code.ByName("gml_Room_Loadiingroom_Create").AppendGML(@"
var chat_nineslices = sprite_nineslice_create()

chat_nineslices.enabled = true
chat_nineslices.left = 32
chat_nineslices.right = 32
chat_nineslices.top = 1
chat_nineslices.bottom = 32

sprite_set_nineslice(asset_get_index(""spr_chatborder""), chat_nineslices)


var text_nineslices = sprite_nineslice_create()

text_nineslices.enabled = true
text_nineslices.left = 16
text_nineslices.right = 16
text_nineslices.top = 16
text_nineslices.bottom = 16

sprite_set_nineslice(asset_get_index(""spr_chattextborder""), text_nineslices)
", Data);

            chatMenu.AddEvent(EventType.Draw, (int)EventSubtypeDraw.DrawGUI, "gml_Object_obj_chatmenu_Draw_64", @"
if (current_height == 0)
    exit

draw_set_colour(c_white)
    
gpu_set_texrepeat(true)

var bg_sprite = asset_get_index(""spr_chatbg"")
var offset_shader = asset_get_index(""sh_scrolling_offset"")
    
shader_set(offset_shader)
shader_set_uniform_f(uni_offset, offset_x, offset_y);
shader_set_uniform_f(uni_res, obj_screensizer.actual_width - 100, current_height);

var tex = sprite_get_texture(bg_sprite, 0)
draw_primitive_begin_texture(pr_trianglestrip, tex)
draw_vertex_texture(50, 0, 0, 0)
draw_vertex_texture(obj_screensizer.actual_width - 50, 0, 1, 0)
draw_vertex_texture(50, current_height, 0, 1)
draw_vertex_texture(obj_screensizer.actual_width - 50, current_height, 1, 1)
draw_primitive_end()
    
reset_shader_fix()
gpu_set_texrepeat(false)

draw_sprite_stretched(asset_get_index(""spr_chatborder""), 0, 50 - 5, 0, obj_screensizer.actual_width - 100 + 6, current_height + 5)
draw_sprite_stretched(asset_get_index(""spr_chattextborder""), 0, 50 + 20, current_height - 20 - 64, obj_screensizer.actual_width - 100 - 40, 64)

if (active)
{
    draw_set_alpha(scale_progress * 0.5);
	draw_rectangle_color(0, 0, obj_screensizer.actual_width, obj_screensizer.actual_height, c_white, c_white, c_white, c_white, false);
}
else
{
    draw_set_alpha((1 - scale_progress) * 0.5);
	draw_rectangle_color(0, 0, obj_screensizer.actual_width, obj_screensizer.actual_height, c_white, c_white, c_white, c_white, false);
}
            ");
        }
    }
}
