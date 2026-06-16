using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// 资源引用计数追溯日志（Resource / Bundle / Pool 三层）。
/// 用途：<b>检查引用计数是否正常</b>、排查过早 UnLoad / 泄漏。
/// Editor：人类可读 Console + 环形缓冲；真机（非 Editor）：追加 JSONL 到 persistentDataPath。
/// </summary>
public static class AssetRefTraceLogger
{
    /// <summary>日志前缀，便于 Console 过滤。</summary>
    public const string LogPrefix = "[AssetRefTrace]";

    /// <summary>用途标识：引用计数校验。</summary>
    public const string Purpose = "AssetRefCountCheck";

    /// <summary>JSON 行 schema。</summary>
    public const string SchemaVersion = "v1-ref-trace";

    const int DefaultBufferCapacity = 256;
    const string DeviceLogRoot = "vFramework/AssetRefTrace";
    const string DeviceLogSubFolder = "Logs";

    static bool enabled;
    static bool deviceJsonOutput;
    static int opIdCounter;
    static int seqCounter;
    static long sessionStartUtcMs;
    static string deviceJsonFilePath;

    static LoadScope loadScope;

    static readonly List<string> recentEntries = new List<string>(DefaultBufferCapacity);
    static readonly object bufferLock = new object();

    /// <summary>是否输出 Trace。</summary>
    public static bool Enabled
    {
        get => enabled;
        set => enabled = value;
    }

    /// <summary>真机 JSONL 文件绝对路径（未创建时为 null）。</summary>
    public static string DeviceJsonFilePath => deviceJsonFilePath;

    static AssetRefTraceLogger()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        enabled = true;
#else
        enabled = false;
#endif

#if !UNITY_EDITOR
        deviceJsonOutput = true;
        sessionStartUtcMs = UtcNowMs();
#else
        deviceJsonOutput = false;
#endif
    }

    #region 加载作用域（Resource ↔ Bundle 关联）

    /// <summary>首次 <see cref="AbstractResource.LoadAsset"/> 前调用，建立 opId / for 关联。</summary>
    public static void BeginResourceLoad(string resourceKey, string loadPath, string mainBundle, int resourceRefBefore)
    {
        if (!enabled)
            return;

        int opId = ++opIdCounter;
        loadScope = new LoadScope(opId, resourceKey, loadPath, mainBundle);

        WriteEntry(new TraceEntry
        {
            Layer = "Scope",
            Reason = "ResourceLoadBegin",
            ResourceKey = resourceKey,
            LoadPath = loadPath,
            MainBundle = mainBundle,
            ResourceRef = resourceRefBefore,
            OpId = opId
        });
    }

    /// <summary>LoadAsset 成功后调用，记录实际 Acquire 的包列表并结束作用域。</summary>
    public static void CompleteResourceLoad(
        string resourceKey,
        int resourceRefAfter,
        string loadPath,
        string mainBundle,
        IReadOnlyList<string> acquiredBundles,
        string assetSource)
    {
        if (!enabled)
            return;

        int opId = loadScope != null ? loadScope.OpId : 0;
        string bundles = FormatBundleList(acquiredBundles);

        WriteEntry(new TraceEntry
        {
            Layer = "Resource",
            Reason = "LoadAsset",
            ResourceKey = resourceKey,
            LoadPath = loadPath,
            MainBundle = mainBundle,
            ResourceRef = resourceRefAfter,
            Delta = 0,
            OpId = opId,
            AcquiredBundles = bundles,
            AssetSource = assetSource
        });

        loadScope = null;
    }

    /// <summary>AcquireBundleWithDependencies 入口：记录主包与依赖顺序（叶→根→主包）。</summary>
    public static void TraceBundleLoadScopeBegin(string mainBundle, string[] dependencies)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Scope",
            Reason = "BundleAcquireBegin",
            MainBundle = mainBundle,
            OpId = loadScope?.OpId ?? 0,
            LoadPath = loadScope?.LoadPath,
            ResourceKey = loadScope?.ResourceKey,
            AcquiredBundles = FormatBundleList(dependencies),
            Note = "order=deps_then_main"
        });
    }

    /// <summary>LoadAsset 失败时清除作用域，避免污染后续 Bundle 关联。</summary>
    public static void CancelLoadScope(string reason)
    {
        if (!enabled || loadScope == null)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Scope",
            Reason = reason ?? "LoadCancelled",
            OpId = loadScope.OpId,
            LoadPath = loadScope.LoadPath,
            ResourceKey = loadScope.ResourceKey,
            MainBundle = loadScope.MainBundle
        });

        loadScope = null;
    }

    #endregion

    #region Resource（AbstractResource.Ref）

    public static void TraceResource(string key, int refAfter, int delta, string reason)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Resource",
            ResourceKey = key,
            ResourceRef = refAfter,
            Delta = delta,
            Reason = reason,
            OpId = loadScope?.OpId ?? 0,
            LoadPath = loadScope?.LoadPath,
            MainBundle = loadScope?.MainBundle
        });
    }

    public static void TraceResourceUnload(string key, int refBeforeUnload, string reason, IReadOnlyList<string> acquiredBundles)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Resource",
            ResourceKey = key,
            ResourceRef = 0,
            Delta = 0,
            Reason = reason,
            Note = "refBeforeUnload=" + refBeforeUnload,
            AcquiredBundles = FormatBundleList(acquiredBundles)
        });
    }

    #endregion

    #region Bundle（BundleManager.BundleEntry.Ref）

    public static void TraceBundle(
        string bundleName,
        int refAfter,
        int delta,
        string reason,
        string role = null,
        string mainBundle = null)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Bundle",
            BundleName = bundleName,
            BundleRef = refAfter,
            Delta = delta,
            Reason = reason,
            BundleRole = role,
            MainBundle = mainBundle ?? loadScope?.MainBundle,
            OpId = loadScope?.OpId ?? 0,
            LoadPath = loadScope?.LoadPath,
            ResourceKey = loadScope?.ResourceKey
        });
    }

    public static void TraceBundleUnloadAll(int bundleCount)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Bundle",
            Reason = "UnloadAll",
            Note = "bundleCount=" + bundleCount
        });
    }

    #endregion

    #region Pool（PrefabPool.refCount）

    public static void TracePoolShare(string loadPath, int shareRefAfter, int delta, string reason, int resourceRefAfter = -1)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Pool",
            LoadPath = loadPath,
            PoolShareRef = shareRefAfter,
            Delta = delta,
            Reason = reason,
            ResourceRef = resourceRefAfter >= 0 ? resourceRefAfter : -1
        });
    }

    #endregion

    #region 通用事件

    public static void TraceEvent(string message)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "Event",
            Reason = message
        });
    }

    /// <summary>CDN 下载完成 Trace（bundleName、bytes、hashOk）。</summary>
    public static void TraceCdnDownload(string bundleName, int bytes, bool hashOk)
    {
        if (!enabled)
            return;

        WriteEntry(new TraceEntry
        {
            Layer = "CDN",
            Reason = "CdnDownload",
            BundleName = bundleName,
            Note = "bytes=" + bytes + " hashOk=" + hashOk
        });
    }

    /// <summary>UnloadAll 前 Resource / Bundle 非零 Ref 摘要（由调用方提供 Trace 回调，避免暴露 internal 类型）。</summary>
    public static void TraceResidualRefsBeforeUnloadAll(
        Action traceResourceRefs,
        Action traceBundleRefs)
    {
        if (!enabled)
            return;

        traceResourceRefs?.Invoke();
        traceBundleRefs?.Invoke();
    }

    #endregion

    #region 缓冲与导出

    /// <summary>最近 N 条人类可读 Trace（newest last）。</summary>
    public static IReadOnlyList<string> GetRecentEntries()
    {
        lock (bufferLock)
            return recentEntries.ToArray();
    }

    /// <summary>将环形缓冲内容一次性打到 Log。</summary>
    public static void DumpRecent(int maxLines = 64)
    {
        if (!enabled)
        {
            Debug.Log(LogPrefix + "[RefCountCheck] DumpRecent skipped (disabled).");
            return;
        }

        lock (bufferLock)
        {
            if (recentEntries.Count == 0)
            {
                Debug.Log(LogPrefix + "[RefCountCheck] DumpRecent: (empty)");
                return;
            }

            int start = Mathf.Max(0, recentEntries.Count - maxLines);
            var sb = new StringBuilder(recentEntries.Count * 80);
            sb.AppendLine(LogPrefix + "[RefCountCheck] DumpRecent (last " + (recentEntries.Count - start) + " lines):");
            if (!string.IsNullOrEmpty(deviceJsonFilePath))
                sb.AppendLine(LogPrefix + "[RefCountCheck] deviceJson=" + deviceJsonFilePath);

            for (int i = start; i < recentEntries.Count; i++)
                sb.AppendLine(recentEntries[i]);

            Debug.Log(sb.ToString());
        }
    }

    /// <summary>真机 JSONL 刷盘并打一条路径提示（UnloadAll / 切场景前可调用）。</summary>
    public static void FlushDeviceJson()
    {
#if !UNITY_EDITOR
        if (!deviceJsonOutput || string.IsNullOrEmpty(deviceJsonFilePath))
            return;

        Debug.Log(LogPrefix + "[RefCountCheck] JSONL path=" + deviceJsonFilePath);
#endif
    }

    public static void ClearRecent()
    {
        lock (bufferLock)
            recentEntries.Clear();
    }

    #endregion

    #region 内部 — 条目与输出

    sealed class LoadScope
    {
        public readonly int OpId;
        public readonly string ResourceKey;
        public readonly string LoadPath;
        public readonly string MainBundle;

        public LoadScope(int opId, string resourceKey, string loadPath, string mainBundle)
        {
            OpId = opId;
            ResourceKey = resourceKey;
            LoadPath = loadPath;
            MainBundle = mainBundle;
        }
    }

    struct TraceEntry
    {
        public string Layer;
        public string Reason;
        public string ResourceKey;
        public string LoadPath;
        public string MainBundle;
        public string BundleName;
        public string BundleRole;
        public string AcquiredBundles;
        public string AssetSource;
        public string Note;
        public int OpId;
        public int ResourceRef;
        public int BundleRef;
        public int PoolShareRef;
        public int Delta;
    }

    static void WriteEntry(TraceEntry entry)
    {
        string humanLine = FormatHumanLine(entry);
        Write(humanLine, entry);
    }

    static string FormatHumanLine(TraceEntry e)
    {
        var sb = new StringBuilder(160);
        sb.Append(LogPrefix);
        sb.Append("[RefCountCheck]");
        sb.Append('[').Append(e.Layer ?? "Event").Append(']');

        if (e.OpId > 0)
            sb.Append(" op=").Append(e.OpId);

        if (!string.IsNullOrEmpty(e.ResourceKey))
            sb.Append(" res=").Append(e.ResourceKey);

        if (!string.IsNullOrEmpty(e.LoadPath))
            sb.Append(" for=").Append(e.LoadPath);

        if (!string.IsNullOrEmpty(e.MainBundle))
            sb.Append(" main=").Append(e.MainBundle);

        if (!string.IsNullOrEmpty(e.BundleName))
            sb.Append(" bundle=").Append(e.BundleName);

        if (!string.IsNullOrEmpty(e.BundleRole))
            sb.Append(" role=").Append(e.BundleRole);

        if (e.ResourceRef >= 0)
            sb.Append(" resRef=").Append(e.ResourceRef);

        if (e.Layer == "Bundle")
            sb.Append(" bundleRef=").Append(e.BundleRef);

        if (e.Layer == "Pool")
            sb.Append(" share=").Append(e.PoolShareRef);

        if (e.Delta != 0)
            sb.Append(" delta=").Append(FormatDelta(e.Delta));

        if (!string.IsNullOrEmpty(e.AcquiredBundles))
            sb.Append(" bundles=").Append(e.AcquiredBundles);

        if (!string.IsNullOrEmpty(e.AssetSource))
            sb.Append(" source=").Append(e.AssetSource);

        if (!string.IsNullOrEmpty(e.Note))
            sb.Append(' ').Append(e.Note);

        sb.Append(" reason=").Append(e.Reason ?? "?");
        return sb.ToString();
    }

    static string FormatDelta(int delta)
    {
        return delta >= 0 ? "+" + delta : delta.ToString();
    }

    static string FormatBundleList(IReadOnlyList<string> names)
    {
        if (names == null || names.Count == 0)
            return string.Empty;

        var sb = new StringBuilder(names.Count * 16);
        for (int i = 0; i < names.Count; i++)
        {
            if (i > 0)
                sb.Append('>');

            sb.Append(names[i]);
        }

        return sb.ToString();
    }

    static void Write(string humanLine, TraceEntry entry)
    {
        Debug.Log(humanLine);

        lock (bufferLock)
        {
            if (recentEntries.Count >= DefaultBufferCapacity)
                recentEntries.RemoveAt(0);

            recentEntries.Add(humanLine);
        }

#if !UNITY_EDITOR
        if (deviceJsonOutput)
            AppendDeviceJsonLine(entry);
#endif
    }

    static void AppendDeviceJsonLine(TraceEntry entry)
    {
        try
        {
            EnsureDeviceJsonFile();
            if (string.IsNullOrEmpty(deviceJsonFilePath))
                return;

            int seq = ++seqCounter;
            long nowMs = UtcNowMs();
            string json = BuildJsonLine(entry, seq, nowMs);
            File.AppendAllText(deviceJsonFilePath, json + "\n", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            Debug.LogWarning(LogPrefix + "[RefCountCheck] JSONL write failed: " + ex.Message);
        }
    }

    static void EnsureDeviceJsonFile()
    {
        if (!string.IsNullOrEmpty(deviceJsonFilePath))
            return;

        string dir = Path.Combine(Application.persistentDataPath, DeviceLogRoot, DeviceLogSubFolder);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        string fileName = "ref_trace_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jsonl";
        deviceJsonFilePath = Path.Combine(dir, fileName);

        string header = BuildJsonHeader();
        File.WriteAllText(deviceJsonFilePath, header + "\n", Encoding.UTF8);
    }

    static string BuildJsonHeader()
    {
        return string.Format(
            "{{\"purpose\":\"{0}\",\"schema\":\"{1}\",\"type\":\"header\",\"note\":\"引用计数是否正常校验\",\"unity\":\"{2}\",\"platform\":\"{3}\",\"package\":\"{4}\",\"sessionStartUtcMs\":{5},\"persistentDataPath\":\"{6}\"}}",
            Purpose,
            SchemaVersion,
            EscapeJson(Application.unityVersion),
            EscapeJson(Application.platform.ToString()),
            EscapeJson(Application.identifier),
            sessionStartUtcMs > 0 ? sessionStartUtcMs : UtcNowMs(),
            EscapeJson(Application.persistentDataPath));
    }

    static string BuildJsonLine(TraceEntry e, int seq, long timeUtcMs)
    {
        var sb = new StringBuilder(256);
        sb.Append('{');
        AppendJsonField(sb, "purpose", Purpose, true);
        AppendJsonField(sb, "schema", SchemaVersion, true);
        AppendJsonField(sb, "type", "entry", true);
        sb.Append("\"seq\":").Append(seq).Append(',');
        sb.Append("\"timeUtcMs\":").Append(timeUtcMs).Append(',');

        if (e.OpId > 0)
            sb.Append("\"opId\":").Append(e.OpId).Append(',');

        AppendJsonField(sb, "layer", e.Layer, true);
        AppendJsonField(sb, "reason", e.Reason, true);

        if (!string.IsNullOrEmpty(e.ResourceKey))
            AppendJsonField(sb, "resourceKey", e.ResourceKey, true);

        if (!string.IsNullOrEmpty(e.LoadPath))
            AppendJsonField(sb, "forLoadPath", e.LoadPath, true);

        if (!string.IsNullOrEmpty(e.MainBundle))
            AppendJsonField(sb, "mainBundle", e.MainBundle, true);

        if (!string.IsNullOrEmpty(e.BundleName))
            AppendJsonField(sb, "bundleName", e.BundleName, true);

        if (!string.IsNullOrEmpty(e.BundleRole))
            AppendJsonField(sb, "bundleRole", e.BundleRole, true);

        if (e.ResourceRef >= 0)
            sb.Append("\"resourceRef\":").Append(e.ResourceRef).Append(',');

        if (e.Layer == "Bundle")
            sb.Append("\"bundleRef\":").Append(e.BundleRef).Append(',');

        if (e.Layer == "Pool")
            sb.Append("\"poolShareRef\":").Append(e.PoolShareRef).Append(',');

        if (e.Delta != 0)
            sb.Append("\"delta\":").Append(e.Delta).Append(',');

        if (!string.IsNullOrEmpty(e.AcquiredBundles))
            AppendJsonField(sb, "acquiredBundles", e.AcquiredBundles, true);

        if (!string.IsNullOrEmpty(e.AssetSource))
            AppendJsonField(sb, "assetSource", e.AssetSource, true);

        if (!string.IsNullOrEmpty(e.Note))
            AppendJsonField(sb, "note", e.Note, true);

        if (sb[sb.Length - 1] == ',')
            sb.Length--;

        sb.Append('}');
        return sb.ToString();
    }

    static void AppendJsonField(StringBuilder sb, string key, string value, bool trailingComma)
    {
        sb.Append('\"').Append(key).Append("\":\"").Append(EscapeJson(value)).Append('\"');
        if (trailingComma)
            sb.Append(',');
    }

    static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    static long UtcNowMs()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    #endregion
}
