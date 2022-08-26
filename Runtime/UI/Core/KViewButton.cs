using System;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class KViewButton : MonoBehaviour
{
    public string viewId;
    public string layerId = "MAIN";
    
    private void Reset()
    {
        var btn = GetComponent<Button>();
        if (btn == null) return;

        var nEvents = btn.onClick.GetPersistentEventCount();
        for (var i = 0; i < nEvents; i++)
        {
            var evtTarget = btn.onClick.GetPersistentTarget(i);
            if (evtTarget == this) return; // Existed
        }
        
        var action = new UnityAction(this.OnClick);
        UnityEventTools.AddVoidPersistentListener(btn.onClick, action);
    }
    
    void OnClick()
    {
        Debug.LogWarning("Clicked!");
    }
}
