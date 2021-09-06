using UnityEngine;

public static class BoundsHelper
{
    static public Bounds GetChildRendererBounds(GameObject go, bool includeInactive = false)
    {
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(includeInactive);

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1, ni = renderers.Length; i < ni; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }
        else
        {
            return new Bounds();
        }
    }
}
