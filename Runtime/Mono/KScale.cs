using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using vn.corelib;

public class KScale : MonoBehaviour
{
    public float maxScale = 2;
    public float minScale = 1;
    public Transform target;

    private float currentScale = 0;
    [Range(1f, 10f)] public float scaleDownSpeed;

    [Button] public void Play()
    {
        currentScale = maxScale;
        target.transform.localScale = Vector3.one * maxScale;
        KUpdate.OnUpdate(ScaleDown);
    }
    
    void ScaleDown()
    {
        currentScale -= Time.deltaTime * scaleDownSpeed;
        if (currentScale <= minScale)
        {
            currentScale = minScale;
            target.localScale = Vector3.one * minScale;
            KUpdate.RemoveUpdate(ScaleDown);
        }
        
        target.localScale = Vector3.one * currentScale;
    }
}
