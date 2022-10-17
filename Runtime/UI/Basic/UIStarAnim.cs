using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using vn.corelib;

public class UIStarAnim : MonoBehaviour
{
    [Range(0f, 1f)] public float timeOffset;
    [Range(0f, 1f)] public float soundDelay = 0.5f;
    public string soundFX = "star";
    public List<Animation> stars = new List<Animation>();
    public List<Image> activeStars = new List<Image>();
    
    [Button] public void Play3()
    {
        SetStar(3);
    }
    
    [Button] public void Play2()
    {
        SetStar(2);
    }
    
    public void SetStar(int value)
    {
        for (var i = 0; i < stars.Count; i++)
        {
            Animation anim = stars[i];
            anim.Play(PlayMode.StopAll);
            anim.Sample();
            anim.Stop();
            
            activeStars[i].enabled = i < value;
            if (i >= value)
            {
                KAsync.Kill(anim);
                KAsync.Kill(activeStars[i]);
                continue;
            }

            var delay = (int) (i * timeOffset * 60);
            
            KAsync.DelayCall(() =>
            {
                anim.Play(PlayMode.StopAll);
            }, delay, anim);
            
            if (string.IsNullOrEmpty(soundFX)) continue;
            
            var delay2 = delay + (int) (soundDelay * 60);
            KAsync.DelayCall(() =>
            {
                KSoundFX.Play(soundFX);
            }, delay2, activeStars[i]);
        }
    }
}
