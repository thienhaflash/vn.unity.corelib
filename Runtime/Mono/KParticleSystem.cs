using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace vn.corelib
{
    public partial class KParticleSystem : MonoBehaviour
    {
        public List<ParticleSystem> states = new List<ParticleSystem>();

        private static void Play(ParticleSystem p)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorPlay(p);
                return;
            }
            #endif
            
            if (p == null) return;
            p.gameObject.SetActive(true);
            p.Stop(true);
            p.Simulate(0, true, true);
            p.Play(true);
        }

        private static void Stop(ParticleSystem p)
        {
            if (p == null) return;
            p.Stop(true);
            p.gameObject.SetActive(false);
        }
        
        public void PlayAt(int index)
        {
            for (var i = 0; i < states.Count; i++)
            {
                ParticleSystem p = states[i];
                if (i == index)
                {
                    Play(p);       
                }
                else
                {
                    Stop(p);
                }
            }
        }

        public void Stop()
        {
            for (var i = 0; i < states.Count; i++)
            {
                Stop(states[i]);
            }
        }
        
        public void Play(string particleName)
        {
            for (var i = 0; i < states.Count; i++)
            {
                ParticleSystem p = states[i];
                if (p.name != particleName)
                {
                    Stop(p);
                    continue;
                }
                
                Play(p);
            }
        }
    }
    
    
#if UNITY_EDITOR
    public partial class KParticleSystem
    {
        [Button(ButtonMode.DisabledInPlayMode)] internal void GetChildren()
        {
            states.Clear();
            for (var i = 0; i < transform.childCount; i++)
            {
                GameObject source = transform.GetChild(i).gameObject;
                var p = source.GetComponent<ParticleSystem>();
                if (p == null) continue;
                states.Add(p);
            }
            
            EditorUtility.SetDirty(this);
        }

        internal static void EditorPlay(ParticleSystem p)
        {
            if (_simulateTarget != null)
            {
                _simulateTarget.Stop(true);
            }
                
            _simulateTarget = p;
            _simulateTime = Time.realtimeSinceStartup;
            _simulateTarget.Simulate(0, true, true);
                
            EditorApplication.update -= RefreshParticle;
            EditorApplication.update += RefreshParticle;
        }
        
        private static ParticleSystem _simulateTarget;
        private static float _simulateTime;
        private static void RefreshParticle()
        {
            if (_simulateTarget == null)
            {
                EditorApplication.update -= RefreshParticle;
                return;
            }

            var dTime = Time.realtimeSinceStartup - _simulateTime;
            _simulateTime = Time.realtimeSinceStartup;
            
            var t = _simulateTarget.time + dTime;
            if (t > _simulateTarget.main.duration * 2f)
            {
                _simulateTarget.Stop(true);
                _simulateTarget = null;
                EditorApplication.update -= RefreshParticle;
                return;
            }
            
            _simulateTarget.Simulate(dTime, true, false);
            SceneView.RepaintAll();
        }
    }
    
    
    [CustomEditor(typeof(KParticleSystem))]
    public class KParticleSystemEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var kps = (KParticleSystem) target;
            if (kps == null) return;
            
            if (kps.states == null || kps.states.Count == 0)
            {
                EditorGUILayout.HelpBox("No Particle State available", MessageType.Warning);
                
                if (GUILayout.Button("Auto Setup"))
                {
                    kps.GetChildren();    
                }
            }
            
            if (kps.states == null || kps.states.Count == 0) return;
            
            for (var i = 0; i < kps.states.Count; i++)
            {
                var s = kps.states[i];
                if (s == null) continue;

                if (GUILayout.Button(kps.states[i].name))
                {
                    kps.PlayAt(i);
                }
            }
        }
    }
#endif
}

