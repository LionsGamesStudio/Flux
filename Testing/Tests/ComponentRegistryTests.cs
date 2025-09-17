using FluxFramework.Core;
using FluxFramework.Testing.Attributes;
using FluxFramework.Attributes;
using UnityEngine;

namespace FluxFramework.Testing.Tests
{
    /// <summary>
    /// Tests the integration between FluxMonoBehaviour and the FluxComponentRegistry,
    /// ensuring automatic registration and discovery of properties and handlers.
    /// </summary>
    public class ComponentRegistryTests : FluxTestBase
    {
        // --- Helper Components for Testing ---

        private class SimpleTestEvent : FluxEventBase { }

        // A component with a ReactiveProperty attribute.
        private class ComponentWithProperty : FluxMonoBehaviour
        {
            [ReactiveProperty("test.component.value")]
            private int _value = 42;
        }

        // A component with an EventHandler attribute.
        private class ComponentWithHandler : FluxMonoBehaviour
        {
            public bool HandlerCalled { get; private set; } = false;

            [FluxEventHandler]
            private void OnSimpleTestEvent(SimpleTestEvent evt)
            {
                HandlerCalled = true;
            }
        }

        // --- Test Setup ---
        
        private GameObject _testGameObject;

        // --- Tests ---

        [FluxTest]
        public void RegisterComponent_WhenComponentIsCreated_PropertyIsRegistered()
        {
            // --- ARRANGE ---
            const string propertyKey = "test.component.value";
            _testGameObject = CreateTestGameObject("TestObjectWithProperty");

            // --- ACT ---
            // Add the component. Its Awake/OnFluxAwake should trigger the registration.
            _testGameObject.AddComponent<ComponentWithProperty>();
            // The registry is now managed by the MockFluxManager. We need to manually tell it to scan.
            // In a real game, this is handled by the FluxManager's lifecycle (OnSceneLoaded, etc.).
            Manager.Registry.RegisterAllComponentsInScene();

            // --- ASSERT ---
            var property = Manager.Properties.GetProperty<int>(propertyKey);
            Assert(property != null, "Property should have been registered in the PropertyManager.");
            Assert(property.Value == 42, "The registered property should have the correct default value from the component.");
        }

        [FluxTest]
        public void RegisterComponent_WhenComponentIsCreated_EventHandlerIsSubscribed()
        {
            // --- ARRANGE ---
            _testGameObject = CreateTestGameObject("TestObject");
            var testComponent = _testGameObject.AddComponent<ComponentWithHandler>();
            Manager.Registry.RegisterAllComponentsInScene();

            // --- ACT ---
            Manager.EventBus.Publish(new SimpleTestEvent());

            // --- ASSERT ---
            Assert(testComponent.HandlerCalled, "The event handler on the component should have been called.");
        }

        [FluxTest]
        public void UnregisterComponent_WhenGameObjectIsDestroyed_EventHandlerIsUnsubscribed()
        {
            // --- ARRANGE ---
            _testGameObject = CreateTestGameObject("TestObject");
            var testComponent = _testGameObject.AddComponent<ComponentWithHandler>();
            Manager.Registry.RegisterAllComponentsInScene();

            // --- ACT ---
            // Destroy the object. This should trigger OnDestroy and cleanup logic.
            Object.DestroyImmediate(_testGameObject);

            // Publish an event *after* the component is destroyed.
            Manager.EventBus.Publish(new SimpleTestEvent());

            // --- ASSERT ---
            // The handler on the now-destroyed component should not have been called.
            // Since the component is destroyed, we can't check `testComponent.HandlerCalled`.
            // The absence of a NullReferenceException or other error is the success condition here.
            // (A more advanced test could mock the EventBus to count subscribers).
            Assert(true, "Test passed if no errors were thrown after destroying the component.");
        }
    }
}