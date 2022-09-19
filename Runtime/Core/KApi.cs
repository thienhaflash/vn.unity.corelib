using System;
using System.Collections.Generic;
using UnityEngine;
using CMD = System.Func<System.Collections.Generic.Dictionary<string, object>, object>;

public static class KApi
{
    private static readonly Dictionary<string, CMD> _map = new();
    public static bool enableLog = true;

    private static CMD GetCommand(string commandId)
    {
        if (_map.TryGetValue(commandId, out var cmd))
        {
            if (cmd != null) return cmd;
        }
		
        LogWarning($"KApi command not found <{commandId}>");
        return null;
    }
	
    private static object ExecuteInternal(string commandId, Dictionary<string, object> data)
    {
        var cmd = GetCommand(commandId);
		
#if UNITY_EDITOR
        return cmd?.Invoke(data);
#else
		try
        {
            return cmd?.Invoke(data);
        }
        catch (Exception e)
        {
            LogWarning($"Exception when executing command {commandId} \n {e}");
        }
		return null;
#endif
    }
	
    public static T ExecuteT<T>(string id, params object[] keyValuePairs)
    {
        var dict = new Dictionary<string, object>();
        for (var i = 0; i < keyValuePairs.Length; i += 2)
        {
            dict.Add((string)keyValuePairs[i], keyValuePairs[i + 1]);
        }
        return (T)ExecuteInternal(id, dict);
    }
	
    public static void Execute(string id, params object[] keyValuePairs)
    {
        var dict = new Dictionary<string, object>();
        for (var i = 0; i < keyValuePairs.Length; i += 2)
        {
            dict.Add((string)keyValuePairs[i], keyValuePairs[i + 1]);
        }

        ExecuteInternal(id, dict);
    }

    public static void RegisterCommand(string commandId, CMD cmd, bool rewrite = false)
    {
        if (cmd == null)
        {
            LogWarning($"Can not register a null cmd, commandId = {commandId}");
            return;
        }
		
        if (_map.ContainsKey(commandId))
        {
            if (rewrite == false)
            {
                LogWarning($"CommandId {commandId} registered before!");
            }
            else
            {
                _map[commandId] = cmd;
            }
			
            return;
        }
		
        _map.Add(commandId, cmd);
    }

    public static void RemoveCommand(string commandId)
    {
        if (_map.ContainsKey(commandId))
        {
            _map.Remove(commandId);
        }
    }

    private static void LogWarning(string message)
    {
        if (enableLog == false) return;
        Debug.LogWarning(message);
    }
}
