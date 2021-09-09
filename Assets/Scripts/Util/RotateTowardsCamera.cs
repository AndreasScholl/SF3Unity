using UnityEngine;

public class RotateTowardsCamera : MonoBehaviour
{
    void Update()
    {
        Vector3 delta = transform.position - Camera.main.transform.position;
        delta.y = 0f;
        transform.rotation = Quaternion.LookRotation(delta);
    }
}
