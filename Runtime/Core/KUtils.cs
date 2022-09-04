using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace vn.corelib
{
    public static class KUtils
    {
		public static List<Transform> GetParents(Transform child, bool includeMe = false, Transform root = null)
		{
			var result = new List<Transform>();
			if (includeMe) result.Add(child);
			if (child == null) return result;

			var p = child.parent;
			while (p != null)
			{
				result.Add(p);
				if (p == root) break;
				p = p.parent;
			}

			// reverse the result to preserve the hierarchy order
			result.Reverse();
			return result;
		}
		
		// public static string GetChildPath(Transform t)
		// {
			
		// }
		
		// public static string GetUniqueComponentID(Transform t)
		// {

		// }
		public static T GetComponent<T>(Transform t)
		{
			var typeT = typeof(T);
			var list = t.GetComponents<MonoBehaviour>();

			foreach (var m in list)
			{
				if (m == null) continue;
				var typeM = m.GetType();
				if (typeT.IsAssignableFrom(typeM)) return (T)(object)m;
			}
			return default(T);
		}

		public static List<T> GetComponentsInChildren<T>(Transform t)
		{
			var typeT = typeof(T);
			var result = new List<T>();

			if (typeof(Component).IsAssignableFrom(typeT)) // find components of Type T
			{
				AppendComponents(t, result);
			}
			else
			{
				AppendInterface(t, result);
			}

			return result;
		}

		static void AppendComponents<T>(Transform t, List<T> result)
		{
			result.AddRange(t.GetComponents<T>());
			if (t.childCount > 0)
			{
				foreach (Transform c in t)
				{
					AppendComponents(c, result);
				}
			}
		}

		static void AppendInterface<T>(Transform t, List<T> result)
		{
			var typeT = typeof(T);
			var list = t.GetComponents<MonoBehaviour>();

			foreach (var m in list)
			{
				if (m == null) continue;
				var typeM = m.GetType();
				if (typeT.IsAssignableFrom(typeM)) result.Add((T)(object)m);
			}

			if (t.childCount > 0)
			{
				foreach (Transform c in t)
				{
					AppendComponents(c, result);
				}
			}
		}
        
        
        public static void SetupButtons(params object[] buttonActionList)
        {
            for (var i = 0; i < buttonActionList.Length; i += 2)
            {
                var btn = buttonActionList[i] as Button;
                if (btn == null)
                {
	                Debug.LogWarning($"Btn is null: {btn}");
	                continue;
                }

                var action = buttonActionList[i + 1] as UnityAction;
                if (action == null)
                {
	                Debug.LogWarning($"Action is null: {action}");
	                continue;
                }

                btn.onClick.RemoveListener(action);
                btn.onClick.AddListener(action);
            }
        }

    }
}