using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace vn.corelib
{
    public class UIViewManager : MonoBehaviour
    {
        internal static UIViewManager defaultInst;

        [Serializable]
        public class ViewDeepLink
        {
            public string link;
            public string[] layerViewIds;
        }

        [Serializable]
        public class ViewInfo
        {
            public string viewId;
            public GameObject prefab;
            [Range(0, 4)] public int layerIndex;
        }

        [Serializable]
        public class ViewLayer
        {
            public string id;
            public Transform parent;

            public bool allowEmpty;
            public bool allowStack;
            public List<ViewContext> stack = new();

            public void AddView(ViewInfo info, object viewData)
            {
                if (!allowStack)
                {
                    if (stack.Count > 0) RemoveViewAt(0);
                }

                GameObject view = Instantiate(info.prefab, parent);
                var context = new ViewContext()
                {
                    info = info,
                    layer = this,
                    viewData = viewData,
                    viewGO = view,
                    viewBase = view.GetComponent<UIViewBase>()
                };
                stack.Add(context);

                if (context.viewBase == null) return;
                context.viewBase.context = context;
                context.viewBase.Init();
            }

            public (ViewContext, int) FindLastViewId(string viewId)
            {
                for (var i = stack.Count - 1; i >= 0; i--)
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
                foreach (ViewContext t in stack)
                {
                    Destroy(t.viewGO);
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

        public bool useAsDefault;
        public List<ViewLayer> viewLayers;
        public List<ViewInfo> viewInfos;
        public List<ViewDeepLink> deepLinks;
        
        [NonSerialized] private readonly Dictionary<string, string[]> _deepLinkCache = new();

        private void Awake()
        {
            if (!useAsDefault) return;
            if (defaultInst != null && defaultInst != this)
            {
                useAsDefault = false;
                Debug.LogWarning("Multiple instances of UIViewManager set to useAsDefault!");
                return;
            }
            
            defaultInst = this;
            DontDestroyOnLoad(this);
        }
        
        #if UNITY_EDITOR
        [ContextMenu("Setup ViewLayers")]
        [Button] private void AutoSetupViewLayers()
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
            
            viewLayers = new List<ViewLayer>
            {
                new() {id = "MAIN", allowEmpty = false, parent = tMain.transform, allowStack = false},
                new() {id = "POPUP", allowEmpty = true, parent = tPopup.transform, allowStack = true}
            };
        }

        [ContextMenu("Add Selected Prefab as ViewInfo")]
        [Button] private void AddSelectedPrefabAsViewInfo()
        {
            var selectedGOs = Selection.gameObjects;
            if (selectedGOs.Length == 0)
            {
                Debug.LogWarning("No prefab selected!");
                return;
            }
            
            var existed = viewInfos.Select(item => item.prefab).ToHashSet();
            foreach (GameObject prefab in selectedGOs)
            {
                if (existed.Contains(prefab))
                {
                    Debug.LogWarning($"Prefab <{prefab.name}> has been included!");
                    continue;
                }
                viewInfos.Add(new ViewInfo()
                {
                    layerIndex = 0,
                    prefab = prefab,
                    viewId = prefab.name
                });
            }
        }
        #endif
        
        // PRIVATE APIs
        private ViewLayer GetLayer(string id)
        {
            return viewLayers.FirstOrDefault(layer => layer.id == id);
        }

        private ViewInfo GetView(string id)
        {
            return viewInfos.FirstOrDefault(info => info.viewId == id);
        }

        private void RebuildDeepLinkCache()
        {
            _deepLinkCache.Clear();
            for (var i = 0; i < deepLinks.Count; i++)
            {
                ViewDeepLink deepLink = deepLinks[i];
                if (_deepLinkCache.ContainsKey(deepLink.link))
                {
                    Debug.LogWarning($"Duplicated deeplink declaration found {deepLink.link} at {i.ToString()}");
                    continue;
                }

                _deepLinkCache.Add(deepLink.link, deepLink.layerViewIds);
            }
        }


        // PRIVATE APIs
        private void Goto(string[] layerViewIds, object viewData = null)
        {
            if (layerViewIds.Length != viewLayers.Count)
            {
                Debug.LogWarning(
                    $"Invalid layerViewIds - expect {viewLayers.Count}, got: {layerViewIds.Length}\nlayerViewIds: {string.Join(",", layerViewIds)}");
                return;
            }

            for (var i = 0; i < viewLayers.Count; i++)
            {
                var viewId = layerViewIds[i];
                if (viewId == "-") continue; // ignore views with -
                if (viewId == string.Empty)
                {
                    viewLayers[i].RemoveAll();
                    continue;
                }

                Show(viewLayers[i], viewId, viewData);
            }
        }

        private void Show(ViewLayer layer, string viewId, object viewData)
        {
            ViewInfo info = GetView(viewId);
            if (info == null)
            {
                Debug.LogWarning($"ViewInfo not found {viewId}");
                return;
            }

            layer.AddView(info, viewData);
        }


        // PUBLIC APIs
        public void Goto(string deeplink, object viewData = null)
        {
            if (_deepLinkCache.Count != deepLinks.Count)
            {
                RebuildDeepLinkCache();
            }

            if (_deepLinkCache.TryGetValue(deeplink, out string[] data))
            {
                Goto(data, viewData);
                return;
            }

            // show viewId on main
            var viewInfo = GetView(deeplink);
            if (viewInfo != null)
            {
                Show(viewLayers[viewInfo.layerIndex], viewInfo.viewId, viewData);
                return;
            }

            Debug.LogWarning($"Unknown deeplink: {deeplink}");
        }

        public void Show(string layerId, string viewId, object viewData = null)
        {
            ViewLayer layer = GetLayer(layerId);
            if (layer == null)
            {
                Debug.LogWarning($"ViewLayer not found {layerId}");
                return;
            }

            if (string.IsNullOrEmpty(viewId))
            {
                HideLayer(layerId);
                return;
            }

            Show(layer, viewId, viewData);
        }

        public void HideLayer(string layerId)
        {
            ViewLayer layer = GetLayer(layerId);
            if (layer == null)
            {
                Debug.LogWarning($"ViewLayer not found {layerId}");
                return;
            }

            layer.RemoveAll();
        }

        public bool HideView(string viewId)
        {
            return viewLayers.Any(layer => layer.RemoveLastViewId(viewId));
        }
    }
}