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

using UnityEngine;
using System;

namespace Smaa
{
	/// <summary>
	/// Implementation of Subpixel Morphological Antialiasing for Unity.
	/// </summary>
	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	[AddComponentMenu("Image Effects/Subpixel Morphological Antialiasing")]
	public class SMAA : MonoBehaviour
	{
		/// <summary>
		/// Render target mode. Keep it to <see cref="HDRMode.Auto"/> unless you know what you're doing.
		/// </summary>
		public HDRMode Hdr = HDRMode.Auto;

		/// <summary>
		/// Use this to fine tune your settings when working in Custom quality mode.
		/// </summary>
		/// <seealso cref="DebugPass"/>
		public DebugPass DebugPass = DebugPass.Off;

		/// <summary>
		/// Quality preset to use. Set to <see cref="QualityPreset.Custom"/> to fine tune every setting.
		/// </summary>
		/// <seealso cref="QualityPreset"/>
		public QualityPreset Quality = QualityPreset.High;

		/// <summary>
		/// You have three edge detection methods to choose from: luma, color or depth.
		/// They represent different quality/performance and anti-aliasing/sharpness tradeoffs, so our recommendation is
		/// for you to choose the one that best suits your particular scenario.
		/// </summary>
		/// <seealso cref="EdgeDetectionMethod"/>
		public EdgeDetectionMethod DetectionMethod = EdgeDetectionMethod.Luma;

		/// <summary>
		/// Predicated thresholding allows to better preserve texture details and to improve performance, by decreasing
		/// the number of detected edges using an additional buffer (the detph buffer).
		/// 
		/// It locally decreases the luma or color threshold if an edge is found in an additional buffer (so the global
		/// threshold can be higher).
		/// </summary>
		public bool UsePredication = false; // Unused with EdgeDetectionMethod.Depth

		/// <summary>
		/// Holds the custom preset to use with <see cref="QualityPreset.Custom"/>.
		/// </summary>
		public Preset CustomPreset;

		/// <summary>
		/// Holds the custom preset to use when <see cref="SMAA.UsePredication"/> is enabled.
		/// </summary>
		public PredicationPreset CustomPredicationPreset;

		/// <summary>
		/// The shader used by the processing effect.
		/// </summary>
		public Shader Shader;

		/// <summary>
		/// This texture allows to obtain the area for a certain pattern and distances to the left and to right of the
		/// line. Automatically set by the component if <c>null</c>.
		/// </summary>
		public Texture2D AreaTex;

		/// <summary>
		/// This texture allows to know how many pixels we must advance in the last step of our line search algorithm,
		/// with a single fetch. Automatically set by the component if <c>null</c>.
		/// </summary>
		public Texture2D SearchTex;

		/// <summary>
		/// A reference to the camera this component is added to.
		/// </summary>
		protected Camera m_Camera;

		/// <summary>
		/// The internal <see cref="Preset"/> used for <c>Low</c>, <c>Medium</c>, <c>High</c>, <c>Ultra</c>.
		/// </summary>
		protected Preset[] m_StdPresets;

		/// <summary>
		/// The internal <c>Material</c> instance. Use <see cref="Material"/> instead.
		/// </summary>
		protected Material m_Material;

		/// <summary>
		/// The <c>Material</c> instance used by the post-processing effect.
		/// </summary>
		public Material Material
		{
			get
			{
				if (m_Material == null)
				{
					m_Material = new Material(Shader);
					m_Material.hideFlags = HideFlags.HideAndDontSave;
				}

				return m_Material;
			}
		}

		void OnEnable()
		{
			// Make sure the helper textures are set
			if (AreaTex == null)
				AreaTex = Resources.Load<Texture2D>("AreaTex");

			if (SearchTex == null)
				SearchTex = Resources.Load<Texture2D>("SearchTex");

			// Misc
			m_Camera = GetComponent<Camera>();

			// Create default presets
			CreatePresets();
		}

		void Start()
		{
			// Disable if we don't support image effects
			if (!SystemInfo.supportsImageEffects)
			{
				enabled = false;
				return;
			}

			// Disable the image effect if the shader can't run on the user's graphics card
			if (!Shader || !Shader.isSupported)
				enabled = false;
		}

		void OnDisable()
		{
			// Cleanup
			if (m_Material != null)
			{
				#if UNITY_EDITOR
				DestroyImmediate(m_Material);
				#else
				Destroy(m_Material);
				#endif
			}
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			int width = m_Camera.pixelWidth;
			int height = m_Camera.pixelHeight;
			Preset preset = CustomPreset;

			if (Quality != QualityPreset.Custom)
				preset = m_StdPresets[(int)Quality];

			// Pass IDs
			int passEdgeDetection = (int)DetectionMethod;
			int passBlendWeights = 4;
			int passNeighborhoodBlending = 5;

			// Render format
			RenderTextureFormat renderFormat = source.format;

			if (Hdr == HDRMode.Off)
				renderFormat = RenderTextureFormat.ARGB32;
			else if (Hdr == HDRMode.On)
				renderFormat = RenderTextureFormat.ARGBHalf;

			// Uniforms
			Material.SetTexture("_AreaTex", AreaTex);
			Material.SetTexture("_SearchTex", SearchTex);

			Material.SetVector("_Metrics", new Vector4(1f / (float)width, 1f / (float)height, width, height));
			Material.SetVector("_Params1", new Vector4(preset.Threshold, preset.DepthThreshold, preset.MaxSearchSteps, preset.MaxSearchStepsDiag));
			Material.SetVector("_Params2", new Vector2(preset.CornerRounding, preset.LocalContrastAdaptationFactor));

			// Handle predication & depth-based edge detection
			Shader.DisableKeyword("USE_PREDICATION");

			if (DetectionMethod == EdgeDetectionMethod.Depth)
			{
				m_Camera.depthTextureMode |= DepthTextureMode.Depth;
			}
			else if (UsePredication)
			{
				m_Camera.depthTextureMode |= DepthTextureMode.Depth;
				Shader.EnableKeyword("USE_PREDICATION");
				Material.SetVector("_Params3", new Vector3(CustomPredicationPreset.Threshold, CustomPredicationPreset.Scale, CustomPredicationPreset.Strength));
			}

			// Diag search & corner detection
			Shader.DisableKeyword("USE_DIAG_SEARCH");
			Shader.DisableKeyword("USE_CORNER_DETECTION");

			if (preset.DiagDetection)
				Shader.EnableKeyword("USE_DIAG_SEARCH");

			if (preset.CornerDetection)
				Shader.EnableKeyword("USE_CORNER_DETECTION");

			// Temporary render textures
			RenderTexture rt1 = TempRT(width, height, renderFormat);
			RenderTexture rt2 = TempRT(width, height, renderFormat);

			// Clear both temp RTs as they could (and will) be filled with garbage
			Clear(rt1);
			Clear(rt2);

			// Edge Detection
			Graphics.Blit(source, rt1, Material, passEdgeDetection);

			if (DebugPass == DebugPass.Edges)
			{
				Graphics.Blit(rt1, destination);
			}
			else
			{
				// Blend Weights
				Graphics.Blit(rt1, rt2, Material, passBlendWeights);

				if (DebugPass == DebugPass.Weights)
				{
					Graphics.Blit(rt2, destination);
				}
				else
				{
					// Neighborhood Blending
					Material.SetTexture("_BlendTex", rt2);
					Graphics.Blit(source, destination, Material, passNeighborhoodBlending);
				}
			}

			// Cleanup
			RenderTexture.ReleaseTemporary(rt1);
			RenderTexture.ReleaseTemporary(rt2);
		}

		void Clear(RenderTexture rt)
		{
			Graphics.Blit(rt, rt, Material, 0);
		}

		RenderTexture TempRT(int width, int height, RenderTextureFormat format)
		{
			// Skip the depth & stencil buffer creation when DebugPass is set to avoid flickering
			// TODO: Stencil buffer not working for some reason
			// int depthStencilBits = DebugPass == DebugPass.Off ? 24 : 0;
			int depthStencilBits = 0;
			return RenderTexture.GetTemporary(width, height, depthStencilBits, format, RenderTextureReadWrite.Linear);
		}

		void CreatePresets()
		{
			m_StdPresets = new Preset[4];

			// Low
			m_StdPresets[0] = new Preset
			{
				Threshold = 0.15f,
				MaxSearchSteps = 4
			};
			m_StdPresets[0].DiagDetection = false; // Can't use object initializer for bool (weird mono bug ?)
			m_StdPresets[0].CornerDetection = false;

			// Medium
			m_StdPresets[1] = new Preset
			{
				Threshold = 0.1f,
				MaxSearchSteps = 8
			};
			m_StdPresets[1].DiagDetection = false;
			m_StdPresets[1].CornerDetection = false;

			// High
			m_StdPresets[2] = new Preset
			{
				Threshold = 0.1f,
				MaxSearchSteps = 16,
				MaxSearchStepsDiag = 8,
				CornerRounding = 25
			};

			// Ultra
			m_StdPresets[3] = new Preset
			{
				Threshold = 0.05f,
				MaxSearchSteps = 32,
				MaxSearchStepsDiag = 16,
				CornerRounding = 25
			};
		}
	}
}
