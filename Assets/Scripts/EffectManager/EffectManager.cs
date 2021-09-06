using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public class EffectManager
{
    [Serializable]
    class EffectData
    {
        public string SubObject;
        public string EffectId;
        public float Duration;
        public bool Loop;
    }

    [Serializable]
    class EffectCompound
    {
        public string Name;
        public List<EffectData> CompoundList;

        public static EffectCompound CreateFromJSON(string filePath)
        {
//            string path = Application.streamingAssetsPath + "/particles/" + file + ".json";

            string jsonString = File.ReadAllText(filePath);

            return JsonUtility.FromJson<EffectCompound>(jsonString);
        }

        public GameObject Create()
        {
            GameObject root = new GameObject(Name);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;

            foreach (EffectData data in CompoundList)
            {
                Transform subParent = root.transform.Find(data.SubObject);
                if (subParent == null)
                {
                    subParent = new GameObject(data.SubObject).transform;
                    subParent.parent = root.transform;
                }

                if (EffectManager.Instance.HasEffect(data.EffectId))
                {
                    GameObject effectObject = GameObject.Instantiate(EffectManager.Instance.GetEffect(data.EffectId));
                    effectObject.transform.parent = subParent;
                    effectObject.SetActive(true);

                    ParticleSystem particleSystem = effectObject.GetComponent<ParticleSystem>();
                    particleSystem.Stop();

                    if (particleSystem != null)
                    {
                        ParticleSystem.MainModule mainModule = particleSystem.main;
                        mainModule.duration = data.Duration;
                        mainModule.loop = data.Loop;
                        mainModule.stopAction = ParticleSystemStopAction.Destroy;
                    }

                    particleSystem.Play();
                }
            }

            return root;
        }
    }

    [Serializable]
    class PrefabData
    {
        public string Name;

        public PrefabData(string name)
        {
            Name = name;
        }
    }

    [Serializable]
    class PrefabTable
    {
        public List<PrefabData> List;

        public static PrefabTable CreateFromJSON(string filePath)
        {
            //            string path = Application.streamingAssetsPath + "/particles/" + file + ".json";

            string jsonString = File.ReadAllText(filePath);

            return JsonUtility.FromJson<PrefabTable>(jsonString);
        }
    }

    private Dictionary<string, GameObject> _effectList;
    private PrefabTable _prefabTable;

    private static EffectManager _instance = null;

    private GameObject _base = null;

    private EffectManager()
    {
    }

    public static EffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new EffectManager();
                _instance.Init();
            }

            return _instance;
        }
    }

    private void Init()
    {
        _base = new GameObject("[EffectManager]");
        _effectList = new Dictionary<string, GameObject>();
    }

    public void Load()
    {
        // preload prefabs
        //
        _prefabTable = PrefabTable.CreateFromJSON(Application.streamingAssetsPath + "/prefabtable.json");

        foreach (PrefabData data in _prefabTable.List)
        {
            GameObject prefabObject = Resources.Load<GameObject>("Prefabs/" + data.Name);
            prefabObject.name = data.Name;
            _effectList[prefabObject.name] = prefabObject;
        }
    }

    public GameObject GetEffect(string name)
    {
        if (_effectList.ContainsKey(name) == false)
        {
            return null;
        }

        GameObject effect = _effectList[name];

        return effect;
    }

    public bool HasEffect(string name)
    {
        if (_effectList.ContainsKey(name) == true)
        {
            return true;
        }

        return false;
    }

    public GameObject CreateOneShot(string name, Vector3 position, float overrideDuration = 0.0f, int repeat = 0, float interval = 0.0f, bool localSpace = false)
    {
        GameObject effect = GameObject.Instantiate<GameObject>(EffectManager.Instance.GetEffect(name));
        effect.SetActive(true);
        effect.transform.position = position;

        ParticleSystem particleSystem = effect.GetComponentInChildren<ParticleSystem>();
        particleSystem.Stop();

        ParticleSystem.MainModule mainModule = particleSystem.main;

        if (overrideDuration > 0.0f)
        {
            mainModule.duration = overrideDuration;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            ParticleSystem.Burst burst = emission.GetBurst(0);
            burst.cycleCount = repeat;
            burst.repeatInterval = interval;
            emission.SetBurst(0, burst);
        }

        if (localSpace == true)
        {
            mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        mainModule.loop = false;
        mainModule.stopAction = ParticleSystemStopAction.Destroy;

        particleSystem.Play();

        return effect;
    }

    public GameObject CreateLooped(string name, Vector3 position, float overrideDuration = 0.0f, bool localSpace = false)
    {
        GameObject effect = GameObject.Instantiate<GameObject>(EffectManager.Instance.GetEffect(name));
        effect.SetActive(true);
        effect.transform.position = position;

        ParticleSystem particleSystem = effect.GetComponent<ParticleSystem>();
        particleSystem.Stop();

        ParticleSystem.MainModule mainModule = particleSystem.main;

        if (overrideDuration > 0.0f)
        {
            mainModule.duration = overrideDuration;
        }

        if (localSpace == true)
        {
            mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
        }

        mainModule.loop = true;
        mainModule.stopAction = ParticleSystemStopAction.Destroy;

        particleSystem.Play();

        return effect;
    }

}
