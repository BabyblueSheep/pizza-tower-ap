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
        internal static void AddCustomSkins()
        {
            HelperMethods.ReplaceTexture("spr_peppalette", "spr_peppalette_0");
            HelperMethods.ReplaceTexture("spr_ratmountpalette", "spr_ratmountpalette_0");
            HelperMethods.ReplaceTexture("spr_noisepalette", "spr_noisepalette_0");
            HelperMethods.ReplaceTexture("spr_noisepalette_rage", "spr_noisepalette_rage_0");

            HelperMethods.AddTexture("spr_appattern1", 19, 18, "spr_appattern1_0");


            GamePatcher.Data.Code.ByName("gml_Object_obj_palettedresser_Create_0").AppendGML(@"
array_push(player_palettes[0], [""ap_blue"", 1, 16])
array_push(player_palettes[1], [""ap_blue"", 1, 29])
array_push(player_palettes[0], [""ap_color"", 1, 12, asset_get_index(""spr_appattern1"")])
array_push(player_palettes[1], [""ap_color"", 1, 12, asset_get_index(""spr_appattern1"")])
            ", GamePatcher.Data);

            GamePatcher.Data.Scripts.ByName("gml_Script_scr_get_texture_array").Code = HelperMethods.AddCode("gml_Script_scr_get_texture_array_archipelago", @"
return [[""ap_color"", asset_get_index(""spr_appattern1"")], [""funny"", 4398], [""itchy"", 511], [""pizza"", 3125], [""stripes"", 1806], [""goldemanne"", 4065], [""bones"", 4322], [""pp"", 4293], [""war"", 917], [""john"", 4315], [""candy"", 4546], [""bloodstained"", 2999], [""bat"", 3635], [""pumpkin"", 1988], [""fur"", 2047], [""flesh"", 4577], [""racer"", 699], [""comedian"", 656], [""banana"", 4035], [""noiseTV"", 3629], [""madman"", 4488], [""bubbly"", 3863], [""welldone"", 2633], [""grannykisses"", 1930], [""towerguy"", 2277]];
            ");

            GamePatcher.MessageHandler($"Custom skins added");
        }
    }
}
