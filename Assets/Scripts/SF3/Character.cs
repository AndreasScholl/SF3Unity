﻿using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Shiningforce
{
    public class Character : MonoBehaviour, ICameraTarget
    {
        InputData _inputData;

        CameraControl _cameraControl;

        private const float _inputMagnitudeThreshold = 0.2f;
        public float _moveSpeed = 15f;
        public float _turnSpeed = 480f;

        public float _directionAngle = 0f;
        public int dir = 0;
        private float _moveAngle = 0f;

        private SpriteRenderer[] _spriteRenderers;
        //private Sprite[] _sprites;
        private SpriteSheets _spriteSheets;
        private SpriteSheet _spriteSheet;
        private int[] _sheetIds;
        private int _selectedId;

        private float _animTime;
        //private int _animFrame = 0;
        //public float _animFrameTime = 0.1f;

        //private int _animIndex = 0;
        //private int[] _animTable;
        //private int _sheetColumns = 6;

        //private int[] _walkTable = new int[] { 0, 1, 2, 3, 4, 3, 2, 1 };
        //private int[] _walkTable = new int[] { 0, 2, 3, 4, 1 };
        //private int[] _standTable = new int[] { 5, 6, 7, 6 };
        //private int[] _walkTable = new int[] { 3, 4, 5, 6, 7, 6, 5, 4 };
        //private int[] _standTable = new int[] { 0, 1, 2, 1 };

        private CapsuleCollider _collider;
        private float _hitRadius = 0.75f;
        //private float _hitRadius = 1.0f;
        private float _colliderHeight = 4f;

        float[] _cameraDistances = new float[] { 30f, 40f, 60f };
        float[] _cameraHeights = new float[] { 25f, 35f, 50f };
        int _currentCameraSetting = 1;

        private List<Trigger> _triggers = new List<Trigger>();

        private bool _processingTrigger = false;
        private Trigger _activeTrigger;
        private GameObject _interactionObject;

        private int _lightVolumeLayer;
        private bool _inLight;
        private float _lightIntensity = 1;
        public float _lightFadeOutSpeed = 5f;

        private int _chpFileIndex = 0;

        void Start()
        {
            _inputData = new InputData();

            _cameraControl = Camera.main.GetComponent<CameraControl>();

            _spriteRenderers = gameObject.GetComponentsInChildren<SpriteRenderer>();

            foreach (SpriteRenderer renderer in _spriteRenderers)
            {
                renderer.gameObject.layer = LayerMask.NameToLayer("Sprite");
            }

            _chpFileIndex = Loader.Instance.GetChpIndex("CBP00");
            //_chpFileIndex = Loader.Instance.GetChpIndex("CBW00");
            //_chpFileIndex = Loader.Instance.GetChpIndex("CBE00");
            //_chpFileIndex = Loader.Instance.GetChpIndex("CBE05");
            //_chpFileIndex = Loader.Instance.GetChpIndex("CBF00");
            LoadSprites();

            _animTime = 0f;

            // collision
            _collider = gameObject.AddComponent<CapsuleCollider>();
            _collider.radius = _hitRadius;
            _collider.height = _colliderHeight;
            _collider.center = new Vector3(0, 2.0f, 0);

            Rigidbody rigidBody = gameObject.AddComponent<Rigidbody>();
            rigidBody.isKinematic = true;

            // map transition triggers (test)
            Trigger trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.MapTransition;
            trigger.TriggerId = 0x22;
            trigger.DestinationTriggerId = 0x23;
            trigger.MapName = "S_RM01";
            trigger.MapPages = new byte[] { 4, 5 };
            trigger.LookDirection = Trigger.TriggerDirection.Left;
            trigger.ViewAngle = 328f;
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.MapTransition;
            trigger.TriggerId = 0x23;
            trigger.DestinationTriggerId = 0x22;
            trigger.MapName = "S_RM01";
            trigger.MapPages = new byte[] { 0, 1 };
            trigger.LookDirection = Trigger.TriggerDirection.Left;
            trigger.ViewAngle = 375f;
            _triggers.Add(trigger);

            // door trigger (test)
            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x26;
            trigger.Object1Id = 0x0001;
            trigger.Object2Id = 0x0002;
            trigger.Init();
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x27;
            trigger.Object1Id = 0x0003;
            trigger.Init();
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x28;
            trigger.Object1Id = 0x0004;
            trigger.Init();
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x29;
            trigger.Object1Id = 0x0005;
            trigger.Init();
            _triggers.Add(trigger);

            // upper room
            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x2a;
            trigger.Object1Id = 0x0006;
            trigger.Init();
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x2b;
            trigger.Object1Id = 0x0007;
            trigger.Init();
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x2c;
            trigger.Object1Id = 0x0008;
            trigger.Init();
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x2d;
            trigger.Object1Id = 0x0009;
            trigger.Init();
            _triggers.Add(trigger);

            trigger = new Trigger();
            trigger.Type = Trigger.TriggerType.Door;
            trigger.TriggerId = 0x2e;
            trigger.Object1Id = 0x000a;
            trigger.Init();
            _triggers.Add(trigger);

            _lightVolumeLayer = LayerMask.NameToLayer("LightVolume");
        }

        void Update()
        {
            // show selected sprite / sheetid
            string spritePath = Loader.Instance.GetChpFileByIndex(_chpFileIndex);
            string spriteFile = Util.FileSystemHelper.GetFileNameWithoutExtensionFromPath(spritePath);
            UI.Instance.Debug1.text = spriteFile + " (" + (_selectedId + 1) + " / " + _sheetIds.Length + ")";

            InputHelper.ReadInput(_inputData);

            DebugSprites();

            float moveAngle = 0f;
            bool moving = false;
            Vector3 moveVector = Vector3.zero;

            if (_processingTrigger == true)
            {
                if (_activeTrigger.Type == Trigger.TriggerType.MapTransition)
                {
                    if (_activeTrigger.AnimateMove)
                    {
                        moving = true;
                        moveAngle = _activeTrigger.MoveAngle;
                    }
                }
            }
            else
            {
                // get move vector and angle from input
                moveVector = CalculateMoveVectorAndAngle(out moveAngle, out moving);
                _moveAngle = moveAngle;
            }

            if (moving)
            {
                TurnToAngle(moveAngle);
            }

            // collision
            Vector3 position = transform.position;

            float moveDistance = 0f;
            float depenetrationDistance;
            Vector3 depenetrationDirection;
            const float maxMoveDistance = 0.05f;
            Vector3 moveVectorDir = moveVector.normalized;
            float moveVectorLength = moveVector.magnitude;
            bool handleOnce = true;
            GameObject touchingObject = null;

            while (moveVectorLength > 0f || handleOnce == true)
            {
                float moveStepLength = moveVectorLength;
                if (moveStepLength > maxMoveDistance)
                {
                    moveStepLength = maxMoveDistance;
                }
                moveVectorLength -= moveStepLength;
                Vector3 moveStepVector = moveVectorDir * moveStepLength;

                HandleCollision(ref position, ref moveStepVector, transform.rotation, _hitRadius, _collider,
                                out touchingObject, out depenetrationDistance, out depenetrationDirection);
                position += moveStepVector;
                moveDistance += moveStepVector.magnitude;

                if (depenetrationDistance > 0f)
                {
                    // apply wall friction
                    //_moveSpeed = _moveSpeed - (((_moveSpeed * _moveSpeed) * wallFriction) * Time.deltaTime);
                    //if (_moveSpeed < minSpeed)
                    //{
                    //    _moveSpeed = 0f;
                    //}

                    //moveVectorLength = 0f;  // stop collision check

                    //if (depenetrationDirection.y > 0.01f)  // moved upwards?
                    //{
                    //    // slow down fall
                    //    if (_speed.y < -5f)
                    //    {
                    //        _speed.y = -5f;
                    //    }
                    //    //_speed.y = 0f;  // stop fall
                    //}
                }

                handleOnce = false;
            }

            //_finalMoveDistance = moveDistance;
            Vector3 normal = Vector3.zero;
            float groundHeight = GetGroundHeight(position, out normal);
            //UI.Instance.Debug1.text = normal.ToString();

            //position = transform.position + moveVector;
            position.y = groundHeight;
            transform.position = position;

            HandleCameraRotation();
            HandleCameraSettings();

            if (moving)
            {
                _spriteSheet.State = AnimationState.WALK;
            }
            else
            {
                if (_inputData.Button3)
                {
                    _spriteSheet.State = AnimationState.KNEELINGSTATIONARY;
                }
                else
                {
                    _spriteSheet.State = AnimationState.IDLE;
                }
            }

            UpdateSpriteAnimation();

            // check for interaction object
            _interactionObject = null;
            int collisionLayerMask = 1 << LayerMask.NameToLayer("Interaction");
            RaycastHit hitInfo;
            const float checkHeight = 0.25f;
            const float checkRange = 3.2f;
            Vector3 forwardDirection = Quaternion.Euler(0f, _directionAngle, 0f) * Vector3.forward;

            bool objHit = Physics.Raycast(position + new Vector3(0, checkHeight, 0), forwardDirection,
                                          out hitInfo, checkRange, collisionLayerMask, QueryTriggerInteraction.Collide);
            if (objHit)
            {
                _interactionObject = hitInfo.transform.parent.gameObject;
                //UI.Instance.Debug1.text = hitInfo.transform.parent.name;
            }

            if (_processingTrigger == false)
            {
                HandleTriggersActivation();
            }
            else
            {
                if (_activeTrigger.Type == Trigger.TriggerType.Door)
                {
                    bool finished = _activeTrigger.AnimateDoor(Time.deltaTime);

                    if (finished == true)
                    {
                        _activeTrigger.Active = false;
                        _processingTrigger = false;
                    }
                }
            }
        }

        private void HandleTriggersActivation()
        {
            int triggerId = MapData.Instance.GetTriggerIdAtPosition(transform.position);
            //UI.Instance.Debug1.text = triggerId.ToString("X2");

            foreach (Trigger trigger in _triggers)
            {
                if (trigger.Active == false)
                {
                    continue;
                }

                switch (trigger.Type)
                {
                    case Trigger.TriggerType.MapTransition:
                    {
                        if (trigger.TriggerId == triggerId)
                        {
                            _processingTrigger = true;
                            _activeTrigger = trigger;

                            ScreenFade.Instance.FadeOut();
                            ScreenFade.Instance.SubscribeOnFadeOut(ExecuteActiveTrigger);

                            _activeTrigger.MoveAngle = _moveAngle;
                            _activeTrigger.AnimateMove = true;
                        }
                        break;
                    }
                    case Trigger.TriggerType.Door:
                        {
                            if (trigger.TriggerId == triggerId)
                            {
                                if (trigger.HasObject(_interactionObject))
                                {
                                    //trigger.Object1.SetActive(false);
                                    //if (trigger.Object2)
                                    //{
                                    //    trigger.Object2.SetActive(false);
                                    //}
                                    //foreach (GameObject wall in trigger.Walls)
                                    //{
                                    //    wall.SetActive(false);
                                    //}
                                    _processingTrigger = true;
                                    _activeTrigger = trigger;
                                }
                            }
                            break;
                        }
                }
            }
        }

        void ExecuteActiveTrigger()
        {
            ScreenFade.Instance.UnsubscribeOnFadeOut(ExecuteActiveTrigger);

            switch (_activeTrigger.Type)
            {
                case Trigger.TriggerType.MapTransition:
                {
                    if (MapData.Instance.GetName() == _activeTrigger.MapName)
                    {
                        MapData.Instance.ShowRoom(_activeTrigger.MapPages);
                    }

                    Rect destinationArea = MapData.Instance.GetTriggerBoundsById(_activeTrigger.DestinationTriggerId);

                    Vector3 destinationPos = transform.position;
                    float dirAngle = 0f;
                    if (_activeTrigger.LookDirection == Trigger.TriggerDirection.Up)
                    {
                        destinationPos.x = destinationArea.center.x;
                        destinationPos.z = destinationArea.yMin;
                        dirAngle = 180f;
                    }
                    else if (_activeTrigger.LookDirection == Trigger.TriggerDirection.Down)
                    {
                        destinationPos.x = destinationArea.center.x;
                        destinationPos.z = destinationArea.yMax;
                        dirAngle = 0f;
                    }
                    else if (_activeTrigger.LookDirection == Trigger.TriggerDirection.Right)
                    {
                        destinationPos.x = destinationArea.xMin;
                        destinationPos.z = destinationArea.center.y;
                        dirAngle = 270f;
                    }
                    else if (_activeTrigger.LookDirection == Trigger.TriggerDirection.Left)
                    {
                        destinationPos.x = destinationArea.xMax;
                        destinationPos.z = destinationArea.center.y;
                        dirAngle = 90f;
                    }
                    destinationPos = GetStandPosInDirection(destinationPos, dirAngle);
                    transform.position = destinationPos;
                    _directionAngle = dirAngle;

                    //_activeTrigger.MoveAngle = _directionAngle;
                    _activeTrigger.AnimateMove = false;

                    _cameraControl.ViewAngle = _activeTrigger.ViewAngle;
                    _cameraControl.SnapToTarget();

                    ScreenFade.Instance.FadeIn();
                    ScreenFade.Instance.SubscribeOnFadeIn(EndTriggerProcessing);
                    break;
                }
            }
        }

        void EndTriggerProcessing()
        {
            ScreenFade.Instance.UnsubscribeOnFadeIn(EndTriggerProcessing);

            _processingTrigger = false;
            _activeTrigger = null;
        }

        Vector3 GetStandPosInDirection(Vector3 position, float angle)
        {
            // get flat standing spot in angle direction
            Quaternion moveRotation = Quaternion.Euler(0f, angle, 0f);

            float groundHeight = 0;
            bool gotPos = false;
            float moveDistance = 1.6f;

            while (gotPos == false && moveDistance < 6.4f)
            {
                Vector3 forward = (moveRotation * Vector3.forward) * moveDistance;
                position += forward;

                Vector3 normal;
                groundHeight = GetGroundHeight(position, out normal);

                if (Mathf.Approximately(normal.y, 1f))
                {
                    gotPos = true;
                }
                else
                {
                    moveDistance += 1.6f;
                }
            }

            position.y = groundHeight;

            return position;
        }

        //void SetSpriteAnimation(int[] table, float frameTime)
        //{
        //    if (_animTable == table)
        //    {
        //        return;
        //    }

        //    _animTable = table;

        //    _animFrameTime = frameTime;
        //    _animTime = _animFrameTime;

        //    _animIndex = 0;
        //    _animFrame = _animTable[_animIndex];
        //}

        void UpdateSpriteAnimation()
        {
            _animTime += Time.deltaTime;

            //_animTime -= Time.deltaTime;
            //if (_animTime <= 0f)
            //{
            //    _animTime += _animFrameTime;
            //    _animIndex++;

            //    if (_animIndex >= _animTable.Length)
            //    {
            //        _animIndex = 0;
            //    }

            //    _animFrame = _animTable[_animIndex];
            //}

            // sprite animation test
            float cameraAngle = _inputData.CameraAngle + 180f;
            float objViewAngle = (360f - _directionAngle) + cameraAngle;

            if (objViewAngle < 0f)
            {
                objViewAngle += 360f;
            }
            if (objViewAngle > 360f)
            {
                objViewAngle -= 360f;
            }

            float objViewAngleCorrected = objViewAngle + 15.0f;
            if (objViewAngleCorrected >= 360.0f)
            {
                objViewAngleCorrected -= 360.0f;
            }
            dir = (int)(objViewAngleCorrected / 30.0f);

            // convert dir to column and flip
            bool flip = false;
            int rotation = GetRotationAndFlipfromDir(dir, ref flip);

            foreach (SpriteRenderer renderer in _spriteRenderers)
            {
                float fps = 30f;
                int animFrame = (int)Mathf.Round(_animTime * fps);
                renderer.sprite = _spriteSheet[animFrame, rotation];
                renderer.flipX = flip;
                //int frame = (_animFrame * _sheetColumns) + column;

                //if (frame < _sprites.Length)
                //{
                //    renderer.sprite = _sprites[frame];
                //    renderer.flipX = flip;
                //}

                if (_inLight)
                {
                    _lightIntensity = 3.5f;
                }
                else
                {
                    _lightIntensity -= _lightFadeOutSpeed * Time.deltaTime;

                    if (_lightIntensity <= 1.0f)
                    {
                        _lightIntensity = 1f;
                    }
                }

                renderer.sharedMaterial.SetFloat("_Intensity", _lightIntensity);
            }
        }

        // input: 
        //   dir: one of 12 directions (0 is down, 3 is right, 6 is up, ...)
        //
        private int GetRotationAndFlipfromDir(int dir, ref bool flip)
        {
            int rotation = 0;

            switch (dir)
            {
                case 0:
                    // down
                    if (_spriteSheet.Active)
                    {
                        rotation = 4;
                    }
                    else
                    {
                        rotation = 0;
                        flip = true;         
                    }
                    break;
                case 1:
                    rotation = 0;
                    break;
                case 2:
                    rotation = 0;
                    break;
                case 3:
                    rotation = 1;         // right
                    break;
                case 4:
                    rotation = 2;
                    break;
                case 5:
                    rotation = 3;
                    break;
                case 6:
                    rotation = _spriteSheet.Active ? 5 : 3;         // up
                    break;
                case 7:
                    rotation = 3;
                    flip = true;
                    break;
                case 8:
                    rotation = 2;
                    flip = true;
                    break;
                case 9:                 // left
                    rotation = 1;
                    flip = true;
                    break;
                case 10:
                    rotation = 0;
                    flip = true;
                    break;
                case 11:
                    rotation = 0;
                    flip = true;
                    break;
            }

            return rotation;
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

            if (moveAngle >= 360f)
            {
                moveAngle -= 360f;
            }
            else if (moveAngle < 0f)
            {
                moveAngle += 360f;
            }

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

        void HandleCameraSettings()
        {
            if (_inputData.Button4Down)
            {
                _currentCameraSetting++;

                if (_currentCameraSetting >= _cameraDistances.Length)
                {
                    _currentCameraSetting = 0;
                }

                _cameraControl.CameraDistance = _cameraDistances[_currentCameraSetting];
                _cameraControl.CameraHeight = _cameraHeights[_currentCameraSetting];
            }
        }

        public float GetMoveSpeed()
        {
            return 1f;
        }

        public Transform GetTransform()
        {
            return gameObject.transform;
        }

        public float GetGroundHeight(Vector3 position, out Vector3 normal)
        {
            // check for floor
            int collisionLayerMask = 1 << LayerMask.NameToLayer("Ground");

            RaycastHit hitInfo;
            const float rayCheckOffset = 10f;
            bool groundHit = Physics.Raycast(position + new Vector3(0, rayCheckOffset, 0), Vector3.down, 
                                             out hitInfo, 1024f, collisionLayerMask, QueryTriggerInteraction.Ignore);

            normal = hitInfo.normal;

            return hitInfo.point.y;
        }

        private void HandleCollision(ref Vector3 position, ref Vector3 moveVector, Quaternion rotation, float hitRadius, CapsuleCollider ownCollider,
                                    out GameObject touchingObject, out float depenetrationDistance, out Vector3 depenetrationDirection)
        {
            Vector3 otherPosition;
            bool overlapped;

            overlapped = false;

            touchingObject = null;

            int collisionLayerMask = 1 << LayerMask.NameToLayer("Wall");

            Collider[] hitColliders = new Collider[32];
            int count = Physics.OverlapCapsuleNonAlloc(position + moveVector, position + moveVector + new Vector3(0f, ownCollider.height, 0f), hitRadius, hitColliders, collisionLayerMask);

            //int cameraLayer = LayerMask.NameToLayer("Camera");

            depenetrationDistance = 0f;
            depenetrationDirection = Vector3.zero;

            for (int index = 0; index < count; index++)
            {
                Collider collider = hitColliders[index];

                if (collider == ownCollider)
                {
                    continue;   // skip self
                }

                if (collider.isTrigger == true)
                {
                    touchingObject = collider.gameObject;
                    continue;
                }

                //if (collider.GetComponent<NoDepenetration>() == false)
                {
                    otherPosition = collider.gameObject.transform.position;

                    Vector3 direction;
                    float distance;

                    overlapped = Physics.ComputePenetration(ownCollider, position + moveVector, rotation,
                                                                 collider, otherPosition, collider.transform.rotation,
                                                                 out direction, out distance);

                    if (overlapped)
                    {
                        // apply depenetration movement
                        moveVector += direction * distance;

                        if (distance > depenetrationDistance)
                        {
                            depenetrationDistance = distance;
                            depenetrationDirection = direction;
                        }
                    }
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer == _lightVolumeLayer)
            {
                //Debug.Log("Enter Light");
                _inLight = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.layer == _lightVolumeLayer)
            {
                //Debug.Log("Exit Light");
                _inLight = false;
            }
        }

        private void LoadSprites()
        {
            string filePath = Loader.Instance.GetChpFileByIndex(_chpFileIndex);
            _spriteSheets = new SpriteSheets(filePath);
            _sheetIds = _spriteSheets.AvailableIDs;

            _selectedId = 0;
            _spriteSheet = _spriteSheets[_sheetIds[_selectedId], true];
        }

        private void DebugSprites()
        {
            // sprite debug
            if (_inputData.Button1Down)
            {
                _chpFileIndex++;

                if (_chpFileIndex == Loader.Instance.GetChpFileCount())
                {
                    _chpFileIndex = 0;
                }

                LoadSprites();
            }

            if (_inputData.Button2Down)
            {
                _selectedId++;

                if (_selectedId >= _sheetIds.Length)
                {
                    _selectedId = 0;
                }

                _spriteSheet = _spriteSheets[_sheetIds[_selectedId], true];
            }
        }
    }
}
