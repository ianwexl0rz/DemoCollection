using UnityEngine;

namespace Utility
{
    public static class TransformExtensions
    {
        public static void RotateAbout (this Transform transform, Vector3 pivotPoint, Quaternion rot)
        {
            transform.position = rot * (transform.position - pivotPoint) + pivotPoint;
            transform.rotation = rot * transform.rotation;
        }
    }
}