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

namespace Smaa
{
	/// <summary>
	/// You have three edge detection methods to choose from: luma, color or depth.
	/// They represent different quality/performance and anti-aliasing/sharpness tradeoffs, so our recommendation is
	/// for you to choose the one that best suits your particular scenario.
	/// </summary>
	public enum EdgeDetectionMethod
	{
		/// <summary>
		/// Luma edge detection is usually more expensive than depth edge detection, but catches visible edges that
		/// depth edge detection can miss.
		/// </summary>
		Luma = 1,

		/// <summary>
		/// Color edge detection is usually the most expensive one but catches chroma-only edges.
		/// </summary>
		Color = 2,

		/// <summary>
		/// Depth edge detection is usually the fastest but it may miss some edges.
		/// </summary>
		Depth = 3
	}
}
