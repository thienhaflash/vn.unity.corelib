using System;
using System.Collections;
using UnityEngine;

public class KSystem : MonoBehaviour
{
    public static Coroutine StartRoutine(IEnumerator routine)
    {
        return api.StartCoroutine(routine);
    }

    public static void StopRoutine(Coroutine routine)
    {
        api.StopCoroutine(routine);
    }
        
    public static void StopRoutine(string routineName)
    {
        api.StopCoroutine(routineName);
    }
    
    internal static Action onUpdate;
    internal static Action onLateUpdate;
    internal static Action onFixedUpdate;
    internal static KSystem api;
    
    private void Awake()
    {
        if (api != null && api != this)
        {
            Debug.LogWarning("Multiple KSystem instance exist!");
            Destroy(this);
            return;
        }

        api = this;
        DontDestroyOnLoad(this);
    }

    private void Update() { onUpdate?.Invoke(); }
    private void LateUpdate() { onLateUpdate?.Invoke(); }
    private void FixedUpdate() { onFixedUpdate?.Invoke(); }
}
