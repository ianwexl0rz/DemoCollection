inline half3 Overlay (half3 a, half3 b)
{
    b /= unity_ColorSpaceGrey * 2.0;
    return a < 0.5 ? 2.0 * a * b : 1.0 - 2.0 * (1.0 - a) * (1.0 - b);
}