using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace vn.corelib
{
    public static class KFileIO
    {
        private static readonly string _dataPath;
        private static readonly string _cachePath;

        static KFileIO()
        {
#if UNITY_EDITOR
            {
                var library = Application.dataPath.Replace("/Assets", "/Library");
                _dataPath = Path.Combine(library, "KFileIO/Data");
                _cachePath = Path.Combine(library, "KFileIO/Cache");
            }
#else
            {
                _dataPath = Path.Combine(Application.persistentDataPath, "KFileIO");
                _cachePath = Path.Combine(Application.temporaryCachePath, "KFileIO");
            }
#endif
            if (Application.isPlaying)
            {
                Debug.Log($"KFileIO init!\ndataPath\t: {_dataPath}\ncachePath \t: {_cachePath}");    
            }
        }
        
        public static string dataPath => _dataPath;
        public static string cachePath => _cachePath;
        private static string GetPath(string relativePath, bool isTemp, bool createPath)
        {
            var path = Path.Combine((isTemp ? _cachePath : _dataPath), relativePath);
            if (!createPath) return path;
            
            var isFile = relativePath.Contains(".");
            var folder =  isFile ? Directory.GetParent(path)?.FullName : path;
            if (folder == null) return path;
            if (Directory.Exists(folder)) return path;
            
            try
            {
                Directory.CreateDirectory(folder);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception: {e}\n{folder}");
            }
            
            return path;
        }
        public static string GetTempPath(string relativePath, bool createPath = false)
        {
            return GetPath(relativePath, true, createPath);
        }
        public static string GetPersistentPath(string relativePath, bool createPath = true)
        {
            return GetPath(relativePath, true, createPath);
        }

        public static bool DeleteFile(string relativePath, bool inTemp = false)
        {
            var path = GetPath(relativePath, inTemp, false);
            if (!File.Exists(path)) return true;
            
            try
            {
                File.Delete(path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"DeleteFile error: {e}\n{relativePath}");
            }
            
            return false;
        }
        public static bool EmptyFolder(string relativePath, bool inTemp = false)
        {
            var path = GetPath(relativePath, inTemp, false);
            if (!Directory.Exists(path)) return true;
            
            try
            {
                Directory.Delete(path, true);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"EmptyFolder error: {e}\n{relativePath}");
            }
            return false;
        }
        
        
        #if UNITY_EDITOR
        [MenuItem("Tools/Local FileIO/Empty data & cache")]
        public static void DeleteAll()
        {
            if (Application.isPlaying)
            {
                Debug.LogWarning("Empty data & cache will not run at runtime to prevent accidentally data deletion!");
                return;
            }
            
            var sb = new StringBuilder();
            sb.AppendLine("Empty data & cache");
            var success1 = EmptyFolder(_cachePath);
            var success2 = EmptyFolder(_dataPath);
            sb.AppendLine($"[{(success1 ? "Deleted" : "Failed to delete")}] cache at: {_cachePath}");
            sb.AppendLine($"[{(success2 ? "Deleted" : "Failed to delete")}] data at: {_dataPath}");
            Debug.Log(sb.ToString());
        }
        #endif
        
        // RW SUPPORT FOR TEXT
        public static string ReadText(string fileName, bool inTemp = false)
        {
            var path = GetPath(fileName, inTemp, true);
            
            try
            {
                return File.Exists(path) ? File.ReadAllText(path) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ReadText error: {e}\n{fileName}\n{path}");
            }
            return null;
        }
        public static bool WriteText(string fileName, string content, bool inTemp = false)
        {
            var path = GetPath(fileName, inTemp, true);
            try
            {
                File.WriteAllText(path, content);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"WriteText error: {e}\n{fileName}\n{path}");   
            }
            
            return false;
        }
        
        // RW SUPPORT FOR BYTES
        public static byte[] ReadBytes(string fileName, bool inTemp = false)
        {
            var path = GetPath(fileName, inTemp, true);
            
            try
            {
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"ReadText error: {e}\n{fileName}\n{path}");
            }
            return null;
        }
        public static bool WriteBytes(string fileName, byte[] bytes, bool inTemp = false)
        {
            var path = GetPath(fileName, inTemp, true);
            try
            {
                File.WriteAllBytes(path, bytes);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"WriteBytes error: {e}\n{fileName}\n{path}");   
            }
            
            return false;
        }
        
        // RW SUPPORT FOR IMAGES
        public static bool SaveImage(string fileName, Texture2D tex, bool inTemp = false)
        {
            var ext = Path.GetExtension(fileName).ToLower();
            byte[] bytes = null;
            switch (ext)
            {
                case ".png":
                    bytes = tex.EncodeToPNG();
                    break;
                case ".jpg":
                    bytes = tex.EncodeToJPG();
                    break;
                default:
                {
                    Debug.LogWarning($"Unsupported save texture to {ext}!");
                    return false;
                }
            }
            
            return bytes != null && WriteBytes(fileName, bytes, inTemp);
        }
        public static Texture2D LoadImage(string fileName, bool inTemp = false, bool markNonReadable = false)
        {
            var bytes = ReadBytes(fileName, inTemp);
            if (bytes == null || bytes.Length == 0) return null;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes, markNonReadable);
            return tex;
        }
    }
}