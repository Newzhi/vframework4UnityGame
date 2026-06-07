using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using vFramework.BaseLayer.AssetLayer;
using vFramework.BaseLayer.AssetLayer.ABundleLayer;

namespace vFramework.Test.ABundleTest
{
    /// <summary>
    /// 全场景 AB 加载测试：正确性、重复加载、压力循环、内存泄漏检测。
    /// Play 模式下点击「开始全场景测试」或调用 RunAllTests()。
    /// </summary>
    public class ABundleLoadTestRunner : MonoBehaviour
    {
        #region Inspector

        [Header("规则")]
        [SerializeField] string rulesXmlPath;

        [Header("测试参数")]
        [SerializeField] int stressCycles = 10;
        [SerializeField] long leakThresholdBytes = 512 * 1024;
        [SerializeField] string logRoot = "Assets/Test/ABundleTest/Logs";

        [Header("UI")]
        [SerializeField] bool showOnGuiButton = true;

        #endregion

        #region 状态

        ABundleLoader _loader;
        ABundleMemoryLogger _logger;
        ABundleMemorySnapshot _baseline;
        bool _running;
        string _status = "就绪";

        #endregion

        #region Unity

        void OnGUI()
        {
            if (!showOnGuiButton)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 320, 140), GUI.skin.box);
            GUILayout.Label("ABundle 全场景测试");
            GUILayout.Label(_status);
            GUI.enabled = !_running;
            if (GUILayout.Button("开始全场景测试", GUILayout.Height(40)))
            {
                StartCoroutine(RunAllTestsCoroutine());
            }

            GUI.enabled = true;
            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            _loader?.Shutdown();
        }

        [ContextMenu("Run All Tests")]
        public void RunAllTests()
        {
            if (!_running)
            {
                StartCoroutine(RunAllTestsCoroutine());
            }
        }

        #endregion

        #region 测试流程

        IEnumerator RunAllTestsCoroutine()
        {
            _running = true;
            _logger = new ABundleMemoryLogger(logRoot);
            _status = "运行中…";

            yield return null;

            if (!TryRunPhaseInit())
            {
                FinishTests();
                yield break;
            }

            yield return RunPhaseAllLocations();
            yield return RunPhaseRepeatLoad();
            yield return RunPhaseStress();
            RunPhaseLeakCheck();
            FinishTests();
        }

        bool TryRunPhaseInit()
        {
            try
            {
                RunPhaseInit();
                return true;
            }
            catch (Exception ex)
            {
                _logger?.AppendReportLine($"FATAL: {ex.Message}");
                Debug.LogException(ex);
                return false;
            }
        }

        void FinishTests()
        {
            _loader?.Shutdown();
            _loader = null;
            _running = false;
            _status = "完成，见 Logs 目录";
        }

        void RunPhaseInit()
        {
            _logger.AppendReportLine("=== Phase 1: 初始化 ===");
            _loader = new ABundleLoader();
            var rules = LoadRules();
            _loader.InitializeFromRules(rules);

            if (!_loader.IsInitialized || _loader.Catalog == null || _loader.Catalog.Locations.Count == 0)
            {
                throw new InvalidOperationException(
                    "Catalog 为空或未初始化。请先在 vFramework → AssetKit → ABundleBuilder 打包。");
            }

            _logger.AppendReportLine($"Catalog Locations: {_loader.Catalog.Locations.Count}");
            _logger.AppendReportLine($"LoadMode: {_loader.LoadMode}");
            _baseline = ABundleMemorySampler.Take(_loader, "baseline");
            _logger.LogSnapshot(_baseline);
        }

        IEnumerator RunPhaseAllLocations()
        {
            _logger.AppendReportLine("=== Phase 2: 全 Location 正确性 ===");
            var catalog = _loader.Catalog;
            var pass = 0;
            var fail = 0;
            var handles = new List<IAssetHandle>();

            foreach (var entry in catalog.Locations)
            {
                if (string.IsNullOrEmpty(entry?.Location))
                {
                    continue;
                }

                var handle = LoadHandleForEntry(entry);
                if (handle.IsValid)
                {
                    pass++;
                    handles.Add(handle);
                }
                else
                {
                    fail++;
                    _logger.AppendReportLine($"FAIL load: {entry.Location} ({entry.AssetType})");
                }

                if ((pass + fail) % 5 == 0)
                {
                    yield return null;
                }
            }

            foreach (var handle in handles)
            {
                handle.Release();
            }

            ABundleMemorySampler.ForceCleanup();
            var snap = ABundleMemorySampler.Take(_loader, "after_all_locations");
            _logger.LogSnapshot(snap);
            _logger.AppendReportLine($"Location 测试: pass={pass} fail={fail}");
        }

        IEnumerator RunPhaseRepeatLoad()
        {
            _logger.AppendReportLine("=== Phase 3: 重复加载 ===");
            if (_loader.Catalog.Locations.Count == 0)
            {
                yield break;
            }

            var testLocation = _loader.Catalog.Locations[0].Location;
            var entry = _loader.Catalog.Locations[0];
            var handles = new IAssetHandle[3];

            for (var i = 0; i < 3; i++)
            {
                handles[i] = LoadHandleForEntry(entry);
                if (!handles[i].IsValid)
                {
                    _logger.AppendReportLine($"FAIL repeat load #{i + 1}: {testLocation}");
                }
            }

            var refCount = _loader.GetBundleRefCount(entry.BundleName);
            _logger.AppendReportLine($"Repeat load refCount({entry.BundleName})={refCount} (expect >= 3)");

            for (var i = 0; i < handles.Length; i++)
            {
                handles[i]?.Release();
            }

            ABundleMemorySampler.ForceCleanup();
            var afterRef = _loader.GetBundleRefCount(entry.BundleName);
            _logger.AppendReportLine($"After release refCount={afterRef} (expect 0)");
            _logger.LogSnapshot(ABundleMemorySampler.Take(_loader, "after_repeat_load"));
            yield return null;
        }

        IEnumerator RunPhaseStress()
        {
            _logger.AppendReportLine($"=== Phase 4: 压力循环 x{stressCycles} ===");
            var catalog = _loader.Catalog;

            for (var cycle = 0; cycle < stressCycles; cycle++)
            {
                var handles = new List<IAssetHandle>();
                foreach (var entry in catalog.Locations)
                {
                    if (string.IsNullOrEmpty(entry?.Location))
                    {
                        continue;
                    }

                    var handle = LoadHandleForEntry(entry);
                    if (handle.IsValid)
                    {
                        handles.Add(handle);
                    }
                }

                foreach (var handle in handles)
                {
                    handle.Release();
                }

                ABundleMemorySampler.ForceCleanup();

                if (cycle == 0 || cycle == stressCycles - 1)
                {
                    _logger.LogSnapshot(ABundleMemorySampler.Take(_loader, $"stress_cycle_{cycle + 1}"));
                }

                if ((cycle + 1) % 2 == 0)
                {
                    yield return null;
                }
            }

            _logger.AppendReportLine($"Stress cycles completed: {stressCycles}");
        }

        void RunPhaseLeakCheck()
        {
            _logger.AppendReportLine("=== Phase 5: 泄漏判定 ===");
            ABundleMemorySampler.ForceCleanup();
            var final = ABundleMemorySampler.Take(_loader, "final");
            _logger.LogSnapshot(final);

            var monoDelta = final.DeltaMonoUsed(_baseline);
            var allocDelta = final.DeltaTotalAllocated(_baseline);
            var leakSuspect = monoDelta > leakThresholdBytes || allocDelta > leakThresholdBytes;

            var loadedBundles = _loader.GetLoadedBundleNames();
            if (loadedBundles.Length > 0)
            {
                leakSuspect = true;
                _logger.AppendReportLine($"WARN: 仍有已加载包: {string.Join(", ", loadedBundles)}");
            }

            if (leakSuspect)
            {
                _logger.AppendReportLine("LEAK_SUSPECT: 内存或包引用未回到基线");
            }
            else
            {
                _logger.AppendReportLine("PASS: 未发现明显泄漏");
            }

            _logger.WriteSummary(leakSuspect, monoDelta, allocDelta, leakThresholdBytes);
        }

        #endregion

        #region 辅助

        ABundleBuildRules LoadRules()
        {
            var path = string.IsNullOrWhiteSpace(rulesXmlPath)
                ? ABundleRulesXmlIO.DefaultRulesRelativePath
                : rulesXmlPath;

            if (File.Exists(ABundleRulesXmlIO.ToFullPath(path)))
            {
                return ABundleRulesXmlIO.Load(path);
            }

            return ABundleRulesXmlIO.CreateDefault();
        }

        IAssetHandle LoadHandleForEntry(AssetLocationEntry entry)
        {
            var typeHint = entry.AssetType ?? string.Empty;

            if (typeHint.IndexOf("Texture", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _loader.LoadHandle<Texture2D>(entry.Location);
            }

            if (typeHint.IndexOf("Sprite", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _loader.LoadHandle<Sprite>(entry.Location);
            }

            if (typeHint.IndexOf("GameObject", StringComparison.OrdinalIgnoreCase) >= 0 ||
                typeHint.IndexOf("Prefab", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _loader.LoadHandle<GameObject>(entry.Location);
            }

            if (typeHint.IndexOf("Material", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _loader.LoadHandle<Material>(entry.Location);
            }

            if (typeHint.IndexOf("Mesh", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return _loader.LoadHandle<Mesh>(entry.Location);
            }

            return _loader.LoadHandle<UnityEngine.Object>(entry.Location);
        }

        #endregion
    }
}
