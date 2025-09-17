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

// TODO: Change this to your project's namespace
namespace MyGame.Data
{{
    /// <summary>
    /// Data container for managing reactive game data.
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/Data/{className}"")]
    public class {className} : FluxDataContainer
    {{
        // Add your reactive properties here.
        // [ReactiveProperty(""my.data.value"")]
        // public int MyValue;
    }}
}}";
        }
        
        private static string GenerateFluxMonoBehaviourTemplate(string className)
        {
            // --- UPDATED TEMPLATE ---
            return $@"using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;
using System; // Required for IDisposable

// TODO: Change this to your project's namespace
namespace MyGame.Logic
{{
    /// <summary>
    /// A MonoBehaviour integrated with the FluxFramework.
    /// Inherits from FluxMonoBehaviour to get the safe, framework-aware lifecycle.
    /// </summary>
    public class {className} : FluxMonoBehaviour
    {{
        // Use [ReactiveProperty] to declare and initialize state.
        // [ReactiveProperty(""my.component.state"")]
        // private bool _myState = true;
        
        // Store IDisposable subscriptions for cleanup.
        // private IDisposable _mySubscription;

        /// <summary>
        /// This is the framework-safe equivalent of Awake().
        /// Use it for component setup and property/event subscriptions.
        /// </summary>
        protected override void OnFluxAwake()
        {{
            // _mySubscription = SubscribeToProperty<float>(""some.property"", OnPropertyChanged);
        }}
        
        /// <summary>
        /// This is the framework-safe equivalent of Start().
        /// Use it for logic that depends on other components being initialized.
        /// </summary>
        protected override void OnFluxStart()
        {{
            
        }}

        /// <summary>
        /// This is the framework-safe equivalent of OnDestroy().
        /// Use it to clean up subscriptions and other resources.
        /// </summary>
        protected override void OnFluxDestroy()
        {{
            // _mySubscription?.Dispose();
        }}
    }}
}}";
        }
        
        private static string GenerateFluxScriptableObjectTemplate(string className)
        {
            return $@"using UnityEngine;
using FluxFramework.Core;
using FluxFramework.Attributes;

// TODO: Change this to your project's namespace
namespace MyGame.Data
{{
    /// <summary>
    /// A ScriptableObject integrated with the FluxFramework reactive system.
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/ScriptableObjects/{className}"")]
    public class {className} : FluxScriptableObject
    {{
        // Add your reactive properties here.
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

// TODO: Change this to your project's namespace
namespace MyGame.Settings
{{
    /// <summary>
    /// A ScriptableObject for game settings with automatic persistence (saving/loading).
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/Settings/{className}"")]
    public class {className} : FluxSettings
    {{
        // Add your settings properties here.
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
using FluxFramework.Core;

// TODO: Change this to your project's namespace
namespace MyGame.UI
{{
    /// <summary>
    /// A custom UI component that supports reactive data binding.
    /// </summary>
    public class {className} : FluxUIComponent
    {{
        // For automatic binding, declare a [SerializeField] for your UI component
        // and add the [FluxBinding] attribute to it.
        // The base class will handle the rest.
        //
        // [Header(""Bindings"")]
        // [FluxBinding(""my.ui.value"", Mode = BindingMode.OneWay)]
        // [SerializeField] private TMPro.TextMeshProUGUI _myTextComponent;

        /// <summary>
        /// This method is for component-specific setup, like getting references.
        /// It's called automatically by the base class.
        /// </summary>
        protected override void InitializeComponent()
        {{
            // e.g., if(_myTextComponent == null) _myTextComponent = GetComponent<TMPro.TextMeshProUGUI>();
        }}
        
        /// <summary>
        /// Override this method to add custom binding logic that cannot be
        /// handled automatically by attributes.
        /// </summary>
        protected override void RegisterCustomBindings()
        {{
            // Example:
            // var myBinding = new MyCustomBinding(""some.key"", someComponent);
            // Flux.Manager.BindingSystem.Bind(""some.key"", myBinding)
            // TrackBinding(myBinding); // Important for automatic cleanup!
        }}

        /// <summary>
        /// Override this to apply styles from the global UI Theme.
        /// </summary>
        public override void ApplyTheme()
        {{
            // var theme = UIThemeManager.CurrentTheme;
            // if (theme != null && _myTextComponent != null)
            // {{
            //     _myTextComponent.color = theme.textColor;
            // }}
        }}
    }}
}}";
        }
        
        private static string GenerateFluxEventTemplate(string className)
        {
            if (!className.EndsWith("Event")) className += "Event";
            
            return $@"using FluxFramework.Core;

// TODO: Change this to your project's namespace
namespace MyGame.Events
{{
    /// <summary>
    /// An event that signifies [describe what happens when this event is published].
    /// </summary>
    public class {className} : FluxEventBase
    {{
        // Add readonly properties to carry data with the event.
        // public string SomeData {{ get; }}

        public {className}(/* string someData */)
        {{
            // SomeData = someData;
        }}
    }}
}}";
        }
        
        private static string GenerateFluxNodeTemplate(string className)
        {
            if (!className.EndsWith("Node")) className += "Node";

            return $@"using System.Collections.Generic;
using UnityEngine;
using FluxFramework.Core;
using FluxFramework.VisualScripting;
using FluxFramework.VisualScripting.Execution;

// TODO: Change this to your project's namespace
namespace MyGame.VisualScripting
{{
    /// <summary>
    /// A custom node that [describe what the node does].
    /// </summary>
    [CreateAssetMenu(fileName = ""{className}"", menuName = ""Flux/Visual Scripting/Custom/{className}"")]
    public class {className} : FluxNodeBase
    {{
        public override string NodeName => ""My Custom Node"";
        public override string Category => ""Custom"";

        protected override void InitializePorts()
        {{
            AddInputPort(""execute"", ""▶ In"", FluxPortType.Execution, ""void"", true);
            AddOutputPort(""onComplete"", ""▶ Out"", FluxPortType.Execution, ""void"", false);
        }}

        protected override void ExecuteInternal(FluxGraphExecutor executor, Dictionary<string, object> inputs, Dictionary<string, object> outputs)
        {{
            // Your logic here...
            
            // Continue the execution flow.
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