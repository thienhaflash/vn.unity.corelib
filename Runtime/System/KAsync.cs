using System;
using System.Collections.Generic;
using UnityEngine;

public static class KAsync
{
    static KAsync()
    {
        KSystem.onUpdate -= UpdateFrame;
        KSystem.onUpdate += UpdateFrame;
    }
    
#if UNITY_EDITOR
    [Serializable] public partial class Info
    {
        public string description;
    }
#endif
    
    public partial class Info
    {
        private static int _counter;
        
        // user config
        public int delay;
        public Action callback;
        
        // internal
        internal bool alive = true;
        internal readonly int instID;
        internal readonly object id;

        public Info(object id)
        {
            instID = ++_counter;
            this.id = id;
        }
        public Info(Action callback, int delay, object id): this(id)
        {   
            this.callback = callback;
            this.delay = delay;
            
#if UNITY_EDITOR
            description = $"[{instID}] {GetType()}: {callback.Target}.{callback.Method.Name}();";
#endif
        }
        public virtual void Callback()
        {
            try
            {
                callback?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception: {e}");
            }
            finally
            {
                alive = false;
            }
        }
    }
    public class Interval : Info
    {
        public readonly int interval;
        public Interval(Action callback, int delay, int interval, object id) : base(callback, delay, id)
        {
            this.interval = interval;
        }
        
        public override void Callback()
        {
            base.Callback();
            delay = interval;
            alive = true;
        }
    }
    public class Wait : Info
    {
        public readonly Func<bool> checkFunc;
        public readonly int interval;
        
        public Wait(Func<bool> checkFunc, Action onComplete, int interval, object id) : base(onComplete, 0, id)
        {
            this.checkFunc = checkFunc;
            this.interval = interval;
        }
        
        public override void Callback()
        {
            if (checkFunc == null)
            {
                alive = false;
                return;
            }
            
            var isFinish = checkFunc();
            if (isFinish)
            {
                base.Callback();
                return;
            }

            delay = interval;
        }
    }
    
    public static int frame;
    public static float time;
    public static float realTime;

    [NonSerialized] private static int _sleepFrame;
    [NonSerialized] private static readonly List<Info> _execQueue = new List<Info>();
    [NonSerialized] internal static readonly List<Info> _queue = new List<Info>();
    [NonSerialized] private static readonly Dictionary<object, Info> _map = new Dictionary<object, Info>();

    private static T TryOverwrite<T>(object id, Action callback, int delayInFrame) where T: Info
    {
        if (!_map.TryGetValue(id, out Info info)) return null;
        info.callback = callback;
        info.delay = delayInFrame;
        return (T)info;
    }
    
    public static void DelayCall(Action callback, int delayInFrame = 0, object customId = null)
    {
        if (callback == null) return;
        _sleepFrame = Mathf.Min(_sleepFrame, delayInFrame);
        
        var id = customId ?? callback;
        if (TryOverwrite<Info>(id, callback, delayInFrame) != null) return;

        var info = new Info(callback, delayInFrame, id);
        _queue.Add(info);
        _map.Add(id, info);
    }
    
    public static void SetInterval(Action callback, int delayInFrame, int intervalInFrame, object customId = null)
    {
        if (callback == null) return;
        _sleepFrame = Mathf.Min(_sleepFrame, delayInFrame);
        
        var id = customId ?? callback;
        if (TryOverwrite<Interval>(id, callback, delayInFrame) != null) return;
        
        var info = new Interval(callback, delayInFrame, intervalInFrame, id);
        _queue.Add(info);
        _map.Add(id, info);
    }
    
    public static void WaitUntil(Func<bool> check, Action onComplete, int checkInterval, object customId = null)
    {
        if (check == null) return;
        var id = customId ?? check;
        _sleepFrame = 0;
        
        if (_map.TryGetValue(id, out Info _))
        {
            Debug.LogWarning("WaitUntil can not be overwritten!");
            return;
        }
        
        var wait = new Wait(check, onComplete, checkInterval, id);
        _queue.Add(wait);
        _map.Add(id, wait);
    }
    
    public static void Kill(object id)
    {
        if (!_map.TryGetValue(id, out Info info)) return;
        _map.Remove(id);
        info.alive = false;
    }
    
    public static void UpdateFrame()
    {
        frame++;
        time = Time.time;
        realTime = Time.realtimeSinceStartup;

        // if (_sleepFrame > 0)
        // {
        //     _sleepFrame--;
        //     return;
        // }
        
        ProcessQueue();
    }

    private static void ProcessQueue()
    {
        // Debug.LogWarning($"Process queue: {_queue.Count}");
        var dieCount = 0;
        _sleepFrame = 1000;
        _execQueue.Clear();
        for (var i = 0; i < _queue.Count; i++)
        {
            Info q = _queue[i];
            if (q == null)
            {
                dieCount++;
                continue;
            }

            if (q.alive == false)
            {
                _queue[i] = null;
                dieCount++;
                continue;
            }
            
            if (q.delay > 0)
            {
                q.delay--;
                _sleepFrame = Mathf.Min(q.delay, _sleepFrame);
                continue;
            }
            
            _execQueue.Add(q);
        }
        
        // compact queue
        if (dieCount > 8 && dieCount >= _queue.Count / 2f) // don't compact too small array it's useless!
        {
            for (var i = _queue.Count - 1; i >= 0; i--)
            {
                if (_queue[i] == null) _queue.RemoveAt(i);
            }
            
            Debug.Log($"Compact --> {_queue.Count}");
        }
        
        for (var i = 0; i < _execQueue.Count; i++)
        {
            Info info = _execQueue[i];
            info.Callback();
            
            if (info.alive) 
            {
                _sleepFrame = Mathf.Min(info.delay, _sleepFrame);
                continue;
            }
            
            _map.Remove(info.id);
        }
    }
}