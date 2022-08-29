using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace vn.corelib
{
	public class KType
	{
		public static void IgnoreAssemblies(params string[] assemblyNames)
		{
			foreach (var fullName in assemblyNames)
			{
				_ignoredAssemblies.Add(fullName);
			}
		}
		
		[NonSerialized] private static readonly Dictionary<string, Type> _cacheType = new ();
		[NonSerialized] private static readonly HashSet<string> _ignoredAssemblies = new ();
		[NonSerialized] private static bool _scanned = false;
		public static void RegisterCacheType(params Type[] types)
		{
			foreach (Type type in types)
			{
				var fullName = type.FullName;
				if (string.IsNullOrEmpty(fullName)) continue;
				if (_cacheType.ContainsKey(fullName))
				{
					Debug.LogWarning($"Duplicated Tpye FullName found: {fullName}");
					continue;
				}
				_cacheType.Add(fullName, type);
			}
		}
		
		public static Type GetTypeByName(string fullName)
		{
			if (string.IsNullOrEmpty(fullName)) return null;
			if (_cacheType.TryGetValue(fullName, out Type result1)) return result1;
			if (_scanned) return null;
			_scanned = true;
			
			foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (_ignoredAssemblies.Contains(assembly.FullName))
				{
					Debug.LogWarning($"Ignored Assembly: {assembly.FullName}");
					continue;
				}
				
				Debug.LogWarning($"Scanning Assembly: {assembly.FullName}");
				RegisterCacheType(assembly.GetTypes());
			}
			
			_cacheType.TryGetValue(fullName, out Type result);

#if UNITY_EDITOR
			{
				if (Application.isPlaying)
				{
					KEditor.LogWarning($"[Editor] Please add {fullName} to _cacheType to improve performance!\n\nSerializableType._cacheType.Add(\"{fullName}\", typeof({fullName}));\n");
				}
			}
#endif
			return result;
		}
	}
}