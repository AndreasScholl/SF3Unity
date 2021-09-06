using UnityEngine;

public static class LayerHelper
{
    public static void SetLayerRecursive(GameObject parent, string layer)
    {
        int layerValue = LayerMask.NameToLayer(layer);
        parent.layer = layerValue;
        Transform[] transforms = parent.GetComponentsInChildren<Transform>();
        foreach (Transform child in transforms)
        {
            child.gameObject.layer = layerValue;
        }
    }
}
