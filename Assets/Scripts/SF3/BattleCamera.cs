using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleCamera : MonoBehaviour
{
    public Vector3 _targetPosition;
    //Vector3 _cameraVelocity = Vector3.zero;
    Vector3 _cameraTargetVelocity = Vector3.zero;
    private float _cameraDistanceVelocity = 0f;
    private float _cameraHeightVelocity = 0f;
    private float _cameraAngleVelocity = 0f;

    public float _cameraDampTime = 1.0f;
    public float DampTime
    {
        get { return _cameraDampTime; }
        set { _cameraDampTime = value; }
    }

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

    public float _viewAngle = 0f;
    public float ViewAngle
    {
        get { return _viewAngle; }
        set { _viewAngle = value; }
    }

    bool _cameraToTarget = false;

    private float _currentCameraHeight;
    private float _currentCameraDistance;
    private float _currentViewAngle;
    private Vector3 _currentTargetPosition;

    private void Awake()
    {
    }

    private void Start()
    {
        Camera camera = GetComponent<Camera>();
        camera.depthTextureMode = DepthTextureMode.DepthNormals;    // necessary for ambient occlusion

        _currentCameraDistance = _cameraDistance;
        _currentCameraHeight = _cameraHeight;
        _currentViewAngle = _viewAngle;
        _currentTargetPosition = _targetPosition;
    }

    void LateUpdate()
    {
        if (_cameraToTarget == false)
        {
            // animate camera distance, height and view angle
            //Vector3 cameraSetting = new Vector3(_currentCameraDistance, _currentCameraHeight, _currentViewAngle);
            //Vector3 cameraTargetSetting = new Vector3(_cameraDistance, _cameraHeight, _viewAngle);
            //cameraSetting = Vector3.SmoothDamp(cameraSetting, cameraTargetSetting, ref _cameraVelocity, _cameraDampTime);
            //_currentCameraDistance = cameraSetting.x;
            //_currentCameraHeight = cameraSetting.y;
            //_currentViewAngle = cameraSetting.z;

            _currentCameraDistance = Mathf.SmoothDamp(_currentCameraDistance, _cameraDistance, ref _cameraDistanceVelocity, _cameraDampTime);
            _currentCameraHeight = Mathf.SmoothDamp(_currentCameraHeight, _cameraHeight, ref _cameraHeightVelocity, _cameraDampTime);
            _currentViewAngle = Mathf.SmoothDamp(_currentViewAngle, _viewAngle, ref _cameraAngleVelocity, _cameraDampTime);
            _currentTargetPosition = Vector3.SmoothDamp(_currentTargetPosition, _targetPosition, ref _cameraTargetVelocity, _cameraDampTime);
        }
        else
        {
            _currentCameraDistance = _cameraDistance;
            _currentCameraHeight = _cameraHeight;
            _currentViewAngle = _viewAngle;

            _currentTargetPosition = _targetPosition;

            _cameraToTarget = false;
        }

        // apply view rotation and height
        Vector3 cameraDirection = Quaternion.Euler(0f, _currentViewAngle, 0f) * Vector3.forward;
        Vector3 cameraPosition = _currentTargetPosition + (cameraDirection * _currentCameraDistance);
        cameraPosition.y += _currentCameraHeight;

        transform.position = cameraPosition;

        transform.LookAt(GetLookAtPosition());
    }

    private Vector3 GetCameraTargetPos()
    {
        return _targetPosition;
    }

    private Vector3 GetLookAtPosition()
    {
        Vector3 cameraLookAt = _currentTargetPosition;
        cameraLookAt.y += _cameraLookAtHeight;

        return cameraLookAt;
    }

    private float GetCameraTargetDistance()
    {
        float distance = Vector3.Distance(GetCameraTargetPos(), transform.position);

        return distance;
    }

    public void SetTargetPosition(Vector3 targetPosition)
    {
        _targetPosition = targetPosition;
    }

    public void LookAtTarget()
    {
        Vector3 lookAtPosition = GetLookAtPosition();

        transform.LookAt(lookAtPosition);
    }

    public void SnapToTarget()
    {
        _cameraToTarget = true;
    }
}
