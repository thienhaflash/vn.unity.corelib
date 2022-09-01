using System;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace vn.corelib
{
	public interface IKPoolHandler
	{
		void OnGet();
		void OnReturn();
		void OnCreate();
		void OnDestroy();
	}
	
	[Serializable] public class KPoolPolicy
	{
		public bool autoActive = true;
		public bool autoParent = true;
		public Transform parent;
		[Range(0, 100)] public int prewarm = 0;
	}
	
	[Serializable] public class KPoolPrefab
	{
		public KPoolPolicy policy;
		public List<GameObject> prefabs = new ();

		public void Init()
		{
			foreach (GameObject go in prefabs)
			{
				KPool.CreatePrefabPool(go, policy);
			}
		}
		
		public void Destroy()
		{
			foreach (GameObject prefab in prefabs)
			{
				KPool.DestroyPrefabPool(prefab.name);
			}
		}
	}
	
	public partial class KPool // STATIC APIs
	{
		private const float PREWARM_TIME_SLICE = 1 / 90f; 
		
		private static readonly Dictionary<string, PrefabPool> poolMap = new ();
		private static readonly Dictionary<GameObject, PrefabInst> useMap = new ();
		private static readonly Queue<PrefabPool> prewarmQueue = new ();

		public static GameObject Get(string poolId, Transform parent = null)
		{
			return poolMap.TryGetValue(poolId, out PrefabPool prefab) ? prefab.Take(parent).go : null;
		}
		public static T Get<T>(string poolId, Transform parent = null) where T : Component
		{
			return poolMap.TryGetValue(poolId, out PrefabPool prefab) ? prefab.Take(parent).GetComponent<T>() : null;
		}

		public static bool Return(GameObject go)
		{
			if (go == null) return false;
			if (!useMap.TryGetValue(go, out PrefabInst inst)) return false; // something wrong?
			useMap.Remove(go);
			
			if (!poolMap.TryGetValue(inst.poolId, out PrefabPool prefabPool)) return false; // pool destroyed?
			prefabPool.Return(inst);
			return true;
		}
		
		public static bool Return<T>(T component) where T : Component
		{
			return component != null && Return(component.gameObject);
		}
		
		public static void Return<T>(ICollection<T> components) where T : Component
		{
			foreach (T item in components)
			{
				if (item == null) continue;
				Return(item.gameObject);
			}
		}
		
		internal static void CreatePrefabPool(GameObject prefab, KPoolPolicy policy)
		{
			var poolId = prefab.name;
			if (poolMap.ContainsKey(poolId))
			{
				Debug.LogWarning($"PoolId existed: {poolId}");
				return;
			}

			var pool = new PrefabPool(poolId, policy, prefab);
			poolMap.Add(poolId, pool);
			if (policy.prewarm == 0) return;
			prewarmQueue.Enqueue(pool);
			KUpdate.OnUpdate(Prewarm);
		}
		internal static void DestroyPrefabPool(string poolId)
		{
			if (!poolMap.TryGetValue(poolId, out PrefabPool pool)) return;
			
			var listInUse = new List<PrefabInst>();
			foreach (var kvp in useMap)
			{
				if (kvp.Value.poolId != poolId) continue;
				listInUse.Add(kvp.Value);
			}

			foreach (PrefabInst inst in listInUse)
			{
				useMap.Remove(inst.go);
				inst.Destroy();
			}
			
			pool.Empty();
			poolMap.Remove(poolId);
		}
		
		internal static void Prewarm()
		{
			var t1 = Time.realtimeSinceStartup;
			while (prewarmQueue.Count > 0)
			{
				PrefabPool first = prewarmQueue.Peek();
				while (first.instCount < first.policy.prewarm)
				{
					first.Instantiate(true);
					var t2 = Time.realtimeSinceStartup;
					if (t2 - t1 > PREWARM_TIME_SLICE) return;
				}
				
				prewarmQueue.Dequeue();
			}
			
			KUpdate.RemoveUpdate(Prewarm);
		}
		
	}
	public partial class KPool // INTERNAL CLASSES
	{
		internal class PrefabInst
		{
			public readonly string poolId;
			public readonly GameObject go;
			public readonly IKPoolHandler handler;
			public Component cache;
			
			public PrefabInst(string poolId, GameObject go)
			{
				this.poolId = poolId;
				this.go = go;
				handler = go.GetComponent<IKPoolHandler>();
			}

			public T GetComponent<T>() where T : Component
			{
				if (cache != null)
				{
					if (cache is T cachedResult) return cachedResult;
					Debug.LogWarning(
						$"GetComponent with different types will not have cache benefit {typeof(T)} | {cache.GetType()}");
					return go.GetComponent<T>();
				}

				var result = go.GetComponent<T>();
				cache = result;
				return result;
			}

			public void Destroy()
			{
				UnityObject.Destroy(go);
			}
		}
		internal class PrefabPool
		{
			public readonly string poolId;
			public readonly GameObject prefab;
			[NonSerialized] public readonly KPoolPolicy policy;
			[NonSerialized] public readonly Queue<PrefabInst> queue = new();
			internal int instCount;
			
			public PrefabPool(string poolId, KPoolPolicy policy, GameObject prefab)
			{
				this.policy = policy;
				this.poolId = poolId;
				this.prefab = prefab;
			}
		
			public void Instantiate(bool applyPolicy)
			{
				instCount++;
				
				GameObject go = UnityObject.Instantiate(prefab, policy.parent, false);
				go.name = prefab.name;

				queue.Enqueue(new PrefabInst(
					poolId,
					go
				));
				
				if (!applyPolicy) return;
				if (policy.autoParent) go.transform.SetParent(policy.parent, false);
				if (policy.autoActive) go.SetActive(false);
			}

			public void Empty()
			{
				while (queue.Count > 0)
				{
					queue.Dequeue().Destroy();
				}
			}
			
			public PrefabInst Take(Transform newParent)
			{
				if (queue.Count == 0) Instantiate(false);

				PrefabInst result = queue.Dequeue();
				useMap.Add(result.go, result);
				
				if (policy.autoParent) result.go.transform.SetParent(newParent, false);
				if (policy.autoActive) result.go.SetActive(true);
				result.handler?.OnGet();
				return result;
			}

			public void Return(PrefabInst inst)
			{
				if (inst.go == null)
				{
					Debug.LogWarning("Pooled object got destroyed!");
					return;
				}
				
				if (policy.autoParent) inst.go.transform.SetParent(policy.parent);
				if (policy.autoActive) inst.go.SetActive(false);
				inst.handler?.OnReturn();
				queue.Enqueue(inst);
			}
		}
	} 
	

	public partial class KPool : MonoBehaviour
	{
		public List<KPoolPrefab> prefabPool = new ();
		
		private void Start()
		{
			foreach (KPoolPrefab pool in prefabPool)
			{
				pool.Init();
			}
		}
		private void OnDestroy()
		{
			foreach (KPoolPrefab pool in prefabPool)
			{
				pool.Destroy();
			}
		}
		
		// public T GetFromPool<T>(string poolId, Transform parent = null) where T : Component
		// {
		// 	if (!pools.TryGetValue(poolId, out PrefabPool result)) return null;
		// 	PrefabInst inst = result.Take(parent);
		// 	inUse.Add(inst.go, inst);
		// 	return inst.GetComponent<T>();
		// }
		//
		// public GameObject GetFromPool(string poolId, Transform parent = null)
		// {
		// 	if (!pools.TryGetValue(poolId, out PrefabPool result)) return null;
		// 	PrefabInst inst = result.Take(parent);
		// 	inUse.Add(inst.go, inst);
		// 	return inst.go;
		// }
		//
		//
		//
		// public void CreatePool(List<GameObject> prefabs, string policyId = null)
		// {
		// 	CreatePool(GetPolicy(policyId), prefabs);
		// }
		//
		// public void CreatePool(List<KPoolPolicy> listPolicies)
		// {
		// 	for (var i = 0; i < listPolicies.Count; i++)
		// 	{
		// 		KPoolPolicy policy = listPolicies[i];
		// 		if (policy.prefabs.Count == 0) continue;
		// 		CreatePool(policy, policy.prefabs);
		// 	}
		// }
		//
		// public void CreatePool(KPoolPolicy policy, List<GameObject> prefabs)
		// {
		// 	for (var j = 0; j < prefabs.Count; j++)
		// 	{
		// 		GameObject prefab = prefabs[j];
		// 		if (prefab == null)
		// 		{
		// 			Debug.LogWarning($"Null prefab found at <{j}> in policy {policy.id}!");
		// 			continue;
		// 		}
		//
		// 		var id = prefab.name;
		// 		if (pools.TryGetValue(id, out _))
		// 		{
		// 			Debug.LogWarning($"Found duplicated prefab / poolId {id}!");
		// 			continue;
		// 		}
		//
		// 		var pool = new PrefabPool(id, policy, prefab);
		// 		pools.Add(id, pool);
		// 	}
		// }
		//
		// IEnumerator CreatePools()
		// {
		// 	// Prewarm
		// 	var t1 = Time.realtimeSinceStartup;
		// 	foreach (KeyValuePair<string, PrefabPool> kvp in pools)
		// 	{
		// 		PrefabPool pool = kvp.Value;
		// 		KPoolPolicy policy = pool.policy;
		//
		// 		if (policy.prewarm == 0) continue;
		//
		// 		for (var i = 0; i < policy.prewarm; i++)
		// 		{
		// 			pool.Add(true);
		//
		// 			// Prewarm without lags: maintaining 90 FPS
		// 			var t2 = Time.realtimeSinceStartup;
		// 			if (t2 - t1 < 1 / 90f) continue;
		// 			t1 = t2;
		// 			yield return null;
		// 		}
		// 	}
		//
		// 	Debug.LogWarning("Prewarm has completed!");
		// }
	}
}