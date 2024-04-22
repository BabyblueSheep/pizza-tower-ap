using ArchpelagoPizzaTower.Patcher.Library;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public static void AddShader(string shaderName, string vertexCode, string fragmentCode)
        {
            UndertaleShader shader = new UndertaleShader();
            shader.Name = GamePatcher.Data.Strings.MakeString(shaderName);
            shader.Type = UndertaleShader.ShaderType.GLSL_ES;

            shader.GLSL_ES_Fragment = GamePatcher.Data.Strings.MakeString(
@"#define LOWPREC lowp
#define	MATRIX_VIEW 					0
#define	MATRIX_PROJECTION 				1
#define	MATRIX_WORLD 					2
#define	MATRIX_WORLD_VIEW 				3
#define	MATRIX_WORLD_VIEW_PROJECTION 	4
#define	MATRICES_MAX					5

uniform mat4 gm_Matrices[MATRICES_MAX]; 

uniform bool gm_LightingEnabled;
uniform bool gm_VS_FogEnabled;
uniform float gm_FogStart;
uniform float gm_RcpFogRange;

#define MAX_VS_LIGHTS	8
#define MIRROR_WIN32_LIGHTING_EQUATION


//#define	MAX_VS_LIGHTS					8
uniform vec4   gm_AmbientColour;							// rgb=colour, a=1
uniform vec4   gm_Lights_Direction[MAX_VS_LIGHTS];		// normalised direction
uniform vec4   gm_Lights_PosRange[MAX_VS_LIGHTS];			// X,Y,Z position,  W range
uniform vec4   gm_Lights_Colour[MAX_VS_LIGHTS];			// rgb=colour, a=1

float CalcFogFactor(vec4 pos)
{
	if (gm_VS_FogEnabled)
	{
		vec4 viewpos = gm_Matrices[MATRIX_WORLD_VIEW] * pos;
		float fogfactor = ((viewpos.z - gm_FogStart) * gm_RcpFogRange);
		return fogfactor;
	}
	else
	{
		return 0.0;
	}
}

vec4 DoDirLight(vec3 ws_normal, vec4 dir, vec4 diffusecol)
{
	float dotresult = dot(ws_normal, dir.xyz);
	dotresult = min(dotresult, dir.w);			// the w component is 1 if the directional light is active, or 0 if it isn't
	dotresult = max(0.0, dotresult);

	return dotresult * diffusecol;
}

vec4 DoPointLight(vec3 ws_pos, vec3 ws_normal, vec4 posrange, vec4 diffusecol)
{
	vec3 diffvec = ws_pos - posrange.xyz;
	float veclen = length(diffvec);
	diffvec /= veclen;	// normalise
	float atten;
	if (posrange.w == 0.0)		// the w component of posrange is 0 if the point light is disabled - if we don't catch it here we might end up generating INFs or NaNs
	{
		atten = 0.0;
	}
	else
	{
#ifdef MIRROR_WIN32_LIGHTING_EQUATION
	// This is based on the Win32 D3D and OpenGL falloff model, where:
	// Attenuation = 1.0f / (factor0 + (d * factor1) + (d*d * factor2))
	// For some reason, factor0 is set to 0.0f while factor1 is set to 1.0f/lightrange (on both D3D and OpenGL)
	// This'll result in no visible falloff as 1.0f / (d / lightrange) will always be larger than 1.0f (if the vertex is within range)
	
		atten = 1.0 / (veclen / posrange.w);
		if (veclen > posrange.w)
		{
			atten = 0.0;
		}	
#else
		atten = clamp( (1.0 - (veclen / posrange.w)), 0.0, 1.0);		// storing 1.0f/range instead would save a rcp
#endif
	}
	float dotresult = dot(ws_normal, diffvec);
	dotresult = max(0.0, dotresult);

	return dotresult * atten * diffusecol;
}

vec4 DoLighting(vec4 vertexcolour, vec4 objectspacepos, vec3 objectspacenormal)
{
	if (gm_LightingEnabled)
	{
		// Normally we'd have the light positions\\directions back-transformed from world to object space
		// But to keep things simple for the moment we'll just transform the normal to world space
		vec4 objectspacenormal4 = vec4(objectspacenormal, 0.0);
		vec3 ws_normal;
		ws_normal = (gm_Matrices[MATRIX_WORLD] * objectspacenormal4).xyz;
		ws_normal = normalize(ws_normal);

		vec3 ws_pos;
		ws_pos = (gm_Matrices[MATRIX_WORLD] * objectspacepos).xyz;

		// Accumulate lighting from different light types
		vec4 accumcol = vec4(0.0, 0.0, 0.0, 0.0);		
		for(int i = 0; i < MAX_VS_LIGHTS; i++)
		{
			accumcol += DoDirLight(ws_normal, gm_Lights_Direction[i], gm_Lights_Colour[i]);
		}

		for(int i = 0; i < MAX_VS_LIGHTS; i++)
		{
			accumcol += DoPointLight(ws_pos, ws_normal, gm_Lights_PosRange[i], gm_Lights_Colour[i]);
		}

		accumcol *= vertexcolour;
		accumcol += gm_AmbientColour;
		accumcol = min(vec4(1.0, 1.0, 1.0, 1.0), accumcol);
		accumcol.a = vertexcolour.a;
		return accumcol;
	}
	else
	{
		return vertexcolour;
	}
}

#define _YY_GLSLES_ 1
" + fragmentCode
                );
            shader.GLSL_ES_Vertex = GamePatcher.Data.Strings.MakeString(
@"precision mediump float;
#define LOWPREC lowp
// Uniforms look like they're shared between vertex and fragment shaders in GLSL, so we have to be careful to avoid name clashes

uniform sampler2D gm_BaseTexture;

uniform bool gm_PS_FogEnabled;
uniform vec4 gm_FogColour;
uniform bool gm_AlphaTestEnabled;
uniform float gm_AlphaRefValue;

void DoAlphaTest(vec4 SrcColour)
{
	if (gm_AlphaTestEnabled)
	{
		if (SrcColour.a <= gm_AlphaRefValue)
		{
			discard;
		}
	}
}

void DoFog(inout vec4 SrcColour, float fogval)
{
	if (gm_PS_FogEnabled)
	{
		SrcColour = mix(SrcColour, gm_FogColour, clamp(fogval, 0.0, 1.0)); 
	}
}

#define _YY_GLSLES_ 1
" + vertexCode);

            shader.GLSL_Vertex = GamePatcher.Data.Strings.MakeString(
@"#version 120
#define LOWPREC 
#define lowp
#define mediump
#define highp
#define precision
#define	MATRIX_VIEW 					0
#define	MATRIX_PROJECTION 				1
#define	MATRIX_WORLD 					2
#define	MATRIX_WORLD_VIEW 				3
#define	MATRIX_WORLD_VIEW_PROJECTION 	4
#define	MATRICES_MAX					5

uniform mat4 gm_Matrices[MATRICES_MAX]; 

uniform bool gm_LightingEnabled;
uniform bool gm_VS_FogEnabled;
uniform float gm_FogStart;
uniform float gm_RcpFogRange;

#define MAX_VS_LIGHTS	8
#define MIRROR_WIN32_LIGHTING_EQUATION


//#define	MAX_VS_LIGHTS					8
uniform vec4   gm_AmbientColour;							// rgb=colour, a=1
uniform vec4   gm_Lights_Direction[MAX_VS_LIGHTS];		// normalised direction
uniform vec4   gm_Lights_PosRange[MAX_VS_LIGHTS];			// X,Y,Z position,  W range
uniform vec4   gm_Lights_Colour[MAX_VS_LIGHTS];			// rgb=colour, a=1

float CalcFogFactor(vec4 pos)
{
	if (gm_VS_FogEnabled)
	{
		vec4 viewpos = gm_Matrices[MATRIX_WORLD_VIEW] * pos;
		float fogfactor = ((viewpos.z - gm_FogStart) * gm_RcpFogRange);
		return fogfactor;
	}
	else
	{
		return 0.0;
	}
}

vec4 DoDirLight(vec3 ws_normal, vec4 dir, vec4 diffusecol)
{
	float dotresult = dot(ws_normal, dir.xyz);
	dotresult = min(dotresult, dir.w);			// the w component is 1 if the directional light is active, or 0 if it isn't
	dotresult = max(0.0, dotresult);

	return dotresult * diffusecol;
}

vec4 DoPointLight(vec3 ws_pos, vec3 ws_normal, vec4 posrange, vec4 diffusecol)
{
	vec3 diffvec = ws_pos - posrange.xyz;
	float veclen = length(diffvec);
	diffvec /= veclen;	// normalise
	float atten;
	if (posrange.w == 0.0)		// the w component of posrange is 0 if the point light is disabled - if we don't catch it here we might end up generating INFs or NaNs
	{
		atten = 0.0;
	}
	else
	{
#ifdef MIRROR_WIN32_LIGHTING_EQUATION
	// This is based on the Win32 D3D and OpenGL falloff model, where:
	// Attenuation = 1.0f / (factor0 + (d * factor1) + (d*d * factor2))
	// For some reason, factor0 is set to 0.0f while factor1 is set to 1.0f/lightrange (on both D3D and OpenGL)
	// This'll result in no visible falloff as 1.0f / (d / lightrange) will always be larger than 1.0f (if the vertex is within range)
	
		atten = 1.0 / (veclen / posrange.w);
		if (veclen > posrange.w)
		{
			atten = 0.0;
		}	
#else
		atten = clamp( (1.0 - (veclen / posrange.w)), 0.0, 1.0);		// storing 1.0f/range instead would save a rcp
#endif
	}
	float dotresult = dot(ws_normal, diffvec);
	dotresult = max(0.0, dotresult);

	return dotresult * atten * diffusecol;
}

vec4 DoLighting(vec4 vertexcolour, vec4 objectspacepos, vec3 objectspacenormal)
{
	if (gm_LightingEnabled)
	{
		// Normally we'd have the light positions\\directions back-transformed from world to object space
		// But to keep things simple for the moment we'll just transform the normal to world space
		vec4 objectspacenormal4 = vec4(objectspacenormal, 0.0);
		vec3 ws_normal;
		ws_normal = (gm_Matrices[MATRIX_WORLD] * objectspacenormal4).xyz;
		ws_normal = normalize(ws_normal);

		vec3 ws_pos;
		ws_pos = (gm_Matrices[MATRIX_WORLD] * objectspacepos).xyz;

		// Accumulate lighting from different light types
		vec4 accumcol = vec4(0.0, 0.0, 0.0, 0.0);		
		for(int i = 0; i < MAX_VS_LIGHTS; i++)
		{
			accumcol += DoDirLight(ws_normal, gm_Lights_Direction[i], gm_Lights_Colour[i]);
		}

		for(int i = 0; i < MAX_VS_LIGHTS; i++)
		{
			accumcol += DoPointLight(ws_pos, ws_normal, gm_Lights_PosRange[i], gm_Lights_Colour[i]);
		}

		accumcol *= vertexcolour;
		accumcol += gm_AmbientColour;
		accumcol = min(vec4(1.0, 1.0, 1.0, 1.0), accumcol);
		accumcol.a = vertexcolour.a;
		return accumcol;
	}
	else
	{
		return vertexcolour;
	}
}

#define _YY_GLSL_ 1
" + vertexCode
                );
            shader.GLSL_Fragment = GamePatcher.Data.Strings.MakeString(
@"#version 120
#define LOWPREC 
#define lowp
#define mediump
#define highp
#define precision
// Uniforms look like they're shared between vertex and fragment shaders in GLSL, so we have to be careful to avoid name clashes

uniform sampler2D gm_BaseTexture;

uniform bool gm_PS_FogEnabled;
uniform vec4 gm_FogColour;
uniform bool gm_AlphaTestEnabled;
uniform float gm_AlphaRefValue;

void DoAlphaTest(vec4 SrcColour)
{
	if (gm_AlphaTestEnabled)
	{
		if (SrcColour.a <= gm_AlphaRefValue)
		{
			discard;
		}
	}
}

void DoFog(inout vec4 SrcColour, float fogval)
{
	if (gm_PS_FogEnabled)
	{
		SrcColour = mix(SrcColour, gm_FogColour, clamp(fogval, 0.0, 1.0)); 
	}
}

#define _YY_GLSL_ 1
" + fragmentCode
            );

            shader.HLSL9_Vertex = GamePatcher.Data.Strings.MakeString(
@"#define	MATRIX_VIEW 				0
#define	MATRIX_PROJECTION 				1
#define	MATRIX_WORLD 					2
#define	MATRIX_WORLD_VIEW 				3
#define	MATRIX_WORLD_VIEW_PROJECTION 	4
#define	MATRICES_MAX					5

float4x4 	gm_Matrices[MATRICES_MAX] : register(c0);

bool 	gm_LightingEnabled;
bool 	gm_VS_FogEnabled;
float 	gm_FogStart;
float 	gm_RcpFogRange;

#define	MAX_VS_LIGHTS					8
float4 gm_AmbientColour;							// rgb=colour, a=1
float3 gm_Lights_Direction[MAX_VS_LIGHTS];			// normalised direction
float4 gm_Lights_PosRange[MAX_VS_LIGHTS];			// X,Y,Z position,  W range
float4 gm_Lights_Colour[MAX_VS_LIGHTS];				// rgb=colour, a=1
    ");
            shader.HLSL9_Fragment = GamePatcher.Data.Strings.MakeString(
@"// GameMaker reserved and common types/inputs

sampler2D gm_BaseTexture : register(S0);

bool 	gm_PS_FogEnabled;
float4 	gm_FogColour;
bool 	gm_AlphaTestEnabled;
float4	gm_AlphaRefValue;

    ");

            shader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute() { Name = GamePatcher.Data.Strings.MakeString("in_Position") });
            shader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute() { Name = GamePatcher.Data.Strings.MakeString("in_Colour") });
            shader.VertexShaderAttributes.Add(new UndertaleShader.VertexShaderAttribute() { Name = GamePatcher.Data.Strings.MakeString("in_TextureCoord") });

            GamePatcher.Data.Shaders.Add(shader);
        }
    }
}
