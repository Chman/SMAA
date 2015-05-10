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
	/// A bunch of quality presets. Use <see cref="QualityPreset.Custom"/> to fine tune every setting.
	/// </summary>
	public enum QualityPreset
	{
		/// <summary>
		/// 60% of the quality.
		/// </summary>
		Low = 0,

		/// <summary>
		/// 80% of the quality.
		/// </summary>
		Medium = 1,

		/// <summary>
		/// 90% of the quality.
		/// </summary>
		High = 2,

		/// <summary>
		/// 99% of the quality (generally overkill).
		/// </summary>
		Ultra = 3,

		/// <summary>
		/// Custom quality settings.
		/// </summary>
		/// <seealso cref="Preset"/>
		/// <seealso cref="SMAA.CustomPreset"/>
		Custom
	}
}
