using UnityEngine;

public static class TransformHelper
{
    public static GameObject FindChild(GameObject parent, string childName)
    {
        Transform[] childTransforms = parent.GetComponentsInChildren<Transform>();

        foreach (Transform child in childTransforms)
        {
            if (child.name == childName)
            {
                return child.gameObject;
            }
        }

        return null;
    }
}
