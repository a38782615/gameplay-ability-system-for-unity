using ToolbarExtension;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    internal sealed class SceneToolBar
    {
        private static readonly GUILayoutOption s_GUILayoutOption = GUILayout.Width(20);

        private static string[] s_SceneNames;
        private static string[] s_SceneGuids;

        [Toolbar(OnGUISide.Left, -999)]
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

            var s_SelectedSceneIndex = -1;
            s_SelectedSceneIndex = EditorGUILayout.Popup(s_SelectedSceneIndex, s_SceneNames, s_GUILayoutOption);
            if (s_SelectedSceneIndex >= 0)
            {
                //当前场景没有保存提示
                if (SceneManager.GetActiveScene().isDirty)
                {
                    if (EditorUtility.DisplayDialog("Scene", "The scene is not saved, do you want to save it?", "Save", "Cancel"))
                    {
                        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                    }
                }
                EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(s_SceneGuids[s_SelectedSceneIndex]));
                s_SceneNames = null;
            }
        }
    }
}