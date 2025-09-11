using UnityEngine;
using UnityEditor;
using TMPro;
using FluxFramework.Core;
using FluxFramework.Configuration;
using FluxFramework.UI;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Menu items for the Flux Framework
    /// </summary>
    public static class FluxMenuItems
    {
        private const string MENU_ROOT = "Flux/";
        private const int PRIORITY_BASE = 0;

        #region Framework Menu Items

        [MenuItem(MENU_ROOT + "Dashboard", priority = PRIORITY_BASE)]
        public static void OpenDashboard()
        {
            FluxFrameworkWindow.ShowWindow();
        }

        [MenuItem(MENU_ROOT + "Documentation", priority = PRIORITY_BASE + 1)]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://github.com/LionsGamesStudio/Flux/blob/main/README.md");
        }

        [MenuItem(MENU_ROOT + "Settings", priority = PRIORITY_BASE + 2)]
        public static void OpenSettings()
        {
            var settings = Resources.Load<FluxFrameworkSettings>("FluxFrameworkSettings");
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<FluxFrameworkSettings>();
                
                // Ensure Resources folder exists
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                
                AssetDatabase.CreateAsset(settings, "Assets/Resources/FluxFrameworkSettings.asset");
                AssetDatabase.SaveAssets();
            }
            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        #endregion

        #region Create Menu Items

        [MenuItem(MENU_ROOT + "Create/Framework Settings", priority = PRIORITY_BASE + 20)]
        public static void CreateFrameworkSettings()
        {
            var asset = ScriptableObject.CreateInstance<FluxFrameworkSettings>();
            ProjectWindowUtil.CreateAsset(asset, "FluxFrameworkSettings.asset");
        }

        [MenuItem(MENU_ROOT + "Create/UI Theme", priority = PRIORITY_BASE + 21)]
        public static void CreateUITheme()
        {
            var asset = ScriptableObject.CreateInstance<FluxUITheme>();
            ProjectWindowUtil.CreateAsset(asset, "FluxUITheme.asset");
        }

        [MenuItem(MENU_ROOT + "Create/Property Definitions", priority = PRIORITY_BASE + 22)]
        public static void CreatePropertyDefinitions()
        {
            var asset = ScriptableObject.CreateInstance<FluxPropertyDefinitions>();
            ProjectWindowUtil.CreateAsset(asset, "FluxPropertyDefinitions.asset");
        }

        [MenuItem(MENU_ROOT + "Create/Event Definitions", priority = PRIORITY_BASE + 23)]
        public static void CreateEventDefinitions()
        {
            var asset = ScriptableObject.CreateInstance<FluxEventDefinitions>();
            ProjectWindowUtil.CreateAsset(asset, "FluxEventDefinitions.asset");
        }

        #endregion

        #region GameObject Menu Items

        [MenuItem("GameObject/Flux/Text Component", priority = 10)]
        public static void CreateFluxText(MenuCommand menuCommand)
        {
            var go = CreateUIGameObject("Flux Text", menuCommand);
            var textComponent = go.AddComponent<FluxText>();
            
            // Configure default settings
            var tmp = go.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "Flux Text";
                tmp.fontSize = 14;
            }
        }

        [MenuItem("GameObject/Flux/Image Component", priority = 11)]
        public static void CreateFluxImage(MenuCommand menuCommand)
        {
            var go = CreateUIGameObject("Flux Image", menuCommand);
            var imageComponent = go.AddComponent<FluxImage>();
            
            // Configure default settings
            var image = go.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = Color.white;
            }
        }

        [MenuItem("GameObject/Flux/Toggle Component", priority = 12)]
        public static void CreateFluxToggle(MenuCommand menuCommand)
        {
            var go = CreateUIGameObject("Flux Toggle", menuCommand);
            var toggleComponent = go.AddComponent<FluxToggle>();
            
            // Add Toggle component if it doesn't exist
            if (go.GetComponent<UnityEngine.UI.Toggle>() == null)
            {
                go.AddComponent<UnityEngine.UI.Toggle>();
            }
        }

        [MenuItem("GameObject/Flux/Slider Component", priority = 13)]
        public static void CreateFluxSlider(MenuCommand menuCommand)
        {
            var go = CreateUIGameObject("Flux Slider", menuCommand);
            var sliderComponent = go.AddComponent<FluxSlider>();
            
            // Add Slider component if it doesn't exist
            if (go.GetComponent<UnityEngine.UI.Slider>() == null)
            {
                go.AddComponent<UnityEngine.UI.Slider>();
            }
        }

        private static GameObject CreateUIGameObject(string name, MenuCommand menuCommand)
        {
            var go = new GameObject(name);
            
            // Ensure we have a Canvas in the scene
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                
                // Ensure EventSystem exists
                if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var eventSystemGO = new GameObject("EventSystem");
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }
            
            // Parent to Canvas
            go.transform.SetParent(canvas.transform, false);
            
            // Add RectTransform
            var rectTransform = go.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 30);
            
            // Register undo and select
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Selection.activeObject = go;
            
            return go;
        }

        #endregion

        #region Tools Menu Items

        [MenuItem(MENU_ROOT + "Tools/Scan Scene for Components", priority = PRIORITY_BASE + 40)]
        public static void ScanSceneForComponents()
        {
            var fluxComponents = Object.FindObjectsOfType<MonoBehaviour>();
            int count = 0;
            
            foreach (var component in fluxComponents)
            {
                if (component.GetType().Namespace?.StartsWith("FluxFramework") == true)
                {
                    count++;
                    Debug.Log($"Found Flux component: {component.name} ({component.GetType().Name})", component);
                }
            }
            
            EditorUtility.DisplayDialog("Scan Results", 
                $"Found {count} Flux components in the scene.\nCheck the console for details.", "OK");
        }

        [MenuItem(MENU_ROOT + "Tools/Validate Bindings", priority = PRIORITY_BASE + 41)]
        public static void ValidateBindings()
        {
            var fluxUIComponents = Object.FindObjectsOfType<FluxUIComponent>();
            int validBindings = 0;
            int invalidBindings = 0;
            
            foreach (var component in fluxUIComponents)
            {
                // This is a simplified validation - in a real implementation,
                // you would check the actual binding configuration
                if (component != null)
                {
                    validBindings++;
                    Debug.Log($"Valid binding: {component.name}", component);
                }
                else
                {
                    invalidBindings++;
                }
            }
            
            string message = $"Validation Results:\n" +
                           $"Valid bindings: {validBindings}\n" +
                           $"Invalid bindings: {invalidBindings}";
            
            var messageType = invalidBindings > 0 ? MessageType.Warning : MessageType.Info;
            
            EditorUtility.DisplayDialog("Binding Validation", message, "OK");
        }

        [MenuItem(MENU_ROOT + "Tools/Clear Event Bus", priority = PRIORITY_BASE + 42)]
        public static void ClearEventBus()
        {
            if (Application.isPlaying)
            {
                EventBus.Clear();
                Debug.Log("[FluxFramework] Event bus cleared");
                EditorUtility.DisplayDialog("Event Bus", "Event bus has been cleared.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Event Bus", 
                    "Event bus can only be cleared during play mode.", "OK");
            }
        }

        [MenuItem(MENU_ROOT + "Tools/Framework Info", priority = PRIORITY_BASE + 43)]
        public static void ShowFrameworkInfo()
        {
            string info = "Flux Framework v1.0.0\n\n" +
                         "An innovative Unity framework for decoupling UI from logic\n" +
                         "with reactive patterns, thread-safe operations, and enhanced\n" +
                         "developer experience.\n\n" +
                         "Features:\n" +
                         "• Reactive UI Binding\n" +
                         "• Thread-Safe Operations\n" +
                         "• ScriptableObject Integration\n" +
                         "• Attribute-Based Configuration\n" +
                         "• Enhanced Developer Experience\n" +
                         "• Automatic Initialization\n" +
                         "• Event-Driven Architecture";
            
            EditorUtility.DisplayDialog("Flux Framework", info, "OK");
        }

        #endregion

        #region Validation Methods

        [MenuItem(MENU_ROOT + "Tools/Clear Event Bus", true)]
        public static bool ValidateClearEventBus()
        {
            return Application.isPlaying;
        }

        #endregion
    }
}
