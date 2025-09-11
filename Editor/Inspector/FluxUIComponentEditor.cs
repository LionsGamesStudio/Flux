#if UNITY_EDITOR
using UnityEditor;
using FluxFramework.UI;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Custom inspector for the FluxUIComponent base class and its children.
    /// It inherits all the functionality from the base MonoBehaviour editor.
    /// </summary>
    [CustomEditor(typeof(FluxUIComponent), true)]
    public class FluxUIComponentEditor : FluxComponentEditor
    {
        
    }
}
#endif

  