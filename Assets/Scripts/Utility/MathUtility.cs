using UnityEngine;

public static class MathUtility
{
    #region float
    public static float SmoothStepAngle(float from, float to, float t)
    {
        to = from + Mathf.DeltaAngle(from, to);
        return Mathf.SmoothStep(from, to, t);
    }
    
    public static float ClampAngle180(float angle, float min, float max)
    {
        angle = Mathf.Repeat(angle, 360f);
        if (angle > 180.0) angle -= 360f;
        return Mathf.Clamp(angle, min, max);
    }
    #endregion

    #region Vector3

    public static Vector3 LerpAngle(Vector3 a, Vector3 b, float t)
    {
        return new Vector3
        (
            Mathf.LerpAngle(a.x, b.x, t),
            Mathf.LerpAngle(a.y, b.y, t),
            Mathf.LerpAngle(a.z, b.z, t)
        );
    }
    
    public static Vector3 SmoothStep(Vector3 from, Vector3 to, float t)
    {
        return new Vector3
        (
            Mathf.SmoothStep(from.x, to.x, t),
            Mathf.SmoothStep(from.y, to.y, t),
            Mathf.SmoothStep(from.z, to.z, t)
        );
    }
    
    public static Vector3 SmoothStepAngle(Vector3 from, Vector3 to, float t)
    {
        return new Vector3
        (
            SmoothStepAngle(from.x, to.x, t),
            SmoothStepAngle(from.y, to.y, t),
            SmoothStepAngle(from.z, to.z, t)
        );
    }
    
    public static Vector3 SmoothDampAngle(Vector3 current, Vector3 target, ref Vector3 velocity, float smoothTime)
    {
        return new Vector3
        (
            Mathf.SmoothDampAngle(current.x, target.x, ref velocity.x, smoothTime),
            Mathf.SmoothDampAngle(current.y, target.y, ref velocity.y, smoothTime),
            Mathf.SmoothDampAngle(current.z, target.z, ref velocity.z, smoothTime)
        );
    }

    public static Vector3 SmoothDampPerAxis(Vector3 current, Vector3 target, ref Vector3 velocity, Vector3 smoothTime)
    {
        return new Vector3
        (
            Mathf.SmoothDamp(current.x, target.x, ref velocity.x, smoothTime.x),
            Mathf.SmoothDamp(current.y, target.y, ref velocity.y, smoothTime.y),
            Mathf.SmoothDamp(current.z, target.z, ref velocity.z, smoothTime.z)
        );
    }
    #endregion

    #region Quaternion

    public static Quaternion SmoothStep(Quaternion from, Quaternion to, float t)
    {
        var fromEuler = from.eulerAngles;
        var toEuler = to.eulerAngles;
        var result = SmoothStepAngle(fromEuler, toEuler, t);
        return Quaternion.Euler(result);
    }

    #endregion
}