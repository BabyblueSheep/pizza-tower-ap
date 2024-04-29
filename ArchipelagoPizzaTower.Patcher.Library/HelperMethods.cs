using ArchpelagoPizzaTower.Patcher.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using UndertaleModLib;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace ArchipelagoPizzaTower.Patcher.Library
{
    public static class HelperMethods
    {
        /// <summary>
        /// Adds instructions to the beginning of a code object.
        /// </summary>
        /// <param name="code">The code object.</param>
        /// <param name="instructions">The instructions to add.</param>
        public static void Prepend(this UndertaleCode code, IList<UndertaleInstruction> instructions)
        {
            if (code.ParentEntry is not null)
                return;

            foreach (UndertaleInstruction instruction in instructions.Reverse())
            {
                code.Instructions.Prepend(instruction);
            }

            code.UpdateAddresses();
        }

        /// <summary>
        /// Adds GML instructions to the beginning of a code object.
        /// </summary>
        /// <param name="code">The code object.</param>
        /// <param name="gmlCode">The GML code to preprend.</param>
        /// <exception cref="Exception"> if the GML code does not compile or if there's an error writing the code to the profile entry.</exception>
        public static void PrependGML(this UndertaleCode code, string gmlCode)
        {
            if (code.ParentEntry is not null)
                return;

            CompileContext context = Compiler.CompileGMLText(gmlCode, GamePatcher.Data, code);
            if (!context.SuccessfulCompile || context.HasError)
            {
                Console.WriteLine(gmlCode);
                throw new Exception("GML Compile Error: " + context.ResultError);
            }

            code.Prepend(context.ResultAssembly);

            GamePatcher.Data.GMLCacheChanged?.Add(code.Name?.Content);

            try
            {
                // Attempt to write text in all modes, because this is a special case.
                string tempPath = Path.Combine(GamePatcher.Data.ToolInfo.AppDataProfiles, GamePatcher.Data.ToolInfo.CurrentMD5, "Temp", code.Name?.Content + ".gml");
                if (File.Exists(tempPath))
                {
                    string readText = File.ReadAllText(tempPath) + "\n" + gmlCode;
                    File.WriteAllText(tempPath, readText);
                }
            }
            catch (Exception exc)
            {
                throw new Exception("Error during writing of GML code to profile:\n" + exc);
            }
        }

        /// <summary>
        /// Adds a function to an extension file.
        /// </summary>
        /// <param name="file">The extension file object.</param>
        /// <param name="name">The name of the function.</param>
        /// <param name="returnType">The return type of the function.</param>
        /// <param name="argumentTypes">The argument types of the function.</param>
        /// 
        public static void AddFunction(this UndertaleExtensionFile file, string name, UndertaleExtensionVarType returnType, params UndertaleExtensionVarType[] argumentTypes)
        {
            UndertaleSimpleList<UndertaleExtensionFunctionArg> arguments = new();
            foreach (UndertaleExtensionVarType arg in argumentTypes)
            {
                arguments.Add(new() { Type = arg });
            }
            UndertaleExtensionFunction function = new()
            {
                Name = GamePatcher.Data.Strings.MakeString(name),
                ExtName = GamePatcher.Data.Strings.MakeString(name),
                Kind = 11,
                ID = GamePatcher.Data.ExtensionFindLastId(),
                RetType = returnType,
                Arguments = arguments
            };
            file.Functions.Add(function);
        }
        
        /// <summary>
        /// Adds a script object to the game data.
        /// </summary>
        /// <param name="scriptName">The name of the script object.</param>
        /// <param name="codeName">The name of the code object.</param>
        /// <param name="code">The code itself.</param>
        public static void AddScript(string scriptName, string codeName, string code)
        {
            GamePatcher.Data.Scripts.Add(new UndertaleScript { Name = GamePatcher.Data.Strings.MakeString(scriptName), Code = AddCode(codeName, code) });
        }

        /// <summary>
        /// Adds a code object to the game data.
        /// </summary>
        /// <param name="codeName">The name of the code object.</param>
        /// <param name="code">The code itself.</</param>
        /// <returns>The code object.</returns>
        public static UndertaleCode AddCode(string codeName, string code)
        {
            UndertaleCode codeObject = new();
            codeObject.Name = GamePatcher.Data.Strings.MakeString(codeName);
            codeObject.ReplaceGML(code, GamePatcher.Data);
            GamePatcher.Data.Code.Add(codeObject);
            return codeObject;
        }

        /// <summary>
        /// Adds a game object to the game data.
        /// </summary>
        /// <param name="name">The name of the game object.</param>
        /// <returns>The game object.</returns>
        public static UndertaleGameObject AddObject(string name)
        {
            UndertaleGameObject gameObject = new()
            {
                Name = GamePatcher.Data.Strings.MakeString(name),
            };
            GamePatcher.Data.GameObjects.Add(gameObject);
            return gameObject;
        }

        /// <summary>
        /// Binds a code object to an event of a game object.
        /// </summary>
        /// <param name="gameObject">The game object.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="eventSubType">The event subtype. This depends on the event type.</param>
        /// <param name="codeName">The name of the code object.</param>
        /// <param name="code">The code itself.</param>
        public static void AddEvent(this UndertaleGameObject gameObject, EventType eventType, uint eventSubType, string codeName, string code)
        {
            UndertaleCode script = AddCode(codeName, code);
            UndertalePointerList<UndertaleGameObject.Event> events = gameObject.Events[(int)eventType];
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

        /// <summary>
        /// Replaces a texture of a sprite object.
        /// </summary>
        /// <param name="textureName">The name of the sprite object.</param>
        /// <param name="fileName">The name of the texture file for replacing.</param>
        /// <param name="frame">Which frame to replace.</param>
        public static void ReplaceTexture(string textureName, string fileName, int frame = 0) => GamePatcher.Data.Sprites.ByName(textureName).Textures[frame].Texture = GamePatcher.Data.TexturePageItems[GamePatcher.NameToPageItem[fileName]];

        /// <summary>
        /// Adds a sprite object to the game data.
        /// </summary>
        /// <param name="textureName">The name of the sprite object.</param>
        /// <param name="width">The width of the sprite.</param>
        /// <param name="height">The height of the sprite.</param>
        /// <param name="fileNames">Names of the texture files.</param>
        public static void AddTexture(string textureName, uint width, uint height, params string[] fileNames)
        {
            UndertaleSprite sprite = new()
            {
                Name = GamePatcher.Data.Strings.MakeString(textureName),
                Height = height,
                Width = width,
                MarginRight = (int)(height - 1),
                MarginBottom = (int)(width - 1),
                OriginX = 0,
                OriginY = 0
            };
            foreach (string fileName in fileNames)
            {
                UndertaleSprite.TextureEntry texture = new();
                texture.Texture = GamePatcher.Data.TexturePageItems[GamePatcher.NameToPageItem[fileName]];
                sprite.Textures.Add(texture);
            }
            GamePatcher.Data.Sprites.Add(sprite);
        }

        public static void AddShader(string shaderName)
        {
            UndertaleShader shader = new();
            shader.Name = GamePatcher.Data.Strings.MakeString(shaderName);
            shader.Type = UndertaleShader.ShaderType.GLSL_ES;

            shader.HLSL11_VertexData.Data = File.ReadAllBytes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"/Assets/Shaders/{shaderName}_Vertex.bin");
            shader.HLSL11_VertexData.IsNull = false;

            shader.HLSL11_PixelData.Data = File.ReadAllBytes(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + $"/Assets/Shaders/{shaderName}_Fragment.bin");
            shader.HLSL11_PixelData.IsNull = false;

            shader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute() { Name = GamePatcher.Data.Strings.MakeString("in_Position") });
            shader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute() { Name = GamePatcher.Data.Strings.MakeString("in_Colour") });
            shader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute() { Name = GamePatcher.Data.Strings.MakeString("in_TextureCoord") });

            GamePatcher.Data.Shaders.Add(shader);
        }
    }
}
