using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace vFramework.Test.ABundleTest
{
    /// <summary>将内存快照与测试报告写入文本 log。</summary>
    public class ABundleMemoryLogger
    {
        #region 字段

        readonly string _sessionId;
        readonly string _logRoot;
        readonly List<ABundleMemorySnapshot> _snapshots = new();
        readonly StringBuilder _report = new();

        #endregion

        #region 构造

        public ABundleMemoryLogger(string logRoot)
        {
            _sessionId = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            _logRoot = ResolveLogRoot(logRoot);
            Directory.CreateDirectory(_logRoot);
        }

        public string SessionId => _sessionId;
        public string LogRoot => _logRoot;

        #endregion

        #region 写入

        public void LogSnapshot(ABundleMemorySnapshot snapshot)
        {
            _snapshots.Add(snapshot);
            var fileName = $"{_sessionId}_{snapshot.Tag}_{_snapshots.Count:D3}.txt";
            File.WriteAllText(Path.Combine(_logRoot, fileName), snapshot.ToString(), Encoding.UTF8);
        }

        public void AppendReportLine(string line)
        {
            _report.AppendLine(line);
            Debug.Log($"[ABundleTest] {line}");
        }

        public void WriteSummary(bool leakSuspect, long monoDelta, long allocatedDelta, long leakThresholdBytes)
        {
            var summaryPath = Path.Combine(_logRoot, $"{_sessionId}_summary.txt");
            var sb = new StringBuilder();
            sb.AppendLine("=== ABundle 测试汇总 ===");
            sb.AppendLine($"Session: {_sessionId}");
            sb.AppendLine($"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"LogRoot: {_logRoot}");
            sb.AppendLine();
            sb.AppendLine(_report.ToString());
            sb.AppendLine("--- 内存差值（末轮 vs 基线）---");
            sb.AppendLine($"MonoDelta:       {ABundleMemorySnapshot.FormatBytes(monoDelta)}");
            sb.AppendLine($"AllocatedDelta:  {ABundleMemorySnapshot.FormatBytes(allocatedDelta)}");
            sb.AppendLine($"LeakThreshold:   {ABundleMemorySnapshot.FormatBytes(leakThresholdBytes)}");
            sb.AppendLine($"LeakSuspect:     {(leakSuspect ? "YES" : "NO")}");
            sb.AppendLine();
            sb.AppendLine("--- 快照列表 ---");
            foreach (var snap in _snapshots)
            {
                sb.AppendLine($"  [{snap.Tag}] Mono={snap.MonoUsedBytes} Alloc={snap.TotalAllocatedBytes}");
            }

            File.WriteAllText(summaryPath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"[ABundleTest] 汇总已写入: {summaryPath}");
        }

        #endregion

        #region 内部

        static string ResolveLogRoot(string logRoot)
        {
            if (string.IsNullOrWhiteSpace(logRoot))
            {
                logRoot = "Assets/Test/ABundleTest/Logs";
            }

            logRoot = logRoot.Replace('\\', '/');
            if (logRoot.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                var projectRoot = Path.GetDirectoryName(Application.dataPath);
                return Path.Combine(projectRoot, logRoot).Replace('\\', '/');
            }

            if (!Path.IsPathRooted(logRoot))
            {
                return Path.Combine(Application.persistentDataPath, logRoot).Replace('\\', '/');
            }

            return logRoot;
        }

        #endregion
    }
}
