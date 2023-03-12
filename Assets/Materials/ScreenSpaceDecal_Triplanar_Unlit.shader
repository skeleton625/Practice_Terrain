Shader "Decal/ScreenSpaceDecal_Triplanar_Unlit"
{
	Properties
	{
		[Header(Decal Main Texture)] [Space(10)]
		[HDR] _MainColor("Main Color", Color) = (0.5,0.5,0.5,1)
		[MainTexture] _MainTex("Main Texture", 2D) = "white" {}

		[Space(20)]
		[Header(Decal Time Setting)][Space(10)]
		_TileScale("Main Scale", Float) = 1
		_TileRotation("Texture Y Rotation", Float) = 0
		_TilePosition("Tile Position", Vector) = (0, 0, 0, 0)
		_TileOffset("Tile Offset", Vector) = (0, 0, 0, 0)
	}

		SubShader
		{
			Tags
			{
				"RenderType" = "Transparent"
				"Queue" = "Geometry+100"
				"RenderPipeline" = "UniversalRenderPipeline"
				"IgnoreProjector" = "True"
			}
			LOD 100

			Pass
			{
				Name "StandardLit"
				Tags{"LightMode" = "UniversalForward"}

				Cull Off
				ZTest Off
				ZWrite Off
				Blend SrcAlpha OneMinusSrcAlpha

				HLSLPROGRAM
				#pragma prefer_hlslcc gles
				#pragma exclude_renderers d3d11_9x
				#pragma target 2.0

				#define _BaseMap _MainTex
				#define _BaseScale _TileScale
				#define _BaseMap_ST _MainTex_ST
				#define sampler_BaseMap sampler_MainTex

			// -------------------------------------
			// Material Keywords
			// unused shader_feature variants are stripped from build automatically
			#pragma shader_feature _NORMALMAP

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

			float _TileScale;
			float _TileRotation;
			Vector _TilePosition;
			Vector _TileOffset;

			struct Attributes
			{
				float4 positionOS   : POSITION;
				float3 normalOS     : NORMAL;
				float4 tangentOS    : TANGENT;
				float4 uv           : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct Varyings
			{
				float4 positionCS               : SV_POSITION;
				float4 positionWSAndFogFactor   : TEXCOORD2;
				half3  normalWS                 : TEXCOORD3;
				float3 worldDirection			: TEXCOORD4;
				float4 uv                       : TEXCOORD0;
				half3 tangentWS                 : TEXCOORD5;
				half3 bitangentWS               : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_DEFINE_INSTANCED_PROP(half4, _MainColor)
			UNITY_INSTANCING_BUFFER_END(Props)

			Varyings LitPassVertex(Attributes input)
			{
				Varyings output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				output.positionWSAndFogFactor = float4(vertexInput.positionWS, ComputeFogFactor(vertexInput.positionCS.z));
				output.uv = vertexInput.positionNDC;
				output.normalWS = TransformObjectToWorldDir(float3(0, 1, 0));
				output.tangentWS = TransformObjectToWorldDir(float3(1, 0, 0));
				output.bitangentWS = TransformObjectToWorldDir(float3(0, 0, 1));

				output.positionCS = vertexInput.positionCS;
				output.worldDirection = vertexInput.positionWS.xyz - _WorldSpaceCameraPos;
				return output;
			}

			TEXTURE2D_X(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);
			TEXTURE2D(_ProgressNoise);
			SAMPLER(sampler_ProgressNoise);

			float SampleSceneDepth(float4 uv)
			{
				return SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(uv.xy / uv.w)).r;
			}

			half4 LitPassFragment(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				float3 positionWS = input.positionWSAndFogFactor.xyz;
				float perspectiveDivide = 1.0f / input.uv.w;
				float3 direction = input.worldDirection * perspectiveDivide;
				float4 testUV = input.uv;

				float depth = SampleSceneDepth(input.uv);
				float sceneZ = LinearEyeDepth(depth, _ZBufferParams);

				float3 wpos = direction * sceneZ + _WorldSpaceCameraPos;
				float2 uv = wpos.xz * _TileScale;

				float2 sc;
				sincos((_TileRotation / 180 * 3.14159), sc.x, sc.y);
				uv += _TilePosition;
				input.uv = float4(float2(dot(uv, float2(sc.y, -sc.x)), dot(uv, sc.xy)) + .5 + _TileOffset.xy, 0, 0);

				half4 albedoAlpha = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
				half4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _MainColor);
				half4 decalColor = albedoAlpha * baseColor;

				float3 absOpos = abs(TransformWorldToObject(wpos));
				decalColor.a *= step(max(absOpos.x, max(absOpos.y, absOpos.z)), 0.5);
				return decalColor;
			}
			ENDHLSL
		}
		}
}
