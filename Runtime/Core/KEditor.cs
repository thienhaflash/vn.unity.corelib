
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using Debug = UnityEngine.Debug;

public class KEditor
{
    private static readonly List<string> _warnings = new List<string>();
    
    [Conditional("UNITY_EDITOR")]
    public static void LogWarning(string message)
    {
        _warnings.Add(message);
        EditorApplication.update -= LogWarning;
        EditorApplication.update += LogWarning;
    }

    private static void LogWarning()
    {
        EditorApplication.update -= LogWarning;
        for (var i =0; i< _warnings.Count; i++)
        {
            Debug.LogWarning(_warnings[i]);
        }
        _warnings.Clear();
    }
}
