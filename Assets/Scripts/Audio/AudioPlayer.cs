using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioPlayer : MonoBehaviour
{
	public TextAsset _xmlAudioFile;
	
	struct AudioInfo
	{
        public AudioInfo(string file = "", float volume = 1f, AudioClip clip = null, GameObject soundObject = null)
        {
            m_file = file;
            m_volume = volume;
            m_clip = clip;
            m_SoundObject = soundObject;
        }

        public string m_file;
		public float  m_volume;
		public AudioClip m_clip;
		public GameObject m_SoundObject;
	}
	
	private Dictionary<string, AudioInfo>	m_audioMap = new Dictionary<string, AudioInfo>();
	private Dictionary<string, AudioSource>	m_audioLoopMap = new Dictionary<string, AudioSource>();
	
	// playing tracks
	private Dictionary<string, AudioInfo>	m_musicMap = new Dictionary<string, AudioInfo>();
	
	private string m_currentTrackName;
	private AudioSource m_currentTrack = null;
	
	public bool Enabled = false;
		
	static private AudioPlayer _Instance = null;

    private GameObject _root = null;

    [Range(0, 1)]
    public float MasterVolume = 1f;

    private bool _fadeOutTrack = false;
    private float _fadeOutDuration;
    private float _fadeOutTime;
    private float _fadeStartVolume;

    static public AudioPlayer GetInstance()
	{
		if (_Instance == null) 
		{
            const string rootName = "[AudioPlayer]";
            GameObject root = GameObject.Find(rootName);

            if (root == null)
            {
                root = new GameObject(rootName);
                root.AddComponent<AudioPlayer>();
            }
            
            _Instance = root.GetComponent<AudioPlayer>();
            _Instance.Init(root);
		}

		return _Instance;
	}

    void Init(GameObject root)
    {
        _root = root;

        Load();
    }

    private void Load()
	{
		if (_xmlAudioFile != null)
		{
			// parse level xml file
			XMLNode node = XMLParser.Parse(_xmlAudioFile.text);

			XMLNodeList soundList = node.GetNodeList("audio>0>sound");
			
			foreach (XMLNode soundNode in soundList) 
			{
				string name = soundNode.GetValue("@name");
				string file = soundNode.GetValue("@file");
				string volume = soundNode.GetValue("@volume");
				
				AudioInfo info = new AudioInfo();
				info.m_file = file;
				info.m_volume = 1.0f;
				info.m_SoundObject = new GameObject("Sound_"+name);
                info.m_SoundObject.transform.parent = _root.transform;

                info.m_SoundObject.AddComponent<AudioSource>();
				info.m_clip = (AudioClip)Resources.Load(info.m_file, typeof(AudioClip));

                if (volume != null)
				{
					info.m_volume = System.Convert.ToSingle(volume);	
				}
				
				m_audioMap[name] = info;
			}
			
			XMLNodeList trackList = node.GetNodeList("audio>0>track");
			
            if (trackList != null)
            {
                foreach (XMLNode trackNode in trackList)
                {
                    string name = trackNode.GetValue("@name");
                    string file = trackNode.GetValue("@file");
                    AudioInfo info = new AudioInfo();
                    info.m_clip = (AudioClip)Resources.Load(file, typeof(AudioClip));
                    info.m_SoundObject = new GameObject("Track_" + name);
                    info.m_SoundObject.transform.parent = _root.transform;

                    info.m_SoundObject.AddComponent<AudioSource>();
                    m_musicMap[name] = info;
                }
            }
        }
		
//		DisableSounds();
	}
	
	public void PlaySound(string name, float pitch = 1f, float volumePercentage = 1f)
	{
		PlaySound(name, false, pitch, volumePercentage);
	}
	
	public AudioSource PlaySound(string name, bool loop, float pitch = 1f, float volumePercentage = 1f)
	{
		if (Enabled == false)
		{
			return null;			
		}
		
		//Debug.Log("PlaySound(" + name + "," + loop + ")");
		
		if (m_audioMap.ContainsKey(name) == false)
		{
			Debug.Log("PlaySound error! Could not find sound " + name);
			return null;
		}
		
		AudioInfo info = m_audioMap[name];
		
		AudioClip clip = info.m_clip;
		AudioSource source =  Play(clip, info.m_volume * volumePercentage, loop, info.m_SoundObject, pitch);
		
		if (loop == true)
		{
			if (m_audioLoopMap.ContainsKey(name) == true)
			{
				AudioSource oldSource = m_audioLoopMap[name];
				oldSource.Stop();
			}
				
			m_audioLoopMap[name] = source;
		}
		
		return source;
	}


    public AudioSource PlayMultiSound(string name, bool loop, float pitch = 1f)
	{
		if (Enabled == false)
		{
			return null;			
		}
		
		if (m_audioMap.ContainsKey(name) == false)
		{
			Debug.Log("PlaySound error! Could not find sound " + name);
			return null;
		}
		
		AudioInfo info = m_audioMap[name];
		
		AudioClip clip = info.m_clip;
		AudioSource source =  Play(clip, info.m_volume, loop, info.m_SoundObject, pitch);
		
		if (loop == true)
		{
/*			if (m_audioLoopMap.ContainsKey(name) == true)
			{
				AudioSource oldSource = m_audioLoopMap[name];
				oldSource.Stop();
			}
				
			m_audioLoopMap[name] = source;
			*/
		}
		
		return source;
	}

	public void StopSound(string name)
	{
        if (m_audioMap.ContainsKey(name) == true)
        {
            AudioInfo info = m_audioMap[name];

            if (info.m_SoundObject == null)
            {
                return;
            }

            AudioSource source = info.m_SoundObject.GetComponent<AudioSource>();

            if (source != null)
            {
                source.Stop();
            }
        }

  //      if (m_audioLoopMap.ContainsKey(name) == true)
		//{
		//	AudioSource source = m_audioLoopMap[name];
		//	source.Stop();
			
		//	//// destroy soundsource gameobejct
		//	//Destroy(source.gameObject);
			
		//	//// remove from map
		//	//m_audioLoopMap.Remove(name);
		//}
	}

	public void StopAllSounds()
	{
		ReleaseAllSounds();
	}

	
	public void PlayTrack(string name, float volume = 1.0f)
	{
		PlayTrack(name, true, volume);
	}
	
	public void PlayTrack(string name, bool loop, float volume)
	{
		if (Enabled == false)
		{
			return;			
		}

        StopTrack();

        if (m_musicMap.ContainsKey(name) == false)
		{
			Debug.Log("PlayTrack error! Could not find track " + name);
			return;
		}
		
		AudioClip clip = m_musicMap[name].m_clip;
			
		AudioSource source = PlayTrack(clip, volume, loop, m_musicMap[name].m_SoundObject, 1f);
		//if (loop == true)
		{
			// save current not looped track for destroying
			m_currentTrack = source;
			m_currentTrackName = name;
		}		
	}

    public void FadeOutTrack(float duration)
    {
        if (m_currentTrack == null)
        {
            return;
        }

        _fadeOutTrack = true;
        _fadeOutDuration = duration;
        _fadeOutTime = 0f;
        _fadeStartVolume = m_currentTrack.volume;
    }

    public void StopTrack()
	{
		if (m_currentTrack != null)
		{
			m_currentTrackName = "";
			m_currentTrack.Stop();
			
			//Destroy(m_currentTrack.gameObject);
			
			m_currentTrack = null;
		}
	}
	
	public string GetStreamedName()
	{
		return m_currentTrackName;
	}
	
	public void SetMasterVolume(float volume)
	{
		AudioListener.volume = volume;
	}
	
	public void DisableSounds()
	{
		Enabled = false;
		AudioListener.pause = true;		
		AudioListener.volume = 0;
	}

	public void EnableSounds()
	{
		Enabled = true;
		AudioListener.pause = false;		
		AudioListener.volume = 1;
	}

	public bool SoundEnabled()
	{
		return Enabled;
	}
	
	public void ReleaseAllSounds()
	{
		AudioSource[] sounds = FindObjectsOfType(typeof(AudioSource)) as AudioSource[];
		
		foreach (AudioSource sound in sounds) 
		{
			sound.Stop();
			DestroyImmediate(sound.gameObject);
		}
	
		m_audioLoopMap.Clear();
	}

	public AudioSource Play(AudioClip clip, float volume, bool loop, GameObject soundObject, float pitch)
    {
        return Play(clip, Vector3.zero, volume, loop, soundObject, pitch);
    }

	public AudioSource PlayTrack(AudioClip clip, float volume, bool loop, GameObject soundObject, float pitch)
    {
		return Play(clip, Vector3.zero, volume, loop, soundObject, pitch);
    }

    /// <summary>
    /// Plays a sound at the given point in space by creating an empty game object with an AudioSource
    /// in that place and destroys it after it finished playing.
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="point"></param>
    /// <param name="volume"></param>
    /// <param name="loop"></param>
    /// <returns></returns>
    public AudioSource Play(AudioClip clip, Vector3 point, float volume, bool loop, GameObject soundObject = null, float pitch = 1.0f)
    {
		GameObject go = soundObject;

        if (soundObject == null)
		{
            // create an empty game object with audio source
            go = new GameObject("Sound" + clip.name);
			go.AddComponent<AudioSource>();
		}
		go.transform.position = point;

        AudioSource source = go.GetComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
		source.loop = loop;
        source.Play();
		
//		if (loop == false)
//		{
//			Destroy(source, clip.length);	
//		}
        
        return source;
    }

    private void Update()
    {
        if (_fadeOutTrack == true)
        {
            _fadeOutTime += Time.deltaTime;

            bool stopTrack = false;

            if (_fadeOutTime > _fadeOutDuration)
            {
                _fadeOutTime = _fadeOutDuration;
                stopTrack = true;
            }

            float percentage = 1f - (_fadeOutTime / _fadeOutDuration);
            m_currentTrack.volume = _fadeStartVolume * percentage;

            if (stopTrack == true)
            {
                StopTrack();
                _fadeOutTrack = false;
            }
        }

        SetMasterVolume(MasterVolume);
    }

    public void AddPCMAudio(float[] pcmData, string name, float volume = 1f, int hz = 44100, int channels = 1, bool isTrack = false)
    {
        AudioInfo info = new AudioInfo();
        info.m_volume = volume;
        info.m_SoundObject = new GameObject("Sound_" + name);
        info.m_SoundObject.transform.parent = _root.transform;

        info.m_SoundObject.AddComponent<AudioSource>();

        int length = pcmData.Length;
        if (channels == 2)
        {
            length = length / 2;
        }
        info.m_clip = AudioClip.Create(name, length, channels, hz, false);
        info.m_clip.SetData(pcmData, 0);

        if (isTrack == false)
        {
            m_audioMap[name] = info;
        }
        else
        {
            m_musicMap[name] = info;
        }
    }
}

