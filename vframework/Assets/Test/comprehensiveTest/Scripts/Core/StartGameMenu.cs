using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>开始场景菜单：进入测试场景 / 导出日志并退出。</summary>
public class StartGameMenu : MonoBehaviour
{
    [SerializeField] Button startButton;
    [SerializeField] Button exitButton;
    [SerializeField] Text statusText;

    void Awake()
    {
        if (startButton == null)
            startButton = GameObject.Find("startGame")?.GetComponent<Button>();

        if (exitButton == null)
            exitButton = FindExitButton();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartGame);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitGame);
    }

    void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(OnStartGame);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(OnExitGame);
    }

    void OnStartGame()
    {
        ComprehensiveTestSceneFlow.LoadGameScene();
    }

    void OnExitGame()
    {
        var lines = new List<string>
        {
            "[系统] 从开始场景退出",
            $"[内存] {ComprehensiveTestLogExporter.BuildMemorySummary()}"
        };

        string path = ComprehensiveTestLogExporter.ExportLog(lines, "StartGame退出");
        string message = $"日志已导出: {path}\n{ComprehensiveTestLogExporter.GetLocationHint()}";
        Debug.Log(message);

        if (statusText != null)
            statusText.text = message;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    static Button FindExitButton()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.name.Trim() == "ExitGame")
                return buttons[i];
        }

        return null;
    }
}
