using System;
#if UNITY_EDITOR
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
#endif

namespace vn.corelib
{
    public enum ButtonMode
    {
        AlwaysEnabled,
        EnabledInPlayMode,
        DisabledInPlayMode
    }

    [Flags]
    public enum ButtonSpacing
    {
        None = 0,
        Before = 1,
        After = 2
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ButtonAttribute : Attribute
    {
        public readonly string name;
        public readonly ButtonMode mode = ButtonMode.AlwaysEnabled;
        public readonly ButtonSpacing spacing = ButtonSpacing.None;

        public ButtonAttribute()
        {
        }
        
        public ButtonAttribute(string name)
        {
            this.name = name;
        }

        public ButtonAttribute(ButtonMode mode)
        {
            this.mode = mode;
        }

        public ButtonAttribute(ButtonSpacing spacing)
        {
            this.spacing = spacing;
        }

        public ButtonAttribute(string name, ButtonMode mode)
        {
            this.name = name;
            this.mode = mode;
        }

        public ButtonAttribute(string name, ButtonSpacing spacing)
        {
            this.name = name;
            this.spacing = spacing;
        }

        public ButtonAttribute(string name, ButtonMode mode, ButtonSpacing spacing)
        {
            this.name = name;
            this.mode = mode;
            this.spacing = spacing;
        }
    }


#if UNITY_EDITOR
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Object), true)]
    public class ObjectEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawEasyButtons(this);
            DrawDefaultInspector();
        }

        private static void DrawEasyButtons(Editor editor)
        {
            GUILayout.BeginHorizontal();
            var methods = editor.target.GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.GetParameters().Length == 0);
            foreach (var method in methods)
            {
                var ba = (ButtonAttribute) Attribute.GetCustomAttribute(method, typeof(ButtonAttribute));

                if (ba == null) continue;

                var wasEnabled = GUI.enabled;
                GUI.enabled = ba.mode == ButtonMode.AlwaysEnabled
                              || (EditorApplication.isPlaying
                                  ? ba.mode == ButtonMode.EnabledInPlayMode
                                  : ba.mode == ButtonMode.DisabledInPlayMode);


                if (((int) ba.spacing & (int) ButtonSpacing.Before) != 0) GUILayout.Space(10);

                var buttonName = string.IsNullOrEmpty(ba.name) ? ObjectNames.NicifyVariableName(method.Name) : ba.name;
                if (GUILayout.Button(buttonName))
                {
                    foreach (var t in editor.targets)
                    {
                        method.Invoke(t, null);
                    }
                }

                if (((int) ba.spacing & (int) ButtonSpacing.After) != 0) GUILayout.Space(10);
                GUI.enabled = wasEnabled;
            }
            
            GUILayout.EndHorizontal();
        }
    }
}
#endif