using System.IO;
using UnityEditor;
using UnityEngine;

namespace FluxFramework.Editor
{
    /// <summary>
    /// Provides utility methods for finding asset paths, especially within a Unity Package context.
    /// </summary>
    public static class FluxEditorPaths
    {
        private static string _packageRootPath;

        /// <summary>
        /// Gets the root path of the Flux Framework package. The result is cached for performance.
        /// Example: "Packages/com.mycompany.fluxframework"
        /// </summary>
        public static string GetPackageRootPath()
        {
            if (string.IsNullOrEmpty(_packageRootPath))
            {
                // Find a known script from the editor assembly to locate the package path.
                // Using a ScriptableObject type is reliable.
                var scriptAsset = MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<VisualScripting.Editor.FluxSearchWindowProvider>());
                string scriptPath = AssetDatabase.GetAssetPath(scriptAsset);
                
                if (string.IsNullOrEmpty(scriptPath))
                {
                    Debug.LogError("[FluxEditorPaths] Could not determine the package path. A known script could not be found.");
                    return null;
                }
                
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(scriptPath);
                if (packageInfo != null)
                {
                    _packageRootPath = packageInfo.assetPath;
                }
                else
                {
                    // Fallback for cases where the asset is not in a package (e.g., direct in Assets/)
                    _packageRootPath = "Assets/FluxFramework"; // Adjust this fallback path if needed
                    if (!Directory.Exists(_packageRootPath))
                    {
                        Debug.LogError("[FluxEditorPaths] Could not find package info and fallback path does not exist.");
                        return null;
                    }
                }
            }
            return _packageRootPath;
        }

        /// <summary>
        /// Constructs a full, relative path to a resource within the Flux Framework package.
        /// </summary>
        /// <param name="relativePathInPackage">The path to the resource relative to the package root. Example: "Editor/Styles/MyStyle.uss"</param>
        /// <returns>A full path usable by AssetDatabase. Example: "Packages/com.mycompany.fluxframework/Editor/Styles/MyStyle.uss"</returns>
        public static string GetFullPath(string relativePathInPackage)
        {
            var root = GetPackageRootPath();
            return string.IsNullOrEmpty(root) ? null : Path.Combine(root, relativePathInPackage).Replace("\\", "/");
        }
    }
}