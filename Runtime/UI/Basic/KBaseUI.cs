using UnityEngine;

namespace vn.corelib
{
    public abstract class KBaseUI : MonoBehaviour
    {
        protected string _state;
        public string state
        {
            get => _state;
            set
            {
                if (_state == value) return;
                _state = value;
                Refresh();
            }
        }
        
        public virtual void Refresh() { }
    }
}