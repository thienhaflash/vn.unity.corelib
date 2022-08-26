using UnityEngine;

namespace vn.corelib
{
    public abstract class KViewBase : MonoBehaviour, IKViewInit, IKViewTransition
    {
        protected KViewContext _context;

        public virtual void Init(KViewContext context)
        {
            _context = context;
        }

        public virtual void OnBeforeShow()
        {
        }

        public virtual void OnAfterShow()
        {
        }

        public virtual void OnBeforeHide()
        {
        }

        public virtual void OnAfterHide()
        {
        }
    }
}