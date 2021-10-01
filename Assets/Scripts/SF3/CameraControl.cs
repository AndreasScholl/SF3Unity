using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    ICameraTarget _target = null;

    Vector3 _cameraVelocity = Vector3.zero;
    Vector3 _cameraVelocityHitMove = Vector3.zero;
    public float _cameraDampTime = 0.25f;

    public float _cameraDistance = 3.5f;
    public float CameraDistance
    {
        get { return _cameraDistance; }
        set { _cameraDistance = value; }
    }

    public float _cameraHeight = 2f;
    public float CameraHeight
    {
        get { return _cameraHeight; }
        set { _cameraHeight = value; }
    }

    public float _cameraLookAtHeight = 1.25f;
    public float LookAtHeight
    {
        get { return _cameraLookAtHeight; }
        set { _cameraLookAtHeight = value; }
    }

    private float _savedCameraDistance;
    private float _savedCameraHeight;
    private float _savedCameraLookAtHeight;

    private Vector3 _savedPosition;
    private Quaternion _savedRotation;

    bool _cameraToTarget = false;

    public bool _fixedPosition = false;
    public bool _fixedRotation = false;

    public float _rotateSpeed = 1f;
    private float _rotateLerp;
    private float _rotateLerpMax;
    private float _rotateLerpMin;
    private float _requestRotationAngle = 0f;
    private float _requestRotationTime = 0f;
    private bool _requestImmdidiateChange;
    private bool _lookAtLerp = false;
    private bool _requestWorldRotation = false;

    private bool _shake = false;
    private Vector3 _shakeVector = Vector3.zero;
    private float _shakeAmplitude;
    private Vector3 _shakeDirection;
    private float _shakeDecay;
    private float _shakeTime;
    private bool _lerpActive;
    private bool _lerpRotation;
    private Vector3 _lerpPosFrom;
    private Vector3 _lerpRotFrom;
    private Vector3 _lerpPosTo;
    private Vector3 _lerpRotTo;
    private float _lerpDuration;
    private float _lerpTime;

    //private Vector2 _viewOffset = Vector2.zero;

    private Vector3 _virtualPosition;
    Transform _virtualPositionTransform;
    private Vector3 _virtualHitPosition;
    private bool _hitMovement;

    Vector3 _targetPosition;
    Vector3 _hitTargetPosition;

    public float _cameraDistanceClosingFactor = 0.1f;
    public float _cameraHeightClosingFactor = 0.05f;

    public float _hitSphereRadius = 0.25f;
    public float _clipAdjustmentDistance = 0.1f;
    public float _minTargetDistance = 0.3f;

    private bool _lookAtXLimit = false;
    private float _lookAtMaxAngleX = 30f;
    public float _cameraMaxSpeed = 20f;
    public float _targetSpeedFactor = 1.5f;

    public float _viewAngle = 0f;
    public float ViewAngle
    {
        get { return _viewAngle; }
        set { _viewAngle = value; }
    }

    private float _currentCameraHeight;
    private float _currentCameraDistance;

    Vector2 _cameraSettingVelocity = Vector2.zero;
    public float _cameraSettingDampTime = 0.25f;

    private void Awake()
    {
        _cameraMaxSpeed = 30f;
        _cameraDampTime = 0.2f;
        _targetSpeedFactor = 2f;

        _rotateSpeed = 20;

        _rotateLerp = 0f;
        _rotateLerpMax = 0.5f;
        _rotateLerpMin = 0.05f;

        _virtualPosition = transform.position;

        _lerpActive = false;
    }

    private void Start()
    {
        Camera camera = GetComponent<Camera>();
        camera.depthTextureMode = DepthTextureMode.DepthNormals;    // necessary for ambient occlusion

        GameObject virtualPosObj = new GameObject("VirtualCameraPos");
        _virtualPositionTransform = virtualPosObj.transform;

        _currentCameraDistance = _cameraDistance;
        _currentCameraHeight = _cameraHeight;

        // set layer cull distances to max view distance
        //float[] distances = new float[32];
        //const float viewDistance = 50f;
        //distances[LayerMask.NameToLayer("Dynamic")] = viewDistance;
        //distances[LayerMask.NameToLayer("DynamicIgnoreProjectile")] = viewDistance;
        //distances[LayerMask.NameToLayer("DynamicCameraCollision")] = viewDistance;
        //camera.layerCullDistances = distances;
    }

    void LateUpdate()
    {
        if (_target == null)
        {
            return;
        }

        // animate camera distance and height
        Vector2 cameraSetting = new Vector2(_currentCameraDistance, _currentCameraHeight);
        Vector2 cameraTargetSetting = new Vector2(_cameraDistance, _cameraHeight);
        cameraSetting = Vector2.SmoothDamp(cameraSetting, cameraTargetSetting, ref _cameraSettingVelocity, _cameraSettingDampTime);
        _currentCameraDistance = cameraSetting.x;
        _currentCameraHeight = cameraSetting.y;

        // subtract previous shake
        transform.position = transform.position - _shakeVector;

        Vector3 lookAtPosition = GetLookAtPosition();

        if (_lerpActive == true)
        {
            _lerpTime += Time.deltaTime;

            if (_lerpTime >= _lerpDuration)
            {
                _lerpTime = _lerpDuration;

                _lerpActive = false;
            }

            float percentage = _lerpTime / _lerpDuration;

            Vector3 lerpPos = Vector3.Lerp(_lerpPosFrom, _lerpPosTo, percentage);

            if (_lerpRotation == true)
            {
                Vector3 lerpRot = Vector3.Lerp(_lerpRotFrom, _lerpRotTo, percentage);
                transform.eulerAngles = lerpRot;
            }
            else
            {
                transform.LookAt(lookAtPosition);
            }

            transform.position = lerpPos;

            ProcessShake();

            return;
        }

        if (_fixedPosition == true)
        {
            if (_lookAtLerp == true)
            {
                Vector3 targetDir = lookAtPosition - transform.position;

                float lerpAngleMax = 20.0f;
                float rotateLerpAccel = 2f;

                float angle = Mathf.Abs(Quaternion.Angle(transform.rotation, Quaternion.LookRotation(targetDir)));
                float lerpTarget = (Mathf.Min(lerpAngleMax, angle) / lerpAngleMax) * _rotateLerpMax;

                if (_rotateLerp < lerpTarget)
                {
                    _rotateLerp += rotateLerpAccel * Time.deltaTime;

                    if (_rotateLerp > lerpTarget)
                    {
                        _rotateLerp = lerpTarget;
                    }
                }
                else if (_rotateLerp > lerpTarget)
                {
                    _rotateLerp = lerpTarget;
                }

                if (_rotateLerp < _rotateLerpMin)
                {
                    _rotateLerp = _rotateLerpMin;
                }

                //Debug.Log(_rotateLerp + " accel: " + (rotateLerpAccel * Time.deltaTime) + " diff: " + angle);

                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(targetDir), _rotateLerp);
            }
            else
            {
                if (_fixedRotation == false)
                {
                    transform.LookAt(lookAtPosition);

                    if (_lookAtXLimit == true)
                    {
                        Vector3 cameraAngle = transform.localEulerAngles;
                        if (cameraAngle.x < -180f)
                        {
                            cameraAngle.x += 360f;
                        }
                        if (cameraAngle.x >= 180f)
                        {
                            cameraAngle.x -= 360f;
                        }
                        if (cameraAngle.x > _lookAtMaxAngleX)
                        {
                            cameraAngle.x = _lookAtMaxAngleX;
                        }
                        if (cameraAngle.x < -_lookAtMaxAngleX)
                        {
                            cameraAngle.x = -_lookAtMaxAngleX;
                        }
                        transform.localEulerAngles = cameraAngle;
                    }
                }
            }

            ProcessShake();

            return;
        }

        Vector3 targetPos = GetCameraTargetPos();

        float cameraTargetDistance = GetCameraTargetDistance();

        float cameraSpeed = _target.GetMoveSpeed() * _targetSpeedFactor;

        if (_requestRotationTime > 0f)
        {
            cameraSpeed = 20f;  // use fixed camera speed while requesting rotations
        }

        float cameraYDistance = GetCameraYDistance(targetPos.y);

        cameraSpeed += cameraYDistance * 2f;

        cameraTargetDistance -= 2.0f;
        if (cameraTargetDistance < 0f)
        {
            cameraTargetDistance = 0f;
        }
        float cameraDistanceSpeed = cameraTargetDistance / 10f;
        cameraSpeed += cameraDistanceSpeed;

        if (cameraSpeed > _cameraMaxSpeed)
        {
            cameraSpeed = _cameraMaxSpeed;
        }

        //
        // note about "virtual position"
        // the concept of virtual position is to have a virtual camera position that is the current camera position
        // without collision applied.
        // it will move smoothly to the target pos.
        //
        // collision handling will be applied based on the current virtual position
        // if there is collision detected a new target pos will be calculated (hit move pos)
        // the current virtualposition will be transfered to a virtualHitPosition)
        // as long as there is collision on the virtualposition the virtualhitposition will be used to move to the target
        // when the collision ends the virtualHitposition will still be used until it matches the virtualposition
        // 

        _targetPosition = targetPos;

        // phase 1: update move position (no collision)
        //
        Vector3 movePosition = _virtualPosition;
        Vector3 startMovePosition = movePosition;

        if (_cameraToTarget == false)
        {
            if (cameraSpeed > 0.01f)
            {
                // camera moves smooth
                movePosition = Vector3.SmoothDamp(movePosition, targetPos, ref _cameraVelocity, _cameraDampTime, cameraSpeed);
            }
        }
        else
        {
            if (_cameraToTarget == true)
            {
                // immediately to target
                movePosition = targetPos;
                _cameraToTarget = false;
                _requestRotationTime = 0f;
            }
        }

        _virtualPosition = movePosition;

        // apply rotation and camera height
        //
        Vector3 cameraDirection = Quaternion.Euler(0f, _viewAngle, 0f) * Vector3.forward;
        movePosition = movePosition + (cameraDirection * _currentCameraDistance);
        movePosition.y += _currentCameraHeight;

        // phase 2: collision based on current move position
        //
        //Vector3 targetDirection = (targetPos - lookAtPosition).normalized;

        //// check for something in vision of path of camera
        //RaycastHit hitInfo;
        //int collisionLayerMask = 1 << LayerMask.NameToLayer("Ground") |
        //                         1 << LayerMask.NameToLayer("Default") |
        //                         1 << LayerMask.NameToLayer("DynamicCameraCollision");

        ////bool hit = Physics.Linecast(lookAtPosition, targetPos, out hitInfo, collisionLayerMask, QueryTriggerInteraction.Ignore);

        //// use boxcast to avoid camera moving into corners
        ////const float boxSize = 0.2f;
        //float distance = (targetPos - lookAtPosition).magnitude;
        ////bool hit = Physics.BoxCast(lookAtPosition, new Vector3(boxSize, boxSize, boxSize), targetDirection, out hitInfo, transform.rotation, distance, collisionLayerMask, QueryTriggerInteraction.Ignore);
        //bool hit = Physics.SphereCast(lookAtPosition, _hitSphereRadius, targetDirection, out hitInfo, distance, collisionLayerMask, QueryTriggerInteraction.Ignore);
        //hitInfo.point = lookAtPosition + (hitInfo.distance * targetDirection);

        //Vector3 hitTargetPosition = targetPos;

        //if (hit == true)
        //{
        //    // vision would be blocked => calculate hit target position
        //    float hitPointDistance = Vector3.Distance(lookAtPosition, hitInfo.point);

        //    hitInfo.distance -= _clipAdjustmentDistance;        // move point away from original hit.point
        //    if (hitInfo.distance < _minTargetDistance)          // ensure minimum distance to lookAtPosition
        //    {
        //        hitInfo.distance = _minTargetDistance;
        //    }

        //    hitTargetPosition = lookAtPosition + (hitInfo.distance * targetDirection);

        //    //hitTargetPosition = hitInfo.point - (targetDirection * _clipAdjustmentDistance);
        //    //hitMovePosition = hitInfo.point + (hitInfo.normal * clipAdjustmentDistance);
        //    float hitMoveDistance = hitInfo.distance; //  Vector3.Distance(lookAtPosition, hitTargetPosition);

        //    if (targetDirection.y >= 0f) // looking from above at target?
        //    {
        //        //const float closeDistanceUpProjectionLimit = 1.5f;
        //        const float closeDistanceUpProjectionLimit = 3.0f;
        //        if (hitInfo.distance <= closeDistanceUpProjectionLimit)
        //        {
        //            // upwards projection of hit target position
        //            float targetDistance = Vector3.Distance(lookAtPosition, targetPos);
        //            float upProjectionDistance = targetDistance - hitMoveDistance;

        //            float maxUpProjectionDistance = 1.25f;
        //            //float cameraHeightChange = 3f - _cameraHeight;
        //            //if (cameraHeightChange > 0f)
        //            //{
        //            //    maxUpProjectionDistance += cameraHeightChange;
        //            //}

        //            if (upProjectionDistance > maxUpProjectionDistance)
        //            {
        //                upProjectionDistance = maxUpProjectionDistance;
        //            }

        //            // check for ceiling / upwards collision
        //            bool hitUp = Physics.Linecast(hitTargetPosition, hitTargetPosition + Vector3.up * upProjectionDistance, out hitInfo, collisionLayerMask, QueryTriggerInteraction.Ignore);

        //            if (hitUp == true)
        //            {
        //                upProjectionDistance = Mathf.Max(hitInfo.distance - _clipAdjustmentDistance, 0f);
        //            }

        //            hitTargetPosition += Vector3.up * upProjectionDistance;
        //        }
        //    }

        //    _hitTargetPosition = hitTargetPosition;
        //}

        //// initiate hit movement if necessary
        //if (hit == true && _hitMovement == false)
        //{
        //    // start hit movement
        //    _hitMovement = true;

        //    _virtualHitPosition = startMovePosition;
        //}

        //// move to hit target instead of target if hit movement enabled
        //if (_hitMovement == true)
        //{
        //    movePosition = _virtualHitPosition;

        //    // camera moves smooth to hit position
        //    cameraSpeed = 10f;
        //    movePosition = Vector3.SmoothDamp(movePosition, hitTargetPosition, ref _cameraVelocityHitMove, _cameraDampTime, cameraSpeed);

        //    _virtualHitPosition = movePosition;

        //    // check for hit move end conditiion (close to target move position)
        //    if (hit == false && Vector3.Distance(_virtualHitPosition, _virtualPosition) < 0.01f)
        //    {
        //        _hitMovement = false;

        //        _virtualPosition = _virtualHitPosition;     // continue normal move from current hit move pos
        //    }
        //}

        transform.position = movePosition;  // apply move position to camera

        transform.LookAt(GetVirtualLookAtPosition());   // look at target

        _virtualPositionTransform.position = _virtualPosition;
        _virtualPositionTransform.LookAt(lookAtPosition);

        //ApplyViewOffset();

        ProcessShake();
    }

    public float GetVirtualCameraAngle()
    {
        return _virtualPositionTransform.eulerAngles.y;
    }

    //private void ApplyViewOffset()
    //{
    //    const float maxViewAngleOffset = 45f;


    //    Vector3 angleOffset = _viewOffset * maxViewAngleOffset;

    //    Vector3 eulerAngles = transform.localEulerAngles;
    //    eulerAngles.x += angleOffset.y;
    //    eulerAngles.y += angleOffset.x;
    //    transform.localEulerAngles = eulerAngles;
    //}

    private void ProcessShake()
    {
        if (_shake == true)
        {
            _shakeVector = Random.Range(-_shakeAmplitude, _shakeAmplitude) * _shakeDirection;

            _shakeAmplitude -= _shakeDecay * Time.deltaTime;

            if (_shakeAmplitude < 0.001f)
            {
                _shakeAmplitude = 0f;
            }

            _shakeTime -= Time.deltaTime;

            if (_shakeTime <= 0f)
            {
                _shake = false;
            }

            //Debug.Log("shake-amp: " + _shakeAmplitude + " shake: " + _shakeVector.y);

            transform.position = transform.position + _shakeVector;
        }
        else
        {
            _shakeVector = Vector3.zero;
        }
    }

    private Vector3 GetLookAtPosition()
    {
        Transform targetTransform;

        targetTransform = _target.GetTransform();

        Vector3 cameraLookAt = targetTransform.position;
        cameraLookAt.y += _cameraLookAtHeight;

        return cameraLookAt;
    }

    private Vector3 GetVirtualLookAtPosition()
    {
        Vector3 cameraLookAt = _virtualPosition;
        cameraLookAt.y += _cameraLookAtHeight;

        return cameraLookAt;
    }

    private Vector3 GetCameraTargetPos()
    {
        //if (_requestWorldRotation == true)
        //{
        //    _requestWorldRotation = false;

        //    return GetCameraTargetPosWithWorldRotation(_requestRotationAngle);
        //}

        //if (_requestRotationTime > 0f)
        //{
        //    _requestRotationTime -= Time.deltaTime;

        //    return GetCameraTargetPosWithRotation(_requestRotationAngle);
        //}

        // get close to target
        //Vector3 cameraDirection = transform.position - _target.GetTransform().position;
        //Vector3 cameraDirection = _virtualPosition - _target.GetTransform().position;
        //cameraDirection.y = 0f;
        //cameraDirection = cameraDirection.normalized;

        Vector3 targetPos = _target.GetTransform().position;

        //Vector3 cameraDirection = Quaternion.Euler(0f, _viewAngle, 0f) * Vector3.forward;

        //Vector3 targetPos = _target.GetTransform().position + (cameraDirection * _cameraDistance);
        //targetPos.y += _cameraHeight;

        return targetPos;
    }

    private Vector3 GetCameraTargetPosWithWorldRotation(float angle)
    {
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0f, angle, 0f));

        Vector3 targetPos = _target.GetTransform().position + (targetRotation * (Vector3.forward * -_cameraDistance));
        targetPos.y += _cameraHeight;

        return targetPos;
    }

    public void RequestWorldRotation(float angle)
    {
        _requestRotationAngle = angle;
        _requestWorldRotation = true;

        _cameraToTarget = true;
    }

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
        _virtualPosition = position;

        _hitMovement = false;       // cancel hit movement
    }

    private float GetCameraTargetDistance()
    {
        float distance = Vector3.Distance(_target.GetTransform().position, transform.position);

        return distance;
    }

    private float GetVirtualTargetDistance()
    {
        Vector3 cameraPos = _virtualPosition;
        Vector3 targetPos = _target.GetTransform().position;

        cameraPos.y = 0f;
        targetPos.y = 0f;

        float distance = Vector3.Distance(targetPos, cameraPos);

        return distance;
    }

    private float GetVirtualTargetCameraHeight()
    {
        Vector3 cameraPos = _virtualPosition;
        Vector3 targetPos = _target.GetTransform().position;

        float height = cameraPos.y - targetPos.y;

        return height;
    }

    private float GetCameraYDistance(float targetY)
    {
        float distance = Mathf.Abs(targetY - transform.position.y);

        if (distance < 0.1f)
        {
            distance = 0f;
        }

        return distance;
    }

    public void SetTarget(ICameraTarget target)
    {
        _target = target;

        if (_target != null)
        {
            _cameraToTarget = true;
        }
    }

    public void LookAtTarget()
    {
        Vector3 lookAtPosition = GetLookAtPosition();

        transform.LookAt(lookAtPosition);
    }

    public void SaveSettings()
    {
        _savedCameraDistance = _cameraDistance;
        _savedCameraHeight = _cameraHeight;
        _savedCameraLookAtHeight = _cameraLookAtHeight;
    }

    public void RestoreSettings()
    {
        _cameraDistance = _savedCameraDistance;
        _cameraHeight = _savedCameraHeight;
        _cameraLookAtHeight = _savedCameraLookAtHeight;
    }

    public void SavePosition()
    {
        _savedPosition = transform.position;
        _savedRotation = transform.rotation;
    }

    public void RestorePosition()
    {
        transform.position = _savedPosition;
        transform.rotation = _savedRotation;
    }

    public void Shake(float amplitude, Vector3 direction, float decay, float time)
    {
        _shake = true;
        _shakeAmplitude = amplitude;
        _shakeDirection = direction;
        _shakeDecay = decay;
        _shakeTime = time;
    }

    public void CancelShake()
    {
        _shake = false;
    }

    public void LerpAnimation(Vector3 posFrom, Vector3 rotFrom, Vector3 posTo, Vector3 rotTo, float duration)
    {
        _lerpActive = true;
        _lerpRotation = true;

        _lerpPosFrom = posFrom;
        _lerpRotFrom = rotFrom;
        _lerpPosTo = posTo;
        _lerpRotTo = rotTo;

        _lerpDuration = duration;
        _lerpTime = 0f;
    }

    public void LerpAnimation(Vector3 posFrom, Vector3 posTo, float duration)
    {
        _lerpActive = true;
        _lerpRotation = false;

        _lerpPosFrom = posFrom;
        _lerpPosTo = posTo;

        _lerpDuration = duration;
        _lerpTime = 0f;
    }

    public void LerpEnd()
    {
        _lerpActive = false;
    }

    public void EndFixedPosition()
    {
        _fixedPosition = false;

        _virtualPosition = transform.position;
        _hitMovement = false;       // cancel hit movement
    }

    //public void SetViewOffset(Vector2 viewOffset)
    //{
    //    _viewOffset = viewOffset;
    //}

    //void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;
    //    Gizmos.DrawSphere(_targetPosition, 0.2f);

    //    Gizmos.color = Color.blue;
    //    Gizmos.DrawSphere(_virtualPosition, 0.2f);

    //    if (_hitMovement == true)
    //    {
    //        Gizmos.color = Color.red;
    //        Gizmos.DrawSphere(_hitTargetPosition, 0.2f);

    //        Gizmos.color = Color.yellow;
    //        Gizmos.DrawSphere(_virtualHitPosition, 0.2f);
    //    }
    //}

    public void SetLookAtXLimit(bool enable, float angle)
    {
        _lookAtXLimit = enable;
        _lookAtMaxAngleX = angle;
    }

    public void SnapToTarget()
    {
        _cameraToTarget = true;
    }
}
