using UnityEditor;
using FluxFramework.VisualScripting.Nodes;

namespace FluxFramework.VisualScripting.Editor
{
    [CustomEditor(typeof(FluxEventListenerNode))]
    public class FluxEventListenerNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // Draw the default inspector first

            var listenerNode = (FluxEventListenerNode)target;
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Event Type Selection", EditorStyles.boldLabel);

            var eventNames = FluxEventDefinitionUtility.GetDefinedEventNames();
            if (eventNames.Count == 0)
            {
                EditorGUILayout.HelpBox("No event definitions found. Create a FluxEventDefinitions asset and scan your project.", MessageType.Warning);
                return;
            }

            int currentIndex = eventNames.IndexOf(listenerNode.EventType);
            if (currentIndex < 0) currentIndex = 0;

            int newIndex = EditorGUILayout.Popup("Event Type", currentIndex, eventNames.ToArray());

            if (newIndex != currentIndex)
            {
                Undo.RecordObject(listenerNode, "Change Event Type");
                listenerNode.EventType = eventNames[newIndex];
                EditorUtility.SetDirty(listenerNode);
            }
        }
    }
}