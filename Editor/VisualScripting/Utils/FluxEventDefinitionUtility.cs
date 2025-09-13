using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using FluxFramework.Configuration;

namespace FluxFramework.VisualScripting.Editor
{
    /// <summary>
    /// A utility class for editor scripts to easily access a list of all defined event names.
    /// It finds and caches the event names from all FluxEventDefinitions assets in the project.
    /// </summary>
    public static class FluxEventDefinitionUtility
    {
        private static List<string> _eventNames;
        private static bool _isLoaded = false;

        public static List<string> GetDefinedEventNames()
        {
            if (!_isLoaded)
            {
                LoadEventNames();
            }
            return _eventNames;
        }

        public static void Reload()
        {
            _isLoaded = false;
            LoadEventNames();
        }

        private static void LoadEventNames()
        {
            var nameSet = new HashSet<string>();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(FluxEventDefinitions)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<FluxEventDefinitions>(path);
                if (asset != null)
                {
                    foreach (var def in asset.events)
                    {
                        nameSet.Add(def.eventName);
                    }
                }
            }

            _eventNames = nameSet.OrderBy(name => name).ToList();
            _isLoaded = true;
        }
    }
}