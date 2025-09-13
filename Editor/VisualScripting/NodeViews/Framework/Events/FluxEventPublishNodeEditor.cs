using UnityEditor;
using FluxFramework.VisualScripting.Nodes;

namespace FluxFramework.VisualScripting.Editor
{
    [CustomEditor(typeof(FluxEventPublishNode))]
    public class FluxEventPublishNodeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); // Draw the default inspector first

            var publishNode = (FluxEventPublishNode)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Event Type Selection", EditorStyles.boldLabel);
            
            var eventNames = FluxEventDefinitionUtility.GetDefinedEventNames();
            eventNames.Insert(0, "[Generic Event]"); // Option to publish a generic event

            if (eventNames.Count <= 1)
            {
                EditorGUILayout.HelpBox("No event definitions found. Create a FluxEventDefinitions asset and scan your project.", MessageType.Warning);
            }

            int currentIndex = eventNames.IndexOf(publishNode.EventType);
            
            // If the current type is empty, it means we are using a generic event
            if (string.IsNullOrEmpty(publishNode.EventType))
            {
                currentIndex = 0;
            }
            
            if (currentIndex < 0) currentIndex = 0; // Default to generic

            int newIndex = EditorGUILayout.Popup("Event Type", currentIndex, eventNames.ToArray());

            if (newIndex != currentIndex)
            {
                Undo.RecordObject(publishNode, "Change Event Type");
                // If index is 0, we set the type to empty string to signify a generic event
                publishNode.EventType = (newIndex == 0) ? "" : eventNames[newIndex];
                EditorUtility.SetDirty(publishNode);
            }
        }
    }
}