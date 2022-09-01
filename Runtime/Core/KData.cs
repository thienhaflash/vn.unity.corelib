using System;
using System.Collections.Generic;
using UnityEngine;

public static class KData
{
    public static Dictionary<K, V> BuildMap<K, V, TList>(Dictionary<K, V> map, List<TList> list, Func<TList, int, (K, V)> func)
    {
        map ??= new Dictionary<K, V>();
        map.Clear();
        
        if (list == null || list.Count == 0) return map;
		    
        for (var i = 0; i < list.Count; i++)
        {
            TList item = list[i];
            if (item == null) continue;

            (K key, V value) = func(item, i);
            if (key == null)
            {
                Debug.LogWarning($"Key should not be null!");
                continue;
            }

            if (map.ContainsKey(key))
            {
                Debug.LogWarning($"Duplicated key found <{key}!");
                continue;
            }
            map.Add(key, value);
        }

        return map;
    }
    public static Dictionary<K, V> BuildMap<K, V, TList>(this List<TList> list, Func<TList, (K, V)> func, Dictionary<K, V> result = null)
    {
        return BuildMap(result, list, (item, _) => func(item));
    }
    public static Dictionary<K, V> BuildMap<K, V>(this List<V> list, Func<V, K> func, Dictionary<K, V> result = null)
    {
        return BuildMap(result, list, (item, _)=> (func(item), item));
    }
    
    public static void Compact<T>(List<T> list)
    {
        // Remove all null items
        var idx = -1;
        var count = list.Count;
        for (var i = 0; i < count; i++) //shift items up in O(N) fashion
        {
            var isNull = list[i] == null;
            if (isNull) continue; // skip null items
            idx++;
            if (idx == i) continue; // did not found any null since start
            list[idx] = list[i]; // there were some null found, and now we need to migrate data items to the left
        }
				
        list.RemoveRange(idx+1, count-1-idx);
    }

    public static void Shuffle<T>(List<T> list)
    {
        throw new NotImplementedException();
    }

    public static void Resize<T>(List<T> list)
    {
        throw new NotImplementedException();
    }
}
