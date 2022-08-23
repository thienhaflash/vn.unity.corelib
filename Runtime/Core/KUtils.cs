using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace vn.corelib
{
    public static class KUtils
    {
        public static void UIGoto(string deeplinkOrViewId, object viewData = null)
        {
            if (UIViewManager.defaultInst == null)
            {
                Debug.LogWarning("UIViewManager's default instance not found!");
                return;
            }

            UIViewManager.defaultInst.Goto(deeplinkOrViewId, viewData);
        }
        
        public static void UIShowPopup(string popupId, object viewData = null)
        {
            if (UIViewManager.defaultInst == null)
            {
                Debug.LogWarning("UIViewManager's default instance not found!");
                return;
            }

            UIViewManager.defaultInst.Show("POPUP", popupId, viewData);
        }

        public static void UIClosePopups()
        {
            if (UIViewManager.defaultInst == null)
            {
                Debug.LogWarning("UIViewManager's default instance not found!");
                return;
            }

            UIViewManager.defaultInst.HideLayer("POPUP");
        }
        
        
        public static void SetupButtons(params object[] buttonActionList)
        {
            for (var i = 0; i < buttonActionList.Length; i += 2)
            {
                var btn = buttonActionList[i] as Button;
                if (btn == null) continue;

                var action = buttonActionList[i + 1] as UnityAction;
                if (action == null) continue;

                btn.onClick.RemoveListener(action);
                btn.onClick.AddListener(action);
            }
        }

    }
}