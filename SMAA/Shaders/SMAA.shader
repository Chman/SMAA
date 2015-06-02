/*
 * Copyright (c) 2015 Thomas Hourdel
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *    1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 
 *    2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 
 *    3. This notice may not be removed or altered from any source
 *    distribution.
 */

Shader "Hidden/Subpixel Morphological Antialiasing"
{
	Properties
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}

	CGINCLUDE
	
		#pragma fragmentoption ARB_precision_hint_fastest 
		#pragma target 3.0
		#pragma glsl
		#pragma exclude_renderers flash
		
		sampler2D _MainTex;
		sampler2D _BlendTex;
		sampler2D _AreaTex;
		sampler2D _SearchTex;

		sampler2D _CameraDepthTexture;
		
		float4 _MainTex_TexelSize;

		float4 _Metrics; // 1f / width, 1f / height, width, height
		float4 _Params1; // SMAA_THRESHOLD, SMAA_DEPTH_THRESHOLD, SMAA_MAX_SEARCH_STEPS, SMAA_MAX_SEARCH_STEPS_DIAG
		float2 _Params2; // SMAA_CORNER_ROUNDING, SMAA_LOCAL_CONTRAST_ADAPTATION_FACTOR
		float3 _Params3; // SMAA_PREDICATION_THRESHOLD, SMAA_PREDICATION_SCALE, SMAA_PREDICATION_STRENGTH

		#define SMAA_RT_METRICS _Metrics
		#define SMAA_THRESHOLD _Params1.x
		#define SMAA_DEPTH_THRESHOLD _Params1.y
		#define SMAA_MAX_SEARCH_STEPS _Params1.z
		#define SMAA_MAX_SEARCH_STEPS_DIAG _Params1.w
		#define SMAA_CORNER_ROUNDING _Params2.x
		#define SMAA_LOCAL_CONTRAST_ADAPTATION_FACTOR _Params2.y
		#define SMAA_PREDICATION_THRESHOLD _Params3.x
		#define SMAA_PREDICATION_SCALE _Params3.y
		#define SMAA_PREDICATION_STRENGTH _Params3.z

		// Can't use SMAA_HLSL_3 as it won't compile with OpenGL, so lets make our own set of defines for Unity
		#define SMAA_CUSTOM_SL

		#define mad(a, b, c) (a * b + c)
		#define SMAATexture2D(tex) sampler2D tex
		#define SMAATexturePass2D(tex) tex
		#define SMAASampleLevelZero(tex, coord) tex2Dlod(tex, float4(coord, 0.0, 0.0))
		#define SMAASampleLevelZeroPoint(tex, coord) tex2Dlod(tex, float4(coord, 0.0, 0.0))
		#define SMAASampleLevelZeroOffset(tex, coord, offset) tex2Dlod(tex, float4(coord + offset * SMAA_RT_METRICS.xy, 0.0, 0.0))
		#define SMAASample(tex, coord) tex2D(tex, coord)
		#define SMAASamplePoint(tex, coord) tex2D(tex, coord)
		#define SMAASampleOffset(tex, coord, offset) tex2D(tex, coord + offset * SMAA_RT_METRICS.xy)

		#define SMAA_FLATTEN UNITY_FLATTEN
		#define SMAA_BRANCH UNITY_BRANCH
		// SMAA_CUSTOM_SL

		#define SMAA_AREATEX_SELECT(sample) sample.rg
		#define SMAA_SEARCHTEX_SELECT(sample) sample.a
		#define SMAA_INCLUDE_VS 0

		struct vInput
		{
			float4 pos : POSITION;
			float2 uv : TEXCOORD0;
		};

		struct fInput_edge
		{
			float4 pos : SV_POSITION;
			float2 uv : TEXCOORD0;
			float4 offset[3] : TEXCOORD1;
		};

		fInput_edge vert_edge(vInput i)
		{
			fInput_edge o;
			o.pos = mul(UNITY_MATRIX_MVP, i.pos);
			o.uv = i.uv;

			#if UNITY_UV_STARTS_AT_TOP
			if (_MainTex_TexelSize.y < 0)
				o.uv.y = 1.0 - o.uv.y;
			#endif

			o.offset[0] = mad(SMAA_RT_METRICS.xyxy, float4(-1.0, 0.0, 0.0, -1.0), o.uv.xyxy);
			o.offset[1] = mad(SMAA_RT_METRICS.xyxy, float4( 1.0, 0.0, 0.0,  1.0), o.uv.xyxy);
			o.offset[2] = mad(SMAA_RT_METRICS.xyxy, float4(-2.0, 0.0, 0.0, -2.0), o.uv.xyxy);
			return o;
		}

	ENDCG

	SubShader
	{
		ZTest Always Cull Off ZWrite Off
		Fog { Mode off }

		// (0) Clear
		Pass
		{
			CGPROGRAM

				#pragma vertex vert_img
				#pragma fragment frag
				#include "UnityCG.cginc"

				float4 frag(v2f_img i) : COLOR
				{
					return float4(0.0, 0.0, 0.0, 0.0);
				}

			ENDCG
		}


		// -----------------------------------------------------------------------------
		// Edge Detection

		// (1) Luma
		Pass
		{
			// TODO: Stencil not working
		//	Stencil
		//	{
		//		Pass replace
		//		Ref 1
		//	}

			CGPROGRAM

				#pragma vertex vert_edge
				#pragma fragment frag
				#pragma multi_compile __ USE_PREDICATION

				#if USE_PREDICATION
				#define SMAA_PREDICATION 1
				#else
				#define SMAA_PREDICATION 0
				#endif

				#include "UnityCG.cginc"
				#include "SMAA.cginc"

				float4 frag(fInput_edge i) : COLOR
				{
					#if SMAA_PREDICATION
					return float4(SMAALumaEdgeDetectionPS(i.uv, i.offset, _MainTex, _CameraDepthTexture), 0.0, 0.0);
					#else
					return float4(SMAALumaEdgeDetectionPS(i.uv, i.offset, _MainTex), 0.0, 0.0);
					#endif
				}

			ENDCG
		}

		// (2) Color
		Pass
		{
			// TODO: Stencil not working
		//	Stencil
		//	{
		//		Pass replace
		//		Ref 1
		//	}

			CGPROGRAM

				#pragma vertex vert_edge
				#pragma fragment frag
				#pragma multi_compile __ USE_PREDICATION

				#if USE_PREDICATION
				#define SMAA_PREDICATION 1
				#else
				#define SMAA_PREDICATION 0
				#endif

				#include "UnityCG.cginc"
				#include "SMAA.cginc"

				float4 frag(fInput_edge i) : COLOR
				{
					#if SMAA_PREDICATION
					return float4(SMAAColorEdgeDetectionPS(i.uv, i.offset, _MainTex, _CameraDepthTexture), 0.0, 0.0);
					#else
					return float4(SMAAColorEdgeDetectionPS(i.uv, i.offset, _MainTex), 0.0, 0.0);
					#endif
				}

			ENDCG
		}

		// (3) Depth
		Pass
		{
			// TODO: Stencil not working
		//	Stencil
		//	{
		//		Pass replace
		//		Ref 1
		//	}

			CGPROGRAM

				#pragma vertex vert_edge
				#pragma fragment frag

				#define SMAA_PREDICATION 0

				#include "UnityCG.cginc"
				#include "SMAA.cginc"

				float4 frag(fInput_edge i) : COLOR
				{
					return float4(SMAADepthEdgeDetectionPS(i.uv, i.offset, _CameraDepthTexture), 0.0, 0.0);
				}

			ENDCG
		}


		// -----------------------------------------------------------------------------
		// Blend Weights Calculation

		// (4) 
		Pass
		{
			// TODO: Stencil not working
		//	Stencil
		//	{
		//		Pass keep
		//		Comp equal
		//		Ref 1
		//	}

			CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile __ USE_DIAG_SEARCH
				#pragma multi_compile __ USE_CORNER_DETECTION

				#if !defined(USE_DIAG_SEARCH)
				#define SMAA_DISABLE_DIAG_DETECTION
				#endif

				#if !defined(USE_CORNER_DETECTION)
				#define SMAA_DISABLE_CORNER_DETECTION
				#endif

				#include "UnityCG.cginc"
				#include "SMAA.cginc"

				struct fInput
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float2 pixcoord : TEXCOORD1;
					float4 offset[3] : TEXCOORD2;
				};

				fInput vert(vInput i)
				{
					fInput o;
					o.pos = mul(UNITY_MATRIX_MVP, i.pos);
					o.uv = i.uv;
					o.pixcoord = o.uv * SMAA_RT_METRICS.zw;
	
					// We will use these offsets for the searches later on (see @PSEUDO_GATHER4):
					o.offset[0] = mad(SMAA_RT_METRICS.xyxy, float4(-0.25, -0.125,  1.25, -0.125), o.uv.xyxy);
					o.offset[1] = mad(SMAA_RT_METRICS.xyxy, float4(-0.125, -0.25, -0.125,  1.25), o.uv.xyxy);
	
					// And these for the searches, they indicate the ends of the loops:
					o.offset[2] = mad(SMAA_RT_METRICS.xxyy, float4(-2.0, 2.0, -2.0, 2.0) * float(SMAA_MAX_SEARCH_STEPS),
									float4(o.offset[0].xz, o.offset[1].yw));

					return o;
				}

				float4 frag(fInput i) : COLOR
				{
					return SMAABlendingWeightCalculationPS(i.uv, i.pixcoord, i.offset, _MainTex, _AreaTex, _SearchTex,
									float4(0.0, 0.0, 0.0, 0.0));
				}

			ENDCG
		}


		// -----------------------------------------------------------------------------
		// Neighborhood Blending

		// (5)
		Pass
		{
			CGPROGRAM

				#pragma vertex vert
				#pragma fragment frag
				#include "UnityCG.cginc"
				#include "SMAA.cginc"

				struct fInput
				{
					float4 pos : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 offset : TEXCOORD1;
				};

				fInput vert(vInput i)
				{
					fInput o;
					o.pos = mul(UNITY_MATRIX_MVP, i.pos);
					o.uv = i.uv;
					o.offset = mad(SMAA_RT_METRICS.xyxy, float4(1.0, 0.0, 0.0, 1.0), o.uv.xyxy);
					return o;
				}

				float4 frag(fInput i) : COLOR
				{
					return SMAANeighborhoodBlendingPS(i.uv, i.offset, _MainTex, _BlendTex);
				}

			ENDCG
		}
	}

	FallBack off
}