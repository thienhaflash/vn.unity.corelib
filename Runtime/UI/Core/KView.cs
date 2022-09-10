using System;
using System.Collections.Generic;
using System.Linq;
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
    
    public interface IKViewDataChange
    {
        void OnViewDataChange(KViewContext context);
    }
    
    public interface IKViewTransition
    {
        void OnBeforeShow();
        void OnAfterShow();
        void OnBeforeHide();
        void OnAfterHide();
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
        public bool allowDuplicated;
        public List<KViewContext> stack = new();

        [NonSerialized] public int index;
        
        public bool HideAt(int stackIndex)
        {
            if (stackIndex < 0 || stackIndex > stack.Count - 1) return false;
            stack[stackIndex].Hide();
            return true;
        }
        
        public bool HideLast()
        {
            return stack.Count > 0 && HideAt(stack.Count - 1);
        }
        public void HideAll()
        {
            for (var i = stack.Count - 1; i >= 0; i--)
            {
                HideAt(i);
            }
        }
    }

    public class KViewContext
    {
        public KView kView;
        public KViewLayer layer;
        public KViewInfo info;
        public object viewData;
        public bool isVisible = true;
        
        // --------- GENERATED ----------
        public RectTransform holder;
        public GameObject viewGO;
        public IKViewTransition viewTrans; // Support transition callbacks
        
        // context API
        public void RefreshViewData(object newViewData)
        {
            viewData = newViewData;
            // CALL --> OnViewDataChanged
        }
        
        public void Hide()
        {
            viewTrans?.OnBeforeHide();
            isVisible = false;
            viewData = null; // remove old data (free memory)
            layer.stack.Remove(this);
            viewGO.SetActive(false);
            viewTrans?.OnAfterHide();
        }
        
        public void Show()
        {
            if (!layer.allowStack) layer.HideLast();
            if (viewGO == null)
            {
                // Debug.LogWarning($"Create: {info.viewId}");
                holder = KView.CreateRectTransform("[Container] " + info.viewId, layer.parent);
                viewGO = UnityObject.Instantiate(info.prefab, holder, false);
                viewGO.name = info.viewId;
                viewGO.SetActive(true);
                
                var kvi = viewGO.GetComponent<IKViewInit>();
                kvi?.Init(this);
                
                viewTrans = viewGO.GetComponent<IKViewTransition>();
            }
            
            viewTrans?.OnBeforeShow();
            
            isVisible = true;
            viewGO.SetActive(true);
            layer.stack.Add(this);

            if (layer.allowStack) BringToTop();
            kView.Dispatch(KView.EVENT_SHOW, layer.id, info.viewId);
            viewTrans?.OnAfterShow();
        }

        internal void BringToTop()
        {
            if (!layer.allowStack) return;
            holder.SetSiblingIndex(holder.parent.childCount - 1);
        }
        
        internal void DestroyView(bool removeFromKView = true)
        {
            if (isVisible)
            {
                Debug.LogWarning("Deleting a visible view?");
                isVisible = false;
                viewData = null; // remove old data (free memory)
                layer.stack.Remove(this);
            }

            if (removeFromKView) kView.listContexts.Remove(this);
            UnityObject.Destroy(viewGO);
            UnityObject.Destroy(holder.gameObject);
            
            // Will null-ize have some positive effect on GC?
            kView = null;
            layer = null;
            info = null;
            holder = null;
            viewGO = null;
        }
    }

    public partial class KView : MonoBehaviour, IKEventSource
    {
        public const string EVENT_SHOW = "KView.Show"; 
        private static readonly Dictionary<string, KView> _kViewMap = new Dictionary<string, KView>();

        public static KView GetKViewById(string kViewId)
        {
            return _kViewMap.TryGetValue(kViewId, out var result) ? result : null;
        }
        
        public string kViewId;
        public bool useAsDefault;
        public string initViewId;
           
        public List<KViewLayer> viewLayers = new ();
        public List<KViewInfo> viewInfos = new ();
        public List<KViewAlias> aliases = new ();

        [NonSerialized] private readonly Dictionary<string, string[]> _aliasCache = new();
        [NonSerialized] internal readonly List<KViewContext> listContexts = new(); // storing created views
        
        private void Awake()
        {
            if (useAsDefault)
            {
                if (_defaultInst == null)
                {
                    _defaultInst = this;
                    DontDestroyOnLoad(this);
                }
                else
                {
                    useAsDefault = false;
                    Debug.LogWarning("Multiple KView instances enabled <useAsDefault>!");
                }    
            }
            
            InitIndex();
            if (!string.IsNullOrEmpty(initViewId))
            {
                ShowView(initViewId);
            }
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(kViewId)) return;
            if (!_kViewMap.TryAdd(kViewId, this))
            {
                Debug.LogWarning($"Duplicated kViewId: {kViewId}");
            }
            else
            {
                Debug.LogWarning($"Added: {kViewId}");   
            }
        }
        
        private void OnDestroy()
        {
            if (string.IsNullOrEmpty(kViewId)) return;
            _kViewMap.Remove(kViewId);
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
        private void ShowViewOnLayer(KViewLayer layer, string viewId, object viewData)
        {
            if (layer == null)
            {
                Debug.LogWarning("Layer should not be null!");
                return;
            }
            
            if (viewId == "-") return; // IGNORE
            if (string.IsNullOrEmpty(viewId)) // EMPTY = HIDE ALL
            {
                layer.HideAll();
                return;
            }
            
            // Find & reuse existed Context
            foreach (KViewContext c in listContexts)
            {
                if (c.layer.id != layer.id) continue; // must be the same layer
                if (c.info.viewId != viewId) continue; // must be the same viewId

                if (!c.isVisible)
                {
                    c.viewData = viewData;
                    c.Show();
                    return;
                }
                
                if (!layer.allowDuplicated) // can overwrite the currently visible View with new data
                {
                    c.RefreshViewData(viewData);
                    return;
                }
            }
            
            // Not existed or can not be used: Add NEW!
            if (!GetView(viewId, out KViewInfo info)) return;
            var context = new KViewContext
            {
                info = info,
                layer = layer,
                kView = this,
                viewData = viewData
            };
            
            listContexts.Add(context);
            context.Show();
        }
        public void ShowView(string viewIdOrAlias, object viewData = null, string layerId = null)
        {
            if (layerId != null) // specified layer --> viewId can not be Alias
            {
                if (!GetLayer(layerId, out KViewLayer layer)) return;
                ShowViewOnLayer(layer, viewIdOrAlias, viewData);
                return;
            }
            
            if (GetAlias(viewIdOrAlias, out var viewIds)) // check if viewIdOrAlias is an Alias
            {
                for (var i = 0; i < viewLayers.Count; i++)
                {
                    ShowViewOnLayer(viewLayers[i], viewIds[i], viewData);
                }
                return;
            }
            
            // show viewId on default layer
            if (!GetView(viewIdOrAlias, out KViewInfo viewInfo)) return;
            ShowViewOnLayer(viewLayers[viewInfo.layerIndex], viewIdOrAlias, viewData);
        }

        public void HideLayer(string layerId)
        {
            if (!GetLayer(layerId, out KViewLayer layer)) return;
            if (!layer.allowEmpty)
            {
                Debug.LogWarning($"Can not hide layer {layerId}: allowEmpty == false!");
                return;
            }
            
            layer.HideAll();
        }
        public void HideLastView(string layerId)
        {
            if (!GetLayer(layerId, out KViewLayer layer)) return;
            if (!layer.allowEmpty && layer.stack.Count == 1)
            {
                Debug.LogWarning($"Can not hide last View on layer: {layerId}: allowEmpty == false!");
                return;
            }
            
            layer.HideLast();
        }
        
        private void FreeUpRam()
        {
            for (var i = listContexts.Count - 1; i >= 0; i--)
            {
                KViewContext context = listContexts[i];
                if (context.isVisible) continue;
                context.DestroyView(false);
                listContexts.RemoveAt(i);
            }
        }
    }

    public partial class KView // STATIC APIs
    {
        private static KView _defaultInst;

        private static bool HasDefaultInstance()
        {
            if (_defaultInst != null) return true;
            Debug.LogWarning("KView's default instance not found!");
            return false;
        }

        public static void Goto(string viewId, object viewData = null, string layer = null)
        {
            if (!HasDefaultInstance()) return;
            _defaultInst.ShowView(viewId, viewData, layer);
        }
        
        public static void ShowPopup(string popupId, object viewData = null, bool stack = false)
        {
            if (!HasDefaultInstance()) return;
            if (!stack) _defaultInst.HideLastView(POPUP_LAYER);
            _defaultInst.ShowView(popupId, viewData, POPUP_LAYER);
        }
        
        public static void HidePopup()
        {
            if (!HasDefaultInstance()) return;
            _defaultInst.HideLayer(POPUP_LAYER);
        }
        
        public static void CleanUp()
        {
            if (!HasDefaultInstance()) return;
            _defaultInst.FreeUpRam();
        }
    }
    
    public partial class KView // CONST + UTILS
    {
        public const string MAIN_LAYER = "MAIN";
        public const string POPUP_LAYER = "POPUP";
        public const string SYSTEM_LAYER = "SYSTEM";
        
        internal static RectTransform CreateRectTransform(string goName, Transform parent)
        {
            var rectTrans = (RectTransform) new GameObject(goName, typeof(RectTransform)).transform;
            rectTrans.SetParent(parent, false);
            rectTrans.anchorMin = new Vector2(0, 0);
            rectTrans.anchorMax = new Vector2(1, 1);
            rectTrans.sizeDelta = Vector2.zero;
            return rectTrans;
        }
        
        
        // UTILS
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
    }
    
    
#if UNITY_EDITOR
    public partial class KView // EDITOR ONLY
    {
        [ContextMenu("Setup ViewLayers")]
        [Button(ButtonMode.DisabledInPlayMode)]
        private void Editor_SetupViewLayers()
        {
            if (viewLayers.Count > 0)
            {
                var validParent = viewLayers.All(t => t.parent != null);
                if (validParent)
                {
                    Debug.LogWarning("ViewLayers has been setup!");
                    return;
                }
            }

            viewLayers = new List<KViewLayer>
            {
                new()
                {
                    id = "MAIN", 
                    allowEmpty = false,
                    allowStack = false,
                    allowDuplicated = false,
                    parent = CreateRectTransform("Main", transform)
                },
                new()
                {
                    id = "POPUP", 
                    allowEmpty = true, 
                    allowStack = true,
                    allowDuplicated = false,
                    parent = CreateRectTransform("Popup", transform)
                }
            };
        }

        [ContextMenu("Add Selected Prefab as KViewInfo")]
        [Button(ButtonMode.DisabledInPlayMode)]
        private void Editor_AddSelectedPrefabAsViewInfo()
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

                viewInfos.Add(new KViewInfo
                {
                    layerIndex = 0,
                    prefab = prefab,
                    viewId = prefab.name
                });
            }
        }
    }
#endif
}