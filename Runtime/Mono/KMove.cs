using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KMove : MonoBehaviour
{
    public RectTransform target;
    public Vector2 direction;
    public float speed;
    
    private Vector2 origin;
    private void Awake()
    {
        origin = target.anchoredPosition;
    }
    
    void Update()
    {
        target.anchoredPosition = origin + Mathf.Abs(Mathf.Sin(Time.time * speed)) * direction;
    }
}
