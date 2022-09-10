#define KEVENT_DEBUG

using System;
using System.Collections.Generic;
using UnityEngine;

namespace vn.corelib
{
    public static class KEventUtils
    {
        public static void AddListener(this IKEventSource source, string eventName, Action handler)
        {
            KEvent.Get(source).Add(eventName, 0, handler);
        }
        public static void AddListener<T>(this IKEventSource source, string eventName, Action<T> handler)
        {
            KEvent.Get(source).Add(eventName, 1, handler);
        }
        public static void AddListener<T1, T2>(this IKEventSource source, string eventName, Action<T1, T2> handler)
        {
            KEvent.Get(source).Add(eventName, 2, handler);
        }
        public static void AddListener<T1, T2, T3>(this IKEventSource source, string eventName,
            Action<T1, T2, T3> handler)
        {
            KEvent.Get(source).Add(eventName, 3, handler);
        }

        public static void RemoveListener(this IKEventSource source, string eventName, Action handler)
        {
            KEvent.Get(source).Remove(eventName, 0, handler);
        }
        public static void RemoveListener<T>(this IKEventSource source, string eventName, Action<T> handler)
        {
            KEvent.Get(source).Remove(eventName, 1, handler);
        }
        public static void RemoveListener<T1, T2>(this IKEventSource source, string eventName, Action<T1, T2> handler)
        {
            KEvent.Get(source).Remove(eventName, 2, handler);
        }
        public static void RemoveListener<T1, T2, T3>(this IKEventSource source, string eventName,
            Action<T1, T2, T3> handler)
        {
            KEvent.Get(source).Remove(eventName, 3, handler);
        }

        public static void Dispatch(this IKEventSource source, string eventName)
        {
            KEvent.Dispatcher dsp = KEvent.Get(source);
            Delegate d = dsp.Get(eventName, false)?[0];
            if (d == null) return;
            dsp.EditorTryDispatch(() => d.DynamicInvoke());
        }
        public static void Dispatch<T>(this IKEventSource source, string eventName, T p1)
        {
            KEvent.Dispatcher dsp = KEvent.Get(source);
            Delegate d = dsp.Get(eventName, false)?[1];
            if (d == null) return;
            dsp.EditorTryDispatch(() => d.DynamicInvoke(p1));
        }
        public static void Dispatch<T1, T2>(this IKEventSource source, string eventName, T1 p1, T2 p2)
        {
            KEvent.Dispatcher dsp = KEvent.Get(source);
            Delegate d = dsp.Get(eventName, false)?[2];
            if (d == null) return;
            dsp.EditorTryDispatch(() => d.DynamicInvoke(p1, p2));
        }
        public static void Dispatch<T1, T2, T3>(this IKEventSource source, string eventName, T1 p1, T2 p2, T3 p3)
        {
            KEvent.Dispatcher dsp = KEvent.Get(source);
            Delegate d = dsp.Get(eventName, false)?[3];
            if (d == null) return;
            dsp.EditorTryDispatch(() => d.DynamicInvoke(p1, p2, p3));
        }
    }

    public interface IKEventSource {}
    public static partial class KEvent
    {
        private static readonly Dictionary<object, Dispatcher> _dispatcherMap = new();

        public static Dispatcher Get(object dsp, bool autoNew = true)
        {
            if (_dispatcherMap.TryGetValue(dsp, out Dispatcher result)) return result;
            if (!autoNew) return null;

            result = new Dispatcher();
            _dispatcherMap.Add(dsp, result);
            return result;
        }
        
#if KEVENT_DEBUG
        [Serializable]
        public class DispatcherEventDesc
        {
            public string eventName;
            public List<string> delegateNames = new();

            public DispatcherEventDesc(string eventName, Delegate[] delegates)
            {
                this.eventName = eventName;
                for (var i = 0; i < delegates.Length; i++)
                {
                    Delegate d = delegates[i];
                    if (d == null) continue;

                    foreach (Delegate item in d.GetInvocationList())
                    {
                        delegateNames.Add($"[{i}] : {item.Target}.{item.Method.Name}()");
                    }
                }
            }
        }
#endif

        [Serializable]
        public class Dispatcher
        {
            private const int MAX_PARAMS = 3;
            public readonly Dictionary<string, Delegate[]> map = new();

#if KEVENT_DEBUG
            public List<DispatcherEventDesc> listEvents = new List<DispatcherEventDesc>();
            void RebuildListEvents()
            {
                listEvents.Clear();

                foreach (KeyValuePair<string, Delegate[]> item in map)
                {
                    listEvents.Add(new DispatcherEventDesc(item.Key, item.Value));
                }
            }
#endif

            // INTERNAL APIs
            internal Delegate[] Get(string eventName, bool autoNew)
            {
                if (map.TryGetValue(eventName, out Delegate[] arr)) return arr;

                if (!autoNew) return null;
                arr = new Delegate[MAX_PARAMS + 1];
                map.Add(eventName, arr);
                KUpdate.OnUpdate(RebuildListEvents, 0, true);
                return arr;
            }
            internal void Add(string eventName, int nParams, Delegate d)
            {
                Delegate[] arrDelegate = Get(eventName, true);
                Delegate c = arrDelegate[nParams];

                // Remove first to prevent duplication
                c = Delegate.Remove(c, d);
                arrDelegate[nParams] = Delegate.Combine(c, d);
                KUpdate.OnUpdate(RebuildListEvents, 0, true);
            }
            internal void Remove(string eventName, int nParams, Delegate d)
            {
                Delegate[] arrDelegate = Get(eventName, false);
                if (arrDelegate == null) return;
                Delegate c = arrDelegate[nParams];
                arrDelegate[nParams] = Delegate.Remove(c, d);
                KUpdate.OnUpdate(RebuildListEvents, 0, true);
            }

            // PUBLIC APIs
            public void Clear(string eventName)
            {
                Delegate[] arrDelegate = Get(eventName, false);
                if (arrDelegate == null) return;
                for (var i = 0; i < arrDelegate.Length; i++)
                {
                    arrDelegate[i] = null;
                }

                KUpdate.OnUpdate(RebuildListEvents, 0, true);
            }
            public void Reset()
            {
                _dispatching = false;
                map.Clear();
                KUpdate.OnUpdate(RebuildListEvents, 0, true);
            }
            private bool _dispatching;
            internal void EditorTryDispatch(Action cb)
            {
                if (_dispatching)
                {
                    Debug.LogWarning($"[{nameof(KEvent)}] Nested event dispatching is not supported just yet!");
                    return;
                }

                _dispatching = true;

#if UNITY_EDITOR
                {
                    cb();
                }
#else
                {
                    try
                    {
                        cb();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                        return;
                    }
                    finally {
                         _dispatching = false;
                    }
                }
#endif
                _dispatching = false;
            }
        }
    }
    
    // GLOBAL EVENT DISPATCHER
    public static partial class KEvent
    {
        private static readonly Dispatcher _global = new Dispatcher();
        
        public static void AddListener(string eventName, Action handler)
        {
            _global.Add(eventName, 0, handler);
        }
        public static void AddListener<T>(string eventName, Action<T> handler)
        {
            _global.Add(eventName, 1, handler);
        }
        public static void AddListener<T1, T2>(string eventName, Action<T1, T2> handler)
        {
            _global.Add(eventName, 2, handler);
        }
        public static void AddListener<T1, T2, T3>(string eventName,
            Action<T1, T2, T3> handler)
        {
            _global.Add(eventName, 3, handler);
        }

        public static void RemoveListener(string eventName, Action handler)
        {
            _global.Remove(eventName, 0, handler);
        }
        public static void RemoveListener<T>(string eventName, Action<T> handler)
        {
            _global.Remove(eventName, 1, handler);
        }
        public static void RemoveListener<T1, T2>(string eventName, Action<T1, T2> handler)
        {
            _global.Remove(eventName, 2, handler);
        }
        public static void RemoveListener<T1, T2, T3>(string eventName,
            Action<T1, T2, T3> handler)
        {
            _global.Remove(eventName, 3, handler);
        }

        public static void Dispatch(string eventName)
        {
            Delegate d = _global.Get(eventName, false)?[0];
            if (d == null) return;
            _global.EditorTryDispatch(() => d.DynamicInvoke());
        }
        public static void Dispatch<T>(string eventName, T p1)
        {
            Delegate d = _global.Get(eventName, false)?[1];
            if (d == null) return;
            _global.EditorTryDispatch(() => d.DynamicInvoke(p1));
        }
        public static void Dispatch<T1, T2>(string eventName, T1 p1, T2 p2)
        {
            Delegate d = _global.Get(eventName, false)?[2];
            if (d == null) return;
            _global.EditorTryDispatch(() => d.DynamicInvoke(p1, p2));
        }
        public static void Dispatch<T1, T2, T3>(string eventName, T1 p1, T2 p2, T3 p3)
        {
            Delegate d = _global.Get(eventName, false)?[3];
            if (d == null) return;
            _global.EditorTryDispatch(() => d.DynamicInvoke(p1, p2, p3));
        }
    }
}