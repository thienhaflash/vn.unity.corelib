
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class KEditor
{
    private static readonly List<string> _warnings = new List<string>();
    
    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(string message)
    {
#if UNITY_EDITOR
        _warnings.Add(message);
        EditorApplication.update -= LogWarning;
        EditorApplication.update += LogWarning;
#endif
        
    }

    private static void LogWarning()
    {
#if UNITY_EDITOR
        EditorApplication.update -= LogWarning;
        for (var i =0; i< _warnings.Count; i++)
        {
            Debug.LogWarning(_warnings[i]);
        }
        _warnings.Clear();
#endif
    }
}
