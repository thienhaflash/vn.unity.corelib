using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class UIStar : MonoBehaviour
{
    public Sprite on;
    public Sprite off;
    public Color onColor = new Color(1,1,1,1);
    public Color offColor = new Color(0.5f,0.5f,0.5f,0.5f);
    public List<Image> images = new List<Image>();
    
    public void SetStar(int value)
    {
        for (var i = 0; i < images.Count; i++)
        {
            Image image = images[i];
            image.sprite = (i >= value) ? off : on;
            image.color = (i >= value) ? offColor : onColor;
        }
    }
    
#if UNITY_EDITOR
    [Range(0, 10)] [SerializeField] private int stars;
    private int _lastStars;
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (string.IsNullOrEmpty(gameObject.scene.name)) return; // prefabMode

        if (images.Count == 0 && transform.childCount > 0)
        {
            images = transform.GetComponentsInChildren<Image>().ToList();
        }
        
        if (_lastStars == stars) return;
        EditorApplication.update -= DelayRefresh;
        EditorApplication.update += DelayRefresh;
    }
		
    void DelayRefresh()
    {
        EditorApplication.update -= DelayRefresh;
        SetStar(stars);
        _lastStars = stars;
    }
		
#endif
}
