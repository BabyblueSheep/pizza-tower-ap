using ArchpelagoPizzaTower.Patcher.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ArchipelagoPizzaTower.Patcher.Library.Patches
{
    internal static partial class Patches
    {
        internal static void AddCustomInput()
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
text_limit = 128
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

            GamePatcher.Data.Code.ByName("gml_Room_Loadiingroom_Create").AppendGML(@"
global.text_input = instance_create(0, 0, obj_custominput)
            ", GamePatcher.Data);

            GamePatcher.MessageHandler($"Added custom input object");
        }
    }
}
