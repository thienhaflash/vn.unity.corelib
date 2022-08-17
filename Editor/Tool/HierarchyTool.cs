#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace vn.corelib
{
    public static class HierarchyTool
    {
        static GameObject GetSelectedHierarchyGO()
        {
            GameObject go = Selection.activeGameObject;
            if (go == null || !go.scene.IsValid()) return null;
            return go;
        }

        static void SetActiveOffset(int offset)
        {
            GameObject go = GetSelectedHierarchyGO();
            if (go == null) return;
            var title = EditorWindow.focusedWindow.titleContent.text;
            if (!title.Contains("Hierarchy") && !title.Contains("Scene"))
            {
                Debug.LogWarning(title);
                return;
            }

            Transform t = go.transform;
            Transform p = t.parent;
            if (p == null) return; // do it later
            var nChildren = p.childCount;
            if (nChildren <= 1) return;

            var idx = t.GetSiblingIndex();
            var nidx = (idx + offset + nChildren) % nChildren;
            var ngo = p.GetChild(nidx).gameObject;

            go.SetActive(false);
            ngo.SetActive(true);
            Selection.activeGameObject = ngo;
        }

        [MenuItem("Tools/Hierarchy/Active Prev Sibling _,")]
        static void ActivePrev()
        {
            SetActiveOffset(-1);
        }

        [MenuItem("Tools/Hierarchy/Active Next Sibling _.")]
        static void ActiveNext()
        {
            SetActiveOffset(1);
        }

        [MenuItem("Tools/Hierarchy/Toggle Active _a")]
        static void Active()
        {
            var s = Selection.gameObjects;
            if (s.Length == 0 || string.IsNullOrEmpty(s[0].scene.name)) return;
            var a = s[0].activeSelf;
            foreach (var t in s)
            {
                t.SetActive(!a);
            }
        }
        
        [MenuItem("Tools/Hierarchy/Focus ^h")]
        static void FocusHierarchyPanel()
        { 
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            foreach (var t in windows)
            {
                if (!t.titleContent.text.Contains("Hierarchy")) continue;
                EditorWindow.FocusWindowIfItsOpen(t.GetType());
                break;
            }
        }
    }
}
#endif