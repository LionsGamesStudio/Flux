using FluxFramework.Core;
using FluxFramework.Testing.Attributes;

namespace FluxFramework.Testing.Tests
{
    /// <summary>
    // Tests for the core functionalities of the FluxPropertyManager.
    /// </summary>
    public class PropertyManagerTests : FluxTestBase
    {
        [FluxTest]
        public void RegisterAndGetProperty_WhenPropertyIsRegistered_ShouldReturnSameInstance()
        {
            // --- ARRANGE ---
            // The Manager is provided by FluxTestBase.
            const string testKey = "test.health";
            var originalProperty = new ReactiveProperty<int>(100);

            // --- ACT ---
            Manager.Properties.RegisterProperty(testKey, originalProperty, isPersistent: false);
            var retrievedProperty = Manager.Properties.GetProperty<int>(testKey);

            // --- ASSERT ---
            Assert(retrievedProperty != null, "Retrieved property should not be null.");
            Assert(retrievedProperty == originalProperty, "Retrieved property should be the exact same instance that was registered.");
        }

        [FluxTest]
        public void GetOrCreateProperty_WhenPropertyDoesNotExist_ShouldCreateAndRegisterIt()
        {
            // --- ARRANGE ---
            const string testKey = "test.playerName";
            const string defaultValue = "Gemini";

            // --- ACT ---
            // Call GetOrCreateProperty on a key that has not been registered yet.
            var createdProperty = Manager.Properties.GetOrCreateProperty(testKey, defaultValue);

            // --- ASSERT ---
            Assert(createdProperty != null, "GetOrCreateProperty should return a non-null property.");
            Assert(createdProperty.Value == defaultValue, $"The new property's value should be '{defaultValue}'.");
            Assert(Manager.Properties.HasProperty(testKey), "The manager should now contain the newly created property.");
        }
        
        [FluxTest]
        public void GetOrCreateProperty_WhenPropertyAlreadyExists_ShouldReturnExistingInstance()
        {
            // --- ARRANGE ---
            const string testKey = "test.score";
            var originalProperty = Manager.Properties.GetOrCreateProperty(testKey, 1000);

            // --- ACT ---
            // Call GetOrCreateProperty again with the same key.
            var retrievedProperty = Manager.Properties.GetOrCreateProperty(testKey, 9999); // Use a different default value

            // --- ASSERT ---
            Assert(retrievedProperty == originalProperty, "Should return the existing property instance.");
            Assert(retrievedProperty.Value == 1000, "The property's value should remain the original value, not the new default.");
        }
        
        [FluxTest]
        public void UnregisterProperty_WhenPropertyExists_ShouldRemoveIt()
        {
            // --- ARRANGE ---
            const string testKey = "test.ammo";
            Manager.Properties.RegisterProperty(testKey, new ReactiveProperty<int>(30), isPersistent: false);
            bool existedBefore = Manager.Properties.HasProperty(testKey);

            // --- ACT ---
            bool wasUnregistered = Manager.Properties.UnregisterProperty(testKey);

            // --- ASSERT ---
            Assert(existedBefore, "The property should exist before being unregistered.");
            Assert(wasUnregistered, "UnregisterProperty should return true for a successful removal.");
            Assert(!Manager.Properties.HasProperty(testKey), "The property should no longer exist after being unregistered.");
        }
    }
}