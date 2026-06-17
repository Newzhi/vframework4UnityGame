using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// ABStressTest 编排：从 Inactive Worker 模板生成 N 个实例，等齐后独占 UnloadAll。
/// </summary>
public class ABStressOrchestrator : MonoBehaviour
{
    const string OrchestratorSource = "ABStressOrchestrator";

    [Header("Stress")]
    [Tooltip("并行 Worker 数量")]
    public int workerCount = 4;

    public ABStressProfile stressProfile = ABStressProfile.Safe;
    public int waves = 3;

    [Tooltip("Catalogue 简路径")]
    public string[] pathsPerWave =
    {
        "Icon/3",
        "Atlas/Role/Hog_Attack_000",
        "Model/Prefabs/tester"
    };

    [Tooltip("每 Worker 错开帧数；Boundary 建议 0")]
    public int staggerFrames = 0;

    public bool releaseAfterEachLoad = true;
    public bool runOnStart = true;
    public bool quitApplicationAfterSave = false;

    [Header("Refs")]
    [Tooltip("场景中 Inactive 的 Worker 模板（仅含 ABStressWorker）")]
    public GameObject workerTemplate;

    public LoadApiTestLogCollector logCollector;
    public Button finishButton;

    bool finishing;

    void Awake()
    {
        if (logCollector == null)
            logCollector = GetComponent<LoadApiTestLogCollector>();
        logCollector = LoadApiTestLogCollector.EnsureShared(logCollector);

        if (workerTemplate == null)
        {
            Transform child = transform.Find("ABStressWorkerTemplate");
            if (child != null)
                workerTemplate = child.gameObject;
        }
    }

    void Start()
    {
        if (finishButton == null)
        {
            GameObject exitGo = GameObject.Find("EndAndLog");
            if (exitGo != null)
                finishButton = exitGo.GetComponent<Button>();
        }

        if (finishButton != null)
        {
            finishButton.onClick.RemoveListener(FinishTestAndSaveLog);
            finishButton.onClick.AddListener(FinishTestAndSaveLog);
        }

        if (runOnStart)
            RunSessionAsync().Forget();
    }

    async UniTaskVoid RunSessionAsync()
    {
        if (workerTemplate == null)
        {
            Debug.LogError("[ABStressOrchestrator] workerTemplate is not assigned.");
            return;
        }

        if (!BundleResLoader.Instance.EnsureReady())
        {
            Debug.LogError("[ABStressOrchestrator] EnsureReady failed.");
            return;
        }

        int count = Mathf.Max(1, workerCount);
        logCollector.expectedConcurrentRunners = count + 1;
        logCollector.sessionIdPrefix = LoadApiTestLogCollector.SessionPrefixABStress;
        logCollector.unloadAllRunnerSource = LoadApiTestLogCollector.UnloadAllRunnerSourceABStress;

        logCollector.BeginSession(OrchestratorSource);

        for (int i = 0; i < count; i++)
        {
            GameObject instance = Instantiate(workerTemplate, transform);
            instance.name = "ABStressWorker_" + i;
            instance.SetActive(true);

            ABStressWorker worker = instance.GetComponent<ABStressWorker>();
            if (worker == null)
            {
                Debug.LogError("[ABStressOrchestrator] workerTemplate missing ABStressWorker.");
                Destroy(instance);
                continue;
            }

            worker.Begin(
                i,
                stressProfile,
                waves,
                pathsPerWave,
                staggerFrames,
                releaseAfterEachLoad,
                logCollector);
        }

        await WaitForAllWorkersAsync(count);
        await UnloadAllAsync();

        Debug.Log("[ABStressOrchestrator] Session complete.");
    }

    async UniTask WaitForAllWorkersAsync(int count)
    {
        if (logCollector == null)
            return;

        float timeout = 120f;
        float elapsed = 0f;

        while (elapsed < timeout)
        {
            bool allDone = true;
            for (int i = 0; i < count; i++)
            {
                if (!logCollector.IsRunnerComplete("ABStressWorker_" + i))
                {
                    allDone = false;
                    break;
                }
            }

            if (allDone)
                return;

            await UniTask.Yield();
            elapsed += Time.deltaTime;
        }

        Debug.LogWarning("[ABStressOrchestrator] timeout waiting for workers.");
    }

    async UniTask UnloadAllAsync()
    {
        if (logCollector == null)
            return;

        if (!logCollector.TryClaimUnloadAll(OrchestratorSource))
        {
            logCollector.Record(OrchestratorSource, 0, 0, "UnloadAll", false, "claim failed");
            return;
        }

        BundleResLoader.Instance.UnloadAll();
        logCollector.Record(OrchestratorSource, 0, 0, "UnloadAll", true, "exclusive cleanup done");
        Debug.Log("[" + OrchestratorSource + "] UnloadAll done");
        logCollector.NotifyRunnerComplete(OrchestratorSource);
        await UniTask.Yield();
    }

    public void FinishTestAndSaveLog()
    {
        if (finishing)
            return;

        finishing = true;
        logCollector?.ForceEndSession();
        string path = logCollector?.LastSavedPath;
        Debug.Log("[ABStressOrchestrator] log saved: " + path);

        if (!quitApplicationAfterSave)
            return;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
