half2 EncodeR5G5B5(half3 color)
{
    // scale up to 5-bit
    color = round(color * 31);

    // encode rgb into a 16-bit float
    float enc = color.r * 1024 + color.g * 32 + color.b;

    // pack it up
    half2 packed;
    packed.x = floor(enc / 256);
    packed.y = enc - packed.x * 256;

    // scale down and return
    return packed / 255;
}

half3 DecodeR5G5B5(float2 packed)
{
    // scale up to 8-bit
    packed = round(packed * 255);

    // split the packed bits
    float2 split = packed / 4; // -rrrrr|GG
    split.y /= 8;              // ggg|bbbbb

    // unpack
    float3 rgb16 = 0.0f.rrr;
    rgb16.rg = floor(split);
    rgb16.gb += frac(split) * 32;

    // scale down and return
    return rgb16 / 31;
}