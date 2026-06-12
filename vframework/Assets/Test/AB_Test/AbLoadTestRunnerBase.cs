using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// AB 集成测试 Runner 基类：复用 Collector、协程 Case 循环、LogOk/LogFail 模式。
/// 现有 <see cref="Myloadtest"/> / <see cref="MyLoadTest2"/> 保持不变；新套系（Router、Stress 等）继承本类。
/// </summary>
public abstract class AbLoadTestRunnerBase : MonoBehaviour
{
    protected abstract string LogSource { get; }
    protected abstract int CaseCount { get; }

    public float intervalSeconds = 5f;
    public bool runOnStart = true;
    public LoadApiTestLogCollector logCollector;
    public Button finishButton;
    public bool quitApplicationAfterSave = true;

    protected int caseIndex;
    protected int currentCaseId;
    protected Coroutine routine;
    bool finishing;

    protected virtual void Awake()
    {
        if (logCollector == null)
            logCollector = GetComponent<LoadApiTestLogCollector>();
        logCollector = LoadApiTestLogCollector.EnsureShared(logCollector);
    }

    protected virtual void Start()
    {
        if (finishButton == null)
        {
            GameObject exitGo = GameObject.Find("ExitBtn");
            if (exitGo != null)
                finishButton = exitGo.GetComponent<Button>();
        }

        if (finishButton != null)
        {
            finishButton.onClick.RemoveListener(FinishTestAndSaveLog);
            finishButton.onClick.AddListener(FinishTestAndSaveLog);
        }

        if (runOnStart)
            routine = StartCoroutine(RunCases());
    }

    protected IEnumerator RunCases()
    {
        logCollector?.BeginSession(LogSource);

        while (caseIndex < CaseCount)
        {
            currentCaseId = caseIndex;
            logCollector?.AppendLine("[" + LogSource + "] ---------- Case " + currentCaseId + " ----------");

            yield return RunCase(currentCaseId);

            caseIndex++;
            float wait = currentCaseId == 0 ? 0f : intervalSeconds;
            if (wait > 0f)
                yield return new WaitForSeconds(wait);
        }

        logCollector?.NotifyRunnerComplete(LogSource);
        routine = null;
    }

    protected abstract IEnumerator RunCase(int caseId);

    public void FinishTestAndSaveLog()
    {
        if (finishing)
            return;

        finishing = true;

        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        logCollector?.ForceEndSession();
        string path = logCollector?.LastSavedPath;
        Debug.Log("[" + LogSource + "] log saved: " + path);
        logCollector?.AppendLine("[" + LogSource + "] Finish: " + path);

        if (!quitApplicationAfterSave)
            return;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    protected void LogOk(string api, string detail)
    {
        Debug.Log("[" + LogSource + "] OK | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, true, detail);
    }

    protected void LogFail(string api, string detail)
    {
        Debug.LogError("[" + LogSource + "] FAIL | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, false, detail);
    }

    protected void LogSkip(string api, string detail)
    {
        Debug.Log("[" + LogSource + "] SKIP | " + api + " | " + detail);
        logCollector?.Record(LogSource, currentCaseId, caseIndex, api, true, "SKIP: " + detail);
    }

    protected virtual void OnDestroy()
    {
        logCollector?.NotifyRunnerComplete(LogSource);
    }
}
