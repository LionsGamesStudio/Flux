using UnityEngine;
using UnityEditor;
using System.IO;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Template generator for FluxFramework classes
    /// Creates pre-configured scripts with proper structure and base implementations
    /// </summary>
    public static class FluxTemplateGenerator
    {
        // This constant is not used, but kept as per original script.
        private const string TEMPLATES_FOLDER = "Assets/Scripts/Flux";

        [MenuItem("Assets/Create/Flux/Framework/FluxDataContainer", priority = 80)]
        public static void CreateFluxDataContainer()
        {
            CreateTemplateWithDialog("MyDataContainer", GenerateFluxDataContainerTemplate);
        }

        [MenuItem("Assets/Create/Flux/Framework/FluxMonoBehaviour", priority = 81)]
        public static void CreateFluxMonoBehaviour()
        {
            CreateTemplateWithDialog("MyFluxComponent", GenerateFluxMonoBehaviourTemplate);
        }

        [MenuItem("Assets/Create/Flux/Framework/FluxScriptableObject", priority = 82)]
        public static void CreateFluxScriptableObject()
        {
            CreateTemplateWithDialog("MyScriptableObject", GenerateFluxScriptableObjectTemplate);
        }

        [MenuItem("Assets/Create/Flux/Framework/FluxSettings", priority = 83)]
        public static void CreateFluxSettings()
        {
            CreateTemplateWithDialog("MyGameSettings", GenerateFluxSettingsTemplate);
        }

        [MenuItem("Assets/Create/Flux/UI/FluxUIComponent", priority = 90)]
        public static void CreateFluxUIComponent()
        {
            CreateTemplateWithDialog("MyUIComponent", GenerateFluxUIComponentTemplate);
        }

        [MenuItem("Assets/Create/Flux/Event/Flux Event", priority = 100)]
        public static void CreateFluxEvent()
        {
            CreateTemplateWithDialog("MyCustomEvent", GenerateFluxEventTemplate);
        }

        [MenuItem("Assets/Create/Flux/Visual Scripting/New Node", priority = 110)]
        public static void CreateFluxNode()
        {
            CreateTemplateWithDialog("MyCustomNode", GenerateFluxNodeTemplate);
        }

        private static void CreateTemplateWithDialog(string defaultName, System.Func<string, string> templateGenerator)
        {
            string className = EditorInputDialog.Show("Create Flux Script", $"Enter class name:", defaultName);

            if (string.IsNullOrEmpty(className))
                return;

            // Ensure valid C# class name
            className = MakeValidClassName(className);

            // Get target directory
            string targetPath = GetTargetPath();
            EnsureDirectoryExists(targetPath);

            // Generate and write template
            string filePath = Path.Combine(targetPath, $"{className}.cs");
            string templateContent = templateGenerator(className);

            if (File.Exists(filePath))
            {
                if (!EditorUtility.DisplayDialog("File Exists",
                    $"File {className}.cs already exists. Overwrite?",
                    "Overwrite", "Cancel"))
                    return;
            }

            File.WriteAllText(filePath, templateContent);
            AssetDatabase.Refresh();

            // Select the created file
            var asset = AssetDatabase.LoadAssetAtPath<Object>(GetRelativePath(filePath));
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);

            Debug.Log($"Created Flux script: {filePath}");
        }

        private static string GetTargetPath()
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);

            if (string.IsNullOrEmpty(selectedPath))
                selectedPath = "Assets";
            else if (!Directory.Exists(selectedPath))
                selectedPath = Path.GetDirectoryName(selectedPath);

            return selectedPath;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }

        private static string GetRelativePath(string fullPath)
        {
            return fullPath.Replace(Application.dataPath, "Assets");
        }

        private static string MakeValidClassName(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "MyFluxClass";

            // Remove invalid characters and ensure it starts with letter
            string result = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "");

            if (result.Length == 0) return "MyFluxClass";

            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }

        #region Template Generators

        private static string GenerateFluxDataContainerTemplate(string className)
        {
            return $@"using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

// You can change this to your project's namespace
namespace MyGame
{{
    /// <summary>
    /// Data container for managing reactive game data.
    /// Inherits from FluxDataContainer which provides automatic property registration and validation features.
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/Data/{className}"")]
    public class {className} : FluxDataContainer
    {{
        // [Header(""{className} Properties"")]
        
        // Add your reactive properties here using the [ReactiveProperty] attribute.
        // The framework will automatically register these with the FluxManager.
        // [ReactiveProperty(""my.data.value"")]
        // [SerializeField] private int _myValue;
    }}
}}";
        }

        private static string GenerateFluxMonoBehaviourTemplate(string className)
        {
            return $@"using UnityEngine;
using FluxFramework.Core;

// You can change this to your project's namespace
namespace MyGame
{{
    /// <summary>
    /// A MonoBehaviour integrated with the FluxFramework.
    /// Inherits from FluxMonoBehaviour to automatically get framework lifecycle integration.
    /// </summary>
    public class {className} : FluxMonoBehaviour
    {{
        // Use [FluxGroup] to organize your inspector fields.
        // Use [FluxBinding] on dummy fields to configure data binding.
        // Use [FluxButton] on methods to create debug buttons.

        /// <summary>
        /// This method is guaranteed to be called after the Flux framework is initialized.
        /// It's the safe equivalent of Awake().
        /// </summary>
        protected override void Awake()
        {{
            base.Awake();
            // Your initialization logic here (e.g., GetComponent).
        }}
        
        // Implement Update, FixedUpdate, etc. as you normally would.

        /// <summary>
        /// This method is called when the component is being destroyed.
        /// Use it for any specific cleanup your component needs.
        /// </summary>
        protected override void OnDestroy()
        {{
            // Your cleanup logic here.
            base.OnDestroy();
        }}
    }}
}}";
        }

        private static string GenerateFluxScriptableObjectTemplate(string className)
        {
            return $@"using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

// You can change this to your project's namespace
namespace MyGame
{{
    /// <summary>
    /// A ScriptableObject integrated with the FluxFramework reactive system.
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/ScriptableObjects/{className}"")]
    public class {className} : FluxScriptableObject
    {{
        // [Header(""{className} Data"")]
        
        // Add your reactive properties here using the [ReactiveProperty] attribute.
        // The framework will automatically register these with the FluxManager when this asset is enabled.
        // [ReactiveProperty(""my.asset.data"")]
        // [SerializeField] private string _myData;
    }}
}}";
        }

        private static string GenerateFluxSettingsTemplate(string className)
        {
            return $@"using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

// You can change this to your project's namespace
namespace MyGame
{{
    /// <summary>
    /// A ScriptableObject for game settings with automatic persistence (saving/loading).
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/Settings/{className}"")]
    public class {className} : FluxSettings
    {{
        // [Header(""{className} Settings"")]
        
        // Add your settings properties here. The [ReactiveProperty] attribute is required
        // for the automatic saving and loading to work.
        // [ReactiveProperty(""settings.volume"")]
        // [Range(0, 1)]
        // [SerializeField] private float _musicVolume = 0.8f;
    }}
}}";
        }

        private static string GenerateFluxUIComponentTemplate(string className)
        {
            return $@"using UnityEngine;
using FluxFramework.UI;
using FluxFramework.Attributes;
using FluxFramework.Binding;

// You can change this to your project's namespace
namespace MyGame.UI
{{
    /// <summary>
    /// A custom UI component that supports reactive data binding.
    /// </summary>
    public class {className} : FluxUIComponent
    {{
        // Example of a dummy field for binding configuration in the inspector.
        // [Header(""Binding Configuration"")]
        // [FluxBinding(""my.ui.value"", Mode = BindingMode.OneWay)]
        // [SerializeField] private string _valueBinding;

        // private MyValueBinding _binding;

        /// <summary>
        /// This is the safe equivalent of Awake().
        /// Use it to get references to your UI components (e.g., Image, Text).
        /// </summary>
        protected override void Awake()
        {{
            base.Awake();
            // e.g., myImageComponent = GetComponent<Image>();
        }}
        
        /// <summary>
        /// Implement this method to create and register your specific UI bindings.
        /// This is called automatically at the correct time.
        /// </summary>
        protected override void RegisterBindings()
        {{
            // Use reflection to read your [FluxBinding] attributes and call ReactiveBindingSystem.Bind().
            // See existing FluxSlider or FluxText for examples.
        }}

        /// <summary>
        /// Implement this method to unregister your UI bindings.
        /// This is crucial for preventing memory leaks.
        /// </summary>
        protected override void UnregisterBindings()
        {{
            // Call ReactiveBindingSystem.Unbind() for each binding you created.
        }}
    }}
}}";
        }

        private static string GenerateFluxEventTemplate(string className)
        {
            // Ensure the class name ends with "Event" for good practice.
            if (!className.EndsWith("Event"))
            {
                className += "Event";
            }

            return $@"using FluxFramework.Core;
// You can change this to your project's namespace
namespace MyGame.Events
{{
    /// <summary>
    /// An event that signifies [describe what happens when this event is published].
    /// </summary>
    public class {className} : FluxEventBase
    {{
        // Add properties to carry data with the event.
        // Make them readonly (get-only) for good practice (immutable events).
        // public string SomeData {{ get; }}
        // public int SomeValue {{ get; }}

        public {className}(/* string someData, int someValue */)
        {{
            // SomeData = someData;
            // SomeValue = someValue;
        }}
    }}
}}";
        }
        
        private static string GenerateFluxNodeTemplate(string className)
        {
            // Ensure the class name ends with "Node" for good practice.
            if (!className.EndsWith("Node"))
            {
                className += "Node";
            }

            return $@"using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Execution;

// You can change this to your project's namespace
namespace MyGame.VisualScripting
{{
    /// <summary>
    /// A custom node that [describe what the node does].
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/Visual Scripting/Custom/{className}"")]
    public class {className} : FluxNodeBase
    {{
        // Use [SerializeField] for any properties you want to configure in the node's inspector.
        // [SerializeField] private float _mySetting = 1.0f;

        // Set the default name and category for the node in the search window.
        public override string NodeName => ""My Custom Node"";
        public override string Category => ""Custom"";

        /// <summary>
        /// This method defines the input and output ports for your node.
        /// </summary>
        protected override void InitializePorts()
        {{
            // --- Execution Ports ---
            // Execution ports control the flow of the graph.
            AddInputPort(""execute"", ""▶ In"", FluxPortType.Execution, ""void"", true);
            AddOutputPort(""onComplete"", ""▶ Out"", FluxPortType.Execution, ""void"", false);

            // --- Data Ports ---
            // Data ports are for passing values between nodes.
            AddInputPort(""myInput"", ""My Input"", FluxPortType.Data, ""float"", false, 0f, ""An example float input."");
            AddOutputPort(""myOutput"", ""My Output"", FluxPortType.Data, ""string"", false, ""An example string output."");
        }}

        /// <summary>
        /// This is the main logic of your node. It's called when the 'execute' port is triggered.
        /// </summary>
        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {{
            // 1. Get values from your input ports.
            float myInputValue = GetInputValue<float>(inputs, ""myInput"");
            
            // 2. Perform your node's logic.
            string result = $""The input value was {{myInputValue}}"";
            Debug.Log(result);

            // If you need the GameObject running the graph (e.g., to start a coroutine):
            // var contextObject = executor.Runner.GetContextObject();
            
            // 3. Set values for your output ports.
            SetOutputValue(outputs, ""myOutput"", result);

            // 4. Trigger an output execution port to continue the graph flow.
            SetOutputValue(outputs, ""onComplete"", null);
        }}
    }}
}}";
        }
            #endregion
    }
    
    
    /// <summary>
    /// Simple input dialog for getting user input in the editor
    /// </summary>
    public class EditorInputDialog : EditorWindow
    {
        private string _inputText = "";
        private string _title = "";
        private string _message = "";
        private System.Action<string> _onComplete;
        private System.Action _onCancel;

        public static string Show(string title, string message, string defaultValue = "")
        {
            string result = defaultValue;
            bool completed = false;

            var dialog = CreateInstance<EditorInputDialog>();
            dialog._title = title;
            dialog._message = message;
            dialog._inputText = defaultValue;
            dialog._onComplete = (text) =>
            {
                result = text;
                completed = true;
                dialog.Close();
            };
            dialog._onCancel = () =>
            {
                result = null;
                completed = true;
                dialog.Close();
            };

            dialog.titleContent = new GUIContent(title);
            dialog.position = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 50, 400, 100);
            dialog.ShowModal();

            // Wait for user input
            while (!completed && dialog != null)
            {
                System.Threading.Thread.Sleep(50);
            }

            return result;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField(_message, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(5);

            GUI.SetNextControlName("InputField");
            _inputText = EditorGUILayout.TextField(_inputText);

            if (Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("InputField");
            }

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Create", GUILayout.Width(80)) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                _onComplete?.Invoke(_inputText);
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)) ||
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                _onCancel?.Invoke();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}