using FluxFramework.Testing.Attributes;
using FluxFramework.Core;
using UnityEngine;
using System.Collections.Generic;

namespace FluxFramework.Testing
{
    /// <summary>
    /// Base class for all Flux framework tests.
    /// It creates a clean, in-memory 'MockFluxManager' for each test,
    /// ensuring a perfectly isolated and predictable test environment.
    /// </summary>
    public abstract class FluxTestBase
    {
        /// <summary>
        /// Provides access to the sandboxed IFluxManager instance for this test.
        /// Use this to interact with the framework's services (Properties, EventBus, etc.).
        /// </summary>
        protected IFluxManager Manager { get; private set; }

        private List<GameObject> _createdGameObjects;

        [FluxSetUp]
        public virtual void SetUp()
        {
            // Before each test, create a brand new, clean instance of our mock manager.
            // This single line replaces all the complex GameObject creation and reflection.
            Manager = new MockFluxManager();

            Flux.Manager = Manager;
            _createdGameObjects = new List<GameObject>();
        }

        [FluxTearDown]
        public virtual void TearDown()
        {
            // After each test, simply discard the manager.
            // The garbage collector will handle the cleanup.
            // All services (EventBus, Properties, etc.) will be destroyed with it.
            for (int i = _createdGameObjects.Count - 1; i >= 0; i--)
            {
                if (_createdGameObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdGameObjects[i]);
                }
            }
            _createdGameObjects.Clear();
            
            Flux.Manager = null;
            Manager = null;
        }
        
        /// <summary>
        /// Helper to create a GameObject that will be automatically cleaned up after the test.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        protected GameObject CreateTestGameObject(string name = "TestObject")
        {
            var go = new GameObject(name);
            _createdGameObjects.Add(go);
            return go;
        }

        // We can now add our custom assertion methods here.

        /// <summary>
        /// Asserts that a condition is true. If not, the test will fail.
        /// </summary>
        protected void Assert(bool condition, string message = "Assertion failed")
        {
            if (!condition)
            {
                // Throw an exception to make the test fail. Our FluxTestRunner will catch it.
                throw new FluxAssertionException(message);
            }
        }
    }
    
    /// <summary>
    /// A custom exception type for failed assertions in Flux tests.
    /// </summary>
    public class FluxAssertionException : System.Exception
    {
        public FluxAssertionException(string message) : base(message) { }
    }
}