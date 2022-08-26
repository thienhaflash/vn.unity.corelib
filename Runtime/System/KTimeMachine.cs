using System;
using System.Collections.Generic;
using UnityEngine;

namespace vn.corelib
{
    public class KTimeMachine
    {
        [Serializable] public class TimeNode
        {
            public float triggerTime = 0;
            
            public virtual void Trigger()
            {
                
            }
        }
        
        [Serializable] public class TimeNodeAction : TimeNode
        {
            public Action action;

            public TimeNodeAction(float triggerTime, Action action)
            {
                this.triggerTime = triggerTime;
                this.action = action;
            }
            
            public override void Trigger()
            {
                action?.Invoke();
            }
        }
        
        private bool _dirty;
        private bool _isPlaying;
        private float _currentTime;
        private int _checkingIndex;
        private readonly List<TimeNode> _listNotes = new ();
        
        public float currentTime => _currentTime;
        
        public void Play()
        {
            if (_isPlaying) return;
            _isPlaying = true;
            KUpdate.OnUpdate(OnTimeUpdate);
        }
        
        public void Pause()
        {
            if (!_isPlaying) return; 
            
            KUpdate.RemoveUpdate(this.OnTimeUpdate);
            _isPlaying = false;
        }
        
        public void Stop()
        {
            Pause();
            
            _checkingIndex = 0;
            _currentTime = 0;
            _dirty = false;
        }
        
        public void StopAndClear()
        {
            Stop();
            _listNotes.Clear();
        }
        
        public void AddTimeNode(TimeNode node)
        {
            if (_isPlaying && node.triggerTime <= _currentTime)
            {
                Debug.LogWarning("Adding a node into the past (triggerTime < _currentTime) - it might never be triggered!");
            }
            
            _listNotes.Add(node);
            _dirty = true;
        }
        
        private void OnTimeUpdate()
        {
            var maxIndex = _listNotes.Count - 1;
            if (_dirty) Sort();
            if (maxIndex <= 0) return;

            _currentTime += Time.deltaTime;
            
            TimeNode cNode = _listNotes[_checkingIndex];
            while (cNode.triggerTime <= _currentTime)
            {
                cNode.Trigger();
                if (_checkingIndex >= maxIndex) break;
                _checkingIndex++;
                cNode = _listNotes[_checkingIndex];
            }
        }
        
        private void Sort()
        {
            if (!_dirty) return;
            _dirty = false;
            _listNotes.Sort((x, y) => x.triggerTime.CompareTo(y.triggerTime));
        }
        
        public void Seek(float time)
        {
            var maxIndex = _listNotes.Count - 1;
            if (_dirty) Sort();
            if (maxIndex <= 0) return;
            
            // seek backward
            TimeNode node = _listNotes[_checkingIndex];
            while (node.triggerTime > time && _checkingIndex > 0)
            {
                _checkingIndex--;
                node = _listNotes[_checkingIndex];
                
                if (node.triggerTime <= time)
                {
                    _checkingIndex++;
                    _currentTime = time;
                    return;
                }
            }
            
            // Seek forward
            while (node.triggerTime <= time && _checkingIndex <= maxIndex)
            {
                node = _listNotes[++_checkingIndex];
                if (node.triggerTime > time)
                {
                    _currentTime = time;
                    return;
                }
                _checkingIndex++;
            }
        }
    }
}

