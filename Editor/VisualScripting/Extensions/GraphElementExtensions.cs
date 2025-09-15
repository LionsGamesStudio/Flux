using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using System.Collections;
using UnityEngine.UIElements;

namespace FluxFramework.VisualScripting.Editor
{
    public static class GraphElementExtensions
    {
        public static void Flash(this GraphElement element, Color color)
        {
            // Store the original color so we can restore it.
            var originalColor = element.style.backgroundColor;
            
            // 1. Immediately change the color.
            element.style.backgroundColor = color;
            
            // 2. Schedule an action to be executed in the future.
            // This is the UI Toolkit equivalent of a delayed call or a short coroutine.
            element.schedule.Execute(() => 
            {
                // This code will run after 150 milliseconds.
                element.style.backgroundColor = originalColor;
                
            }).StartingIn(150); // Delay in milliseconds.
        }

        private static IEnumerator FlashCoroutine(GraphElement element, Color flashColor)
        {
            var originalColor = element.style.backgroundColor;
            element.style.backgroundColor = flashColor;
            
            yield return new WaitForSeconds(0.15f); // Duration of the flash
            
            element.style.backgroundColor = originalColor;
        }
    }
}