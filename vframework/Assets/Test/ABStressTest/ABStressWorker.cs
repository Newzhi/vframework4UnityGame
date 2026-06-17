using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 单 Worker 波次加载；由 <see cref="ABStressOrchestrator"/> 从模板实例化，不自行克隆。
/// </summary>
public class ABStressWorker : MonoBehaviour
{
    int workerIndex;
    ABStressProfile stressProfile;
    int waves;
    string[] pathsPerWave;
    int staggerFrames;
    bool releaseAfterEachLoad;
    LoadApiTestLogCollector logCollector;

    string WorkerSource => "ABStressWorker_" + workerIndex;

    public void Begin(
        int index,
        ABStressProfile profile,
        int waveCount,
        string[] paths,
        int stagger,
        bool releaseEach,
        LoadApiTestLogCollector collector)
    {
        workerIndex = index;
        stressProfile = profile;
        waves = waveCount;
        pathsPerWave = paths;
        staggerFrames = stagger;
        releaseAfterEachLoad = releaseEach;
        logCollector = collector;

        RunStressAsync().Forget();
    }

    async UniTaskVoid RunStressAsync()
    {
        if (staggerFrames > 0)
        {
            int wait = staggerFrames * workerIndex;
            for (int f = 0; f < wait; f++)
                await UniTask.Yield();
        }

        int waveCount = Mathf.Max(1, waves);
        int failTotal = 0;

        for (int wave = 0; wave < waveCount; wave++)
        {
            string path = ResolvePathForWave(wave);
            bool ok = await RunWaveAsync(wave, path);
            if (!ok)
                failTotal++;

            string api = stressProfile == ABStressProfile.Boundary
                ? "Boundary Wave"
                : "Safe Wave";
            string detail = string.Format(
                "worker={0} wave={1} path={2} mode={3}",
                workerIndex,
                wave,
                path,
                DescribeWorkerMode(wave));

            if (ok)
                LogOk(api, detail);
            else
                LogFail(api, detail + " | load_verify_failed");
        }

        logCollector?.NotifyRunnerComplete(WorkerSource);
        Debug.Log("[" + WorkerSource + "] finished failTotal=" + failTotal);
    }

    string ResolvePathForWave(int wave)
    {
        if (pathsPerWave == null || pathsPerWave.Length == 0)
            return "Icon/3";

        if (stressProfile == ABStressProfile.Boundary)
            return pathsPerWave[wave % pathsPerWave.Length];

        return pathsPerWave[(workerIndex + wave) % pathsPerWave.Length];
    }

    string DescribeWorkerMode(int wave)
    {
        int mode = workerIndex % 3;
        if (stressProfile == ABStressProfile.Boundary)
        {
            if (mode == 0) return "sync";
            if (mode == 1) return "async";
            return "sync_then_async";
        }

        if (mode == 0) return "sync";
        if (mode == 1) return "async";
        return wave % 2 == 0 ? "sync_alt" : "async_alt";
    }

    async UniTask<bool> RunWaveAsync(int wave, string path)
    {
        int mode = workerIndex % 3;

        if (stressProfile == ABStressProfile.Boundary)
        {
            if (mode == 0)
                return TryLoadSync(path);
            if (mode == 1)
                return await TryLoadAsync(path);
            return await TryLoadSyncThenAsync(path);
        }

        if (mode == 0)
            return TryLoadSync(path);
        if (mode == 1)
            return await TryLoadAsync(path);
        return wave % 2 == 0 ? TryLoadSync(path) : await TryLoadAsync(path);
    }

    bool TryLoadSync(string path)
    {
        IAssetHandle handle = LoadByPath(path);
        if (!VerifyHandle(path, handle))
            return false;

        if (releaseAfterEachLoad)
            handle.Release();
        return true;
    }

    async UniTask<bool> TryLoadAsync(string path)
    {
        IAssetHandle handle = await LoadByPathAsync(path);
        if (!VerifyHandle(path, handle))
            return false;

        if (releaseAfterEachLoad)
            handle.Release();
        return true;
    }

    async UniTask<bool> TryLoadSyncThenAsync(string path)
    {
        IAssetHandle syncHandle = LoadByPath(path);
        bool syncOk = VerifyHandle(path, syncHandle);
        if (releaseAfterEachLoad && syncHandle != null)
            syncHandle.Release();

        IAssetHandle asyncHandle = await LoadByPathAsync(path);
        bool asyncOk = VerifyHandle(path, asyncHandle);
        if (releaseAfterEachLoad && asyncHandle != null)
            asyncHandle.Release();

        return syncOk && asyncOk;
    }

    static IAssetHandle LoadByPath(string path)
    {
        if (IsPrefabPath(path))
            return BundleResLoader.Instance.Load<GameObject>(path);
        return BundleResLoader.Instance.Load<Sprite>(path);
    }

    static async UniTask<IAssetHandle> LoadByPathAsync(string path)
    {
        if (IsPrefabPath(path))
            return await BundleResLoader.Instance.LoadUniTaskAsync<GameObject>(path);
        return await BundleResLoader.Instance.LoadUniTaskAsync<Sprite>(path);
    }

    static bool IsPrefabPath(string path)
    {
        return path != null
            && path.IndexOf("Prefab", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static bool VerifyHandle(string path, IAssetHandle handle)
    {
        if (handle == null)
            return false;

        if (IsPrefabPath(path))
            return handle.GetAsset<GameObject>() != null;

        return handle.GetAsset<Sprite>() != null;
    }

    void LogOk(string api, string detail)
    {
        Debug.Log("[" + WorkerSource + "] OK | " + api + " | " + detail);
        logCollector?.Record(WorkerSource, workerIndex, workerIndex, api, true, detail);
    }

    void LogFail(string api, string detail)
    {
        Debug.LogError("[" + WorkerSource + "] FAIL | " + api + " | " + detail);
        logCollector?.Record(WorkerSource, workerIndex, workerIndex, api, false, detail);
    }
}
