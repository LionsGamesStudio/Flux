using System.Collections;
using System.Text;

namespace FluxFramework.Editor.Utils
{
    public static class EditorDebugUtils
    {
        /// <summary>
        /// Formats any object into a human-readable string for editor displays.
        /// It's especially useful for collections.
        /// </summary>
        /// <param name="value">The object to format.</param>
        /// <param name="maxItems">The maximum number of collection items to show in the preview.</param>
        /// <returns>A formatted string representation of the value.</returns>
        public static string ToPrettyString(object value, int maxItems = 5)
        {
            if (value == null)
            {
                return "null";
            }

            // For collections (List, Dictionary, Array, etc.) but not strings
            if (value is IEnumerable collection && value is not string)
            {
                var sb = new StringBuilder();
                int count = 0;

                sb.Append($"[{value.GetType().Name}] (");

                foreach (var item in collection)
                {
                    count++;
                }

                sb.Append($"Count: {count}) {{ ");

                int currentItem = 0;
                foreach (var item in collection)
                {
                    if (currentItem >= maxItems)
                    {
                        sb.Append("...");
                        break;
                    }
                    sb.Append(item?.ToString() ?? "null");
                    sb.Append(", ");
                    currentItem++;
                }

                if (count > 0 && sb.Length > 2)
                {
                    sb.Length -= 2; // Remove the trailing ", "
                }

                sb.Append(" }");
                return sb.ToString();
            }

            // For all other types
            return value.ToString();
        }
    }
}