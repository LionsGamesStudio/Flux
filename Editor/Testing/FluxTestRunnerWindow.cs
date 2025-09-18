using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FluxFramework.Testing;

namespace FluxFramework.Editor.Testing
{
    public class FluxTestRunnerWindow : EditorWindow
    {
        private List<FluxTestResult> _results = new List<FluxTestResult>();
        private Vector2 _scrollPosition;
        
        private int _passCount;
        private int _failCount;
        private long _totalTime;

        [MenuItem("Flux/Testing/Test Runner...")]
        public static void ShowWindow()
        {
            GetWindow<FluxTestRunnerWindow>("Flux Test Runner");
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawHeader();
            DrawResults();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Run All Tests", EditorStyles.toolbarButton))
                {
                    ExecuteTests();
                }
                
                if (GUILayout.Button("Clear Results", EditorStyles.toolbarButton))
                {
                    ClearResults();
                }
            }
        }

        private void DrawHeader()
        {
            if (_results.Count == 0)
            {
                EditorGUILayout.HelpBox("Click 'Run All Tests' to begin.", MessageType.Info);
                return;
            }

            var headerStyle = new GUIStyle(EditorStyles.boldLabel);
            var successStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.green } };
            var failStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.red } };

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"Total Tests: {_results.Count}", headerStyle);
                GUILayout.Label($"Passed: {_passCount}", successStyle);
                GUILayout.Label($"Failed: {_failCount}", failStyle);
                GUILayout.Label($"Time: {_totalTime}ms", headerStyle);
            }
        }

        private void DrawResults()
        {
            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scrollView.scrollPosition;

                var groupedResults = _results.GroupBy(r => r.FixtureName);

                foreach (var group in groupedResults)
                {
                    GUILayout.Label(group.Key, EditorStyles.boldLabel);
                    
                    foreach (var result in group)
                    {
                        DrawResult(result);
                    }
                    
                    EditorGUILayout.Space();
                }
            }
        }

        private void DrawResult(FluxTestResult result)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                var statusColor = result.Status == TestStatus.Success ? "green" : "red";
                var statusText = result.Status == TestStatus.Success ? "✔" : "✖";

                GUILayout.Label($"<color={statusColor}>{statusText}</color> {result.TestName}", new GUIStyle(EditorStyles.label) { richText = true });
                GUILayout.FlexibleSpace();
                GUILayout.Label($"{result.DurationMilliseconds}ms", EditorStyles.miniLabel);
            }
            
            if (result.Status == TestStatus.Failed)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(result.Message, MessageType.Error);
                EditorGUI.indentLevel--;
            }
        }

        private void ExecuteTests()
        {
            _results = FluxTestRunner.RunAllTests();
            
            // Recalculate stats
            _passCount = _results.Count(r => r.Status == TestStatus.Success);
            _failCount = _results.Count(r => r.Status == TestStatus.Failed);
            _totalTime = _results.Sum(r => r.DurationMilliseconds);
            
            Repaint();
        }

        private void ClearResults()
        {
            _results.Clear();
            _passCount = 0;
            _failCount = 0;
            _totalTime = 0;
            Repaint();
        }
    }
}