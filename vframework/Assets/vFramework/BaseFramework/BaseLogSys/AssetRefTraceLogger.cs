using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// 资源引用计数追溯日志（Resource / Bundle / Pool 三层）。
/// 用于加载侧与对象池 Ref 排查、内存泄漏初筛；首版输出 Debug.Log + 环形缓冲。
/// </summary>
public static class AssetRefTraceLogger
{
    public const string LogPrefix = "[AssetRefTrace]";

    const int DefaultBufferCapacity = 256;

    static bool enabled = true;
    static readonly List<string> recentEntries = new List<string>(DefaultBufferCapacity);
    static readonly object bufferLock = new object();

    /// <summary>是否输出 Trace（Editor / Development 默认 true）。</summary>
    public static bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    static AssetRefTraceLogger()
    {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        enabled = false;
#endif
    }

    #region Resource（AbstractResource.Ref）

    public static void TraceResource(string key, int refAfter, int delta, string reason)
    {
        if (!enabled)
            return;

        string line = FormatLine("Resource", key, refAfter, delta, reason);
        Write(line);
    }

    public static void TraceResourceLoad(string key, int refAfter, string loadPath = null)
    {
        string reason = string.IsNullOrEmpty(loadPath) ? "LoadAsset" : "LoadAsset loadPath=" + loadPath;
        TraceResource(key, refAfter, 0, reason);
    }

    public static void TraceResourceUnload(string key, string reason = "UnLoad")
    {
        TraceResource(key, 0, 0, reason);
    }

    #endregion

    #region Bundle（BundleManager.BundleEntry.Ref）

    public static void TraceBundle(string bundleName, int refAfter, int delta, string reason)
    {
        if (!enabled)
            return;

        string line = FormatLine("Bundle", bundleName, refAfter, delta, reason);
        Write(line);
    }

    #endregion

    #region Pool（PrefabPool.refCount）

    public static void TracePoolShare(string loadPath, int shareRefAfter, int delta, string reason, int resourceRefAfter = -1)
    {
        if (!enabled)
            return;

        string extra = resourceRefAfter >= 0 ? " resRef=" + resourceRefAfter : string.Empty;
        string line = string.Format(
            "{0}[Pool] path={1} share={2} delta={3}{4} reason={5}",
            LogPrefix,
            loadPath ?? "?",
            shareRefAfter,
            FormatDelta(delta),
            extra,
            reason ?? "?");

        Write(line);
    }

    #endregion

    #region 通用事件

    public static void TraceEvent(string message)
    {
        if (!enabled)
            return;

        string line = LogPrefix + "[Event] " + message;
        Write(line);
    }

    #endregion

    #region 缓冲与导出

    /// <summary>最近 N 条 Trace（ newest last ）。</summary>
    public static IReadOnlyList<string> GetRecentEntries()
    {
        lock (bufferLock)
        {
            return recentEntries.ToArray();
        }
    }

    /// <summary>将环形缓冲内容一次性打到 Log。</summary>
    public static void DumpRecent(int maxLines = 64)
    {
        if (!enabled)
        {
            Debug.Log(LogPrefix + " DumpRecent skipped (disabled).");
            return;
        }

        lock (bufferLock)
        {
            if (recentEntries.Count == 0)
            {
                Debug.Log(LogPrefix + " DumpRecent: (empty)");
                return;
            }

            int start = Mathf.Max(0, recentEntries.Count - maxLines);
            var sb = new StringBuilder(recentEntries.Count * 64);
            sb.AppendLine(LogPrefix + " DumpRecent (last " + (recentEntries.Count - start) + " lines):");
            for (int i = start; i < recentEntries.Count; i++)
                sb.AppendLine(recentEntries[i]);

            Debug.Log(sb.ToString());
        }
    }

    public static void ClearRecent()
    {
        lock (bufferLock)
            recentEntries.Clear();
    }

    #endregion

    #region 内部

    static string FormatLine(string layer, string id, int refAfter, int delta, string reason)
    {
        return string.Format(
            "{0}[{1}] id={2} ref={3} delta={4} reason={5}",
            LogPrefix,
            layer,
            id ?? "?",
            refAfter,
            FormatDelta(delta),
            reason ?? "?");
    }

    static string FormatDelta(int delta)
    {
        return delta >= 0 ? "+" + delta : delta.ToString();
    }

    static void Write(string line)
    {
        Debug.Log(line);

        lock (bufferLock)
        {
            if (recentEntries.Count >= DefaultBufferCapacity)
                recentEntries.RemoveAt(0);

            recentEntries.Add(line);
        }
    }

    #endregion
}
