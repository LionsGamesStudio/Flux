using FluxFramework.Testing.Attributes;
using FluxFramework.Core;

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

        [FluxSetUp]
        public virtual void SetUp()
        {
            // Before each test, create a brand new, clean instance of our mock manager.
            // This single line replaces all the complex GameObject creation and reflection.
            Manager = new MockFluxManager();
        }

        [FluxTearDown]
        public virtual void TearDown()
        {
            // After each test, simply discard the manager.
            // The garbage collector will handle the cleanup.
            // All services (EventBus, Properties, etc.) will be destroyed with it.
            Manager = null;
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