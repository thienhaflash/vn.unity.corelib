using System.Collections.Generic;
using UnityEngine;
using vn.corelib;

public class UIStarAnim : MonoBehaviour
{
    [Range(0f, 1f)] public float timeOffset;
    public List<Animation> stars = new List<Animation>();


    [Button] public void Play3()
    {
        SetStar(3);
    }
    
    public void SetStar(int value)
    {
        for (var i = 0; i < stars.Count; i++)
        {
            Animation anim = stars[i];
            anim.Play();
            anim.Sample();
            anim.Stop();
            
            if (i >= value) continue;
            
            KAsync.DelayCall(() =>
            {
                anim.Play(PlayMode.StopAll);
            }, (int)(i * timeOffset * 60));
        }
    }
}
