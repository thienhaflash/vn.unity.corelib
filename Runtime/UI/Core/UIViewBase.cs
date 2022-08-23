using System;
using UnityEngine;

namespace vn.corelib
{
    public abstract class UIViewBase : MonoBehaviour
    {
        public GameObject viewPrefab;
        [NonSerialized] public UIViewManager.ViewContext context;
		
        public void Init()
        {
            if (viewPrefab == null) return;
            Instantiate(viewPrefab, transform, false);
        }

        public void Hide()
        {
            if (context == null)
            {
                Debug.LogWarning("Context is null - don't know how to hide!");
                return;
            }
			
            context.Hide();
        }
    }
}