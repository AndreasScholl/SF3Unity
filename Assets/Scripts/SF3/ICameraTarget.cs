using UnityEngine;

public interface ICameraTarget
{
    float GetMoveSpeed();
    Transform GetTransform();
    //Collider GetAvoidanceCollider();
    //Collider GetCollider();

    //Quaternion GetLookRotationWithOffset(float angleOffset);
}
