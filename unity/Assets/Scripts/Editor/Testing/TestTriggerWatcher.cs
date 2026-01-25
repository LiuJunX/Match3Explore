// Disable this file if Test Runner API is inaccessible (e.g., in Tuanjie Engine)
// Add DISABLE_TEST_TRIGGER_WATCHER to Scripting Define Symbols to disable
#if !DISABLE_TEST_TRIGGER_WATCHER

using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Match3.Unity.Editor.Testing
{
    /// <summary>
    /// Watches for a trigger file to run tests automatically.
    /// This enables external tools (Claude) to trigger test runs while the editor is open.
    /// </summary>
    [InitializeOnLoad]
    public static class TestTriggerWatcher
    {
        private static readonly string ProjectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        private static readonly string TriggerPath = Path.Combine(ProjectRoot, ".test-trigger");
        private static readonly string LockPath = Path.Combine(ProjectRoot, ".test-running");

        private static FileSystemWatcher _watcher;
        private static bool _testRequested;
        private static TestResultExporter _exporter;
        private static ITestRunnerApi _testRunner;

        static TestTriggerWatcher()
        {
            // Clean up any stale files on editor start
            CleanupFiles();

            // Setup file watcher
            SetupWatcher();

            // Hook into editor update for processing
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.quitting += OnEditorQuitting;

            Debug.Log("[TestTriggerWatcher] Initialized. Watching for: " + TriggerPath);
        }

        private static void SetupWatcher()
        {
            try
            {
                _watcher = new FileSystemWatcher(ProjectRoot)
                {
                    Filter = ".test-trigger",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
                    EnableRaisingEvents = true
                };

                _watcher.Created += OnTriggerFileChanged;
                _watcher.Changed += OnTriggerFileChanged;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestTriggerWatcher] Failed to setup file watcher: {ex.Message}");
            }
        }

        private static void OnTriggerFileChanged(object sender, FileSystemEventArgs e)
        {
            _testRequested = true;
        }

        private static void OnEditorUpdate()
        {
            // Also poll for the file (backup in case FileSystemWatcher misses it)
            if (!_testRequested && File.Exists(TriggerPath))
            {
                _testRequested = true;
            }

            if (_testRequested)
            {
                _testRequested = false;
                RunTests();
            }
        }

        private static void RunTests()
        {
            // Check if tests are already running
            if (File.Exists(LockPath))
            {
                Debug.LogWarning("[TestTriggerWatcher] Tests already running, ignoring trigger");
                return;
            }

            // Remove trigger file
            try
            {
                if (File.Exists(TriggerPath))
                {
                    File.Delete(TriggerPath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TestTriggerWatcher] Could not delete trigger file: {ex.Message}");
            }

            // Create lock file
            try
            {
                File.WriteAllText(LockPath, DateTime.Now.ToString("O"));
            }
            catch
            {
                // Ignore lock file errors
            }

            Debug.Log("[TestTriggerWatcher] Running EditMode tests...");

            // Setup test runner
            _testRunner = ScriptableObject.CreateInstance<TestRunnerApi>();
            _exporter = new TestResultExporter();
            _exporter.SetOnComplete(OnTestsComplete);
            _testRunner.RegisterCallbacks(_exporter);

            // Run EditMode tests
            var filter = new Filter
            {
                testMode = TestMode.EditMode
            };

            _testRunner.Execute(new ExecutionSettings(filter));
        }

        private static void OnTestsComplete()
        {
            Debug.Log("[TestTriggerWatcher] Tests completed. Results exported.");

            // Clean up
            try
            {
                if (File.Exists(LockPath))
                {
                    File.Delete(LockPath);
                }
            }
            catch
            {
                // Ignore
            }

            if (_testRunner != null)
            {
                _testRunner.UnregisterCallbacks(_exporter);
                _testRunner = null;
                _exporter = null;
            }
        }

        private static void OnEditorQuitting()
        {
            CleanupFiles();

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Dispose();
                _watcher = null;
            }
        }

        private static void CleanupFiles()
        {
            try
            {
                if (File.Exists(TriggerPath)) File.Delete(TriggerPath);
                if (File.Exists(LockPath)) File.Delete(LockPath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        /// <summary>
        /// Menu item to manually trigger tests (for debugging).
        /// </summary>
        [MenuItem("Match3/Run Tests (Export JSON)")]
        public static void RunTestsManually()
        {
            // Create trigger file to activate the watcher
            File.WriteAllText(TriggerPath, DateTime.Now.ToString("O"));
        }
    }
}

#endif // !DISABLE_TEST_TRIGGER_WATCHER
