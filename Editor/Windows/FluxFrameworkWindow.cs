using UnityEngine;
using UnityEditor;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Configuration;
using FluxFramework.VisualScripting.Editor;

namespace FluxFramework.Editor
{
    /// <summary>
    /// The main control panel for the Flux Framework.
    /// Provides access to all tools, debuggers, and configuration assets.
    /// </summary>
    public class FluxFrameworkWindow : EditorWindow
    {
        [MenuItem("Flux/Control Panel...", false, 0)]
        public static void ShowWindow()
        {
            GetWindow<FluxFrameworkWindow>("Flux Control Panel");
        }
        
        private void OnEnable() => EditorApplication.update += Repaint;
        private void OnDisable() => EditorApplication.update -= Repaint;

        private void OnGUI()
        {
            DrawHeader();
            DrawFrameworkStatus();
            EditorGUILayout.Space();
            DrawToolbox();
            DrawFooter();
        }

        private void DrawHeader()
        {
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField("Flux Control Panel", headerStyle);
            EditorGUILayout.HelpBox("This is the central hub for all Flux Framework tools and settings.", MessageType.Info);
        }

        private void DrawFrameworkStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Live Status (Play Mode)", EditorStyles.boldLabel);

            bool isInitialized = Application.isPlaying && Flux.Manager != null && Flux.Manager.IsInitialized;
            if (isInitialized)
            {
                EditorGUILayout.LabelField("Registered Properties:", $"{Flux.Manager.Properties.PropertyCount}");
                EditorGUILayout.LabelField("Event Subscriptions:", $"{Flux.Manager.EventBus.GetTotalSubscriberCount()}");
                EditorGUILayout.LabelField("Active UI Bindings:", $"{Flux.Manager.BindingSystem.GetActiveBindingCount()}");
            }
            else
            {
                EditorGUILayout.LabelField("Enter Play Mode to see live statistics.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbox()
        {
            EditorGUILayout.LabelField("Framework Toolbox", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Row 1: Main Tools
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Health Check", "Scans the project for broken bindings and other issues."), GUILayout.Height(30)))
            {
                FluxHealthCheckWindow.ShowWindow();
            }
            if (GUILayout.Button(new GUIContent("Keys Generator", "Generates the static 'FluxKeys' class for type-safe bindings."), GUILayout.Height(30)))
            {
                FluxKeysGeneratorWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();

            // Row 2: Debug Windows
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Properties Inspector", "View and edit all reactive properties at runtime."), GUILayout.Height(30)))
            {
                ReactivePropertyInspectorWindow.ShowWindow();
            }
            if (GUILayout.Button(new GUIContent("Event Bus Monitor", "Watch all events flow through the system at runtime."), GUILayout.Height(30)))
            {
                EventBusMonitorWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
            
            // Row 3: Visual Scripting & Configuration
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Visual Scripting Editor", "Open the graph editor for visual scripting."), GUILayout.Height(30)))
            {
                FluxVisualScriptingWindow.OpenWindow();
            }
            if (GUILayout.Button(new GUIContent("Configuration Manager", "Manage all configuration assets like settings, themes, and definitions."), GUILayout.Height(30)))
            {
                // We keep this window as it has specialized logic, but launch it from here.
                FluxConfigurationWindow.ShowWindow(); 
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Documentation", EditorStyles.miniButtonLeft)) Application.OpenURL("https://github.com/LionsGamesStudio/Flux/blob/main/README.md");
            if (GUILayout.Button("Settings Asset", EditorStyles.miniButtonRight)) Selection.activeObject = FindOrCreateSettingsAsset();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Flux Framework v3.0", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }
        
        private FluxFrameworkSettings FindOrCreateSettingsAsset()
        {
            // Simplified logic to find or create the main settings asset
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FluxFrameworkSettings)}");
            if (guids.Length > 0)
            {
                return AssetDatabase.LoadAssetAtPath<FluxFrameworkSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            // If not found, create it in a default location
            var settings = CreateInstance<FluxFrameworkSettings>();
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateAsset(settings, $"Assets/Resources/{nameof(FluxFrameworkSettings)}.asset");
            AssetDatabase.SaveAssets();
            return settings;
        }
    }
}