using UnityEngine;

namespace Shiningforce
{
    public class Character : MonoBehaviour, ICameraTarget
    {
        InputData _inputData;

        CameraControl _cameraControl;

        private const float _inputMagnitudeThreshold = 0.2f;
        public float _moveSpeed = 20f;
        public float _turnSpeed = 720f;

        private float _directionAngle = 0f;

        void Start()
        {
            _inputData = new InputData();

            _cameraControl = Camera.main.GetComponent<CameraControl>();
        }

        void Update()
        {
            InputHelper.ReadInput(_inputData);

            float moveAngle;
            bool moving;
            Vector3 moveVector = CalculateMoveVectorAndAngle(out moveAngle, out moving);

            if (moving)
            {
                TurnToAngle(moveAngle);
            }

            transform.position = transform.position + moveVector;
            transform.eulerAngles = new Vector3(0f, _directionAngle, 0f);

            HandleCameraRotation();
        }

        void TurnToAngle(float moveAngle)
        {
            // turn
            float angleDistance = Mathf.DeltaAngle(_directionAngle, moveAngle);

            float turnAngleDistance = _turnSpeed * Time.deltaTime;

            if (Mathf.Abs(angleDistance) <= turnAngleDistance)
            {
                _directionAngle = moveAngle;
            }
            else
            {
                if (angleDistance < 0)
                {
                    _directionAngle -= turnAngleDistance;
                }
                else
                {
                    _directionAngle += turnAngleDistance;
                }
            }
        }

        Vector3 CalculateMoveVectorAndAngle(out float moveAngle, out bool moving)
        {
            Vector2 inputDirection = new Vector2(_inputData.Horizontal, _inputData.Vertical);

            moving = false;
            float moveStrength = 0.0f;
            //BrUI.Instance.Debug1.text = inputDirection.magnitude.ToString("n2");
            //BrUI.Instance.Debug1.text = inputDirection.x.ToString("n2") + " / " + inputDirection.y.ToString("n2");

            if (inputDirection.magnitude > _inputMagnitudeThreshold)
            {
                moving = true;

                moveStrength = inputDirection.magnitude / 0.5f;

                if (moveStrength > 1.0f)
                {
                    moveStrength = 1.0f;
                }
            }

            inputDirection.Normalize();

            Vector3 directionVector = new Vector3(inputDirection.x, 0f, inputDirection.y);
            moveAngle = Vector3.SignedAngle(new Vector3(0f, 0f, 1f), directionVector, Vector3.up);

            moveAngle += _inputData.CameraAngle;

            Quaternion moveRotation = Quaternion.Euler(0f, moveAngle, 0f);
            Vector3 forward = Vector3.forward * _moveSpeed * Time.deltaTime;

            Vector3 moveVector = Vector3.zero;

            if (moving)
            {
                moveVector = moveRotation * forward;
            }
            //moveVector += (moveRotation * _speed) * Time.deltaTime; // apply speed movement (from jump or other sources)

            return moveVector;
        }

        void HandleCameraRotation()
        {
            const float rotationSpeed = 90f;

            float cameraAngle = _cameraControl.ViewAngle;

            if (_inputData.Button5)
            {
                cameraAngle -= rotationSpeed * Time.deltaTime;
            }
            else if (_inputData.Button6)
            {
                cameraAngle += rotationSpeed * Time.deltaTime;
            }

            _cameraControl.ViewAngle = cameraAngle;
        }

        public float GetMoveSpeed()
        {
            return 1f;
        }

        public Transform GetTransform()
        {
            return gameObject.transform;
        }
    }
}
