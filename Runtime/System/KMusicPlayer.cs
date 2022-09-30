using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KMusicPlayer : MonoBehaviour
{
    [Range(0f, 2f)] public float transitionTime;
    [NonSerialized] private List<AudioSource> sources = new List<AudioSource>();
    [NonSerialized]private int _playingIndex;
    [NonSerialized]private AudioClip _clip;
    
    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (sources.Count > 0)
        {
            Debug.LogWarning($"Source has been init with length: {sources.Count}");
            return;
        }
        
        _playingIndex = 0;
        for (var i =0; i < 2; i++)
        {
            var audioSource = new GameObject($"music-{i+1}").AddComponent<AudioSource>();
            audioSource.transform.SetParent(transform, false);
            sources.Add(audioSource);
        }
    }
    
    public void Play(AudioClip clip)
    {
        if (_clip == clip)
        {
            // Debug.LogWarning("Same -->");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("Something wrong? clip is null????");
            Debug.Break();
            return;
        }
        
        _clip = clip;
        StopAllCoroutines();
        StartCoroutine(SwapAudioRoutine());
    }

    IEnumerator SwapAudioRoutine()
    {
        AudioSource mutingSrc = sources[_playingIndex];
        mutingSrc.loop = false;
        
        _playingIndex = (_playingIndex+1) % sources.Count;
        AudioSource playingSrc = sources[_playingIndex];
        playingSrc.clip = _clip;
        playingSrc.volume = 0;
        playingSrc.loop = true;
        playingSrc.Play();
        
        // Debug.LogWarning($"Playing --> {_clip.name}");
        
        var stTime = Time.time;
        while (true)
        {
            var t = Time.time - stTime;
            var pct = transitionTime > 0 ? Mathf.Min(1f, t / transitionTime) : 1f;

            mutingSrc.volume = Mathf.Lerp(mutingSrc.volume, 0f, pct);
            playingSrc.volume = Mathf.Lerp(playingSrc.volume, 1f, pct);
            if (pct >= 1) break;
            yield return null;
        }
        
        // if (mutingSrc.clip != null) Debug.LogWarning($"Muting --> {mutingSrc.clip.name}");
        mutingSrc.clip = null;
    }

    public void Play()
    {
        sources[_playingIndex].Stop();
        sources[_playingIndex].Play();
    }
    
    public void Pause()
    {
        sources[_playingIndex].Pause();
    }

    public void Resume()
    {
        sources[_playingIndex].UnPause();
    }

    public void Stop()
    {
        sources[_playingIndex].Stop();
    }

    public void ClearAndStop()
    {
        StopAllCoroutines();
        _playingIndex = 0;
        _clip = null;
        
        for (var i =0; i < sources.Count; i++)
        {
            sources[i].Stop();
            sources[i].clip = null;
            sources[i].volume = 0;
        }
    }
}
