
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor.Events;
#endif

namespace vn.corelib
{
    public class KViewButton : MonoBehaviour
    {
        public string viewId;
        public string layerId = "MAIN";
        public KView view;
        
        #if UNITY_EDITOR
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
        #endif

        void OnClick()
        {
            KUtils.PlaySFX("click");
            
            if (view != null)
            {
                view.ShowView(viewId, null, layerId);
                return;
            }
            
            KView.Goto(viewId, null, layerId);
        }

        [Button] void FindKView()
        {
            var kView = transform.GetComponentInParent<KView>();
            if (kView == null) return;
            if (kView.useAsDefault) return;
            view = kView;

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }
    }
}