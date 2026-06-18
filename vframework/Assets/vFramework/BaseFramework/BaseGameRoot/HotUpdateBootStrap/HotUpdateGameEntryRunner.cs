using UnityEngine;

namespace BaseFramework.BaseGameRoot.HotUpdateBootStrap
{
    /// <summary>
    /// Editor / 本地联调用：挂到 Bootstrap Scene 任意 GameObject，Play 时自动 <see cref="HotUpdateGameEntry.OnHotfixLoaded"/>。
    /// 正式热更流程就绪后，可移除此组件，改由热更 DLL 反射调用 HotUpdateGameEntry。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HotUpdateGameEntryRunner : MonoBehaviour
    {
        [Tooltip("Play 时自动 TryStart；若热更入口已在别处调用，请关闭避免重复启动。")]
        public bool autoStartOnPlay = true;

        void Start()
        {
            if (!autoStartOnPlay)
                return;

            if (GameRoot.Instance != null && GameRoot.Instance.IsStarted)
                return;

            HotUpdateGameEntry.OnHotfixLoaded();
        }
    }
}
