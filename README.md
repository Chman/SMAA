# SMAA for Unity3D

This is a highly customizable implementation of [Subpixel Morphological Antialiasing](http://www.iryoku.com/smaa/) for Unity3D.

Tested with Unity 5+ (Personal or Pro). Works with the deferred & forward rendering paths, in gamma or linear color space. Works with Directx 9 and Directx 11 targets, but it doesn't fully works with OpenGL at the moment (the edge detection pass is a bit broken).

It comes with a few quality presets but you can easily build your own in the inspector. Every inspector setting comes with a help popup so you shouldn't have to dig into the (documented) source code.

Right now it implements SMAA 1x (+ predication). Implementing Temporal SMAA (T2x) should be doable, but the spatial (S2x) and spatial + temporal (4x) variants aren't possible in Unity right now.

## Instructions

Drop the `SMAA` folder in your project and add the `SMAA` script to your camera (or select your camera and use `Component -> Image Effects -> Subpixel Morphological Antialiasing`).

Pull requests are welcomed !

## License

Zlib (see [License.txt](LICENSE.txt))
