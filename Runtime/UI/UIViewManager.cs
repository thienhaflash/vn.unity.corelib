using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace vn.corelib
{
	public class UIViewManager : MonoBehaviour
	{
		[Serializable] public class ViewInfo
		{
			public string viewId;
			public GameObject prefab;
		}
		
		[Serializable] public class ViewLayer
		{
			public string id;
			public Transform parent;
			
			public bool allowEmpty;
			public bool allowStack;
			public List<ViewContext> stack = new ();

			public void AddView(ViewInfo info, object viewData)
			{
				if (!allowStack)
				{
					if (stack.Count > 0) RemoveViewAt(0);
				}

				var context = new ViewContext()
				{
					info = info,
					layer = this,
					viewData = viewData,
					
				};

				var view = Instantiate(info.prefab, parent);
				context.viewGO = view;
				context.viewBase = view.GetComponent<UIViewBase>();
				stack.Add(context);

				if (context.viewBase != null)
				{
					context.viewBase.context = context;	
					context.viewBase.Init();
				}
			}

			public (ViewContext, int) FindLastViewId(string viewId)
			{
				for (var i = stack.Count-1; i >= 0; i--)
				{
					if (stack[i].info.viewId != viewId) continue;
					return (stack[i], i);
				}
				
				return (null, -1);
			}
			
			public bool RemoveLastViewId(string viewId)
			{
				var (_, idx) = FindLastViewId(viewId);
				return idx != -1 && RemoveViewAt(idx);
			}
			
			public bool RemoveViewAt(int index)
			{
				if (index < 0 || index > stack.Count - 1) return false;
				ViewContext context = stack[index];
				stack.RemoveAt(index);
				Destroy(context.viewGO);
				return true;
			}

			public bool RemoveView(ViewContext context)
			{
				var idx = stack.IndexOf(context);
				return RemoveViewAt(idx);
			}

			public void RemoveAll()
			{
				for (var i = 0; i < stack.Count; i++)
				{
					Destroy(stack[i].viewGO);
				}
				stack.Clear();
			}
		}
		
		public class ViewContext
		{
			public ViewInfo info;
			public ViewLayer layer;
			public GameObject viewGO;

			public UIViewBase viewBase;
			public object viewData;

			// context API
			public void Hide()
			{
				layer.RemoveView(this);
			}
		}
		
		public ViewInfo[] viewInfos;
		public ViewLayer[] viewLayers;
		
		private ViewLayer GetLayer(string id)
		{
			return viewLayers.FirstOrDefault(layer => layer.id == id);
		}
		public ViewInfo GetView(string id)
		{
			return viewInfos.FirstOrDefault(info => info.viewId == id);
		}
		
		public void Show(string layerId, string viewId, object viewData = null)
		{
			ViewLayer layer = GetLayer(layerId);
			if (layer == null)
			{
				Debug.LogWarning($"ViewLayer not found {layerId}");
				return;
			}

			ViewInfo info = GetView(viewId);
			if (info == null)
			{
				Debug.LogWarning($"ViewInfo not found {viewId}");
				return;
			}
			
			layer.AddView(info, viewData);
		}

		public void HideAll(string layerId)
		{
			ViewLayer layer = GetLayer(layerId);
			if (layer == null)
			{
				Debug.LogWarning($"ViewLayer not found {layerId}");
				return;
			}

			layer.RemoveAll();
		}

		public bool Hide(string viewId)
		{
			for (var i = 0; i < viewLayers.Length; i++)
			{
				ViewLayer layer = viewLayers[i];
				if (layer.RemoveLastViewId(viewId)) return true;
			}
			
			return false;
		}
	}
	
	public abstract class UIViewBase : MonoBehaviour
	{
		public GameObject viewPrefab;
		[NonSerialized] public UIViewManager.ViewContext context;
		
		public void Init()
		{
			if (viewPrefab == null) return;
			Instantiate(viewPrefab, transform, false);
		}

		public void Hide()
		{
			if (context == null)
			{
				Debug.LogWarning("Context is null - don't know how to hide!");
				return;
			}
			
			context.Hide();
		}
	}
}

