using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FluxFramework.Testing
{
    /// <summary>
    /// Command-Line Interface (CLI) for running Flux tests, designed for CI/CD environments.
    /// </summary>
    public static class FluxTestRunnerCli
    {
        public static void RunTests()
        {
            Debug.Log("=== Flux Test Runner CLI: Starting Tests ===");
            
            var results = FluxTestRunner.RunAllTests();
            var failedTests = results.Where(r => r.Status == TestStatus.Failed).ToList();

            Debug.Log($"=== Flux Test Runner CLI: Test Run Complete ===");
            Debug.Log($"Total Tests: {results.Count}, Passed: {results.Count - failedTests.Count}, Failed: {failedTests.Count}");

            if (failedTests.Any())
            {
                Debug.LogError("=== The following tests failed: ===");
                foreach (var result in failedTests)
                {
                    Debug.LogError($"- {result.FixtureName}.{result.TestName}: {result.Message}");
                }
                
                // Quitte Unity avec un code d'erreur pour faire échouer le job CI/CD
                EditorApplication.Exit(1); 
            }
            else
            {
                Debug.Log("=== All tests passed successfully! ===");
                // Quitte Unity avec un code de succès
                EditorApplication.Exit(0);
            }
        }
    }
}