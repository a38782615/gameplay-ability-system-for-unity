using System.IO;
using System.Linq;
using ToolbarExtension;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal sealed class LauncherSceneToolBar
    {
        private static string[] s_SceneNames;
        private static string[] s_SceneGuids;
        private static readonly GUILayoutOption s_GUILayoutOption = GUILayout.Width(20);

        [Toolbar(OnGUISide.Left, 100)]
        static void OnToolbarGUI()
        {
            if (s_SceneNames == null)
            {
                var sceneList = AssetDatabase.FindAssets("t:scene").ToList();
                s_SceneGuids = sceneList.ToArray();
                s_SceneNames = new string[s_SceneGuids.Length];
                for (int i = 0; i < s_SceneNames.Length; i++)
                {
                    s_SceneNames[i] = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(s_SceneGuids[i]));
                }
            }

            int s_SelectedSceneIndex = -1;
            s_SelectedSceneIndex = EditorGUILayout.Popup(s_SelectedSceneIndex, s_SceneNames, s_GUILayoutOption);
            if (s_SelectedSceneIndex >= 0)
            {
                var p = AssetDatabase.GUIDToAssetPath(s_SceneGuids[s_SelectedSceneIndex]);
                SceneHelper.StartScene(p);
            }
        }
    }

    internal static class SceneHelper
    {
        private const string UnityEditorSceneToOpenKey = "UnityEditorSceneToOpen";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnBeforeSceneLoad()
        {
            if (EditorPrefs.HasKey(UnityEditorSceneToOpenKey))
            {
                string scenePath = EditorPrefs.GetString(UnityEditorSceneToOpenKey);
                if (!SceneManager.GetActiveScene().path.Equals(scenePath))
                {
                    SceneManager.LoadScene(scenePath);
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            if (EditorPrefs.HasKey(UnityEditorSceneToOpenKey))
            {
                EditorPrefs.DeleteKey(UnityEditorSceneToOpenKey);
            }
        }

        public static void StartScene(string scenePathName)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }
            EditorPrefs.SetString(UnityEditorSceneToOpenKey, scenePathName);
            EditorApplication.isPlaying = true;
        }
    }
}