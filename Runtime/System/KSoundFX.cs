using System;
using System.Collections.Generic;
using UnityEngine;

public class KSoundFX : MonoBehaviour
{
    private static KSoundFX _api;
    public static KSoundFX Api => _api;


    [Range(1, 10)] public int maxSFX = 5;
    public List<AudioClip> clips = new List<AudioClip>();
    
    [NonSerialized] private readonly List<AudioSource> _audioPool = new List<AudioSource>();
    [NonSerialized] private readonly Dictionary<string, AudioClip> _clipMap = new Dictionary<string, AudioClip>();
    [NonSerialized] private int _playingIdx = -1;

    private void Awake()
    {
        if (_api != null && _api != this)
        {
            Add(clips);
            Destroy(this);
            return;
        }

        _api = this;
        Init();
    }
    
    public void Init()
    {
        KApi.RegisterCommand("PlaySFX", PlaySFXApi);
        KApi.RegisterCommand("AddSFX", AddSFXApi);
        
        for (var i = 0; i < maxSFX; i++)
        {
            var go = new GameObject($"SFX_{i + 1}");
            DontDestroyOnLoad(go);
            
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            _audioPool.Add(src);
        }

        Add(clips);
        DontDestroyOnLoad(this);
    }
    
    private object AddSFXApi(Dictionary<string, object> data)
    {
        if (data.ContainsKey("clip"))
        {
            Add(data["clip"] as AudioClip);
            return null;
        }
        
        if (data.ContainsKey("clips"))
        {
            Add(data["clips"] as List<AudioClip>);
            return null;
        }
        
        return null;
    }
    
    private object PlaySFXApi(Dictionary<string, object> data)
    {
        Play(data["id"] as string);
        return null;
    }
    
    public void Add(AudioClip clip, string clipName = null)
    {
        if (string.IsNullOrEmpty(clipName)) clipName = clip.name;
        if (_clipMap.TryGetValue(clipName, out var result))
        {
            Debug.LogWarning($"ClipName {clipName} existed!");
            return;
        }
        
        _clipMap.Add(clipName, clip);
    }
    
    public void Add(List<AudioClip> listClips)
    {
        for (var i = 0; i < listClips.Count; i++)
        {
            Add(listClips[i]);
        }
    }
    
    public void Play(string clipName)
    {
        if (!_clipMap.TryGetValue(clipName, out AudioClip clip))
        {
            Debug.LogWarning($"Clip not found <{clipName}>");
            return;
        }
        
        _playingIdx = (_playingIdx + 1) % maxSFX;
        AudioSource src = _audioPool[_playingIdx];
        
        // Debug.LogWarning($"Play {clipName} at {_playingIdx}");
        
        src.clip = clip;
        src.Play();
    }
    
    
}
