using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace Match3.Unity.Editor.Testing
{
    /// <summary>
    /// Exports test results to JSON format for external tools (Claude) to consume.
    /// </summary>
    public class TestResultExporter : ICallbacks
    {
        private static readonly string ResultsPath = Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            "test-results.json"
        );

        private readonly List<TestResult> _results = new();
        private DateTime _startTime;
        private Action _onComplete;

        public void RunStarted(ITestAdaptor testsToRun)
        {
            _results.Clear();
            _startTime = DateTime.Now;
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            ExportResults(result);
            _onComplete?.Invoke();
        }

        public void TestStarted(ITestAdaptor test)
        {
        }

        public void TestFinished(ITestResultAdaptor result)
        {
            // Only collect leaf tests (not suites)
            if (!result.HasChildren)
            {
                _results.Add(new TestResult
                {
                    Name = result.Test.Name,
                    FullName = result.Test.FullName,
                    ClassName = result.Test.TypeInfo?.Name ?? "Unknown",
                    Status = MapStatus(result.TestStatus),
                    Duration = result.Duration,
                    Message = result.Message,
                    StackTrace = result.StackTrace
                });
            }
        }

        public void SetOnComplete(Action callback)
        {
            _onComplete = callback;
        }

        private static string MapStatus(TestStatus status)
        {
            return status switch
            {
                TestStatus.Passed => "Passed",
                TestStatus.Failed => "Failed",
                TestStatus.Skipped => "Skipped",
                TestStatus.Inconclusive => "Inconclusive",
                _ => "Unknown"
            };
        }

        private void ExportResults(ITestResultAdaptor rootResult)
        {
            var duration = (DateTime.Now - _startTime).TotalSeconds;

            var passed = _results.Count(r => r.Status == "Passed");
            var failed = _results.Count(r => r.Status == "Failed");
            var skipped = _results.Count(r => r.Status == "Skipped");

            var json = BuildJson(passed, failed, skipped, duration);

            try
            {
                File.WriteAllText(ResultsPath, json, Encoding.UTF8);
                Debug.Log($"[TestResultExporter] Results exported to: {ResultsPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TestResultExporter] Failed to export results: {ex.Message}");
            }
        }

        private string BuildJson(int passed, int failed, int skipped, double duration)
        {
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"timestamp\": \"{DateTime.Now:O}\",");
            sb.AppendLine($"  \"total\": {_results.Count},");
            sb.AppendLine($"  \"passed\": {passed},");
            sb.AppendLine($"  \"failed\": {failed},");
            sb.AppendLine($"  \"skipped\": {skipped},");
            sb.AppendLine($"  \"duration\": {duration:F3},");
            sb.AppendLine("  \"tests\": [");

            for (int i = 0; i < _results.Count; i++)
            {
                var test = _results[i];
                var comma = i < _results.Count - 1 ? "," : "";
                sb.AppendLine("    {");
                sb.AppendLine($"      \"name\": {EscapeJson(test.Name)},");
                sb.AppendLine($"      \"fullName\": {EscapeJson(test.FullName)},");
                sb.AppendLine($"      \"className\": {EscapeJson(test.ClassName)},");
                sb.AppendLine($"      \"status\": \"{test.Status}\",");
                sb.AppendLine($"      \"duration\": {test.Duration:F3}");

                if (!string.IsNullOrEmpty(test.Message))
                {
                    // Remove trailing comma from duration line and add message
                    sb.Length -= Environment.NewLine.Length;
                    sb.Remove(sb.Length - 1, 1); // Remove the closing brace line break
                    sb.AppendLine(",");
                    sb.AppendLine($"      \"message\": {EscapeJson(test.Message)}");

                    if (!string.IsNullOrEmpty(test.StackTrace))
                    {
                        sb.Length -= Environment.NewLine.Length;
                        sb.Remove(sb.Length - 1, 1);
                        sb.AppendLine(",");
                        sb.AppendLine($"      \"stackTrace\": {EscapeJson(test.StackTrace)}");

                        // Try to extract source file info from stack trace
                        var sourceInfo = ExtractSourceInfo(test.StackTrace);
                        if (sourceInfo.HasValue)
                        {
                            sb.Length -= Environment.NewLine.Length;
                            sb.Remove(sb.Length - 1, 1);
                            sb.AppendLine(",");
                            sb.AppendLine($"      \"sourceFile\": {EscapeJson(sourceInfo.Value.file)},");
                            sb.AppendLine($"      \"sourceLine\": {sourceInfo.Value.line}");
                        }
                    }
                }

                sb.AppendLine($"    }}{comma}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string EscapeJson(string value)
        {
            if (value == null) return "null";

            var sb = new StringBuilder();
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: sb.Append(c); break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        private static (string file, int line)? ExtractSourceInfo(string stackTrace)
        {
            if (string.IsNullOrEmpty(stackTrace)) return null;

            // Pattern: "at ClassName.Method() in /path/to/file.cs:line 42"
            // Or: "ClassName.cs:42"
            var lines = stackTrace.Split('\n');
            foreach (var line in lines)
            {
                // Look for .cs: pattern
                var csIndex = line.IndexOf(".cs:", StringComparison.OrdinalIgnoreCase);
                if (csIndex > 0)
                {
                    // Find the start of the file path
                    var pathStart = line.LastIndexOf(" in ", csIndex, StringComparison.OrdinalIgnoreCase);
                    if (pathStart < 0) pathStart = line.LastIndexOf("at ", csIndex, StringComparison.OrdinalIgnoreCase);

                    var fileStart = pathStart >= 0 ? pathStart + (line.Contains(" in ") ? 4 : 3) : 0;
                    var filePath = line.Substring(fileStart, csIndex + 3 - fileStart).Trim();

                    // Extract line number
                    var lineNumStart = csIndex + 4;
                    var lineNumEnd = lineNumStart;
                    while (lineNumEnd < line.Length && char.IsDigit(line[lineNumEnd]))
                    {
                        lineNumEnd++;
                    }

                    if (lineNumEnd > lineNumStart && int.TryParse(line.Substring(lineNumStart, lineNumEnd - lineNumStart), out var lineNum))
                    {
                        // Convert to Assets-relative path if possible
                        var assetsIndex = filePath.IndexOf("Assets", StringComparison.OrdinalIgnoreCase);
                        if (assetsIndex >= 0)
                        {
                            filePath = filePath.Substring(assetsIndex);
                        }

                        return (filePath, lineNum);
                    }
                }
            }

            return null;
        }

        private class TestResult
        {
            public string Name;
            public string FullName;
            public string ClassName;
            public string Status;
            public double Duration;
            public string Message;
            public string StackTrace;
        }
    }
}
