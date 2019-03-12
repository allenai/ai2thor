Magic Mirror, by Jeff Johnson, CEO Digital Ruby, LLC
http://www.digitalruby.com

Version: 1.2.0

Magic Mirror is a very realistic mirror effect for Unity.

A demonstration prefab is included with a mirror frame, as well as a crack to give you an idea of how to construct a mirror. When loading this prefab into your scene, you may want to copy and paste it, or break the prefab so that you can customize your own mirror how you like. The crack and frame are entirely optional, and are just added for extra effect. If you don't like them, simply delete them.

MirrorScript.cs controls handling of the mirror render texture as well as dealing with how the mirror camera behaves. The script supports any rotation of the mirror, so you can have it face up, down, sideways, rotated however you want.

The mirror material uses a custom shader with a number of parameters that control how the mirror renders.

- _MainTex ("", 2D) = "black" {}
This texture is set by MirrorScript.cs to show a reflection. It is possible to not use MirrorScript.cs and supply your own texture if you want to really make a magic mirror, such as a camera in another part of your scene, or a static texture of your choice. In this case, leave _MainTex null and set _DetailTex to your render texture or other texture.

- _DetailTex ("Detail Texture (RGB)", 2D) = "white" {}
This texture can give the mirror a silvery / filmy like overlay, and is entirely optional. The supplied prefab sets this to a silvery / filmy texture. If you have removed the MirrorScript.cs file and are supplying your own texture, make sure to set _DetailTex, and NOT _MainTex, _MainTex should be null if you are supplying your own mirror texture (like your own camera somewhere else in the scene).

- _Color ("Detail Tint Color", Color) = (1,1,1,1)
Controls tinting of the _DetailTex parameter. For a mostly shiny mirror, you would want to keep this to a mostly black color. If you want the reflection to be barely visible and have the detail texture show more, you can change this to a mostly white color. Should you be supplying your own mirror texture and disabled MirrorScript.cs, you'll probably want the tint color mostly white.

- _SpecColor ("Specular Color", Color) = (1,1,1,1)
Tints the color of light as it reflects off of the mirror

- _SpecularArea ("Specular Area", Range (0, 0.99)) = 0.1
How much the total surface area of the mirror reflects light. The larger the value, the more the surface reflects light. Higher values cause the entire mirror to reflect light, while smaller values keep the majority of the reflection to the point of the mirror that the light impacts most.

- _SpecularIntensity ("Specular Intensity", Range (0, 1)) = 0.75
The reflectivity of light off the mirror. The higher the value, the more the mirror will reflect light off and give a more blinding like effect.

- _ReflectionColor ("Reflection Tint Color", Color) = (1,1,1,1)
Allows tinting of the reflection (_MainTex)

Limitations / Other Tidbits:
- Magic Mirror works well as long as there isn't another magic mirror in the visible plane of another magic mirror. For proper mirror recursion, see Magic Mirror Pro: https://www.assetstore.unity3d.com/en/#!/content/103687?aid=1011lGnL
- Water refuses to play nice with this, and so for now, the water layer will not be rendered inside mirrors.
- Magic Mirror can be performance intensive, especially if placed outdoors or in scenes with lots of lights or other effects. To reduce performance problems, you can tweak the MaximumPerPixelLights parameter to be lower.
- Magic Mirror works best on quads, and it's best to stick with the prefab object and customize it as needed.

Contact Me:
I am always very willing to answer any other questions you may have. Please contact me at jjxtra@gmail.com and I'll be happy to help you with Magic Mirror.

If you need more than one mirror and/or recursive reflections that support water, get Magic Mirror Pro: Recursive edition
https://www.assetstore.unity3d.com/en/#!/content/103687?aid=1011lGnL

Thanks!

- Jeff



