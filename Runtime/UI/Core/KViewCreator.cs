#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
#endif

namespace vn.corelib
{
    public class KViewCreator : MonoBehaviour
    {
#if UNITY_EDITOR
        public enum CreateState
        {
            None,
            CreatedGO,
            CreatedScript,
            AttachedScript,
            SavedPrefab
        }

        private const string SCRIPT_TEMPLATE =
            "using UnityEngine;\n" +
            "namespace NAMESPACE {\n" +
                "\tpublic partial class CLASS_NAME : MonoBehaviour\n" +
                "\t{\n" +
                "\t}\n"+
            "}\n";
        
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            var listCreators = Resources.FindObjectsOfTypeAll<KViewCreator>();
            for (var i =0 ; i< listCreators.Length;i ++)
            {
                KViewCreator c = listCreators[i];
                if (c.automate == false) continue;
                c.ContinueAutomate();
            }
        }
        
        [Serializable] public class CreateViewInfo
        {
            public string id;
            public Sprite guideSprite;
            public RectTransform trans;
            
            public string className;
            public string classPath;
            public string prefabPath;
            public string folderPath;
            
            public GameObject prefab;
            public MonoScript script;
        }
        
        public string path = "Assets/UI";
        public string classNamespace = "vn.corelib";
        public CreateState state;
        public bool automate;
        public List<CreateViewInfo> listInfo = new List<CreateViewInfo>();

        [Button] public void CreateViewFromSelection()
        {
            automate = true;
            state = CreateState.None;
            
            var s = Selection.objects;
            listInfo.Clear();
            for (var i = 0; i < s.Length; i++)
            {
                Debug.LogWarning(s[i]);
                var assetPath = AssetDatabase.GetAssetPath(s[i]);
                
                listInfo.Add(new CreateViewInfo()
                {
                    id = s[i].name,
                    guideSprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath)
                });
            }
            
            ContinueAutomate();
        }
        
        private void ContinueAutomate()
        {
            EditorApplication.update -= ContinueAutomate;
            
            switch (state)
            {
                case CreateState.None:
                    CreateGO();
                    break;
                
                case CreateState.CreatedGO:
                    CreateScript();
                    return;
                
                case CreateState.CreatedScript:
                    AttachScript();
                    break;
                
                case CreateState.AttachedScript:
                    SavePrefab();
                    break;
                case CreateState.SavedPrefab:
                    Debug.LogWarning("All Done!");
                    return;
            }
            
            EditorApplication.update += ContinueAutomate;
        }
        public void CreateGO()
        {
            for (var i = 0; i < listInfo.Count; i++)
            {
                CreateViewInfo info = listInfo[i];
                
                if (info.trans != null)
                {
                    Debug.LogWarning($"{info.trans} created!");
                    continue;
                }

                if (string.IsNullOrEmpty(info.id))
                {
                    Debug.LogWarning("Invalid viewId");
                    return;
                }
                
                var inst = new GameObject($"{info.id}", typeof(RectTransform), typeof(Canvas));
                var trans = (RectTransform) inst.transform;
                info.trans = trans;
                info.className = $"{info.id}";
                info.folderPath = $"{path}/{info.id}";
                info.prefabPath = $"{info.folderPath}/{info.id}.prefab";
                info.classPath = $"{info.folderPath}/{info.id}.cs";
            
                // setup default
                trans.SetParent(transform, false);
                trans.anchorMin = Vector2.zero;
                trans.anchorMax = Vector2.one;
                trans.sizeDelta = Vector2.zero;
                
                // add preview image
                var imgGO = new GameObject("preview", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                var imgTrans = (RectTransform)imgGO.transform;
                imgTrans.SetParent(trans, false);
                imgTrans.anchorMin = Vector2.zero;
                imgTrans.anchorMax = Vector2.one;
                imgTrans.sizeDelta = Vector2.zero;
                imgTrans.GetComponent<Image>().sprite = info.guideSprite;
            }
            
            state = CreateState.CreatedGO;
            EditorUtility.SetDirty(this);
        }
        
        public void CreateScript()
        {
            if (state != CreateState.CreatedGO)
            {
                Debug.LogWarning($"Invalid state: {state}");
                return;
            }
            
            for (var i = 0; i < listInfo.Count; i++)
            {
                CreateViewInfo info = listInfo[i];
                Directory.CreateDirectory(info.folderPath);
            
                File.WriteAllText(info.classPath, 
                    SCRIPT_TEMPLATE
                        .Replace("CLASS_NAME", info.className)
                        .Replace("NAMESPACE", classNamespace)
                );
                
                AssetDatabase.ImportAsset(info.classPath);
            }
            
            state = CreateState.CreatedScript;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        public void AttachScript()
        {
            if (state != CreateState.CreatedScript)
            {
                Debug.LogWarning($"Invalid state: {state}");
                return;
            }

            for (var i = 0; i < listInfo.Count; i++)
            {
                CreateViewInfo info = listInfo[i];
                if (!File.Exists(info.classPath))
                {
                    Debug.LogWarning($"Class not generated: {info.classPath}");
                    continue;
                }

                var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(info.classPath);
                if (monoScript == null)
                {
                    Debug.LogWarning("Mono script is null!");
                    continue;
                }

                var classType = monoScript.GetClass();
                if (classType == null)
                {
                    Debug.LogWarning($"MonoScript is not Component?\n{info.classPath}");
                    continue;
                }
            
                Component c = info.trans.gameObject.GetComponent(classType);
                if (c != null)
                {
                    Debug.LogWarning($"MonoScript attached!\n{info.classPath}");
                    continue;
                }

                c = info.trans.gameObject.AddComponent(classType);
                if (c == null)
                {
                    Debug.LogWarning($"Invalid component: {monoScript.GetClass()}");
                    continue;
                }
            }
            
            state = CreateState.AttachedScript;
            EditorUtility.SetDirty(this);
        }

        public void SavePrefab()
        {
            if (state != CreateState.AttachedScript)
            {
                Debug.LogWarning($"Invalid state: {state}");
                return;
            }

            for (var i = 0; i < listInfo.Count; i++)
            {
                CreateViewInfo info = listInfo[i];
                PrefabUtility.SaveAsPrefabAssetAndConnect(info.trans.gameObject, info.prefabPath,
                    InteractionMode.AutomatedAction);
            }
            
            state = CreateState.SavedPrefab;
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }    
    #endif
}

