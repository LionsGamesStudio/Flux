using UnityEditor;
using UnityEngine;

namespace FluxFramework.VisualScripting.Editor
{
    [CustomEditor(typeof(FluxVisualGraph)), CanEditMultipleObjects]
    public class FluxVisualGraphEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector fields (for the internal lists, etc.)
            // This is useful for debugging.
            DrawDefaultInspector();

            EditorGUILayout.Space(10);
            
            // Set a distinct color to make the button stand out.
            GUI.backgroundColor = new Color(0.4f, 0.7f, 1f); // A nice blue

            // Make the button large and easy to click.
            if (GUILayout.Button("Open in Graph Editor", GUILayout.Height(40)))
            {
                // When clicked, call our new static method to open the window with this graph.
                FluxVisualScriptingWindow.OpenWithGraph(target as FluxVisualGraph);
            }

            // Reset the color to default for any other GUI elements.
            GUI.backgroundColor = Color.white;
        }
    }
}