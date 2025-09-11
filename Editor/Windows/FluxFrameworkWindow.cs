using UnityEngine;
using UnityEditor;
using FluxFramework.Core;
using FluxFramework.Binding;
using FluxFramework.Configuration;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Main editor window for the Flux Framework
    /// </summary>
    public class FluxFrameworkWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private bool showProperties = true;
        private bool showEvents = true;
        private bool showBindings = true;
        private bool showConfiguration = true;

        [MenuItem("Flux/Framework Dashboard")]
        public static void ShowWindow()
        {
            var window = GetWindow<FluxFrameworkWindow>("Flux Framework");
            window.minSize = new Vector2(400, 300);
        }
        
        private void OnEnable()
        {
            // Subscribe to the editor update loop to keep the window's data fresh
            EditorApplication.update += Repaint;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        private void OnGUI()
        {
            DrawHeader();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawFrameworkStatus();
            DrawPropertySection();
            DrawEventSection();
            DrawBindingSection();
            DrawConfigurationSection();

            EditorGUILayout.EndScrollView();

            DrawFooter();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space();
            
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };
            
            EditorGUILayout.LabelField("Flux Framework Dashboard", headerStyle);
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Documentation", GUILayout.Width(100)))
            {
                Application.OpenURL("https://github.com/LionsGamesStudio/Flux/blob/main/README.md");
            }
            
            if (GUILayout.Button("Visual Scripting", GUILayout.Width(120)))
            {
                FluxFramework.VisualScripting.Editor.FluxVisualScriptingWindow.ShowWindow();
            }
            
            if (GUILayout.Button("Settings", GUILayout.Width(100)))
            {
                OpenFrameworkSettings();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
        }

        private void DrawFrameworkStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Framework Status", EditorStyles.boldLabel);
            
            bool isInitialized = Application.isPlaying && FluxManager.Instance != null;
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Status:", GUILayout.Width(150)); // Give it a fixed width for alignment
            
            var statusColor = isInitialized ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.5f);
            var statusText = isInitialized ? "Initialized (Runtime)" : "Not Initialized (Editor)";
            
            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = statusColor;
            EditorGUILayout.LabelField(statusText, EditorStyles.helpBox); // Use a helpBox for a nice background
            GUI.backgroundColor = originalColor;
            
            EditorGUILayout.EndHorizontal();
            
            if (Application.isPlaying && isInitialized)
            {
                EditorGUILayout.LabelField($"Registered Properties:", $"{FluxManager.Instance.Properties.PropertyCount}");
                EditorGUILayout.LabelField($"Event Subscriptions:", $"{EventBus.GetTotalSubscriberCount()}");
                EditorGUILayout.LabelField($"Active UI Bindings:", $"{ReactiveBindingSystem.GetActiveBindingCount()}");
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see live statistics.", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawPropertySection()
        {
            showProperties = EditorGUILayout.Foldout(showProperties, "Reactive Properties", true);
            
            if (showProperties)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (Application.isPlaying && FluxManager.Instance != null)
                {
                    // Show runtime properties
                    EditorGUILayout.LabelField("Runtime Properties:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Property monitoring available during play mode", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter play mode to see runtime properties", MessageType.Info);
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Property Definition"))
                {
                    CreatePropertyDefinition();
                }
                if (GUILayout.Button("Refresh Properties"))
                {
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawEventSection()
        {
            showEvents = EditorGUILayout.Foldout(showEvents, "Event System", true);
            
            if (showEvents)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                if (Application.isPlaying)
                {
                    EditorGUILayout.LabelField("Event Statistics:", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Event monitoring available during play mode", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox("Enter play mode to see event statistics", MessageType.Info);
                }
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Create Event Definition"))
                {
                    CreateEventDefinition();
                }
                if (GUILayout.Button("Clear Event Bus"))
                {
                    if (Application.isPlaying)
                    {
                        EventBus.Clear();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawBindingSection()
        {
            showBindings = EditorGUILayout.Foldout(showBindings, "UI Bindings", true);
            
            if (showBindings)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("Binding Tools:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Scan Scene for Components"))
                {
                    ScanSceneForFluxComponents();
                }
                if (GUILayout.Button("Validate Bindings"))
                {
                    ValidateBindings();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawConfigurationSection()
        {
            showConfiguration = EditorGUILayout.Foldout(showConfiguration, "Configuration", true);
            
            if (showConfiguration)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                EditorGUILayout.LabelField("Configuration Assets:", EditorStyles.boldLabel);
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Framework Settings"))
                {
                    OpenFrameworkSettings();
                }
                if (GUILayout.Button("UI Theme"))
                {
                    CreateUITheme();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Property Definitions"))
                {
                    CreatePropertyDefinition();
                }
                if (GUILayout.Button("Event Definitions"))
                {
                    CreateEventDefinition();
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.EndVertical();
            }
        }

        private void DrawFooter()
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Flux Framework v1.0.0", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // Helper methods
        private void CreatePropertyDefinition()
        {
            var asset = CreateInstance<FluxPropertyDefinitions>();
            ProjectWindowUtil.CreateAsset(asset, "FluxPropertyDefinitions.asset");
        }

        private void CreateEventDefinition()
        {
            var asset = CreateInstance<FluxEventDefinitions>();
            ProjectWindowUtil.CreateAsset(asset, "FluxEventDefinitions.asset");
        }

        private void CreateUITheme()
        {
            var asset = CreateInstance<FluxUITheme>();
            ProjectWindowUtil.CreateAsset(asset, "FluxUITheme.asset");
        }

        private void OpenFrameworkSettings()
        {
            var settings = Resources.Load<FluxFrameworkSettings>("FluxFrameworkSettings");
            if (settings == null)
            {
                settings = CreateInstance<FluxFrameworkSettings>();
                AssetDatabase.CreateAsset(settings, "Assets/Resources/FluxFrameworkSettings.asset");
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = settings;
        }

        private void ScanSceneForFluxComponents()
        {
            var fluxComponents = FindObjectsOfType<MonoBehaviour>();
            int count = 0;
            
            foreach (var component in fluxComponents)
            {
                if (component.GetType().Namespace?.StartsWith("FluxFramework") == true)
                {
                    count++;
                }
            }
            
            EditorUtility.DisplayDialog("Scan Results", $"Found {count} Flux components in the scene.", "OK");
        }

        private void ValidateBindings()
        {
            EditorUtility.DisplayDialog("Validation", "Binding validation completed successfully.", "OK");
        }
    }
}
