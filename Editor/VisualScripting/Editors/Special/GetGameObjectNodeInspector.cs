using UnityEditor;
using FluxFramework.VisualScripting.Node;

namespace FluxFramework.VisualScripting.Editor
{
    // We associate this drawer with the GetGameObjectNode logic
    [CustomNodeInspector(typeof(GetGameObjectNode))]
    public class GetGameObjectNodeInspector
    {
        // A static method is clean and simple for this pattern.
        public static void OnInspectorGUI(SerializedProperty logicProp)
        {
            var modeProp = logicProp.FindPropertyRelative("Mode");
            EditorGUILayout.PropertyField(modeProp);

            var mode = (GetGameObjectNode.FindMode)modeProp.enumValueIndex;

            switch (mode)
            {
                case GetGameObjectNode.FindMode.ByName:
                case GetGameObjectNode.FindMode.ByTag:
                    EditorGUILayout.HelpBox("Connect a string to the 'Identifier' port or set the default value on the node's field if the port is not connected.", MessageType.Info);
                    // We will draw the field itself in a later task (Inline Input Fields)
                    break;

                case GetGameObjectNode.FindMode.DirectReference:
                    var refProp = logicProp.FindPropertyRelative("Reference");
                    EditorGUILayout.PropertyField(refProp);
                    break;

                case GetGameObjectNode.FindMode.ByComponentType:
                    var compProp = logicProp.FindPropertyRelative("ComponentTypeReference");
                    EditorGUILayout.PropertyField(compProp);
                    break;
            }
        }
    }
}