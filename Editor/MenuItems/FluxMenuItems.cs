using UnityEngine;
using UnityEditor;
using FluxFramework.Core;
using FluxFramework.Configuration;
using FluxFramework.Editor;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Defines the main menu structure for the Flux Framework.
    /// </summary>
    public static class FluxMenuItems
    {
        private const string MENU_ROOT = "Flux/";
        
        // --- Main Window ---
        // The Control Panel is now in its own script (FluxFrameworkWindow.cs)

        // --- Tools Submenu ---
        [MenuItem(MENU_ROOT + "Tools/Health Check...", false, 20)]
        public static void ShowHealthCheck() => FluxHealthCheckWindow.ShowWindow();
        
        [MenuItem(MENU_ROOT + "Tools/Generate Static Keys...", false, 21)]
        public static void ShowKeysGenerator() => FluxKeysGeneratorWindow.ShowWindow();

        [MenuItem(MENU_ROOT + "Tools/Refresh Component Registry", false, 40)]
        public static void RefreshComponentRegistry()
        {
            FluxEditorServices.ComponentRegistry?.ClearCache();
            FluxEditorServices.ComponentRegistry?.Initialize();
            FluxFramework.Core.Flux.Manager.Logger.Info("[FluxFramework] Editor Component Registry has been refreshed.");
        }

        // --- Debug Submenu ---
        [MenuItem(MENU_ROOT + "Debug/Reactive Properties Inspector...", false, 0)]
        public static void ShowPropertyInspector() => ReactivePropertyInspectorWindow.ShowWindow();

        [MenuItem(MENU_ROOT + "Debug/Event Bus Monitor...", false, 1)]
        public static void ShowEventMonitor() => EventBusMonitorWindow.ShowWindow();

        // --- Configuration Submenu ---
        [MenuItem(MENU_ROOT + "Configuration/Framework Settings...", false, 0)]
        public static void SelectFrameworkSettings() => SelectAsset<FluxFrameworkSettings>();
        
        [MenuItem(MENU_ROOT + "Configuration/UI Theme...", false, 1)]
        public static void SelectUITheme() => SelectAsset<FluxUITheme>();
        
        [MenuItem(MENU_ROOT + "Configuration/Property Definitions...", false, 20)]
        public static void ShowPropertyDefinitions() => PropertyKeyViewerWindow.ShowWindow();

        [MenuItem(MENU_ROOT + "Configuration/Event Definitions...", false, 21)]
        public static void SelectEventDefinitions() => SelectAsset<FluxEventDefinitions>();
        
        [MenuItem(MENU_ROOT + "Configuration/Configuration Manager...", false, 100)]
        public static void ShowConfigurationManager() => FluxConfigurationWindow.ShowWindow();

        // --- Separator and Documentation ---
        [MenuItem(MENU_ROOT + "Documentation", false, 200)]
        public static void OpenDocumentation() => Application.OpenURL("https://github.com/LionsGamesStudio/Flux/blob/main/README.md");

        // --- Helper to select the first asset of a given type, or prompt to create one ---
        private static void SelectAsset<T>() where T : ScriptableObject
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<T>(path);
            }
            else
            {
                if (EditorUtility.DisplayDialog("Asset Not Found", $"Could not find any asset of type '{typeof(T).Name}'. Would you like to create one now?", "Create", "Cancel"))
                {
                    var asset = ScriptableObject.CreateInstance<T>();
                    ProjectWindowUtil.CreateAsset(asset, $"{typeof(T).Name}.asset");
                }
            }
        }
    }
}