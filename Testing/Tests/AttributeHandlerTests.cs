using FluxFramework.Core;
using FluxFramework.Testing.Attributes;
using FluxFramework.Attributes;
using UnityEngine;

namespace FluxFramework.Testing.Tests
{
    /// <summary>
    /// Tests for attribute-based handlers like [FluxPropertyChangeHandler].
    /// </summary>
    public class AttributeHandlerTests : FluxTestBase
    {
        // --- Helper Component ---
        
        private class ComponentWithPropertyHandler : FluxMonoBehaviour
        {
            public int ChangeCount { get; private set; } = 0;
            public float LastReceivedValue { get; private set; } = -1f;
            public float OldValue { get; private set; } = -1f;

            // Handler with one parameter (new value)
            [FluxPropertyChangeHandler("test.handler.value")]
            private void OnValueChanged(float newValue)
            {
                LastReceivedValue = newValue;
                ChangeCount++;
            }

            // Handler with two parameters (old and new value)
            [FluxPropertyChangeHandler("test.handler.valueWithOld")]
            private void OnValueWithOldChanged(float oldValue, float newValue)
            {
                OldValue = oldValue;
            }
        }
        
        private GameObject _testGameObject;
        
        // --- Tests ---

        [FluxTest]
        public void PropertyChangeHandler_WhenPropertyChanges_HandlerIsCalledWithNewValue()
        {
            // --- ARRANGE ---
            _testGameObject = CreateTestGameObject("TestObjectWithProperty");
            var testComponent = _testGameObject.AddComponent<ComponentWithPropertyHandler>();
            var property = Manager.Properties.GetOrCreateProperty("test.handler.value", 100f);
            
            Manager.Registry.RegisterComponentInstance(testComponent);

            // --- ACT ---
            property.Value = 200f;

            // --- ASSERT ---
            Assert(testComponent.ChangeCount == 2, "Handler should have been called twice (once on subscribe, once on change).");
            Assert(testComponent.LastReceivedValue == 200f, "Handler did not receive the correct new value.");
        }
        
        [FluxTest]
        public void PropertyChangeHandler_WithTwoParams_ReceivesOldAndNewValue()
        {
            // --- ARRANGE ---
            _testGameObject = CreateTestGameObject("TestObjectWithProperty");
            var testComponent = _testGameObject.AddComponent<ComponentWithPropertyHandler>();
            var property = Manager.Properties.GetOrCreateProperty("test.handler.valueWithOld", 50f);
            
            Manager.Registry.RegisterComponentInstance(testComponent);

            // --- ACT ---
            property.Value = 75f;

            // --- ASSERT ---
            Assert(testComponent.OldValue == 50f, "Handler did not receive the correct old value.");
        }
    }
}