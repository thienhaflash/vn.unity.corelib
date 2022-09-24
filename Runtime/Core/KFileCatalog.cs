using System;
using System.Collections.Generic;
using UnityEngine;

namespace vn.corelib
{
    [Serializable] public class KCatalogEntry
    {
        public string hash;
        public string localPath;
    }

    [Serializable] public class KFileCatalog : KFileCatalogT<KCatalogEntry>
    {
        public KFileCatalog(string catalogFileName, bool doLoad = false): base(catalogFileName, doLoad) {}

        public bool Add(string hash, string localPath)
        {
            return Add(new KCatalogEntry() {hash = hash, localPath = localPath});
        }
    }

    [Serializable] public class KFileCatalogT<TEntry> where TEntry: KCatalogEntry
    {
        [SerializeField] private List<TEntry> _entries = new List<TEntry>();
        [NonSerialized] private bool _loaded = false;
        [NonSerialized] private readonly string _catalogFileName;
        [NonSerialized] private readonly Dictionary<string, TEntry> _map = new Dictionary<string, TEntry>();

        public KFileCatalogT(string catalogFileName, bool doLoad = false)
        {
            _catalogFileName = catalogFileName;
            if (doLoad) Load();
        }
        
        protected void Load()
        {
            if (_loaded)
            {
                Debug.LogWarning("Load() called before!");
                return;
            }
			
            _loaded = true;
			
            var json = KFileIO.ReadText(_catalogFileName);
            if (string.IsNullOrEmpty(json)) return;
            JsonUtility.FromJsonOverwrite(json, this);
            RebuildMap();
        }
        
        protected void RebuildMap()
        {
            _map.Clear();
            for (var i = 0; i < _entries.Count; i++)
            {
                TEntry e = _entries[i];
                _map.Add(e.hash, e);
            }
        }
        
        protected void Save()
        {
            if (!_loaded)
            {
                Debug.LogWarning("Something wrong! Can not save without read first");
                return;
            }
			
            var json = JsonUtility.ToJson(this);
            KFileIO.WriteText(_catalogFileName, json);
        }

        public string GetLocalPath(string hash)
        {
            return Get(hash)?.localPath;
        }
        
        public TEntry Get(string hash)
        {
            if (!_loaded) Load();
            return _map.TryGetValue(hash, out var result) ? result : null;
        }
        
        public bool Add(TEntry entry)
        {
            if (string.IsNullOrWhiteSpace(entry.hash))
            {
                Debug.LogWarning($"Invalid entry: {entry} (hash == null or empty)");
                return false;
            }
            
            if (!_loaded) Load();
            
            if (_map.ContainsKey(entry.hash))
            {
                Debug.LogWarning($"Hash existed: {entry.hash}");
                return false;
            }
            
            _map.Add(entry.hash, entry);
            _entries.Add(entry);
            KAsync.DelayCall(Save);
            return true;
        }
        
        public bool Remove(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                Debug.LogWarning($"Invalid hash!");
                return false;
            }
            
            if (!_loaded) Load();
            if (!_map.TryGetValue(hash, out TEntry result)) return false;
            
            _map.Remove(hash);
            for (var i = _entries.Count - 1; i >= 0; i--)
            {
                if (_entries[i].hash == hash)
                {
                    _entries.RemoveAt(i);
                    break;
                }
            }
                
            KAsync.DelayCall(Save);
            return true;
        }
    }    
}

