using UnityEngine;
using System;

namespace Smaa
{
	/// <summary>
	/// Holds a set of settings to use when <see cref="SMAA.UsePredication"/> is enabled.
	/// </summary>
	[Serializable]
	public class PredicationPreset
	{
		/// <summary>
		/// Threshold to be used in the additional predication buffer.
		/// </summary>
		[Min(0.0001f)]
		public float Threshold = 0.01f;

		/// <summary>
		/// How much to scale the global threshold used for luma or color edge detection when using predication.
		/// </summary>
		[Range(1f, 5f)]
		public float Scale = 2f;

		/// <summary>
		/// How much to locally decrease the threshold.
		/// </summary>
		[Range(0f, 1f)]
		public float Strength = 0.4f;
	}
}
