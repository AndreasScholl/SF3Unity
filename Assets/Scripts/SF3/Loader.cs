using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shiningforce
{
    public class Loader : MonoBehaviour
    {
        public Material _opaqueMaterial;
        public Material _transparentMaterial;

        private Animation _anim;
        private int _animTest;

        private int _objCount = 0;
        private GameObject _object = null;
        private Vector3 _objectPos;
        private Vector3 _objectAngle;
        private float _animTime = 0f;

        private Vector3 _cameraAngle;

        private Vector3 _camPos1 = new Vector3(-80.64f, 23.10f, -63.18f);
        private Vector3 _camAngle1 = new Vector3(20.63f, 222.58f, -0f);

        private Vector3 _camPos2 = new Vector3(-126.86f, 6.53f, -98.01f);
        private Vector3 _camAngle2 = new Vector3(14.44f, 189.76f, 0f);

        private float _cameraTime = 0f;
        public float _cameraStartTime = 5f;
        public float _cameraDuration = 10f;

        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _currentAngleVelocity = Vector3.zero;

        string _imagePath;

        void Start()
        {
            Debug.Log("Loader");

            _imagePath = GetImagePath();

            if (_imagePath == "")
            {
                return;
            }

            //AudioPlayer.GetInstance().PlayTrack("sf3");

            MapData mapData = gameObject.AddComponent<MapData>();
            //mapData.ReadFile(_imagePath + "/BTL02.MPD");
            //mapData.ReadFile(_imagePath + "/SARA02.MPD");
            mapData.ReadFile(_imagePath + "/SARA06.MPD");
            mapData.CreateObject(_opaqueMaterial, _transparentMaterial);

            // objects (test)
            //Transform camTransform = Camera.main.transform;
            //camTransform.position = new Vector3(-140.0f, 16f, -96f);
            //camTransform.eulerAngles = new Vector3(28f, 131f, 0f);
            //_cameraAngle = Camera.main.transform.localEulerAngles;

            //GameObject dantares = LoadObject(1);
            //dantares.transform.position = new Vector3(-133f, 0f, -114.5f);
            //dantares.transform.eulerAngles = new Vector3(0f, -90f, 0f);

            //GameObject masquirin = LoadObject(2);
            //masquirin.transform.position = new Vector3(-133f, 0f, -108f);
            //masquirin.transform.eulerAngles = new Vector3(0f, -90f, 0f);

            //GameObject grace = LoadObject(3);
            //grace.transform.position = new Vector3(-134f, 0f, -111f);
            //grace.transform.eulerAngles = new Vector3(0f, -90f, 0f);

            //GameObject monk1 = LoadObject(80, true);
            //GameObject monk2 = LoadObject(80, true);
            //GameObject monk3 = LoadObject(80, true);
            //GameObject monk4 = LoadObject(80, true);
            //GameObject monk5 = LoadObject(80, true);
            //monk1.transform.position = new Vector3(-123f, 0f, -113f);
            //monk2.transform.position = new Vector3(-123f, 0f, -109f);
            //monk3.transform.position = new Vector3(-121f, 0f, -106f);
            //monk4.transform.position = new Vector3(-121f, 0f, -115f);
            //monk5.transform.position = new Vector3(-119f, 0f, -111f);
            //monk1.transform.eulerAngles = new Vector3(0f, 90f, 0f);
            //monk2.transform.eulerAngles = new Vector3(0f, 90f, 0f);
            //monk3.transform.eulerAngles = new Vector3(0f, 90f, 0f);
            //monk4.transform.eulerAngles = new Vector3(0f, 90f, 0f);
            //monk5.transform.eulerAngles = new Vector3(0f, 90f, 0f);

            //GameObject synbios = LoadObject(0);
            //synbios.transform.position = new Vector3(-131f, 0f, -111f);
            //synbios.transform.eulerAngles = new Vector3(0f, -90f, 0f);

            //_object = synbios;
            //_objectPos = synbios.transform.position;
            //_objectAngle = synbios.transform.eulerAngles;

            return;

            //_objCount = 11;
            //Transform camTransform = Camera.main.transform;
            //camTransform.position = new Vector3(-43f, 11f, -38.7f);
            //camTransform.eulerAngles = new Vector3(-9f, 50f, 0f);
            //_object = LoadObject(_objCount);
        }

        private GameObject LoadObject(int objCount, bool enemy = false)
        {
            BattleMesh battleMesh = new BattleMesh();

            int variant = 0;

            bool loaded = false;

            if (enemy)
            {
                string filePath = _imagePath + "/X8PC7" + objCount.ToString("D2") + ".BIN";
                loaded = battleMesh.ReadFile(filePath);
            }
            else
            {
                while (loaded == false && variant < 10)
                {
                    string filePath = _imagePath + "/X8PC" + objCount.ToString("D2") + (char)('A' + variant) + ".BIN";
                    //Debug.Log(filePath);
                    loaded = battleMesh.ReadFile(filePath);

                    if (loaded == false)
                    {
                        variant++;
                    }
                }
            }

            if (loaded == true)
            {
                GameObject obj = battleMesh.CreateObject(_opaqueMaterial, _transparentMaterial);
                _anim = battleMesh.CreateAnimations(obj);
                _anim.Play("anim0");
                _animTime = 0f;

                return obj;
            }

            return null;
        }

        void AnimateCamera()
        {
            //_cameraAngle.x = Camera.main.transform.localEulerAngles.x;
            //_cameraAngle.y += (Time.deltaTime / 20f) * 360f;
            //Camera.main.transform.localEulerAngles = _cameraAngle;

            _cameraTime += Time.deltaTime;

            float time = _cameraTime - _cameraStartTime;
            float percentage = time / _cameraDuration;

            if (percentage > 1f)
            {
                percentage = 1f;
            }
            if (percentage< 0f)
            {
                percentage = 0f;
                Camera.main.transform.position = _camPos1;
                Camera.main.transform.eulerAngles = _camAngle1;
            }

            //Camera.main.transform.position = Vector3.Lerp(_camPos1, _camPos2, percentage);

            if (time >= 0f)
            {
                Camera.main.transform.position = Vector3.SmoothDamp(Camera.main.transform.position, _camPos2, ref _currentVelocity, _cameraDuration);
                Camera.main.transform.eulerAngles = Vector3.SmoothDamp(Camera.main.transform.eulerAngles, _camAngle2, ref _currentAngleVelocity, _cameraDuration);
                //Camera.main.transform.eulerAngles = Vector3.Lerp(_camAngle1, _camAngle2, percentage);
            }
        }

        void Update()
        {
            if (_imagePath == "")
            {
                return;
            }

            //AnimateCamera();

            Keyboard keyboard = Keyboard.current;

            if (keyboard.f12Key.wasPressedThisFrame)
            {
                _cameraTime = 0f;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                if (_animTest < _anim.GetClipCount() - 1)
                {
                    _animTest++;
                }

                _anim.Stop();

                string animName = "anim" + _animTest;
                _anim[animName].wrapMode = WrapMode.ClampForever;

                _anim.Play(animName);
                _animTime = _anim[animName].length;
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                if (_animTest > 0)
                {
                    _animTest--;
                }

                _anim.Stop();
                _anim.Play("anim" + _animTest);
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                _objCount++;

                if (_object)
                {
                    Destroy(_object);
                }
                _object = LoadObject(_objCount, false);

                if (_object != null)
                {
                    _object.transform.position = _objectPos;
                    _object.transform.eulerAngles = _objectAngle;
                }

                _animTest = 0;
            }

            if (_animTime > 0f)
            {
                _animTime -= Time.deltaTime;

                if (_animTime <= 0f)
                {
                    _animTime = 0f;

                    _anim.Stop();
                    _anim.Play("anim0");
                }
            }
        }

        private string GetImagePath()
        {
            string imageFolder = "Image";

            bool pathValid = IsPathValid(imageFolder);

            if (pathValid == true)
            {
                return imageFolder;
            }

            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in allDrives)
            {
                pathValid = IsPathValid(drive.Name);

                if (pathValid == true)
                {
                    return drive.Name;
                }
            }

            return "";
        }

        private bool IsPathValid(string imageFolder)
        {
            string mainFile = "1st.bin";

            string mainFilePath = imageFolder + "/" + mainFile;

            if (File.Exists(mainFilePath))
            {
                FileInfo fileInfo = new FileInfo(mainFilePath);

                return true;
            }

            return false;
        }
    }
}
