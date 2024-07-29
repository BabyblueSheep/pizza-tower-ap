using ArchpelagoPizzaTower.Patcher.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;

namespace ArchipelagoPizzaTower.Patcher.Library.Patches
{
    internal static partial class Patches
    {
        internal static void ModifyMainMenu()
        {
            GamePatcher.Data.Code.ByName("gml_Object_obj_mainmenu_Create_0").AppendGML(@"
connectselect = 0
is_typing = false

ini_open_from_string(obj_savesystem.ini_str_options)
ap_ip = ini_read_string(""Archipelago"", ""ip_address"", """")
ap_port = ini_read_string(""Archipelago"", ""ip_port"", """")
ap_name = ini_read_string(""Archipelago"", ""slot_name"", """")
ap_password = ini_read_string(""Archipelago"", ""password"", """")
obj_savesystem.ini_str_options = ini_close()

did_tip = false
            ", GamePatcher.Data);

            GamePatcher.Data.Code.ByName("gml_Object_obj_mainmenu_Step_0").AppendGML(@"
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
            alarm[1] = 220;
        }
        did_tip = true;
    }

    if (is_typing)
    {
        switch(connectselect)
        {
            case 0:
                ap_ip = global.text_input.text
                break
            case 1:
                ap_port = global.text_input.text
                break
            case 2:
                ap_name = global.text_input.text
                break
            case 3:
                ap_password = global.text_input.text
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
                    global.text_input.text = ap_ip
                    global.text_input.pointer_position = string_length(text_input.text)
                    break
                case 1:
                    fmod_event_one_shot(""event:/sfx/ui/step"")
                    is_typing = true
                    global.text_input.text  = ap_port
                    global.text_input.pointer_position = string_length(text_input.text)
                    break
                case 2:
                    fmod_event_one_shot(""event:/sfx/ui/step"")
                    is_typing = true
                    global.text_input.text  = ap_name
                    global.text_input.pointer_position = string_length(text_input.text)
                    break
                case 3:
                    fmod_event_one_shot(""event:/sfx/ui/step"")
                    is_typing = true
                    global.text_input.text  = ap_password
                    global.text_input.pointer_position = string_length(text_input.text)
                    break
                case 4:
                    if (ap_get_state() == global.AP_STATE_SLOT_CONNECTED)
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
    if (ap_get_state() == global.AP_STATE_SLOT_CONNECTED)
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
            ", GamePatcher.Data);

            HelperMethods.AddTexture("spr_connectap", 104, 66, "spr_connectap_0");
            GamePatcher.Data.Code.ByName("gml_Object_obj_mainmenu_Draw_0").AppendGML(@"
draw_set_alpha(extrauialpha)
lang_draw_sprite(asset_get_index(""spr_connectap""), 0, 400, 5)
scr_draw_text_arr(440, 40, scr_compile_icon_text(""[y]""), c_white, extrauialpha)
draw_set_alpha(1)
            ", GamePatcher.Data);

            HelperMethods.AddTexture("spr_menu_archipelago", 70, 83, "spr_menu_archipelago_0", "spr_menu_archipelago_1", "spr_menu_archipelago_2");
            HelperMethods.AddTexture("spr_menu_connecting", 111, 103, "spr_menu_connecting_0", "spr_menu_connecting_1", "spr_menu_connecting_2", "spr_menu_connecting_3", "spr_menu_connecting_4", "spr_menu_connecting_5", "spr_menu_connecting_6", "spr_menu_connecting_7");
            HelperMethods.AddTexture("spr_creditsfont_cursor", 8, 30, "spr_creditsfont_cursor_0");
            GamePatcher.Data.Code.ByName("gml_Object_obj_mainmenu_Draw_64").AppendGML(@"       
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
    if (ap_get_state() == global.AP_STATE_SLOT_CONNECTED)
        tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 120, lang_get_value(""menu_apdisconnect""), c4, c4, c4, c4, 1)
    else
        tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 120, lang_get_value(""menu_apconnect""), c4, c4, c4, c4, 1)
    tdp_draw_text_color(obj_screensizer.actual_width / 2, (obj_screensizer.actual_height / 2) + 150, lang_get_value(""menu_apleave""), c5, c5, c5, c5, 1)
                    
    if (is_typing and global.text_input.blinking)
    {
        switch (connectselect)
        {
            case 0:
                var str = embed_value_string(lang_get_value(""menu_apip""), [ap_ip])
                var text_width = string_width(string_copy(str, 1, global.text_input.pointer_position + 11)) - string_width(str) / 2
                draw_sprite(spr_creditsfont_cursor, 0, (obj_screensizer.actual_width / 2) + text_width - 4, (obj_screensizer.actual_height / 2) - 7.5)
                break
            case 1:
                var str = embed_value_string(lang_get_value(""menu_apport""), [ap_port])
                var text_width = string_width(string_copy(str, 1, global.text_input.pointer_position + 5)) - string_width(str) / 2
                draw_sprite(spr_creditsfont_cursor, 0, (obj_screensizer.actual_width / 2) + text_width - 4, (obj_screensizer.actual_height / 2) + 30 - 7.5)
                break
            case 2:
                var str = embed_value_string(lang_get_value(""menu_apname""), [ap_name])
                var text_width = string_width(string_copy(str, 1, global.text_input.pointer_position + 10)) - string_width(str) / 2
                draw_sprite(spr_creditsfont_cursor, 0, (obj_screensizer.actual_width / 2) + text_width - 4, (obj_screensizer.actual_height / 2) + 60 - 7.5)
                break
            case 3:
                var str = embed_value_string(lang_get_value(""menu_appass""), [ap_password])
                var text_width = string_width(string_copy(str, 1, global.text_input.pointer_position + 9)) - string_width(str) / 2
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
", GamePatcher.Data);

            GamePatcher.MessageHandler($"Added new submenu to main menu");
        }
    }
}
