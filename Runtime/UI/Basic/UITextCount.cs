using System;
using System.Collections;
using System.Reflection;
using UnityEngine;
using vn.corelib;

public class UITextCount : MonoBehaviour
{
    public Component textWrapper;
    
    public void StartCount(int from, int to, float duration = 1f)
    {
        StopCoroutine(nameof(CountRoutine));  
        StartCoroutine(CountRoutine(from, to, duration));
    }

    IEnumerator CountRoutine(int from, int to, float duration)
    {
        var stTime = Time.time;
        while (true)
        {
            var cTime = Time.time;
            var t = duration == 0 ? 1 : Mathf.Min(1, cTime - stTime) / duration;
            var v = (int)Mathf.Lerp(from, to, t);
            SetText(v.ToString());
            if (t >= 1) yield break;
            
            yield return null;
        }
    }
    
    [NonSerialized] private PropertyInfo _property;
    [NonSerialized] private bool _tried = false;
    public void SetText(string v)
    {
        if (_property == null)
        {
            if (_tried) return;
            _tried = true;
            _property = textWrapper.GetType().GetProperty("text");
            if (_property == null) return;
        }
        
        _property?.SetValue(textWrapper, v);
    }
}
