using UnityEditor;
using UnityEngine;
using FluxFramework.Core;
using System.Diagnostics;
using System.IO;

namespace FluxFramework.Editor
{
    [InitializeOnLoad]
    public static class FluxScriptableObjectRegistry
    {
        private static FluxScriptableObjectCache _cache;
        private const string CacheAssetPath = "Assets/Editor/Resources/Cache/FluxSOCache.asset";

        static FluxScriptableObjectRegistry()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                InitializeAllFluxScriptableObjects();
            }
        }
        
        [MenuItem("Flux/Tools/Rebuild ScriptableObject Cache", false, 100)]
        public static void RebuildCacheMenu()
        {
            RebuildCache();
            UnityEngine.Debug.Log("[FluxFramework] ScriptableObject cache rebuilt manually.");
        }

        private static void InitializeAllFluxScriptableObjects()
        {
            if (Flux.Manager == null || !Flux.Manager.IsInitialized)
            {
                UnityEngine.Debug.LogWarning("[FluxFramework] Manager not ready. Deferring SO initialization.");
                return;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _cache = GetOrCreateCache();

            if (_cache.isCacheDirty)
            {
                UnityEngine.Debug.Log("[FluxFramework] Cache is dirty, rebuilding...");
                RebuildCache();
            }

            int initializedCount = 0;
            foreach (string guid in _cache.fluxScriptableObjectGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;

                var so = AssetDatabase.LoadAssetAtPath<FluxScriptableObject>(path);

                if (so != null)
                {
                    so.InitializeReactiveProperties(Flux.Manager);
                    initializedCount++;
                }
            }
            
            stopwatch.Stop();
            UnityEngine.Debug.Log($"<color=cyan>[FluxFramework] Initialized {initializedCount} FluxScriptableObjects in {stopwatch.ElapsedMilliseconds}ms (from cache).</color>");
        }

        private static void RebuildCache()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            _cache = GetOrCreateCache();
            _cache.fluxScriptableObjectGUIDs.Clear();

            string[] guids = AssetDatabase.FindAssets("t:FluxScriptableObject");
            _cache.fluxScriptableObjectGUIDs.AddRange(guids);

            _cache.isCacheDirty = false;
            EditorUtility.SetDirty(_cache);
            AssetDatabase.SaveAssets();
            
            stopwatch.Stop();
            UnityEngine.Debug.Log($"[FluxFramework] Cache rebuilt with {_cache.fluxScriptableObjectGUIDs.Count} entries in {stopwatch.ElapsedMilliseconds}ms.");
        }

        public static FluxScriptableObjectCache GetOrCreateCache()
        {
            // First, try loading from resources, which is fastest.
            // Note: Resources.Load path is relative to a "Resources" folder and omits the extension.
            string resourcePath = Path.Combine(Path.GetDirectoryName(CacheAssetPath).Replace("Assets/Editor/Resources/", ""), Path.GetFileNameWithoutExtension(CacheAssetPath));
            var cache = Resources.Load<FluxScriptableObjectCache>(resourcePath);
            if (cache != null) return cache;
            
            // If not found, search the whole project (slower, but a reliable fallback).
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FluxScriptableObjectCache)}");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<FluxScriptableObjectCache>(path);
            }
            else
            {
                // If it doesn't exist anywhere, create it.
                var newCache = ScriptableObject.CreateInstance<FluxScriptableObjectCache>();
                
                // Ensure the full directory path exists before creating the asset.
                EnsureDirectoryExists(CacheAssetPath);

                AssetDatabase.CreateAsset(newCache, CacheAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return newCache;
            }
        }

        /// <summary>
        /// A helper function that ensures a directory path exists by creating all missing folders.
        /// </summary>
        /// <param name="assetPath">The full path to the asset, e.g., "Assets/Editor/Some/Folder/MyAsset.asset"</param>
        private static void EnsureDirectoryExists(string assetPath)
        {
            string directory = Path.GetDirectoryName(assetPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}