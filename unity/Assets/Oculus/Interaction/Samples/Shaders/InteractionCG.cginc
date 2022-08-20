uniform half _DitherStrength;

inline half DitherAnimatedNoise(half2 screenPos) {
	half noise = frac(
		dot(uint3(screenPos, floor(fmod(_Time.y * 10, 4))), uint3(2, 7, 23) / 17.0f));
	noise -= 0.5; // remap from [0..1[ to [-0.5..0.5[
	half noiseScaled = (noise / _DitherStrength);
	return noiseScaled;
}