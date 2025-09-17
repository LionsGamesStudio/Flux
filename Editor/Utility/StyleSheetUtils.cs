using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;

namespace FluxFramework.Editor
{
    public static class StyleSheetUtils
    {
        /// <summary>
        /// Adds a StyleSheet from the Resources folder to a VisualElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="styleSheetName"></param>
        private static void AddStyleSheet(this VisualElement element, string styleSheetName)
        {
            var styleSheet = Resources.Load<StyleSheet>(styleSheetName);
            if (styleSheet != null)
            {
                element.styleSheets.Add(styleSheet);
            }
            else
            {
                Debug.LogWarning($"StyleSheet '{styleSheetName}' not found in Resources.");
            }
        }

        /// <summary>
        /// Removes a StyleSheet from the Resources folder from a VisualElement.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="styleSheetName"></param>
        private static void RemoveStyleSheet(this VisualElement element, string styleSheetName)
        {
            var styleSheet = Resources.Load<StyleSheet>(styleSheetName);
            if (styleSheet != null && element.styleSheets.Contains(styleSheet))
            {
                element.styleSheets.Remove(styleSheet);
            }
        } 

        /// <summary>
        /// Adds a StyleSheet to a VisualElement from either the Resources folder or a relative path in the project.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="relativePath"></param>
        /// <param name="fromResources"></param>
        public static void AddStyleSheet(this VisualElement element, string relativePath, bool fromResources)
        {
            if (fromResources)
            {
                AddStyleSheet(element, relativePath);
            }
            else
            {
                string fullPath = FluxEditorPaths.GetFullPath(relativePath);
                if (string.IsNullOrEmpty(fullPath))
                {
                    Debug.LogError($"Could not determine full path for stylesheet: {relativePath}");
                    return;
                }

                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(fullPath);
                if (styleSheet != null)
                {
                    element.styleSheets.Add(styleSheet);
                }
                else
                {
                    Debug.LogWarning($"Could not load stylesheet at path: {fullPath}");
                }
            }
        }

        /// <summary>
        /// Removes a StyleSheet from a VisualElement from either the Resources folder or a relative path in the project.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="relativePath"></param>
        /// <param name="fromResources"></param>
        public static void RemoveStyleSheet(this VisualElement element, string relativePath, bool fromResources)
        {
            if (fromResources)
            {
                RemoveStyleSheet(element, relativePath);
            }
            else
            {
                string fullPath = FluxEditorPaths.GetFullPath(relativePath);
                if (string.IsNullOrEmpty(fullPath))
                {
                    Debug.LogError($"Could not determine full path for stylesheet: {relativePath}");
                    return;
                }

                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(fullPath);
                if (styleSheet != null && element.styleSheets.Contains(styleSheet))
                {
                    element.styleSheets.Remove(styleSheet);
                }
            }
        }

        /// <summary>
        /// Clears all StyleSheets from a VisualElement.
        /// </summary>
        /// <param name="element"></param>
        public static void ClearStyleSheets(this VisualElement element)
        {
            element.styleSheets.Clear();
        }

        /// <summary>
        /// Gets a StyleSheet by name from a VisualElement. Returns null if not found.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="styleSheetName"></param>
        /// <returns></returns>
        public static StyleSheet GetStyleSheet(this VisualElement element, string styleSheetName)
        {
            for (int i = 0; i < element.styleSheets.count; i++)
            {
                var sheet = element.styleSheets[i];
                if (sheet != null && sheet.name == styleSheetName)
                    return sheet;
            }
            return null;
        }

        public static void AddClass(this VisualElement element, string className)
        {
            if (!element.ClassListContains(className))
            {
                element.AddToClassList(className);
            }
        }

        public static void RemoveClass(this VisualElement element, string className)
        {
            if (element.ClassListContains(className))
            {
                element.RemoveFromClassList(className);
            }
        }

        public static bool HasClass(this VisualElement element, string className)
        {
            return element.ClassListContains(className);
        }

        /// <summary>
        /// Loads a StyleSheet from a relative path
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public static bool LoadStyleSheet(this VisualElement element, string relativePath)
        {
            string fullPath = FluxEditorPaths.GetFullPath(relativePath);
            if (string.IsNullOrEmpty(fullPath))
            {
                Debug.LogError($"[FluxGraphView] Could not determine full path for stylesheet: {relativePath}");
                return false;
            }

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(fullPath);
            if (styleSheet != null)
            {
                element.styleSheets.Add(styleSheet);
                Debug.Log($"[FluxGraphView] Successfully loaded stylesheet: {relativePath}");
                return true;
            }
            else
            {
                Debug.LogWarning($"[FluxGraphView] Could not load stylesheet at path: {fullPath}");
                return false;
            }
        }
    }
    
}