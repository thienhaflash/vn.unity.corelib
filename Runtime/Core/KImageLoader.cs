using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace vn.corelib
{
	[Serializable] internal class ImageCatalogEntry : KCatalogEntry
	{
		[NonSerialized] public Texture2D texture;
		public string url
		{
			get => hash;
			set => hash = value;
		}
	}
	
	[Serializable] internal class ImageCatalog : KFileCatalogT<ImageCatalogEntry>
	{
		internal ImageCatalog() : base("image_catalog.json") {}
		
		private ImageCatalogEntry FindImageCache(string url)
		{
			if (!_loaded) Load();
			return _entries.FirstOrDefault(t => t.url == url);
		}
		
		public void ReleaseRAM()
		{
			if (!_loaded) return;

			// Remove reference to textures
			for (var i = 0; i < _entries.Count; i++)
			{
				_entries[i].texture = null;
			}
		}
		public bool Add2Cache(Texture2D tex, string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				Debug.LogWarning("url should not be null or empty!");
				return false;
			}

			if (tex == null)
			{
				Debug.LogWarning("tex should not be null or empty!");
				return false;
			}

			ImageCatalogEntry cache = FindImageCache(url);
			if (cache != null) return false;

			var hash = new Hash128();
			hash.Append(url);
			hash.Append(tex.name);

			var fileName = $"{hash.ToString()}.png";
			if (!KFileIO.SaveImage(fileName, tex)) return false;
			return Add(new ImageCatalogEntry {localPath = fileName, texture = tex, url = url});
		}
		public Texture2D LoadFromDisk(string url)
		{
			ImageCatalogEntry cache = FindImageCache(url);
			if (cache == null) return null;
			if (cache.texture != null) return cache.texture;

			Texture2D result = KFileIO.LoadImage(cache.localPath);
			if (result == null) // Actual file deleted : remove from cache as well
			{
				Remove(cache.hash);
				return null;
			}
			
			// save for next time
			cache.texture = result;
			return result;
		}
	}
	
	public static class KImageLoader
	{
		private static readonly ImageCatalog _catalog = new ImageCatalog();
		private static readonly Dictionary<string, Texture2D> _loadedMap = new Dictionary<string, Texture2D>();
		private static readonly Dictionary<string, LoaderItem> _loadingMap = new Dictionary<string, LoaderItem>();
		
		public class LoaderItem
		{
			public string url;
			public Action<Texture2D> onComplete;
		}
		
		public static void Load(string url, Action<Texture2D> onComplete = null)
		{
			if (string.IsNullOrEmpty(url))
			{
				Debug.Log("Can not load a null url!");
				return;
			}
#if VERBOSE_LOG
        Debug.Log($"Load: {url}");
#endif

			if (_loadedMap.TryGetValue(url, out Texture2D result))
			{
				onComplete?.Invoke(result);
				return;
			}

			if (_loadingMap.TryGetValue(url, out LoaderItem ldi))
			{
				if (onComplete == null) return;
				ldi.onComplete -= onComplete;
				ldi.onComplete += onComplete;
				return;
			}

#if VERBOSE_LOG
        Debug.Log($"CheckLocal: {url}");
#endif

			// Check Local
			Texture2D tex = _catalog.LoadFromDisk(url);
			if (tex != null)
			{
				_loadedMap.Add(url, tex);
				onComplete?.Invoke(tex);
				return;
			}

			// Load from Web
			KSystem.StartRoutine(LoadImageRoutine(new LoaderItem
			{
				url = url,
				onComplete = onComplete
			}));
		}

		static IEnumerator LoadImageRoutine(LoaderItem item)
		{
#if VERBOSE_LOG
        Debug.Log($"Start load: {item.url}");
#endif

			_loadingMap.Add(item.url, item);
			UnityWebRequest request = UnityWebRequestTexture.GetTexture(item.url);
			request.SendWebRequest();

			while (!request.isDone)
			{
#if VERBOSE_LOG
            Debug.Log($"loading : {request.downloadedBytes} bytes\n{item.url}");
#endif

				yield return new WaitForSeconds(1f);
			}

			_loadingMap.Remove(item.url);

			if (request.result != UnityWebRequest.Result.Success) // failed
			{
				Debug.LogWarning($"LoadImageRoutine error: {request.error}\n{item.url}");
				yield break;
			}

#if VERBOSE_LOG
        Debug.Log($"Load complete: {item.url}");
#endif


			Texture2D tex = ((DownloadHandlerTexture) request.downloadHandler).texture;
			_loadedMap.Add(item.url, tex);
			_catalog.Add2Cache(tex, item.url);
			item.onComplete?.Invoke(tex);

#if VERBOSE_LOG
        Debug.Log($"End load: {item.url}");
#endif
		}
	}
}