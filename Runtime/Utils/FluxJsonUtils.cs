using Newtonsoft.Json;
using System;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;
using FluxFramework.Core;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FluxFramework.Utils
{
    /// <summary>
    /// A centralized utility class for JSON serialization and deserialization using Newtonsoft.Json.
    /// It comes pre-configured with custom converters for common Unity types.
    /// </summary>
    public static class FluxJsonUtils
    {
        private static readonly JsonSerializerSettings _settings = new JsonSerializerSettings
        {
            // This setting allows deserializing into the correct derived types if your data uses polymorphism.
            TypeNameHandling = TypeNameHandling.Auto,

            // Add all our custom converters for Unity types.
            Converters = new JsonConverter[]
            {
                new Vector3Converter(),
                new Vector2Converter(),
                new QuaternionConverter(),
                new ColorConverter(),
                new GameObjectConverter()
            }
        };

        /// <summary>
        /// Serializes an object to a JSON string using pre-configured settings.
        /// </summary>
        public static string Serialize(object value)
        {
            return JsonConvert.SerializeObject(value, _settings);
        }

        /// <summary>
        /// Deserializes a JSON string to an object of the specified type using pre-configured settings.
        /// </summary>
        public static object Deserialize(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type, _settings);
        }

        public static void AddConverter(JsonConverter converter)
        {
            if (converter != null && !_settings.Converters.Contains(converter))
            {
                _settings.Converters.Add(converter);
            }
        }
    }

    // --- Custom Converters for Unity Types ---

    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WriteEndObject();
        }

        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject obj = JObject.Load(reader);
                return new Vector3(obj["x"].Value<float>(), obj["y"].Value<float>(), obj["z"].Value<float>());
            }
            return Vector3.zero;
        }
    }

    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WriteEndObject();
        }

        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject obj = JObject.Load(reader);
                return new Vector2(obj["x"].Value<float>(), obj["y"].Value<float>());
            }
            return Vector2.zero;
        }
    }

    public class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.x);
            writer.WritePropertyName("y");
            writer.WriteValue(value.y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.z);
            writer.WritePropertyName("w");
            writer.WriteValue(value.w);
            writer.WriteEndObject();
        }

        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                JObject obj = JObject.Load(reader);
                return new Quaternion(obj["x"].Value<float>(), obj["y"].Value<float>(), obj["z"].Value<float>(), obj["w"].Value<float>());
            }
            return Quaternion.identity;
        }
    }

    public class ColorConverter : JsonConverter<Color>
    {
        public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
        {
            // Store as a hex string for readability and precision.
            writer.WriteValue("#" + ColorUtility.ToHtmlStringRGBA(value));
        }

        public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                if (ColorUtility.TryParseHtmlString((string)reader.Value, out Color color))
                {
                    return color;
                }
            }
            return Color.white;
        }
    }

    /// <summary>
    /// Serializes a GameObject by storing a reference to it.
    /// - If it's a prefab, it stores the asset path.
    /// - If it's a scene object, it stores the unique ID from a FluxIdentifier component.
    /// </summary>
    public class GameObjectConverter : JsonConverter<GameObject>
    {
        private const string TypeKey = "type";
        private const string PathKey = "path";
        private const string GuidKey = "guid";

        public override void WriteJson(JsonWriter writer, GameObject value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();

#if UNITY_EDITOR
            // --- Serialization logic ---
            // Case 1: It's a prefab.
            if (PrefabUtility.IsPartOfPrefabAsset(value))
            {
                string path = AssetDatabase.GetAssetPath(value);
                writer.WritePropertyName(TypeKey);
                writer.WriteValue("prefab");
                writer.WritePropertyName(PathKey);
                writer.WriteValue(path);
            }
            // Case 2: It's a scene instance with an identifier.
            else if (value.TryGetComponent<FluxIdentifier>(out var identifier))
            {
                writer.WritePropertyName(TypeKey);
                writer.WriteValue("sceneObject");
                writer.WritePropertyName(GuidKey);
                writer.WriteValue(identifier.Id);
            }
            // Case 3: It's a scene object without an identifier (cannot be reliably saved).
            else
            {
                Debug.LogWarning($"Cannot serialize GameObject '{value.name}' because it is not a prefab and does not have a FluxIdentifier component. It will be saved as null.", value);
                // We write an empty object to signify that saving failed.
            }
#else
            // In builds, we cannot check if it's a prefab. We rely only on the identifier.
            if (value.TryGetComponent<FluxIdentifier>(out var identifier))
            {
                writer.WritePropertyName(TypeKey);
                writer.WriteValue("sceneObject");
                writer.WritePropertyName(GuidKey);
                writer.WriteValue(identifier.Id);
            }
            else
            {
                Debug.LogWarning($"Cannot serialize GameObject '{value.name}' because it does not have a FluxIdentifier component. It will be saved as null.", value);
            }
#endif

            writer.WriteEndObject();
        }

        public override GameObject ReadJson(JsonReader reader, Type objectType, GameObject existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject obj = JObject.Load(reader);
            string type = obj[TypeKey]?.Value<string>();

            if (type == "prefab")
            {
#if UNITY_EDITOR
                string path = obj[PathKey]?.Value<string>();
                if (!string.IsNullOrEmpty(path))
                {
                    return AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
#else
                // In builds, `Resources.Load` is the standard method. The path must be relative to a "Resources" folder.
                // Note: This requires a project convention from the user.
                string path = obj[PathKey]?.Value<string>();
                if (!string.IsNullOrEmpty(path))
                {
                    // Remove "Assets/Resources/" and the ".prefab" extension
                    if (path.StartsWith("Assets/Resources/"))
                    {
                        path = path.Substring("Assets/Resources/".Length);
                    }
                    if (path.EndsWith(".prefab"))
                    {
                        path = path.Substring(0, path.Length - ".prefab".Length);
                    }
                    return Resources.Load<GameObject>(path);
                }
#endif
            }
            else if (type == "sceneObject")
            {
                string guid = obj[GuidKey]?.Value<string>();
                if (!string.IsNullOrEmpty(guid))
                {
                    // Find the FluxIdentifier component with the matching ID across the scene.
                    // This is potentially slow; use sparingly.
                    return GameObject.FindObjectsOfType<FluxIdentifier>()
                        .FirstOrDefault(i => i.Id == guid)?.gameObject;
                }
            }

            return null;
        }
    }
}