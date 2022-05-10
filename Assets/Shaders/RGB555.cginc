half2 EncodeR5G5B5(half3 color)
{
    // scale up to 5-bit
    color = round(color * 31);

    // encode rgb into a 16-bit float
    float enc = color.r * 1024 + color.g * 32 + color.b;

    // pack it up
    half2 packed;
    packed.x = floor(enc / 256);
    packed.y = frac(enc / 256) * 256;
    packed /= 255;

    // to linear
    packed.r = GammaToLinearSpaceExact(packed.r);
    packed.g = GammaToLinearSpaceExact(packed.g);

    return packed;
}

half3 DecodeR5G5B5(half2 packed)
{
    // to gamma
    packed.r = LinearToGammaSpaceExact(packed.r);
    packed.g = LinearToGammaSpaceExact(packed.g);
    
    // scale up to 8-bit
    packed = round(packed * 255);   // _rrrrrgg, gggbbbbb

    // split the packed bits
    float2 split = packed / 4;      // rrrrr.gg, gggbbb.bb
    split.y /= 8;                   // rrrrr.gg, ggg.bbbbb

    // unpack
    float3 rgb16 = (0).rrr;
    rgb16.rg = floor(split);        // rrrrr, __ggg, _____
    rgb16.gb += frac(split) * 32;   //       +gg___ +bbbbb
    rgb16 /= 31;

    return rgb16;
}