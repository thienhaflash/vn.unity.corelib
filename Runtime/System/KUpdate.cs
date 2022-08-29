// #define KUPDATE_DEBUG

using System;
using System.Collections.Generic;
using UnityEngine;

namespace vn.corelib
{
	public class KUpdate : MonoBehaviour
	{
		private static int COUNTER = 1;
		private static KUpdate _api;
		private static readonly UpdateQueue updateQueue = new ();
		private static readonly UpdateQueue lateUpdateQueue = new ();


		[Serializable]
		class UpdateInfo
		{
#if KUPDATE_DEBUG
            public string description;
#endif

			public int id;
			public bool once;
			public int priority;
			public Action callback;

			public UpdateInfo(Action callback, int priority, bool once)
			{
				id = COUNTER++;

				this.callback = callback;
				this.priority = priority;
				this.once = once;

#if KUPDATE_DEBUG
                this.description = callback.Target.ToString() + "." + callback.Method.Name + "()";
#endif
			}
		}

		[Serializable]
		class UpdateQueue
		{
			private bool _dirty;
			internal List<UpdateInfo> queue = new ();
			private readonly Dictionary<Action, UpdateInfo> _map = new ();
			
			public int Add(Action callback, int priority = 0, bool once = false, bool checkExist = true)
			{
				if (callback == null)
				{
					Debug.LogWarning("callback should not be null!");
					return -1;
				}

				if (checkExist && _map.TryGetValue(callback, out UpdateInfo info))
				{
					Debug.LogWarning("Trying to add the same callback!");
					return info.id;
				}

				var item = new UpdateInfo(callback, priority, once);
				if (checkExist) _map.Add(callback, item);
				queue.Add(item);

				_dirty = true;
				return item.id;
			}

			public bool Remove(int updateId)
			{
				for (var i = 0; i < queue.Count; i++)
				{
					UpdateInfo item = queue[i];
					if (item.id != updateId) continue;
					
					_map.Remove(item.callback);
					item.callback = null; // do not remove items here!
					return true;
				}
				
				return false;
			}

			public bool Remove(Action callback)
			{
				if (callback == null)
				{
					Debug.LogWarning("callback should not be null!");
					return false;
				}

				if (!_map.TryGetValue(callback, out UpdateInfo info)) return false;

				_map.Remove(callback);
				info.callback = null; // do not remove items here!
				return true;
			}

			int QueueSorter(UpdateInfo item1, UpdateInfo item2)
			{
				var n1 = item1 == null;
				var n2 = item2 == null;

				if (n1) return n2 ? 0 : 1;
				if (n2) return -1;

				var result = item1.priority.CompareTo(item2.priority);
				return (result == 0) ? item1.id.CompareTo(item2.id) : result;
			}

			bool ExecuteCallback(UpdateInfo info) // Return true if the callback is alive (will call next time)
			{
				if (info?.callback == null) return false;

#if UNITY_EDITOR
				{
					info.callback();
				}
#else
                {
                    try
                    {
                        info.callback();
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e);
                        return false;
                    }
                }
#endif

				return !info.once;
			}

			public void Dispatch()
			{
				if (_dirty)
				{
					_dirty = false;
					queue.Sort(QueueSorter);
				}

				var dieCount = 0;
				for (var i = 0; i < queue.Count; i++)
				{
					UpdateInfo item = queue[i];
					if (item == null)
					{
						Debug.LogWarning("Something wrong? item == null!");
						dieCount++;
						continue;
					}
					
					var alive = ExecuteCallback(item);
					if (alive) continue;

					dieCount++;
					
					// Remove dead items
					_map.Remove(item.callback); // no need to check for existence
					item.callback = null;
					queue[i] = null;
				}
				
				if (dieCount == 0) return;
				var itemIndex = -1;
				for (var i = 0; i < queue.Count; i++) //shift items up in O(N) fashion
				{
					var isNull = queue[i] == null;
					if (isNull) continue; // skip null items

					itemIndex++;
					if (itemIndex == i) continue; // did not found any null since start
					queue[itemIndex] = queue[i]; // there were some null found, and now we need to shift items left
				}
				
				queue.RemoveRange(itemIndex+1, queue.Count-1-itemIndex);
			}
		}

		public static int OnUpdate(Action callback, int priority = 0, bool once = false)
		{
			if (_api == null) Debug.LogWarning("KUpdate instance not found!");
			return updateQueue.Add(callback, priority, once, true);
		}

		public static int OnLateUpdate(Action callback, int priority = 0, bool once = false)
		{
			if (_api == null) Debug.LogWarning("KUpdate instance not found!");
			return lateUpdateQueue.Add(callback, priority, once, true);
		}

		public static void RemoveLateUpdate(Action callback)
		{
			if (_api == null) return;
			lateUpdateQueue.Remove(callback);
		}

		public static void RemoveUpdate(Action callback)
		{
			if (_api == null) return;
			updateQueue.Remove(callback);
		}

		private void Awake()
		{
			if (_api != null && _api != this)
			{
				Debug.LogWarning("Multiple KUpdate found!");
				Destroy(this);
				return;
			}

			_api = this;
			DontDestroyOnLoad(this);

#if KUPDATE_DEBUG
            updateQueue = _updateQueue.queue;
            lateUpdateQueue = _lateUpdateQueue.queue;
#endif
		}

#if KUPDATE_DEBUG
        // VIEW-ONLY
        public List<UpdateInfo> updateQueue;
        public List<UpdateInfo> lateUpdateQueue;
#endif

		private void Update()
		{
			updateQueue.Dispatch();
		}

		private void LateUpdate()
		{
			lateUpdateQueue.Dispatch();
		}
	}
}