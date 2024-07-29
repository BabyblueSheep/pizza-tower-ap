using ArchpelagoPizzaTower.Patcher.Library;
using System.Reflection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace ArchipelagoPizzaTower.Patcher.Library.Patches
{
    internal static partial class Patches
    {
        internal static void AddExtension(string folderPath)
        {
            UndertaleExtension apExtension = new()
            {
                Name = GamePatcher.Data.Strings.MakeString("ArchipelagoPizzaTower.GameMakerExtension"),
                ClassName = GamePatcher.Data.Strings.MakeString(""),
                Version = GamePatcher.Data.Strings.MakeString("1.0.0"),
                FolderName = GamePatcher.Data.Strings.MakeString("")
            };
            GamePatcher.Data.Extensions.Add(apExtension);

            UndertaleExtensionFile extensionFile = new()
            {
                Kind = UndertaleExtensionKind.Dll,
                Filename = GamePatcher.Data.Strings.MakeString("ArchipelagoPizzaTower.GameMakerExtension_x64.dll"),
                InitScript = GamePatcher.Data.Strings.MakeString(""),
                CleanupScript = GamePatcher.Data.Strings.MakeString("")
            };
            apExtension.Files.Add(extensionFile);

            extensionFile.AddFunction("ap_connect", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String);
            extensionFile.AddFunction("ap_disconnect", UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_connect_slot", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String, UndertaleExtensionVarType.String, UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_poll", UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_get_all_messages", UndertaleExtensionVarType.String);
            extensionFile.AddFunction("ap_send_message", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String);
            extensionFile.AddFunction("ap_get_state", UndertaleExtensionVarType.Double);
            extensionFile.AddFunction("ap_wants_deathlink", UndertaleExtensionVarType.Double);

            File.Copy(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Assets/ArchipelagoPizzaTower.GameMakerExtension.dll", folderPath + @"\ArchipelagoPizzaTower.GameMakerExtension_x64.dll");

            GamePatcher.Data.Code.ByName("gml_Room_Loadiingroom_Create").AppendGML(@"
global.AP_STATE_DISCONNECTED = 0
global.AP_STATE_SOCKET_CONNECTING = 1
global.AP_STATE_SOCKET_CONNECTED = 2
global.AP_STATE_ROOM_INFO = 3
global.AP_STATE_SLOT_CONNECTED = 4
            ", GamePatcher.Data);

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
if (ap_get_state() == global.AP_STATE_ROOM_INFO and !sent_info)
{
    ap_connect_slot(name, password, ap_wants_deathlink())
    sent_info = true
}
            ");

            GamePatcher.MessageHandler($"Added Archipelago extension");
        }
    }
}
