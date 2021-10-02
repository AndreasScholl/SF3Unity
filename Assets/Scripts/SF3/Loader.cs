using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using Util;

namespace Shiningforce
{
    public class Loader : MonoBehaviour
    {
        static public Loader Instance;

        public Material _opaqueMaterial;
        public Material _transparentMaterial;
        public Material _emissiveMaterial;
        public Material _unlitMaterial;
        public GameObject _sky = null;
        public GameObject _dirLight = null;

        private Animation _anim;
        private int _animTest;

        private int _objCount = 0;
        private GameObject _object = null;
        private Vector3 _objectPos;
        private Vector3 _objectAngle;
        private float _animTime = 0f;

        private Vector3 _cameraAngle;

        private Vector3 _camPos1 = new Vector3(-80.64f, 23.10f, -63.18f);
        private Vector3 _camAngle1 = new Vector3(20.63f, 222.58f, 0f);

        private Vector3 _camPos2 = new Vector3(-126.86f, 6.53f, -98.01f);
        private Vector3 _camAngle2 = new Vector3(14.44f, 189.76f, 0f);

        private Vector3 _camPosOverview = new Vector3(-200f, 150f, -200f);
        private Vector3 _camAngleOverview = new Vector3(40.0f, 40.0f, 0f);

        private Vector3 _camPosRoomTest = new Vector3(-4f, 35f, -147f);
        private Vector3 _camAngleRoomTest = new Vector3(38.0f, 218.0f, 0f);

        private float _cameraTime = 0f;
        public float _cameraStartTime = 5f;
        public float _cameraDuration = 10f;

        private Vector3 _currentVelocity = Vector3.zero;
        private Vector3 _currentAngleVelocity = Vector3.zero;

        public string _imagePath;

        string[] _mapFiles;
        GameObject _mapRoot = null;
        int _mapCount = 0;

        string[] _battleTerrainFiles;
        GameObject _battleTerrainRoot = null;
        int _battleTerrainCount = 0;
        private string[] _chpFiles;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            Debug.Log("Loader");

            _imagePath = GetImagePath();

            if (_imagePath == "")
            {
                Debug.LogError("No game image found!");
                return;
            }

            EffectManager.Instance.Load();

            // character(sprite) data test
            _chpFiles = Directory.GetFiles(_imagePath, "*.chp", SearchOption.AllDirectories);

            //foreach (string file in _chpFiles)
            //{
            //    ChpData chrData = new ChpData();
            //    chrData.ReadFile(file);
            //}

            //ChpData chrData = new ChpData();
            //Texture2D[] textures = chrData.ReadFile(_imagePath + "/CBP00.CHP");

            //return;

            //_battleTerrainFiles = Directory.GetFiles(_imagePath, "X2*.bin", SearchOption.AllDirectories);
            _mapFiles = Directory.GetFiles(_imagePath, "*.mpd", SearchOption.AllDirectories);

            //ToneExtractor extractor = new ToneExtractor(_imagePath + "/SE.TON", "");

            //StartBattle();

            //return;

            //AudioPlayer.GetInstance().PlayTrack("sf3");

            //_mapCount = GetMapIndex("BTL02");
            //_mapCount = GetMapIndex("SARA02");
            //_mapCount = GetMapIndex("SARA06");
            _mapCount = GetMapIndex("S_RM01");
            //_mapCount = GetMapIndex("S_RM02");
            //_mapCount = GetMapIndex("S_RM03");
            //_mapCount = GetMapIndex("A_RM02");
            CreateMap(_mapCount);

            // map test
            Transform camTransform = Camera.main.transform;
            //camTransform.position = _camPosOverview;
            //camTransform.eulerAngles = _camAngleOverview;
            camTransform.position = _camPosRoomTest;
            camTransform.eulerAngles = _camAngleRoomTest;

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

            GameObject player = new GameObject("Player");
            // 3d character
            //GameObject synbiosModel = LoadObject(0);
            //synbiosModel.transform.parent = player.transform;
            //synbiosModel.transform.eulerAngles = new Vector3(0f, -180f, 0f);
            // 2d character
            GameObject prefab = Resources.Load("Prefabs/Sprite") as GameObject;
            GameObject spriteObj = Instantiate(prefab);
            spriteObj.transform.parent = player.transform;
            prefab = Resources.Load("Prefabs/SpriteShadow") as GameObject;
            GameObject spriteShadowObj = Instantiate(prefab);
            spriteShadowObj.transform.parent = player.transform;

            //synbios.transform.position = new Vector3(-131f, 0f, -111f);
            //synbios.transform.eulerAngles = new Vector3(0f, -90f, 0f);
            player.transform.position = new Vector3(-22.8f, 6.4f, -157f);
            player.transform.eulerAngles = new Vector3(0f, 180f, 0f);
            Character character = player.AddComponent<Character>();

            CameraControl cameraControl = Camera.main.GetComponent<CameraControl>();
            cameraControl.SetTarget(character);

            return;
        }

        public GameObject LoadObject(int objIndex)
        {
            BattleMesh battleMesh = new BattleMesh();

            int variant = 0;

            bool loaded = false;

            bool enemy = false;

            if (objIndex > 100)
            {
                objIndex -= 100;
                enemy = true;
            }

            if (enemy)
            {
                string filePath = _imagePath + "/X8PC7" + objIndex.ToString("D2") + ".BIN";
                loaded = battleMesh.ReadFile(filePath);
            }
            else
            {
                while (loaded == false && variant < 10)
                {
                    string filePath = _imagePath + "/X8PC" + objIndex.ToString("D2") + (char)('A' + variant) + ".BIN";
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
            if (percentage < 0f)
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

            // TEST animate battle camera
            //CameraControl cameraControl = Camera.main.GetComponent<CameraControl>();
            //cameraControl.CameraDistance = 23f;
            //cameraControl.CameraHeight = 2.5f;
            //cameraControl.LookAtHeight = 2.5f;
            //cameraControl.ViewAngle = 180f;

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

            //if (keyboard.f2Key.wasPressedThisFrame)
            //{
            //    ResetBattleCamera();

                //if (_animTest > 0)
                //{
                //    _animTest--;
                //}

                //_anim.Stop();
                //_anim.Play("anim" + _animTest);
            //}

            //if (keyboard.f3Key.wasPressedThisFrame)
            //{
                //_objCount++;

                //if (_object)
                //{
                //    Destroy(_object);
                //}
                //_object = LoadObject(_objCount, false);

                //if (_object != null)
                //{
                //    _object.transform.position = _objectPos;
                //    _object.transform.eulerAngles = _objectAngle;
                //}

                //_animTest = 0;
            //}

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

            if (keyboard.f4Key.wasPressedThisFrame)
            {
                _mapCount++;

                if (_mapCount == _mapFiles.Length)
                {
                    _mapCount = 0;
                }

                CreateMap(_mapCount);
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                _battleTerrainCount++;

                if (_battleTerrainCount == _battleTerrainFiles.Length)
                {
                    _battleTerrainCount = 0;
                }

                CreateBattleTerrain(_battleTerrainCount);
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

        private void CreateMap(int mapCount)
        {
            // destroy loaded map
            MapData mapData = gameObject.GetComponent<MapData>();
            if (mapData)
            {
                Destroy(mapData);
            }

            if (_mapRoot)
            {
                Destroy(_mapRoot);

                _mapRoot = null;
            }

            mapData = gameObject.AddComponent<MapData>();
            mapData.ReadFile(_mapFiles[_mapCount]);
            //_mapRoot = mapData.Create(_opaqueMaterial, _transparentMaterial);
            _mapRoot = mapData.Create(_opaqueMaterial, _emissiveMaterial);

            // room test
            mapData.ShowRoom(new byte[] { 0, 1 });
            //mapData.ShowRoom(new byte[] { 4, 5 });
        }

        private int GetMapIndex(string mapName)
        {
            int index = 0;

            foreach (string mapPath in _mapFiles)
            {
                if (FileSystemHelper.GetFileNameWithoutExtensionFromPath(mapPath).ToLower() == mapName.ToLower())
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private void CreateBattleTerrain(int index)
        {
            if (_battleTerrainRoot)
            {
                Destroy(_battleTerrainRoot);
                _battleTerrainRoot = null;
            }

            BattleTerrain terrain = new BattleTerrain();
            terrain.ReadFile(_battleTerrainFiles[index]);

            _battleTerrainRoot = terrain.CreateObject(_unlitMaterial, _transparentMaterial);
        }

        private void StartBattle()
        {
            AudioPlayer.GetInstance().PlayTrack("battle", 0.5f);

            // 00 brown cobblestone plane (no objects)
            // 01 city (saraband)
            // 06 storage (stonefloor and boxes)
            // 08 fenced plain
            // 09 inside railroad
            // 11 graveyard (dark)
            // 12 restaurant
            // 15 forest
            // 18 cave
            // 20 temple (pillars)
            // 22 castle wall
            // 23 castle wall (on top)
            // 25 castle park?
            _battleTerrainCount = 1;
            CreateBattleTerrain(_battleTerrainCount);

            _dirLight.transform.eulerAngles = new Vector3(50f, 225f, 0f);

            CameraControl cameraControl = Camera.main.GetComponent<CameraControl>();
            cameraControl.enabled = false;

            BattleScene battleScene = gameObject.AddComponent<BattleScene>();
            battleScene.Init(180, 0);

            X4Image image = new X4Image();
            Texture2D skyTexture = image.ReadFile(_imagePath + "/X4EN001.BIN");
            //image.ReadFile(_imagePath + "/X4EN002.BIN");
            //image.ReadFile(_imagePath + "/X4EN003.BIN");
            //image.ReadFile(_imagePath + "/X4EN004.BIN");

            // replace sky capsule texture
            if (_sky != null)
            {
                const float skyScale = 5f;

                Material skyMaterial = _sky.GetComponent<Renderer>().material;
                skyMaterial.mainTexture = skyTexture;
                skyMaterial.mainTextureScale = new Vector2(-5.5f, -6.5f);
                skyMaterial.mainTextureOffset = new Vector2(0f, 1.16f);

                skyMaterial.SetFloat("_SpeedX", 0f);
                skyMaterial.SetFloat("_SpeedY", 0f);

                // set cubemap texture for reflections
                //ReplaceCubemap replaceCubemap = new ReplaceCubemap();
                //replaceCubemap.SetImage(imageLoader.GetTextureByIndex(0));
            }
        }

        public string GetChpFileByIndex(int index)
        {
            return _chpFiles[index];
        }

        public int GetChpFileCount()
        {
            return _chpFiles.Length;
        }

        public int GetChpIndex(string chpName)
        {
            int index = 0;

            foreach (string filePath in _chpFiles)
            {
                if (FileSystemHelper.GetFileNameWithoutExtensionFromPath(filePath).ToLower() == chpName.ToLower())
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}