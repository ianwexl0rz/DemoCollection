using UnityEngine;

public static class LockOnExtensions
{
    public static Vector3 GetCenter(this ILockOnTarget lockOnTarget)
    {
        return (lockOnTarget.GetLookPosition() + lockOnTarget.GetGroundPosition()) * 0.5f;
    }

    public static float GetHeight(this ILockOnTarget lockOnTarget)
    {
        return Vector3.Distance(lockOnTarget.GetLookPosition(), lockOnTarget.GetGroundPosition()) * 2f;
    }

}