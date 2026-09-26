using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheShadowWood.Editor
{
    /// <summary>
    /// Main toolbar dropdown that opens any project scene without browsing the Project window.
    /// Lists Build Settings scenes first, then every other scene under the project folders.
    /// </summary>
    public static class SceneSwitcherToolbar
    {
        private const string ElementPath = "The Shadow Wood/Scene Switcher";
        private static readonly string[] SearchFolders = { "Assets/_Project/Scenes", "Assets/Scenes" };

        [MainToolbarElement(ElementPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement CreateElement()
        {
            Texture2D icon = EditorGUIUtility.IconContent("SceneAsset Icon").image as Texture2D;
            string activeScene = SceneManager.GetActiveScene().name;
            string label = string.IsNullOrEmpty(activeScene) ? "Untitled" : activeScene;
            MainToolbarContent content = new MainToolbarContent(label, icon, "Switch scene");

            return new MainToolbarDropdown(content, ShowMenu);
        }

        [InitializeOnLoadMethod]
        private static void SubscribeRefresh()
        {
            EditorSceneManager.activeSceneChangedInEditMode += (_, _) => MainToolbar.Refresh(ElementPath);
            EditorApplication.playModeStateChanged += _ => MainToolbar.Refresh(ElementPath);
        }

        private static void ShowMenu(Rect dropdownRect)
        {
            GenericMenu menu = new GenericMenu();
            string activePath = SceneManager.GetActiveScene().path;
            bool canSwitch = !EditorApplication.isPlayingOrWillChangePlaymode;
            HashSet<string> listed = new HashSet<string>();

            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (string.IsNullOrEmpty(buildScene.path) || !File.Exists(buildScene.path))
                {
                    continue;
                }

                listed.Add(buildScene.path);
                string prefix = buildScene.enabled ? "Build/" : "Build (disabled)/";
                AddSceneItem(menu, prefix + Path.GetFileNameWithoutExtension(buildScene.path), buildScene.path, activePath, canSwitch);
            }

            List<string> otherScenes = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", GetExistingFolders()))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!listed.Contains(path))
                {
                    otherScenes.Add(path);
                }
            }

            otherScenes.Sort(System.StringComparer.OrdinalIgnoreCase);
            if (otherScenes.Count > 0 && listed.Count > 0)
            {
                menu.AddSeparator(string.Empty);
            }

            foreach (string path in otherScenes)
            {
                // Keep sub-folders (e.g. Dev/) as sub-menus.
                string relative = ToMenuPath(path);
                AddSceneItem(menu, relative, path, activePath, canSwitch);
            }

            if (menu.GetItemCount() == 0)
            {
                menu.AddDisabledItem(new GUIContent("No scenes found"));
            }

            menu.DropDown(dropdownRect);
        }

        private static void AddSceneItem(GenericMenu menu, string menuPath, string scenePath, string activePath, bool canSwitch)
        {
            GUIContent item = new GUIContent(menuPath);
            if (!canSwitch)
            {
                menu.AddDisabledItem(item, scenePath == activePath);
                return;
            }

            menu.AddItem(item, scenePath == activePath, () => OpenScene(scenePath));
        }

        private static void OpenScene(string scenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        private static string ToMenuPath(string scenePath)
        {
            foreach (string folder in SearchFolders)
            {
                if (scenePath.StartsWith(folder + "/"))
                {
                    string relative = scenePath.Substring(folder.Length + 1);
                    return Path.ChangeExtension(relative, null);
                }
            }

            return Path.GetFileNameWithoutExtension(scenePath);
        }

        private static string[] GetExistingFolders()
        {
            List<string> folders = new List<string>();
            foreach (string folder in SearchFolders)
            {
                if (AssetDatabase.IsValidFolder(folder))
                {
                    folders.Add(folder);
                }
            }

            return folders.ToArray();
        }
    }
}
