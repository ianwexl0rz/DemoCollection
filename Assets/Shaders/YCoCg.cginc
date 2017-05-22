inline half mod(half x, half y)
{
    // This has the same sign as y, unlike fmod which has the same sign as x.
    return x - y * floor(x / y);
}

inline half2 ForwardLift(half x, half y)
{
    half diff = mod(y - x, 256);
    half average = mod(x + floor(diff * 0.5), 256);
    return half2(average, diff);
}

inline half2 ReverseLift(half average, half diff)
{
    half2 o;
    o.x = mod(average - floor(diff * 0.5), 256);
    o.y = mod(o.x + diff, 256);
    return o;
}

inline half3 RGBToYCoCg(half3 color)
{
    color = round(color * 255);

    half4 ycocg;
    ycocg.ag = ForwardLift(color.r, color.b);
    ycocg.rb = ForwardLift(color.g, ycocg.a);
    return ycocg.rgb / 255;
}

inline half3 YCoCgToRGB(half3 ycocg)
{
    ycocg = round(ycocg * 255);

    half4 color;
    color.ga = ReverseLift(ycocg.r, ycocg.b);
    color.rb = ReverseLift(color.a, ycocg.g);
    return color.rgb / 255;
}