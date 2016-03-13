# SMAA for Unity3D

This is a highly customizable implementation of [Subpixel Morphological Antialiasing](http://www.iryoku.com/smaa/) for Unity3D.

Tested with Unity 5+ (Personal or Pro). Works with the deferred & forward rendering paths, in gamma or linear color space, with Directx 9, Directx 11 and OpenGL targets.

It comes with a few quality presets but you can easily build your own in the inspector. Every inspector setting comes with a help popup so you shouldn't have to dig into the (documented) source code.

Right now it implements SMAA 1x (+ predication). Implementing Temporal SMAA (T2x) should be doable, but the spatial (S2x) and spatial + temporal (4x) variants aren't possible in Unity right now.

[Comparison screenshots with FXAA and Supersampling](http://imgur.com/a/J75KB).

## Instructions

Drop the `SMAA` folder in your project and add the `SMAA` script to your camera (or select your camera and use `Component -> Image Effects -> Subpixel Morphological Antialiasing`). The effect should be the first in the post-processing chain (before `Bloom`, `Tonemapping`, `DoF` etc) or it will miss some edges, especially when working in HDR.

## To infinity and beyond

An updated version of this effect is available in Unity's official [Cinematic Image Effects](https://bitbucket.org/Unity-Technologies/cinematic-image-effects) repository (Unity 5.3+ only).

## License

Zlib (see [License.txt](LICENSE.txt))
