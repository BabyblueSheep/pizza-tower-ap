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
using UndertaleModLib;

namespace ArchipelagoPizzaTower.Patcher.Library.Patches
{
    internal static partial class Patches
    {
        internal static void AddChatMenu()
        {
            #region Hatred (i had to make a separate atlas just for a looping sprite)
            Image<Rgba32> texturePageImage = new(200, 200);
            UndertaleEmbeddedTexture? texturePage = new();
            texturePage.TextureHeight = texturePage.TextureWidth = 200;
            GamePatcher.Data.EmbeddedTextures.Add(texturePage);

            Image image = Image.Load(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/spr_chatbg_0.png");
            texturePageImage.Mutate(i => i.DrawImage(image, new Point(0, 0), 1));

            UndertaleTexturePageItem pageItem = new();
            pageItem.SourceX = 0;
            pageItem.SourceY = 0;
            pageItem.SourceWidth = pageItem.TargetWidth = pageItem.BoundingWidth = (ushort)image.Width;
            pageItem.SourceHeight = pageItem.TargetHeight = pageItem.BoundingHeight = (ushort)image.Height;
            pageItem.TexturePage = texturePage;
            GamePatcher.Data.TexturePageItems.Add(pageItem);

            UndertaleSprite sprite = new()
            {
                Name = GamePatcher.Data.Strings.MakeString("spr_chatbg"),
                Height = 200,
                Width = 200,
                MarginRight = 199,
                MarginBottom = 199,
                OriginX = 0,
                OriginY = 0
            };
            UndertaleSprite.TextureEntry texture = new();
            texture.Texture = GamePatcher.Data.TexturePageItems.Last();
            sprite.Textures.Add(texture);
            GamePatcher.Data.Sprites.Add(sprite);

            using (MemoryStream ms = new MemoryStream())
            {
                texturePageImage.Save(ms, PngFormat.Instance);
                texturePage.TextureData = new UndertaleEmbeddedTexture.TexData { TextureBlob = ms.ToArray() };
            }
            #endregion

            GamePatcher.Data.Code.ByName("gml_Object_obj_debugcontroller_Create_0").ReplaceGML(@"
                instance_destroy()
            ", GamePatcher.Data);

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

text_surface = -1
input_text_surface = -1
scroll_height = 0
bottom_locked = true

active = false

current_text = """"
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

    current_height = lerp(current_height, intended_height, 1 - cos((scale_progress * pi) / 2))
}

if (active)
{
    current_text = global.text_input.text

    if keyboard_check_pressed(vk_enter)
    {
        global.text_input.text = """"
        ap_send_message(current_text)
    }
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

    global.text_input.text = current_text
    global.text_input.pointer_position = string_length(text_input.text)
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

            GamePatcher.Data.Scripts.ByName("gml_Script_scr_start_game").Code = HelperMethods.AddCode("gml_Script_scr_start_game_archipelago", @"
if (argument1 == undefined)
    argument1 = 1
instance_create(x, y, obj_fadeout)
with (obj_player)
{
    targetRoom = 661
    targetDoor = ""A""
}
with (obj_savesystem)
    ispeppino = argument1
global.currentsavefile = argument0
if (ap_get_state() != global.AP_STATE_DISCONNECTED)
    global.chatmenu = instance_create_unique(0, 0, obj_apchatmenu)
gamesave_async_load()
            ");
            GamePatcher.Data.Code.ByName("gml_Object_obj_mainmenu_Create_0").AppendGML(@"
instance_destroy(obj_apchatmenu)
            ", GamePatcher.Data);

            HelperMethods.AddShader("sh_scrolling_offset");

            HelperMethods.AddTexture("spr_chatborder", 65, 34, "spr_chatborder_0");
            HelperMethods.AddTexture("spr_chattextborder", 33, 33, "spr_chattextborder_0");
            GamePatcher.Data.Code.ByName("gml_Room_Loadiingroom_Create").AppendGML(@"
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
", GamePatcher.Data);

            chatMenu.AddEvent(EventType.Keyboard, (int)EventSubtypeKey.vk_up, "gml_Object_obj_chatmenu_Keyboard_38", @"
var text_height = string_height_ext(ap_get_all_messages(), -1, obj_screensizer.actual_width - 160)
if (text_height < current_height - 180)
    exit
if (bottom_locked)
{
    scroll_height = text_height - current_height + 180
    bottom_locked = false
}
scroll_height -= 10
if (scroll_height < 0)
    scroll_height = 0
            ");
            chatMenu.AddEvent(EventType.Keyboard, (int)EventSubtypeKey.vk_down, "gml_Object_obj_chatmenu_Keyboard_40", @"
var text_height = string_height_ext(ap_get_all_messages(), -1, obj_screensizer.actual_width - 160)
if (text_height < current_height - 180)
    exit
scroll_height += 10
if (scroll_height > (text_height - current_height + 180))
{
    bottom_locked = true
    scroll_height = 0
}
            ");

            chatMenu.AddEvent(EventType.Draw, (int)EventSubtypeDraw.DrawGUI, "gml_Object_obj_chatmenu_Draw_64", @"
if (current_height == 0)
    exit

draw_set_alpha((current_height / (obj_screensizer.actual_height * 0.85)) * 0.5);
draw_rectangle_color(0, 0, obj_screensizer.actual_width, obj_screensizer.actual_height, c_white, c_white, c_white, c_white, false);

draw_set_alpha(1);
draw_set_colour(c_white)
    
gpu_set_texrepeat(true)

var bg_sprite = asset_get_index(""spr_chatbg"")
var offset_shader = asset_get_index(""sh_scrolling_offset"")
    
shader_set(offset_shader)
shader_set_uniform_f(uni_offset, offset_x, offset_y)
shader_set_uniform_f(uni_res, obj_screensizer.actual_width - 100, current_height)

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
draw_sprite_stretched(asset_get_index(""spr_chattextborder""), 0, 50 + 20, current_height - 20 - 60, obj_screensizer.actual_width - 100 - 40, 60)



draw_set_font(lang_get_font(""creditsfont""))
draw_set_halign(fa_left)
draw_set_valign(fa_bottom)

var text_height = string_height_ext(ap_get_all_messages(), -1, obj_screensizer.actual_width - 160)
if (!surface_exists(text_surface))
{
    text_surface = surface_create(obj_screensizer.actual_width - 160, text_height)
}
surface_set_target(text_surface)
draw_text_ext(0, text_height, ap_get_all_messages(), -1, obj_screensizer.actual_width - 160)
surface_reset_target()

if bottom_locked
    draw_surface_part(text_surface, 0, text_height - current_height + 180, obj_screensizer.actual_width - 160, current_height - 120, 60, 0)
else
    draw_surface_part(text_surface, 0, scroll_height, obj_screensizer.actual_width - 160, current_height - 120, 60, 0)
surface_free(text_surface)



draw_set_halign(fa_left)
draw_set_valign(fa_center)
if (!surface_exists(input_text_surface))
{
    input_text_surface = surface_create(obj_screensizer.actual_width - 100 - 80, 60)
}
surface_set_target(input_text_surface)
draw_text(0, 40, current_text)
surface_reset_target()
draw_surface_part(input_text_surface, 0, 0, 50 + 20 + 20, current_height - 20 - 60 + 20 + 10, obj_screensizer.actual_width - 100 - 40 - 40, 60)
surface_free(input_text_surface)
            ");

            GamePatcher.MessageHandler($"Chat menu added");
        }
    }
}
