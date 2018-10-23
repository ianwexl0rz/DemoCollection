inline half3 RGBToYCoCg(half3 c)
{
    return half3(0.25 * c.r + 0.5 * c.g + 0.25 * c.b, 0.5 * c.r - 0.5 * c.b + 0.5, -0.25 * c.r + 0.5 * c.g - 0.25 * c.b + 0.5);
}

inline half3 YCoCgToRGB(half3 c)
{
    c.y -= 0.5;
    c.z -= 0.5;
    return half3(c.r + c.g - c.b, c.r + c.b, c.r - c.g - c.b);
}