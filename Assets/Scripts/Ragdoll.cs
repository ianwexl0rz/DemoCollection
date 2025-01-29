using UnityEngine;

public class Ragdoll : MonoBehaviour
{
    [Tooltip("The rig to update with the copied transforms.")]
    public Transform targetRig;

    public void CopyTransforms(Transform source, Rigidbody sourceRb, Transform target)
    {
        // Copy position, rotation, and scale
        target.localPosition = source.localPosition;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;

        var targetRb = target.GetComponent<Rigidbody>();
        if (targetRb)
        {
            targetRb.velocity = sourceRb.velocity;
        }

        // Recursively copy transforms where names match
        for (int i = 0; i < target.childCount; i++)
        {
            var targetChild = target.GetChild(i);

            // Find a matching child in the source by name
            var sourceChild = FindChildByName(source, targetChild.name);

            // Copy transforms only if a match is found
            if (sourceChild != null)
            {
                CopyTransforms(sourceChild, sourceRb, targetChild);
            }
        }
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        // Find and return the first child of the specified name
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
        }

        // Return null if no child with the given name is found
        return null;
    }
}