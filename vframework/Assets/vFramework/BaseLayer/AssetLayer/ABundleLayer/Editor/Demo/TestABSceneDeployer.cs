// TestABSceneDeployer.cs — Editor 演示部署工具
// 用途：一键生成/部署 TestAB.unity 演示场景（Canvas、按钮绑定、压测 Runner）。
// 菜单：vFramework → AssetKit → Deploy TestAB Scene

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using vFramework.BaseLayer.AssetLayer.ABundleLayer;
using vFramework.BaseLayer.AssetLayer.ABundleLayer.Demo;
using vFramework.Test.ABundleTest;

namespace vFramework.BaseLayer.AssetLayer.ABundleLayer.Editor
{
    /// <summary>一键部署 TestAB.unity 演示场景（UI 绑定、Canvas、测试 Runner）。</summary>
    public static class TestABSceneDeployer
    {
        const string ScenePath = "Assets/vFramework/BaseLayer/AssetLayer/ABundleLayer/Demo/TestAB.unity";

        [MenuItem("vFramework/AssetKit/Deploy TestAB Scene")]
        public static void DeployFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            DeployActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TestAB] 场景部署完成: " + ScenePath);
        }

        public static void DeployActiveScene()
        {
            EnsureEventSystem();
            var demo = EnsureDemoRunner();
            EnsureScopeLoader(demo.gameObject);
            EnsureCanvasUi(demo);
            EnsureTestRunner();
        }

        static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        static ABundleDemoRunner EnsureDemoRunner()
        {
            var demo = Object.FindObjectOfType<ABundleDemoRunner>();
            if (demo != null)
            {
                return demo;
            }

            var root = new GameObject("ABundleDemo");
            return root.AddComponent<ABundleDemoRunner>();
        }

        static void EnsureScopeLoader(GameObject host)
        {
            if (host.GetComponent<ABundleScopeLoader>() == null)
            {
                host.AddComponent<ABundleScopeLoader>();
            }
        }

        static void EnsureCanvasUi(ABundleDemoRunner demo)
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("Canvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
                var rt = canvasGo.GetComponent<RectTransform>();
                rt.localScale = Vector3.one;
            }
            else
            {
                canvas.GetComponent<RectTransform>().localScale = Vector3.one;
            }

            if (demo.transform != canvas.transform && demo.transform.parent != canvas.transform)
            {
                demo.transform.SetParent(canvas.transform, false);
            }

            var rawImage = Object.FindObjectOfType<RawImage>();
            if (rawImage == null)
            {
                var previewGo = CreateUiObject<RawImage>("IconPreview", canvas.transform);
                rawImage = previewGo.GetComponent<RawImage>();
            }

            var previewRect = rawImage.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0.5f);
            previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = Vector2.zero;
            previewRect.sizeDelta = new Vector2(256, 256);

            var loadBtn = FindOrCreateButton("BtnLoad", canvas.transform, "加载资源", new Vector2(-100, -220));
            var unloadBtn = FindOrCreateButton("BtnUnload", canvas.transform, "卸载资源", new Vector2(100, -220));

            WireButton(loadBtn, demo, nameof(ABundleDemoRunner.OnClickLoad));
            WireButton(unloadBtn, demo, nameof(ABundleDemoRunner.OnClickUnload));

            var so = new SerializedObject(demo);
            so.FindProperty("location").stringValue = "icon/3";
            so.FindProperty("iconPreview").objectReferenceValue = rawImage;
            so.FindProperty("showOnGuiButtons").boolValue = false;
            so.FindProperty("useScopeLoader").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureTestRunner()
        {
            var runner = Object.FindObjectOfType<ABundleLoadTestRunner>();
            if (runner != null)
            {
                return;
            }

            var go = new GameObject("ABundleTestRunner");
            var test = go.AddComponent<ABundleLoadTestRunner>();
            var so = new SerializedObject(test);
            so.FindProperty("logRoot").stringValue = "Assets/Test/ABundleTest/Logs";
            so.FindProperty("showOnGuiButton").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static GameObject CreateUiObject<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.AddComponent<CanvasRenderer>();
            go.AddComponent<T>();
            return go;
        }

        static Button FindOrCreateButton(string name, Transform parent, string label, Vector2 anchoredPos)
        {
            var existing = parent.Find(name);
            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = DefaultControls.CreateButton(new DefaultControls.Resources());
                go.name = name;
                go.transform.SetParent(parent, false);
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(160, 36);

            var text = go.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }

            return go.GetComponent<Button>();
        }

        static void WireButton(Button button, ABundleDemoRunner demo, string methodName)
        {
            button.onClick.RemoveAllListeners();
            if (methodName == nameof(ABundleDemoRunner.OnClickLoad))
            {
                button.onClick.AddListener(demo.OnClickLoad);
            }
            else
            {
                button.onClick.AddListener(demo.OnClickUnload);
            }
        }
    }
}
