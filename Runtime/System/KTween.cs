using UnityEngine;
using System;
using System.Collections.Generic;

namespace vn.corelib
{
    public static class KTween
    {
        public delegate float EaseFunc(float from, float to, float progress);

        public class Ease
        {
            public static float Linear(float from, float to, float progress) { return from + (to - from) * progress; }
        }
        
        public enum Status
        {
            New, // in queue (delay)
            Start, // onStart called
            Update, // updating progress
            Complete, // done
            Die // or killed
        }
        
        [Serializable] public class Info
        {
            public float delay;
            public float duration;
            
            public float from;
            public float to;
            public EaseFunc ease;
            
            public Action onStart;
            public Action<float> onUpdate;
            public Action onComplete;

            internal float tweenTime;
            internal readonly object id;
            internal Status status;
            
            public Info(object id)
            {
                this.id = id;
                status = Status.New;
            }
        }
        
        internal static readonly List<Info> execQueue = new List<Info>(); // temp
        internal static readonly List<Info> queue = new List<Info>();
        internal static readonly Dictionary<object, Info> map = new Dictionary<object, Info>();

        public static Info Get(object id, bool autoNew = true)
        {
            if (map.TryGetValue(id, out Info result)) return result;
            if (!autoNew) return null;

            var info = new Info(id);
            map.Add(id, info);
            queue.Add(info);
            return info;
        }
        
        public static Info Update(Action<float> onUpdate, float duration = 0.3f, float from = 0f, float to = 1f)
        {
            Info info = Get(onUpdate, true);
            info.onUpdate = onUpdate;
            info.duration = duration;
            info.from = from;
            info.to = to;
            return info;
        }
        
        public static void Kill(object id)
        {
            if (!map.TryGetValue(id, out Info result)) return;
            map.Remove(id);
            result.status = Status.Die;
        }
        
        static KTween()
        {
            KSystem.onUpdate += UpdateFrame;
        }

        static void UpdateFrame()
        {
            if (queue.Count == 0) return;
            
            var dieCount = 0;
            var deltaTime = Mathf.Min(0.05f, Time.deltaTime); // min 20 FPS?
            
            execQueue.Clear();
            for (var i = 0; i < queue.Count; i++)
            {
                var q = queue[i];
                if (q == null)
                {
                    dieCount++;
                    continue;
                }
                
                if (q.status == Status.Die)
                {
                    dieCount++;
                    queue[i] = null;
                    continue;
                }

                if (q.status == Status.New)
                {
                    if (q.delay >= deltaTime)
                    {
                        q.delay -= deltaTime;
                        continue;
                    }
                }
                
                execQueue.Add(q);
            }
            
            // clean up - once in a while
            if (dieCount >= queue.Count / 2f) 
            {
                for (var i = queue.Count - 1; i >= 0; i--)
                {
                    if (queue[i] == null) queue.RemoveAt(i);   
                }
            }
            
            // Update
            for (var i = 0; i < execQueue.Count; i++)
            {
                var q = execQueue[i];
                
                if (q.status == Status.New)
                {
                    q.ease ??= Ease.Linear;
                    q.status = Status.Start;
                    q.onStart?.Invoke();
                }

                if (q.status == Status.Start)
                {
                    // first update
                    q.status = Status.Update;
                    q.onUpdate?.Invoke(q.from);
                    continue;
                }
                
                if (q.status == Status.Update)
                {
                    q.tweenTime += deltaTime;
                    var tp = q.duration == 0 ? 1f : Mathf.Clamp01(q.tweenTime / q.duration);
                    var progress = q.ease(q.from, q.to, tp);
                    q.onUpdate?.Invoke(progress);
                    
                    if (q.tweenTime >= q.duration) {
                        q.status = Status.Complete;
                    }
                }

                if (q.status == Status.Complete)
                {
                    // Early remove so that we can add the same id inside onComplete
                    map.Remove(q.id);
                    q.onComplete?.Invoke();
                    q.status = Status.Die;
                }
            }
        }
    }    
}

