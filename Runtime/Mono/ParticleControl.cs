using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using vn.corelib;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace vn.corelib
{
    public class ParticleControl : MonoBehaviour
    {
        [Range(0f, 10f)] public float stopDuration;
        public List<ParticleSystem> particles = new List<ParticleSystem>();
        public UnityEvent onStop;
    
#if UNITY_EDITOR
        [Button(ButtonMode.DisabledInPlayMode)] void GetChildren()
        {
            particles = GetComponentsInChildren<ParticleSystem>(true).ToList();
            EditorUtility.SetDirty(this);
        }
#endif

        [Button] public void Play()
        {
            foreach (ParticleSystem p in particles)
            {
                if (p == null) continue;
                p.Play();
            }
            
        }

        [Button] public void Stop()
        {
            foreach (ParticleSystem p in particles)
            {
                if (p == null) continue;
                p.Stop();
            }
        }
    }
}

