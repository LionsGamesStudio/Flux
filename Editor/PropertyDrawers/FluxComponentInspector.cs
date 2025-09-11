using UnityEngine;
using UnityEditor;
using FluxFramework.Attributes;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Custom inspector for FluxComponent classes only
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class FluxComponentInspector : UnityEditor.Editor
    {
        private bool isFluxComponent;
        private FluxComponentAttribute fluxAttribute;

        private void OnEnable()
        {
            var targetType = target.GetType();
            fluxAttribute = System.Attribute.GetCustomAttribute(targetType, typeof(FluxComponentAttribute)) as FluxComponentAttribute;
            isFluxComponent = fluxAttribute != null;
        }

        public override void OnInspectorGUI()
        {
            if (isFluxComponent)
            {
                DrawFluxComponentHeader();
            }
            
            DrawDefaultInspector();
            
            if (isFluxComponent)
            {
                DrawFluxComponentFooter();
            }
        }

        private void DrawFluxComponentHeader()
        {
            EditorGUILayout.Space();
            
            var headerStyle = new GUIStyle(EditorStyles.helpBox)
            {
                normal = { background = MakeTexture(2, 2, new Color(0.3f, 0.6f, 1f, 0.3f)) }
            };
            
            EditorGUILayout.BeginVertical(headerStyle);
            
            var titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = new Color(0.2f, 0.4f, 0.8f) }
            };
            
            EditorGUILayout.LabelField("🌊 Flux Component", titleStyle);
            
            if (fluxAttribute.AutoRegister)
            {
                EditorGUILayout.LabelField("✓ Auto-registered with framework", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.LabelField($"Priority: {fluxAttribute.InitializationPriority}", EditorStyles.miniLabel);
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        private void DrawFluxComponentFooter()
        {
            EditorGUILayout.Space();
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Flux Component Tools", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Open Framework Dashboard"))
            {
                FluxFrameworkWindow.ShowWindow();
            }
            
            if (GUILayout.Button("Documentation"))
            {
                Application.OpenURL("https://github.com/LionsGamesStudio/Flux/blob/main/README.md");
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            
            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
