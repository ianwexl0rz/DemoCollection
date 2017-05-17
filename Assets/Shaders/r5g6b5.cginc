float2 EncodeR5G6B5(float3 rgb24)
{
	// scale up to 8-bit
	rgb24 *= 255.0f;

	// remove the 3 LSB of red and blue, and the 2 LSB of green
	int3 rgb16 = rgb24 / int3(8, 4, 8);

	// split the green at bit 3 (we'll keep the 6 bits around the split)
	float greenSplit = rgb16.g / 8.0f;

	// pack it up (capital G's are MSB, the rest are LSB)
	float2 packed;
	packed.x = rgb16.r * 8 + floor(greenSplit); // rrrrrGGG
	packed.y = frac(greenSplit) * 256 + rgb16.b; // gggbbbbb

	// scale down and return
	packed /= 255.0f;
	return packed;
}

float3 DecodeR5G6B5(float2 packed)
{
	// scale up to 8-bit
	packed *= 255.0f;

	// round and split the packed bits
	float2 split = round(packed) / 8; // first component at bit 3
	split.y /= 4; // second component at bit 5

	// unpack (obfuscated yet optimized crap follows)
	float3 rgb16 = 0.0f.rrr;
	rgb16.gb = frac(split) * 256;
	rgb16.rg += floor(split) * 4;
	rgb16.r *= 2;

	// scale down and return
	rgb16 /= 255.0f;
	return rgb16;
}