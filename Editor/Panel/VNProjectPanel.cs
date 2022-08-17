using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using UnityEditor.SceneManagement;

namespace vn.corelib
{
	public class VNProjectPanel : EditorWindow
	{
		private static VNProjectPanel _window;
		static Texture2D iconScene;
		static Texture2D iconPlay;
		static Texture2D iconFolder;
		static GUIStyle productNameStyle;
		static string projectPath;

		private static int activeSceneIndex;


		[MenuItem("Window/Extra/Project ^P")]
		private static void ShowWindow()
		{
			if (_window != null) return;

			_window = CreateInstance<VNProjectPanel>();
			_window.titleContent = new GUIContent("Project");
			_window.Show();
		}

		void Init()
		{
			iconScene = AssetPreview.GetMiniTypeThumbnail(typeof(SceneAsset));
			iconPlay = EditorGUIUtility.FindTexture("PlayButton");
			iconFolder = EditorGUIUtility.FindTexture("Folder Icon");


			productNameStyle = new GUIStyle(EditorStyles.largeLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				fontSize = 32
			};

			projectPath = Application.dataPath;
			List<string> arr = projectPath.Split('/').ToList();
			arr.RemoveAt(arr.Count - 1);

			if (arr.Count > 3)
			{
				arr.RemoveRange(0, arr.Count - 3);
			}

			projectPath = string.Join("/", arr);
		}

		static void DrawBigPlayButton()
		{
			EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
			if (scenes.Length == 0)
			{
				EditorGUILayout.HelpBox("No scene in Build Setting!", MessageType.Warning);
				return;
			}
			
			EditorBuildSettingsScene scene = scenes[activeSceneIndex];

			GUILayout.BeginHorizontal();
			{
				EditorGUILayout.Space();
				GUILayout.BeginVertical();
				{
					GUILayout.Space(16);
					GUILayout.Label(scene.path);
					activeSceneIndex = EditorGUILayout.IntSlider(activeSceneIndex, 0, scenes.Length - 1);
				}
				GUILayout.EndVertical();
				if (GUILayout.Button(iconPlay, GUILayout.Width(64f), GUILayout.Height(64f)))
				{
					EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
					EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
					EditorApplication.isPlaying = true;
				}
			}
			GUILayout.EndHorizontal();
		}
		
		static void DrawProjectInfo()
		{
			GUILayout.Label(PlayerSettings.productName, productNameStyle);

			var projectPathContent = new GUIContent(projectPath, iconFolder, "click to open folder");

			if (GUILayout.Button(projectPathContent, EditorStyles.toolbarButton))
			{
				EditorUtility.RevealInFinder(Application.dataPath);
			}
		}

		static void DrawListScenes()
		{
			EditorBuildSettingsScene[] listScenes = EditorBuildSettings.scenes;
			var n = Mathf.Min(listScenes.Length, listScenes.Length);

			GUILayout.BeginVertical();
			{
				for (var i = 0; i < n; i++)
				{
					EditorBuildSettingsScene scene = listScenes[i];
					if (!scene.enabled) continue;

					GUILayout.BeginHorizontal();
					{
						var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);

						if (GUILayout.Button(iconScene, EditorStyles.toolbarButton, GUILayout.Width(25f)))
						{
							EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
							EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
						}

						EditorGUILayout.ObjectField(asset, typeof(SceneAsset), false);

						if (GUILayout.Button(iconPlay, EditorStyles.toolbarButton, GUILayout.Width(25f)))
						{
							EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
							EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
							EditorApplication.isPlaying = true;
						}
					}
					GUILayout.EndHorizontal();
				}
			}
			GUILayout.EndVertical();
		}


		public void OnGUI()
		{
			if (productNameStyle == null) Init();

			DrawProjectInfo();
			EditorGUILayout.Space();
			DrawBigPlayButton();
			EditorGUILayout.Space();
			DrawListScenes();
		}
	}
}