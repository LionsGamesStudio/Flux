using FluxFramework.Core;
using FluxFramework.Testing.Attributes;
using System;
using System.Collections.Generic;

namespace FluxFramework.Testing.Tests
{
    /// <summary>
    /// Tests for the core functionalities of the EventBus.
    /// </summary>
    public class EventBusTests : FluxTestBase
    {
        // --- Helper event classes for testing ---
        private class TestEventA : FluxEventBase {
            public int Value { get; }
            public TestEventA(int value = 0) { Value = value; }
        }
        private class TestEventB : FluxEventBase { }

        // --- Tests ---

        [FluxTest]
        public void SubscribeAndPublish_WhenSubscribed_HandlerIsCalled()
        {
            // --- ARRANGE ---
            bool handlerCalled = false;
            Manager.EventBus.Subscribe<TestEventA>(evt => handlerCalled = true);

            // --- ACT ---
            Manager.EventBus.Publish(new TestEventA());

            // --- ASSERT ---
            Assert(handlerCalled, "The event handler should have been called after publishing the event.");
        }

        [FluxTest]
        public void Publish_HandlerReceivesCorrectData()
        {
            // --- ARRANGE ---
            int receivedValue = -1; // Start with an invalid value
            Manager.EventBus.Subscribe<TestEventA>(evt => receivedValue = evt.Value);

            // --- ACT ---
            Manager.EventBus.Publish(new TestEventA(42));

            // --- ASSERT ---
            Assert(receivedValue == 42, "The event handler did not receive the correct data from the event. Expected 42, but got " + receivedValue);
        }

        [FluxTest]
        public void Unsubscribe_ViaDispose_HandlerIsNotCalled()
        {
            // --- ARRANGE ---
            bool handlerCalled = false;
            var subscription = Manager.EventBus.Subscribe<TestEventA>(evt => handlerCalled = true);

            // --- ACT ---
            subscription.Dispose(); // Unsubscribe immediately
            Manager.EventBus.Publish(new TestEventA()); // Publish the event *after* unsubscribing

            // --- ASSERT ---
            Assert(!handlerCalled, "The event handler should NOT be called after its subscription has been disposed.");
        }

        [FluxTest]
        public void Publish_UnrelatedHandler_IsNotCalled()
        {
            // --- ARRANGE ---
            bool handlerForBCalled = false;
            Manager.EventBus.Subscribe<TestEventB>(evt => handlerForBCalled = true);

            // --- ACT ---
            // We publish Event A, but we are listening for Event B.
            Manager.EventBus.Publish(new TestEventA());

            // --- ASSERT ---
            Assert(!handlerForBCalled, "A handler for TestEventB should not be called when TestEventA is published.");
        }
        
        [FluxTest]
        public void Publish_WithPriorities_HandlersAreCalledInCorrectOrder()
        {
            // --- ARRANGE ---
            var callOrder = new List<string>();
            
            // Subscribe handlers in a random order, but with specific priorities.
            Manager.EventBus.Subscribe<TestEventA>(evt => callOrder.Add("low_priority"), priority: 0);
            Manager.EventBus.Subscribe<TestEventA>(evt => callOrder.Add("high_priority"), priority: 100);
            Manager.EventBus.Subscribe<TestEventA>(evt => callOrder.Add("medium_priority"), priority: 50);

            // --- ACT ---
            Manager.EventBus.Publish(new TestEventA());

            // --- ASSERT ---
            Assert(callOrder.Count == 3, "All three handlers should have been called.");
            Assert(callOrder[0] == "high_priority", "The high priority handler should be called first.");
            Assert(callOrder[1] == "medium_priority", "The medium priority handler should be called second.");
            Assert(callOrder[2] == "low_priority", "The low priority handler should be called last.");
        }
    }
}