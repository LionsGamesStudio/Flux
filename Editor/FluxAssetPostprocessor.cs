using UnityEditor;
using UnityEngine;
using FluxFramework.Core;
using System.Linq;

namespace FluxFramework.Editor
{
    /// <summary>
    /// This editor class automatically listens for changes to project assets.
    /// Its sole purpose is to intelligently detect when the list of FluxScriptableObjects
    /// might have changed, and if so, mark our cache as "dirty" so it can be rebuilt.
    /// </summary>
    public class FluxAssetPostprocessor : AssetPostprocessor
    {
        /// <summary>
        /// This method is called by Unity after any assets are imported, moved, or deleted.
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            bool needsRebuild = false;

            // --- OPTIMIZATION 1: More Precise Detection ---
            // We check imported and moved assets first, as we can determine their type accurately.
            // We combine them using LINQ for cleaner code.
            foreach (string path in importedAssets.Concat(movedAssets))
            {
                // Is the modified asset a FluxScriptableObject?
                if (IsFluxScriptableObjectAsset(path))
                {
                    needsRebuild = true;
                    break; // Found a reason to rebuild, no need to check further.
                }
            }

            // If we haven't already decided to rebuild, check the deleted assets.
            // For deleted assets, we can't get their type anymore, so we make an educated guess.
            // If any file ending in ".asset" was deleted, we play it safe and rebuild.
            if (!needsRebuild)
            {
                // We combine all paths that represent a removed location.
                foreach (string path in deletedAssets.Concat(movedFromAssetPaths))
                {
                    if (path.EndsWith(".asset"))
                    {
                        needsRebuild = true;
                        break; // A ScriptableObject was likely removed, so we must rebuild.
                    }
                }
            }
            
            if (needsRebuild)
            {
                // --- OPTIMIZATION 2: Centralized Cache Access ---
                // We no longer have a duplicate GetOrCreateCache method here.
                // We call the public method on the registry, which is the single source of truth.
                // This makes the code much easier to maintain.
                var cache = FluxScriptableObjectRegistry.GetOrCreateCache();
                
                if (cache != null && !cache.isCacheDirty)
                {
                    Debug.Log("[FluxFramework] Asset changes detected. Invalidating FluxScriptableObject cache.");
                    cache.isCacheDirty = true;
                    EditorUtility.SetDirty(cache); // Important: Mark the asset as changed so Unity saves it.
                }
            }
        }

        /// <summary>
        /// A highly optimized helper method to check if an asset at a given path is a FluxScriptableObject.
        /// </summary>
        /// <param name="path">The asset path (e.g., "Assets/MyData/PlayerData.asset")</param>
        /// <returns>True if the asset is a type that inherits from FluxScriptableObject.</returns>
        private static bool IsFluxScriptableObjectAsset(string path)
        {
            // A quick filter to ignore non-asset files immediately.
            if (!path.EndsWith(".asset"))
            {
                return false;
            }

            // --- OPTIMIZATION 3: Faster Type Checking ---
            // Instead of using AssetDatabase.LoadAssetAtPath<T>(), which loads the whole file into memory,
            // we use GetMainAssetTypeAtPath(). This is much faster as it only reads the asset's metadata.
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(path);

            // Check if the type exists and if it is, or inherits from, FluxScriptableObject.
            return assetType != null && typeof(FluxScriptableObject).IsAssignableFrom(assetType);
        }
    }
}