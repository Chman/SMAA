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
using UnityEditor;
using Smaa;

namespace SmaaEditor
{
	[CustomEditor(typeof(SMAA))]
	public class SMAAEditor : Editor
	{
		SerializedProperty m_Hdr;
		SerializedProperty m_DebugPass;
		SerializedProperty m_DetectionMethod;
		SerializedProperty m_Quality;

		SerializedProperty m_CustomPreset;
		SerializedProperty m_CustomDiagDetection;
		SerializedProperty m_CustomCornerDetection;
		SerializedProperty m_CustomThreshold;
		SerializedProperty m_CustomDepthThreshold;
		SerializedProperty m_CustomMaxSearchSteps;
		SerializedProperty m_CustomMaxSearchStepsDiag;
		SerializedProperty m_CustomCornerRounding;
		SerializedProperty m_CustomLocalContrastAdaptationFactor;

		SerializedProperty m_UsePredication;
		SerializedProperty m_CustomPredicationPreset;
		SerializedProperty m_PredicationThreshold;
		SerializedProperty m_PredicationScale;
		SerializedProperty m_PredicationStrength;

		void OnEnable()
		{
			m_Hdr = serializedObject.FindProperty("Hdr");
			m_DebugPass = serializedObject.FindProperty("DebugPass");
			m_DetectionMethod = serializedObject.FindProperty("DetectionMethod");
			m_Quality = serializedObject.FindProperty("Quality");

			m_CustomPreset = serializedObject.FindProperty("CustomPreset");
			m_CustomDiagDetection = m_CustomPreset.FindPropertyRelative("DiagDetection");
			m_CustomCornerDetection = m_CustomPreset.FindPropertyRelative("CornerDetection");
			m_CustomThreshold = m_CustomPreset.FindPropertyRelative("Threshold");
			m_CustomDepthThreshold = m_CustomPreset.FindPropertyRelative("DepthThreshold");
			m_CustomMaxSearchSteps = m_CustomPreset.FindPropertyRelative("MaxSearchSteps");
			m_CustomMaxSearchStepsDiag = m_CustomPreset.FindPropertyRelative("MaxSearchStepsDiag");
			m_CustomCornerRounding = m_CustomPreset.FindPropertyRelative("CornerRounding");
			m_CustomLocalContrastAdaptationFactor = m_CustomPreset.FindPropertyRelative("LocalContrastAdaptationFactor");

			m_UsePredication = serializedObject.FindProperty("UsePredication");
			m_CustomPredicationPreset = serializedObject.FindProperty("CustomPredicationPreset");
			m_PredicationThreshold = m_CustomPredicationPreset.FindPropertyRelative("Threshold");
			m_PredicationScale = m_CustomPredicationPreset.FindPropertyRelative("Scale");
			m_PredicationStrength = m_CustomPredicationPreset.FindPropertyRelative("Strength");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_Hdr, new GUIContent("HDR", "Render target mode. Keep it to Auto unless you know what you're doing."));
			EditorGUILayout.PropertyField(m_DebugPass, new GUIContent("Debug Pass", "Use this to fine tune your settings when working in Custom quality mode."));
			EditorGUILayout.PropertyField(m_DetectionMethod, new GUIContent("Edge Detection Method", "You've three edge detection methods to choose from: luma, color or depth.\nThey represent different quality/performance and anti-aliasing/sharpness tradeoffs, so our recommendation is for you to choose the one that best suits your particular scenario:\n\n- Depth edge detection is usually the fastest but it may miss some edges.\n- Luma edge detection is usually more expensive than depth edge detection, but catches visible edges that depth edge detection can miss.\n- Color edge detection is usually the most expensive one but catches chroma-only edges."));
			EditorGUILayout.PropertyField(m_Quality, new GUIContent("Quality Preset", "Low: 60% of the quality.\nMedium: 80% of the quality.\nHigh: 95% of the quality.\nUltra: 99% of the quality."));

			if ((m_DetectionMethod.enumValueIndex == (int)EdgeDetectionMethod.Depth - 1) && IsOpenGL())
			{
				EditorGUILayout.HelpBox("EdgeDetectionMethod.Depth isn't supported on OpenGL. Please use Luma or Color instead.", MessageType.Warning);
			}

			if (m_Quality.enumValueIndex == (int)QualityPreset.Custom)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Custom Settings", EditorStyles.boldLabel);

				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(m_CustomDiagDetection, new GUIContent("Diagonal Detection", "Enables/Disables diagonal processing."));
				EditorGUILayout.PropertyField(m_CustomCornerDetection, new GUIContent("Corner Detection", "Enables/Disables corner detection. Leave this on to avoid blurry corners."));

				if (m_DetectionMethod.enumValueIndex != (int)EdgeDetectionMethod.Depth - 1)
					EditorGUILayout.PropertyField(m_CustomThreshold, new GUIContent("Threshold", "Specifies the threshold or sensitivity to edges. Lowering this value you will be able to detect more edges at the expense of performance.\n0.1 is a reasonable value, and allows to catch most visible edges. 0.05 is a rather overkill value, that allows to catch 'em all."));
				else
					EditorGUILayout.PropertyField(m_CustomDepthThreshold, new GUIContent("Depth Threshold", "Specifies the threshold for depth edge detection. Lowering this value you will be able to detect more edges at the expense of performance."));

				EditorGUILayout.PropertyField(m_CustomMaxSearchSteps, new GUIContent("Max Search Steps", "Specifies the maximum steps performed in the horizontal/vertical pattern searches, at each side of the pixel.\nIn number of pixels, it's actually the double. So the maximum line length perfectly handled by, for example 16, is 64 (by perfectly, we meant that longer lines won't look as good, but still antialiased)."));

				if (m_CustomDiagDetection.boolValue)
					EditorGUILayout.PropertyField(m_CustomMaxSearchStepsDiag, new GUIContent("Max Diagonal Search Steps", "Specifies the maximum steps performed in the diagonal pattern searches, at each side of the pixel. In this case we jump one pixel at time, instead of two.\nOn high-end machines it is cheap (between a 0.8x and 0.9x slower for 16 steps), but it can have a significant impact on older machines."));

				if (m_CustomCornerDetection.boolValue)
					EditorGUILayout.PropertyField(m_CustomCornerRounding, new GUIContent("Corner Rounding", "Specifies how much sharp corners will be rounded."));

				if (m_DetectionMethod.enumValueIndex != (int)EdgeDetectionMethod.Depth - 1)
					EditorGUILayout.PropertyField(m_CustomLocalContrastAdaptationFactor, new GUIContent("Local Contrast Adaptation Factor", "If there is an neighbor edge that has a local contrast factor times bigger contrast than current edge, current edge will be discarded.\nThis allows to eliminate spurious crossing edges, and is based on the fact that, if there is too much contrast in a direction, that will hide perceptually contrast in the other neighbors."));

				EditorGUI.indentLevel--;
			}

			if (m_DetectionMethod.enumValueIndex != (int)EdgeDetectionMethod.Depth - 1)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Predication", EditorStyles.boldLabel);

				EditorGUI.indentLevel++;
				EditorGUILayout.PropertyField(m_UsePredication, new GUIContent("Use Predication", "Predicated thresholding allows to better preserve texture details and to improve performance, by decreasing the number of detected edges using an additional buffer (the detph buffer).\nIt locally decreases the luma or color threshold if an edge is found in an additional buffer (so the global threshold can be higher)."));

				if (m_UsePredication.boolValue)
				{
					EditorGUILayout.PropertyField(m_PredicationThreshold, new GUIContent("Threshold", "Threshold to be used in the additional predication buffer."));
					EditorGUILayout.PropertyField(m_PredicationScale, new GUIContent("Scale", "How much to scale the global threshold used for luma or color edge detection when using predication."));
					EditorGUILayout.PropertyField(m_PredicationStrength, new GUIContent("Strength", "How much to locally decrease the threshold."));
				}
				EditorGUI.indentLevel--;
			}

			serializedObject.ApplyModifiedProperties();
		}

		bool IsOpenGL()
		{
			return SystemInfo.graphicsDeviceVersion.IndexOf("OpenGL") > -1;
		}
	}
}
