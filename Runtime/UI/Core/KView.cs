using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityObject = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace vn.corelib
{
	public interface IKViewInit
	{
		void Init(KViewContext context);
	}

	public interface IKViewTransition
	{
		void OnBeforeShow();
		void OnAfterShow();
		void OnBeforeHide();
		void OnAfterHide();
	}

	public abstract class KViewBase : MonoBehaviour, IKViewInit, IKViewTransition
	{
		protected KViewContext _context;
		public virtual void Init(KViewContext context)
		{
			_context = context;
		}
		
		public virtual void OnBeforeShow() {}
		public virtual void OnAfterShow() {}
		public virtual void OnBeforeHide() {}
		public virtual void OnAfterHide() {}
	}
	
	[Serializable]
	public class KViewAlias
	{
		public string alias;
		public string[] layerViewIds;
	}

	[Serializable]
	public class KViewInfo
	{
		public string viewId;
		public GameObject prefab;
		[Range(0, 4)] public int layerIndex;
		[NonSerialized] public int index;
	}

	[Serializable]
	public class KViewLayer
	{
		public string id;
		public Transform parent;

		public bool allowEmpty;
		public bool allowStack;
		public List<KViewContext> stack = new();
		
		[NonSerialized] public int index;

		internal void AddView(KView kView, KViewInfo info, object viewData)
		{
			var context = new KViewContext()
			{
				info = info,
				layer = this,
				kView = kView,
				viewData = viewData
			};
			
			kView.listContexts.Add(context);
			context.Show();
		}

		internal bool HideLastView()
		{
			return stack.Count > 0 && HideViewAt(stack.Count - 1);
		}
		
		public bool HideViewAt(int stackIndex)
		{
			if (stackIndex < 0 || stackIndex > stack.Count - 1) return false;
			KViewContext context = stack[stackIndex];
			context.Hide();
			return true;
		}
		
		public void HideAll()
		{
			for (var i = stack.Count - 1; i >= 0; i--)
			{
				HideViewAt(i);
			}
		}
	}

	public class KViewContext
	{
		public KView kView;
		public KViewLayer layer;
		public KViewInfo info;
		
		public object viewData;
		public GameObject viewGO;
		public bool isVisible = true;
		
		// Support transition callbacks
		public IKViewTransition viewTrans;
		
		// context API
		public void Hide()
		{
			// Debug.LogWarning($"Hide: {info.viewId}");
			// Handle OnBeforeHide / OnAfterHide
			
			isVisible = false;
			viewData = null; // remove old data (free memory)
			layer.stack.Remove(this);
			viewGO.SetActive(false);
		}
		
		public void Show()
		{
			if (!layer.allowStack) layer.HideLastView();
			
			if (viewGO == null)
			{
				// Debug.LogWarning($"Create: {info.viewId}");
				viewGO = UnityObject.Instantiate(info.prefab, layer.parent, false);
				viewGO.name = info.viewId;
				
				var kvi = viewGO.GetComponent<IKViewInit>();
				kvi?.Init(this);
			}
			
			// Debug.LogWarning($"Show: {info.viewId}");
			isVisible = true;
			viewGO.SetActive(true);	
			layer.stack.Add(this);

			if (layer.allowStack) // Move to top
			{
				Transform t = viewGO.transform;
				t.SetSiblingIndex(t.parent.childCount-1);
			}
			
			// Handle OnBeforeShow / OnAfterShow
		}
		
		

		internal void Destroy(bool removeFromKView = true)
		{
			if (isVisible)
			{
				Debug.LogWarning("Deleting a visible view?");
				isVisible = false;
				viewData = null; // remove old data (free memory)
				layer.stack.Remove(this);	
			}
			
			if (removeFromKView) kView.listContexts.Remove(this);
			Destroy(viewGO);
			
			// Will null-ize have some positive effect on GC?
			kView = null;
			layer = null;
			info = null;
			viewGO = null;
		}
	}
	
	public partial class KView
	{
		public const string MAIN_LAYER = "MAIN";
		public const string POPUP_LAYER = "POPUP";
		public const string SYSTEM_LAYER = "SYSTEM";
		
		public bool useAsDefault;
		public List<KViewLayer> viewLayers;
		public List<KViewInfo> viewInfos;
		public List<KViewAlias> aliases;
		
		[NonSerialized] private readonly Dictionary<string, string[]> _aliasCache = new();
		[NonSerialized] internal readonly List<KViewContext> listContexts = new(); // storing created views
		
		private void Awake()
		{
			if (!useAsDefault) return;

			InitIndex();
			
			if (defaultInst != null && defaultInst != this)
			{
				useAsDefault = false;
				Debug.LogWarning("Multiple KView instances enabled <useAsDefault>!");
				return;
			}

			defaultInst = this;
			DontDestroyOnLoad(this);
		}

		private void InitIndex()
		{
			for (var i = 0; i < viewLayers.Count; i++)
			{
				viewLayers[i].index = i;
			}

			for (var i = 0; i < viewInfos.Count; i++)
			{
				viewInfos[i].index = i;
			}
		}
		
		// PRIVATE UTILS
		private bool GetLayer(string layerId, out KViewLayer result)
		{
			result = viewLayers.FirstOrDefault(layer => layer.id == layerId);
			if (result != null) return true;
			
			Debug.LogWarning($"Layer not found <{layerId}");
			return false;
		}
		private bool GetView(string viewId, out KViewInfo result)
		{
			result = viewInfos.FirstOrDefault(info => info.viewId == viewId);
			if (result != null) return true;
			Debug.LogWarning($"View not found <{viewId}");
			return false;
		}
		private bool GetAlias(string alias, out string[] result)
		{
			if (aliases.Count != _aliasCache.Count)
			{
				aliases.BuildMap(item => (item.alias, item.layerViewIds), _aliasCache);
			}
			return _aliasCache.TryGetValue(alias, out result);
		}
		
		private void Show(KViewLayer layer, string viewId, object viewData)
		{
			if (!GetView(viewId, out KViewInfo info)) return;
			layer.AddView(this, info, viewData);
		}
		
		// PUBLIC APIs
		public void Show(string viewId, object viewData = null, string layerId = null)
		{
			if (!GetView(viewId, out KViewInfo info)) return;
			
			// context
			// Debug.LogWarning("Existed: " + listContexts.Count);
			foreach (KViewContext c in listContexts)
			{
				if (c.isVisible)
				{
					// Debug.LogWarning("Existed but visible (in-use)");
					continue;
				}
				
				if (c.info.viewId != viewId) continue;
				if (layerId != null && c.layer.id != layerId) continue;
				
				c.Show();
				return;
			}
			
			// create new
			KViewLayer layer = null;
			if (!string.IsNullOrEmpty(layerId)) GetLayer(layerId, out layer);
			layer ??= viewLayers[info.layerIndex];
			layer.AddView(this, info, viewData);
		}
		public void HideLastView(string layerId)
		{
			if (!GetLayer(layerId, out KViewLayer layer)) return;
			layer.HideLastView();
		}
		public void HideLayer(string layerId)
		{
			if (!GetLayer(layerId, out KViewLayer layer)) return;
			layer.HideAll();
		}

		private void FreeUpRam()
		{
			for (var i = listContexts.Count - 1; i >= 0; i--)
			{
				KViewContext context = listContexts[i];
				if (context.isVisible) continue;
				context.Destroy();
			}
		}
		
		// private void Goto(string[] layerViewIds, object viewData = null)
		// {
		// 	if (layerViewIds.Length != viewLayers.Count)
		// 	{
		// 		Debug.LogWarning(
		// 			$"Invalid layerViewIds - expect {viewLayers.Count}, got: {layerViewIds.Length}\nlayerViewIds: {string.Join(",", layerViewIds)}");
		// 		return;
		// 	}
		//
		// 	for (var i = 0; i < viewLayers.Count; i++)
		// 	{
		// 		var viewId = layerViewIds[i];
		// 		switch (viewId)
		// 		{
		// 			case "-" : continue; // ignore views with -
		// 			case "":
		// 			{
		// 				viewLayers[i].HideAll();
		// 				continue;
		// 			}
		//
		// 			default:
		// 			{
		// 				Show(viewLayers[i], viewId, viewData);
		// 				break;
		// 			}
		// 		}
		// 	}
		// }
	}

	public partial class KView // STATIC UTILS
	{
		private static KView defaultInst;

		private static bool HasDefaultInstance()
		{
			if (defaultInst != null) return true;
			Debug.LogWarning("KView's default instance not found!");
			return false;
		}
		
		public static void Goto(string viewId, object viewData = null, string layer = null)
		{
			if (!HasDefaultInstance()) return;
			defaultInst.Show(viewId, viewData, layer ?? MAIN_LAYER);
		}
        
		public static void ShowPopup(string popupId, object viewData = null, bool stack = false)
		{
			if (!HasDefaultInstance()) return;
			
			if (!stack) defaultInst.HideLastView(POPUP_LAYER);
			defaultInst.Show(popupId, viewData, POPUP_LAYER);
		}
		
		public static void HidePopup()
		{
			if (!HasDefaultInstance()) return;
			defaultInst.HideLastView(POPUP_LAYER);
		}
		
		public static void CloseAllPopups()
		{
			if (!HasDefaultInstance()) return;
			defaultInst.HideLayer(POPUP_LAYER);
		}

		public static void CleanUp()
		{
			if (!HasDefaultInstance()) return;
			defaultInst.FreeUpRam();
		}

		// void Goto2(string viewId, object viewData = null)
		// {
		// 	if (!HasDefaultInstance()) return;
		// 	if (!defaultInst.GetView(viewId, out KViewInfo info)) return;
		// 	KViewLayer layer = defaultInst.viewLayers[info.layerIndex];
		// 	defaultInst.Show(layer, info.viewId, viewData);
		// }
	}


#if UNITY_EDITOR
	public partial class KView // EDITOR ONLY
	{
		[ContextMenu("Setup ViewLayers")]
		[Button(ButtonMode.DisabledInPlayMode)]
		private void AutoSetupViewLayers()
		{
			if (viewLayers.Count > 0)
			{
				Debug.LogWarning("ViewLayers has been setup!");
				return;
			}

			var tMain = new GameObject("Main");
			var tPopup = new GameObject("Popup");

			tMain.transform.SetParent(transform, false);
			tPopup.transform.SetParent(transform, false);

			viewLayers = new List<KViewLayer>
			{
				new() {id = "MAIN", allowEmpty = false, parent = tMain.transform, allowStack = false},
				new() {id = "POPUP", allowEmpty = true, parent = tPopup.transform, allowStack = true}
			};
		}

		[ContextMenu("Add Selected Prefab as KViewInfo")]
		[Button(ButtonMode.DisabledInPlayMode)]
		private void AddSelectedPrefabAsViewInfo()
		{
			GameObject[] selectedGOs = Selection.gameObjects;
			if (selectedGOs.Length == 0)
			{
				Debug.LogWarning("No prefab selected!");
				return;
			}

			HashSet<GameObject> existed = viewInfos.Select(item => item.prefab).ToHashSet();
			foreach (GameObject prefab in selectedGOs)
			{
				if (existed.Contains(prefab))
				{
					Debug.LogWarning($"Prefab <{prefab.name}> has been included!");
					continue;
				}

				viewInfos.Add(new KViewInfo()
				{
					layerIndex = 0,
					prefab = prefab,
					viewId = prefab.name
				});
			}
		}
	}
#endif


	public partial class KView : MonoBehaviour
	{
		
		
	}
}