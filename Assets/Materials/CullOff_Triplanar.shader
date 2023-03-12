Shader "Custom/CullOff_Triplanar"
{
	Properties
	{
		[Header(CullOff Main Texture)] [Space(10)]
		[HDR] _MainColor("Main Color", Color) = (0.5,0.5,0.5,1)
		[MainTexture] _MainTex("Main Texture", 2D) = "white" {}

		[Space(20)]
		[Header(Triplanar Scale Setting)][Space(10)]
		_TileScale("Tile Scale", Float) = 1

	}

		SubShader
		{
			Tags
			{
				"RenderType" = "Opaque"
				"Queue" = "Geometry+100"
				"RenderPipeline" = "UniversalRenderPipeline"
				"IgnoreProjector" = "True"
			}
			LOD 300

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
				#define sampler_BaseMap sampler_MainTex

			//--------------------------------------
			// GPU Instancing
			#pragma multi_compile_instancing

			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

			half4 _MainColor;

			float _TileScale;
			float _TileRotation;

			float _Progress;
			float _Specular;
			float _Occlusion;

			struct VertexInput
			{
				float4 position		: POSITION;
				float3 normal		: NORMAL;
				float3 uv			: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct VertexOutput
			{
				float4 position		: SV_POSITION;
				float3 normal		: NORMAL;
				float3 triPosition	: TEXCORRD2;
				float3 uv			: TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct TriplanarUV
			{
				float2 x, y, z;
			};

			UNITY_INSTANCING_BUFFER_START(Props)
			UNITY_INSTANCING_BUFFER_END(Props)

			VertexOutput LitPassVertex(VertexInput input)
			{
				VertexOutput output;

				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);

				output.position = TransformObjectToHClip(input.position.xyz);
				output.triPosition = TransformObjectToWorld(input.position.xyz);
				output.normal = input.normal;
				output.uv = input.uv;
				return output;
			}

			half4 LitPassFragment(VertexOutput input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);

				TriplanarUV triUV;
				float3 p = input.triPosition * _TileScale;
				triUV.x = p.yz;
				triUV.y = p.xz;
				triUV.z = p.xy;
				if (input.normal.x < 0) triUV.x.x = -triUV.x.x;
				if (input.normal.y < 0) triUV.y.x = -triUV.y.x;
				if (input.normal.z < 0) triUV.z.x = -triUV.z.x;
				triUV.x.y += 0.5;
				triUV.z.x += 0.5;

				half4 texX = _BaseMap.Sample(sampler_BaseMap, triUV.x) * _MainColor;
				half4 texY = _BaseMap.Sample(sampler_BaseMap, triUV.y) * _MainColor;
				half4 texZ = _BaseMap.Sample(sampler_BaseMap, triUV.z) * _MainColor;

				float3 triW = abs(input.normal);
				triW = saturate(triW);
				triW *= lerp(1, (float3)0, 0.5);
				triW = pow(triW, 8);
				triW /= (triW.x + triW.y + triW.z);

				return (texX * triW.x + texY * triW.y + texZ * triW.z);
			}
			ENDHLSL
		}
	}
}
