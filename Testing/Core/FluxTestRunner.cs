using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using FluxFramework.Testing.Attributes;

namespace FluxFramework.Testing
{
    /// <summary>
    /// Discovers and executes tests that use the Flux testing attributes.
    /// This class is decoupled from the UI and returns a structured list of results.
    /// </summary>
    public static class FluxTestRunner
    {
        public static List<FluxTestResult> RunAllTests()
        {
            var allResults = new List<FluxTestResult>();
            var testFixtures = FindTestFixtures();

            foreach (var fixtureType in testFixtures)
            {
                allResults.AddRange(RunTestsInFixture(fixtureType));
            }
            
            return allResults;
        }

        private static IEnumerable<FluxTestResult> RunTestsInFixture(Type fixtureType)
        {
            var testMethods = GetMethodsWithAttribute<FluxTestAttribute>(fixtureType);
            var setUpMethod = GetMethodsWithAttribute<FluxSetUpAttribute>(fixtureType).FirstOrDefault();
            var tearDownMethod = GetMethodsWithAttribute<FluxTearDownAttribute>(fixtureType).FirstOrDefault();
            
            var fixtureResults = new List<FluxTestResult>();

            foreach (var testMethod in testMethods)
            {
                var result = new FluxTestResult
                {
                    FixtureName = fixtureType.Name,
                    TestName = testMethod.Name,
                    Status = TestStatus.NotRun
                };

                object fixtureInstance = null;
                var stopwatch = new Stopwatch();

                try
                {
                    fixtureInstance = Activator.CreateInstance(fixtureType);
                    
                    setUpMethod?.Invoke(fixtureInstance, null);
                    
                    stopwatch.Start();
                    testMethod.Invoke(fixtureInstance, null);
                    stopwatch.Stop();

                    result.Status = TestStatus.Success;
                    result.Message = "Passed";
                }
                catch (Exception e)
                {
                    stopwatch.Stop();
                    result.Status = TestStatus.Failed;
                    // Unwrap the TargetInvocationException to get the real error.
                    var innerException = e.InnerException ?? e;
                    result.Message = $"{innerException.GetType().Name}: {innerException.Message}";
                }
                finally
                {
                    tearDownMethod?.Invoke(fixtureInstance, null);
                    result.DurationMilliseconds = stopwatch.ElapsedMilliseconds;
                    fixtureResults.Add(result);
                }
            }
            return fixtureResults;
        }

        private static List<Type> FindTestFixtures()
        {
            // This method remains the same as before.
            var fixtures = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    foreach (var type in types)
                    {
                        if (type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                                .Any(m => m.IsDefined(typeof(FluxTestAttribute), false)))
                        {
                            fixtures.Add(type);
                        }
                    }
                }
                catch (ReflectionTypeLoadException) { /* Ignore */ }
            }
            return fixtures;
        }
        
        private static IEnumerable<MethodInfo> GetMethodsWithAttribute<T>(Type type) where T : Attribute
        {
            return type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                       .Where(m => m.IsDefined(typeof(T), false));
        }
    }
}