using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// A temporary ScriptableObject used as a target for the Unity inspector
    /// to display and edit the properties of a FluxNodeConnection.
    /// </summary>
    public class ConnectionProxy : ScriptableObject
    {
        // We use reflection to avoid a hard dependency on the runtime connection class.
        public object targetConnection;
        private FieldInfo _durationField;

        public void Initialize(object connection)
        {
            targetConnection = connection;
            _durationField = connection.GetType().GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public float Duration
        {
            get => (float)(_durationField?.GetValue(targetConnection) ?? 0f);
            set => _durationField?.SetValue(targetConnection, value);
        }
    }

    /// <summary>
    /// The custom editor that draws the inspector for our ConnectionProxy.
    /// </summary>
    [CustomEditor(typeof(ConnectionProxy))]
    public class ConnectionProxyEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var proxy = target as ConnectionProxy;
            if (proxy?.targetConnection == null) return;
            
            var so = new SerializedObject(proxy);
            var durationProp = so.FindProperty("Duration");

            EditorGUILayout.LabelField("Connection Settings", EditorStyles.boldLabel);
                
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(durationProp, new GUIContent("Duration (s)", "..."));
            
            if (EditorGUI.EndChangeCheck())
            {
                so.ApplyModifiedProperties();
            }
        }
    }
}