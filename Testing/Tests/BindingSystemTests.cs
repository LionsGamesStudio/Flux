using FluxFramework.Core;
using FluxFramework.Testing.Attributes;
using FluxFramework.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FluxFramework.Testing.Tests
{
    /// <summary>
    /// Integration tests for the UI Binding System using a real FluxUIComponent (FluxImage).
    /// </summary>
    public class BindingSystemTests : FluxTestBase
    {
        private GameObject _testGameObject;

        [FluxTest]
        public void Binding_WhenDataChanges_FluxImageColorIsUpdated()
        {
            // --- ARRANGE ---
            // 1. Crée un environnement de test avec les composants nécessaires.
            _testGameObject = CreateTestGameObject("TestFluxImage");
            var unityImageComponent = _testGameObject.AddComponent<Image>();
            var fluxImageComponent = _testGameObject.AddComponent<FluxImage>();

            // 2. Crée la propriété de données à laquelle on va se lier.
            const string colorKey = "ui.test.imageColor";
            var colorProperty = Manager.Properties.GetOrCreateProperty(colorKey, Color.black);
            
            // 3. Configure le composant FluxImage comme on le ferait dans l'inspecteur.
            SetPrivateField(fluxImageComponent, "_colorPropertyKey", colorKey);
            SetPrivateField(fluxImageComponent, "imageComponent", unityImageComponent);

            // --- ACT ---
            // 4. Utilise le point d'entrée PUBLIC et OFFICIEL pour enregistrer et initialiser le composant.
            //    Ceci va déclencher en interne l'appel à InitializeComponent, RegisterCustomBindings, etc.
            //    dans le bon ordre et de manière sécurisée.
            Manager.Registry.RegisterComponentInstance(fluxImageComponent);
            
            // 5. Modifie la donnée.
            colorProperty.Value = Color.red;

            // --- ASSERT ---
            // 6. Vérifie que le composant UI a bien été mis à jour.
            Assert(unityImageComponent.color == Color.red, 
                $"The Image component's color should have been updated to red by the binding. Actual color: {unityImageComponent.color}");
        }
        
        // --- Helper Method ---
        
        /// <summary>
        /// A helper to set the value of a private field on an object using reflection.
        /// </summary>
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                throw new System.ArgumentException($"Field '{fieldName}' not found on object of type '{obj.GetType().Name}'.");
            }
        }
    }
}