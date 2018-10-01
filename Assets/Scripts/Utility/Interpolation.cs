using UnityEngine;

public static class Interpolation
{
    public static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float s = 1 - t;
        float tt = t*t;
        float ss = s*s;
        float sss = ss * s;
        float ttt = tt * t;
        
        Vector3 p = sss * p0;
        p += 3 * ss * t * p1;
        p += 3 * s * tt * p2;
        p += ttt * p3;
        
        return p;
    }
    
    public static Vector3 Hermite(Vector3 p0, Vector3 p1, Vector3 v0, Vector3 v1, float t)
    {
        float s = 1 - t;
        float tt = t*t;
        float ss = s*s;
        
        Vector3 p = ss * (1 + 2 * t) * p0;
        p += tt * (1 + 2 * s) * p1;
        p += ss * t * v0;
        p -= tt * s * v1;
        
        return p;
    }
    
    public static Vector3 Cardinal(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension)
    {
        float length = 1f - tension;
        
        Vector3 v1 = length * (p2 - p0) * 0.5f;
        Vector3 v2 = length * (p3 - p1) * 0.5f;
        
        return Hermite(p1, p2, v1, v2, t);
    }
    
    public static Vector3 CentripetalCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        if (p1 == p2) return p2;
        
        float alpha = 0.5f;
        
        float d0 = 0f;
        float d1 = Mathf.Pow((p1 - p0).magnitude, alpha);
        float d2 = Mathf.Pow((p2 - p1).magnitude, alpha) + d1;
        float d3 = Mathf.Pow((p3 - p2).magnitude, alpha) + d2;
        
        t = Mathf.Lerp(d1,d2,t);
        
        Vector3 a1 = p1;
        Vector3 a2 = (d2 - t) / (d2 - d1) * p1 + (t - d1) / (d2 - d1) * p2;
        Vector3 a3 = p2;
        
        if (d1 > d0) a1 = (d1 - t) / (d1 - d0) * p0 + (t - d0) / (d1 - d0) * p1;
        if (d3 > d2) a3 = (d3 - t) / (d3 - d2) * p2 + (t - d2) / (d3 - d2) * p3;
        
        Vector3 b1 = (d2 - t) / (d2 - d0) * a1 + (t - d0) / (d2 - d0) * a2;
        Vector3 b2 = (d3 - t) / (d3 - d1) * a2 + (t - d1) / (d3 - d1) * a3;
        
        return (d2 - t) / (d2 - d1) * b1 + (t - d1) / (d2 - d1) * b2;
    }

    public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float alpha)
    {
        if (p1 == p2) return p2;
        
        float d0 = 0f;
        float d1 = Mathf.Pow((p1 - p0).magnitude, alpha);
        float d2 = Mathf.Pow((p2 - p1).magnitude, alpha) + d1;
        float d3 = Mathf.Pow((p3 - p2).magnitude, alpha) + d2;
        
        t = Mathf.Lerp(d1,d2,t);
        
        Vector3 a1 = p1;
        Vector3 a2 = (d2 - t) / (d2 - d1) * p1 + (t - d1) / (d2 - d1) * p2;
        Vector3 a3 = p2;
        
        if (d1 > d0) a1 = (d1 - t) / (d1 - d0) * p0 + (t - d0) / (d1 - d0) * p1;
        if (d3 > d2) a3 = (d3 - t) / (d3 - d2) * p2 + (t - d2) / (d3 - d2) * p3;
        
        Vector3 b1 = (d2 - t) / (d2 - d0) * a1 + (t - d0) / (d2 - d0) * a2;
        Vector3 b2 = (d3 - t) / (d3 - d1) * a2 + (t - d1) / (d3 - d1) * a3;
        
        return (d2 - t) / (d2 - d1) * b1 + (t - d1) / (d2 - d1) * b2;
    }
    
    public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return Cardinal(p0, p1, p2, p3, t, 0f);
    }
    
    public static Vector3 KochanekBartel(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t, float tension, float continuity, float bias)
    {
        float length = 1f - tension;
        float pull = 1f + continuity;
        float push = 1f - continuity;
        float easeIn = 1f + bias;
        float easeOut = 1f - bias;
        
        Vector3 v1  = length * pull * easeIn * (p1 - p0) * 0.5f;
                v1 += length * push * easeOut * (p2 - p1) * 0.5f;
        
        Vector3 v2  = length * push * easeIn * (p2 - p1) * 0.5f;
                v2 += length * pull * easeOut * (p3 - p2) * 0.5f;
        
        return Hermite(p1, p2, v1, v2, t);
    }
}
