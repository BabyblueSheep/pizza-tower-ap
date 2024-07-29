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
        internal static void ModifyPauseMenu()
        {
            GamePatcher.Data.Scripts.ByName("gml_Script_scr_pause_deactivate_objects").Code = HelperMethods.AddCode("gml_Script_scr_pause_deactivate_objects_archipelago", @"
if (argument[0] == undefined)
    argument[0] = 1
if argument[0]
    fmod_event_instance_set_paused_all(1)
ds_list_clear(instance_list)
for (i = 0; i < instance_count; i++)
{
    obj = instance_find(all, i)
    if (instance_exists(obj) && obj.object_index != asset_get_index(""obj_pause"") && obj.object_index != asset_get_index(""obj_inputAssigner"") && obj.object_index != asset_get_index(""obj_screensizer""))
        ds_list_add(instance_list, obj)
}
instance_deactivate_all(true)
instance_activate_object(asset_get_index(""obj_inputAssigner""))
instance_activate_object(asset_get_index(""obj_savesystem""))
instance_activate_object(asset_get_index(""obj_pause""))
instance_activate_object(asset_get_index(""obj_screensizer""))
instance_activate_object(asset_get_index(""obj_music""))
instance_activate_object(asset_get_index(""obj_fmod""))
instance_activate_object(asset_get_index(""obj_globaltimer""))
instance_activate_object(asset_get_index(""obj_archipelago""))
        ");

            GamePatcher.Data.Code.ByName("gml_Object_obj_player1_Step_0").ReplaceGML(@"
if (room == rm_editor)
{
    visible = false
    return;
}
if (room == custom_lvl_room)
{
    if place_meeting(x, y, par_camera_editor)
    {
        cam = instance_place(x, y, par_camera_editor)
        cam_width = instance_place(x, y, par_camera_editor).width
        cam_height = instance_place(x, y, par_camera_editor).height
        with (obj_camera)
            bound_camera = 1
    }
    else
    {
        cam = -4
        cam_width = 0
        cam_height = 0
        instance_activate_all()
        with (obj_camera)
            bound_camera = 0
    }
}
var should_input = true
if instance_exists(obj_apchatmenu)
{
    if global.chatmenu.active && ap_get_state() != global.AP_STATE_DISCONNECTED
        should_input = false
}

if should_input
    scr_getinput()
else
{
    var _dvc = obj_inputAssigner.player_input_device[obj_inputAssigner.player_index]
    var _dvc2 = obj_inputAssigner.player_input_device[(!(obj_inputAssigner.player_index))]
    key_fightball = 0
    key_jump_p2 = 0
    key_jump2_p2 = 0
    key_taunt_p2 = 0
    key_taunt2_p2 = 0
    key_slap_p2 = 0
    key_up_p2 = 0
    key_start_p2 = 0
    if (global.swapmode && _dvc != _dvc2)
    {
        tdp_input_update(_dvc2)
        key_fightball = (_dvc2 >= 0 ? tdp_input_get(""player_attackC"").held : tdp_input_get(""player_attack"").held)
        key_jump_p2 = (_dvc2 >= 0 ? tdp_input_get(""player_jumpC"").pressed : tdp_input_get(""player_jump"").pressed)
        key_jump2_p2 = (_dvc2 >= 0 ? tdp_input_get(""player_jumpC"").held : tdp_input_get(""player_jump"").held)
        key_taunt_p2 = (_dvc2 >= 0 ? tdp_input_get(""player_tauntC"").pressed : tdp_input_get(""player_taunt"").pressed)
        key_taunt2_p2 = (_dvc2 >= 0 ? tdp_input_get(""player_tauntC"").held : tdp_input_get(""player_taunt"").held)
        key_slap_p2 = (_dvc2 >= 0 ? tdp_input_get(""player_slapC"").pressed : tdp_input_get(""player_slap"").pressed)
        key_up_p2 = (_dvc2 >= 0 ? tdp_input_get(""player_upC"").held : tdp_input_get(""player_up"").held)
        key_start_p2 = (_dvc2 >= 0 ? tdp_input_get(""menu_startC"").pressed : tdp_input_get(""menu_start"").pressed)
    }
    tdp_input_update(_dvc)
}
event_inherited()
            ", GamePatcher.Data);
            GamePatcher.Data.Code.ByName("gml_Object_obj_player2_Step_0").ReplaceGML(@"
if (room == rm_editor)
{
	visible = 0
	exit
}
visible = false
x = -10000
y = -10000
var should_input = true
if instance_exists(obj_apchatmenu)
{
    if global.chatmenu.active && ap_get_state() != global.AP_STATE_DISCONNECTED
        should_input = false
}

if should_input
    scr_getinput2()
if !global.coop
{
	obj_player1.spotlight = true
	x = -1000
	y = -1000
	state = (18 << 0)
	if (instance_exists(obj_coopflag))
		instance_destroy(obj_coopflag)
	if (instance_exists(obj_cooppointer))
		instance_destroy(obj_cooppointer)
}
else if (key_start && (!fightball) && obj_player1.state != (121 << 0) && obj_player1.state != (4 << 0))
    state = (186 << 0)
if !visible && state == (95 << 0)
{
	coopdelay++
	image_index = 0
	if coopdelay == 50
	{
		visible = true
		coopdelay = 0
	}
}
            ", GamePatcher.Data);


            GamePatcher.MessageHandler($"Pause menu modified");
        }
    }
}
